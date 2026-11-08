namespace TicTacToe.Web.Hubs;

using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Features.Rooms.Invitations.Command;
using TicTacToe.Application.Features.Rooms.Invitations.Query;
using TicTacToe.Application.Features.Rooms.Online.Command;
using TicTacToe.Application.Features.Rooms.Online.Query;
using TicTacToe.Domain.DTOs.Rooms;

[Authorize]
public sealed class GameHub(IPresenceTracker presence, IMediator mediator, IInvitationManager invitations, IRoomStore rooms, TimeProvider clock) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        if (Guid.TryParse(Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            presence.AddUser(userId, Context.User?.Identity?.Name ?? userId.ToString());
            await BroadcastOnline();
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        if (Guid.TryParse(Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            presence.RemoveUser(userId);
            var stillOnline = presence.ConnectionCount(userId) > 0;
            await BroadcastOnline();
            if (stillOnline) return; // Multi-tab: another connection still counts as online.
            var pending = invitations.GetPendingForUser(userId);
            invitations.CancelForOffline(userId);
            foreach (var inv in pending)
            {
                var counterpart = inv.FromUserId == userId ? inv.ToUserId : inv.FromUserId;
                await Clients.User(counterpart.ToString()).SendAsync(
                    "InvitationCancelled",
                    new { invitationId = inv.Id.ToString(), reason = "offline" });
            }
            var deadline = clock.GetUtcNow().AddSeconds(60);
            foreach (var roomId in rooms.GetPlayingRoomIds(userId))
            {
                rooms.MarkDisconnected(roomId, userId, deadline);
                await Clients.Group($"room-{roomId}").SendAsync("PlayerDisconnected", new { id = roomId.ToString(), userId = userId.ToString(), secondsLeft = 60 });
            }
        }
    }

    public Task<OperationResult<IReadOnlyList<OnlinePlayerDto>>> GetOnlinePlayers()
        => mediator.SendAsync(new GetOnlinePlayersQuery());

    public async Task<OperationResult<RoomState>> GetRoom(Guid roomId)
    {
        var result = await mediator.SendAsync(new GetRoomQuery(roomId));
        if (result.IsSuccess && result.Value is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{roomId}");
            // Reconnect cancels grace for this player.
            if (Guid.TryParse(Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                rooms.MarkConnected(roomId, userId);
                await Clients.Group($"room-{roomId}").SendAsync("PlayerReconnected", new { userId = userId.ToString() });
                await BroadcastOnline();
            }
        }
        return result;
    }

    public async Task<OperationResult<RoomState>> MakeMove(Guid roomId, int index)
    {
        var result = await mediator.SendAsync(new MakeOnlineMoveCommand(roomId, index));
        if (result.IsSuccess && result.Value is not null)
        {
            await Clients.Group($"room-{roomId}").SendAsync("MoveApplied", new { room = result.Value });
        }
        return result;
    }

    public async Task<OperationResult> Leave(Guid roomId)
    {
        var result = await mediator.SendAsync(new LeaveRoomCommand(roomId));
        if (result.IsSuccess)
        {
            var room = rooms.Get(roomId);
            if (room is not null)
            {
                await Clients.Group($"room-{roomId}").SendAsync("PlayerLeft", new { room });
                if (room.Status.ToString() == "Closed")
                {
                    await Clients.Group($"room-{roomId}").SendAsync("RoomClosed", new { id = roomId.ToString(), reason = "left" });
                }
            }
        }
        return result;
    }

    public async Task<OperationResult<RoomState>> RequestRematch(Guid roomId)
    {
        var result = await mediator.SendAsync(new RequestRematchCommand(roomId));
        if (result.IsSuccess && result.Value is not null)
        {
            if (result.Value.Status.ToString() == "Playing")
            {
                await Clients.Group($"room-{roomId}").SendAsync("RematchStarted", new { room = result.Value });
            }
            else
            {
                // Single-side request → notify only the other participant.
                var me = Guid.TryParse(Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (Guid?)null;
                var other = me.HasValue && result.Value.PlayerXId == me.Value ? result.Value.PlayerOId : result.Value.PlayerXId;
                await Clients.User(other.ToString()).SendAsync(
                    "RematchRequested",
                    new { roomId = roomId.ToString(), byName = Context.User?.Identity?.Name });
            }
        }
        return result;
    }

    public async Task<OperationResult<InvitationStateDto>> InvitePlayer(Guid targetUserId)
    {
        var result = await mediator.SendAsync(new SendInvitationCommand(targetUserId));
        if (result.IsSuccess && result.Value is not null)
        {
            await Clients.User(targetUserId.ToString()).SendAsync(
                "InvitationReceived",
                new
                {
                    invitationId = result.Value.InvitationId.ToString(),
                    fromUserId = result.Value.FromUserId.ToString(),
                    toUserId = result.Value.ToUserId.ToString(),
                    fromName = Context.User?.Identity?.Name,
                    expiresInSeconds = 30,
                });
        }
        return result;
    }

    public async Task<OperationResult> CancelInvitation(Guid invitationId)
    {
        var inv = invitations.Get(invitationId);
        var result = await mediator.SendAsync(new CancelInvitationCommand(invitationId));
        if (result.IsSuccess && inv is not null)
        {
            await Clients.User(inv.ToUserId.ToString()).SendAsync(
                "InvitationCancelled",
                new { invitationId = invitationId.ToString(), reason = "cancelled", byName = Context.User?.Identity?.Name });
        }
        return result;
    }

    public async Task<OperationResult<RoomState>> RespondToInvitation(Guid invitationId, bool accept)
    {
        var inv = invitations.Get(invitationId);
        var result = await mediator.SendAsync(new RespondToInvitationCommand(invitationId, accept));
        if (inv is not null && result.IsSuccess)
        {
            if (accept && result.Value is not null)
            {
                await SendToBoth(inv,
                    "InvitationAccepted",
                    new { invitationId = invitationId.ToString(), room = result.Value });
            }
            else if (!accept)
            {
                await Clients.User(inv.FromUserId.ToString()).SendAsync(
                    "InvitationDeclined",
                    new { invitationId = invitationId.ToString(), byName = Context.User?.Identity?.Name });
            }
        }
        return result;
    }

    private async Task SendToBoth(TicTacToe.Domain.Online.Invitation inv, string method, object payload)
    {
        await Clients.User(inv.FromUserId.ToString()).SendAsync(method, payload);
        await Clients.User(inv.ToUserId.ToString()).SendAsync(method, payload);
    }

    private async Task BroadcastOnline()
    {
        var list = presence.GetOnline();
        await Clients.All.SendAsync("OnlinePlayersUpdated", new { players = list });
    }
}

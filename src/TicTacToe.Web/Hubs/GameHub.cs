namespace TicTacToe.Web.Hubs;

using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Features.Rooms.Invitations.Command;
using TicTacToe.Application.Features.Rooms.Invitations.Query;
using TicTacToe.Domain.DTOs.Rooms;

/// <summary>
/// Thin command gateway (ADR-0003): authenticates, dispatches IMediator, broadcasts.
/// Presence connect/disconnect is the only exception — tracked via IPresenceTracker.
/// </summary>
[Authorize]
public sealed class GameHub(IPresenceTracker presence, IMediator mediator, IInvitationManager invitations) : Hub
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
            invitations.CancelForOffline(userId);
            await BroadcastOnline();
        }
    }

    /// <summary>Initial lobby list (registered only; status derived server-side).</summary>
    public Task<OperationResult<IReadOnlyList<OnlinePlayerDto>>> GetOnlinePlayers()
        => mediator.SendAsync(new GetOnlinePlayersQuery());

    public async Task<OperationResult<InvitationStateDto>> InvitePlayer(Guid targetUserId)
    {
        var result = await mediator.SendAsync(new SendInvitationCommand(targetUserId));
        if (result.IsSuccess && result.Value is not null)
        {
            await Clients.All.SendAsync("InvitationReceived", new { invitationId = result.Value.InvitationId.ToString(), fromUserId = result.Value.FromUserId.ToString(), fromName = Context.User?.Identity?.Name, expiresInSeconds = 30 });
        }
        return result;
    }

    public Task<OperationResult> CancelInvitation(Guid invitationId)
        => mediator.SendAsync(new CancelInvitationCommand(invitationId));

    public async Task<OperationResult<RoomState>> RespondToInvitation(Guid invitationId, bool accept)
    {
        var result = await mediator.SendAsync(new RespondToInvitationCommand(invitationId, accept));
        // Broadcast outcome to inviter + target (M10: room page arrives M11, popup shows result now).
        if (accept && result.IsSuccess && result.Value is not null)
        {
            await Clients.All.SendAsync("InvitationAccepted", new { invitationId = invitationId.ToString(), room = result.Value });
        }
        else if (!accept)
        {
            await Clients.All.SendAsync("InvitationDeclined", new { invitationId = invitationId.ToString(), byName = Context.User?.Identity?.Name });
        }
        return result;
    }

    private async Task BroadcastOnline()
    {
        var list = presence.GetOnline();
        await Clients.All.SendAsync("OnlinePlayersUpdated", new { players = list });
    }
}

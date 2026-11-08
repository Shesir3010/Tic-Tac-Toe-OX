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
            var pending = invitations.GetPendingForUser(userId);
            presence.RemoveUser(userId);
            invitations.CancelForOffline(userId);
            foreach (var inv in pending)
            {
                var counterpart = inv.FromUserId == userId ? inv.ToUserId : inv.FromUserId;
                await Clients.User(counterpart.ToString()).SendAsync(
                    "InvitationCancelled",
                    new { invitationId = inv.Id.ToString(), reason = "offline" });
            }
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
            // Target ONLY (per-user), never All — otherwise the inviter sees their own popup.
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
            await SendToBoth(inv,
                "InvitationCancelled",
                new { invitationId = invitationId.ToString(), reason = "cancelled", byName = Context.User?.Identity?.Name });
        }
        return result;
    }

    public async Task<OperationResult<RoomState>> RespondToInvitation(Guid invitationId, bool accept)
    {
        var inv = invitations.Get(invitationId);
        var result = await mediator.SendAsync(new RespondToInvitationCommand(invitationId, accept));
        // Broadcast only to the two participants (never All — bystanders must not see toasts/popups).
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
                await SendToBoth(inv,
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

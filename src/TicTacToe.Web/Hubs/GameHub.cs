namespace TicTacToe.Web.Hubs;

using Cortex.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Features.Rooms.Invitations.Query;
using TicTacToe.Domain.DTOs.Rooms;

/// <summary>
/// Thin command gateway (ADR-0003): authenticates, dispatches IMediator, broadcasts.
/// Presence connect/disconnect is the only exception — tracked via IPresenceTracker.
/// </summary>
[Authorize]
public sealed class GameHub(IPresenceTracker presence, IMediator mediator) : Hub
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
            await BroadcastOnline();
        }
    }

    /// <summary>Initial lobby list (registered only; status derived server-side).</summary>
    public Task<OperationResult<IReadOnlyList<OnlinePlayerDto>>> GetOnlinePlayers()
        => mediator.SendAsync(new GetOnlinePlayersQuery());

    private async Task BroadcastOnline()
    {
        var list = presence.GetOnline();
        await Clients.All.SendAsync("OnlinePlayersUpdated", new { players = list });
    }
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicTacToe.Application.Contracts;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Settings;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Infrastructure.Services;

/// <summary>5s tick: grace forfeits (both rows once) + orphaned close (no saves) + invitation expiry.</summary>
public sealed class RoomCleanupService(
    IRoomStore rooms,
    IInvitationManager invitations,
    IMatchRepository matches,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    IOptions<RoomSettings> settings,
    ILogger<RoomCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Room cleanup tick failed");
            }
            await Task.Delay(TimeSpan.FromSeconds(settings.Value.CleanupIntervalSeconds), stoppingToken);
        }
    }

    public async Task TickAsync(CancellationToken ct)
    {
        var sweep = rooms.SweepExpired(clock, settings.Value.GraceSeconds);
        foreach (var finish in sweep.ForfeitFinishes)
        {
            var now = clock.GetUtcNow();
            var csv = string.Join(",", finish.Moves);
            await matches.AddAsync(new Match
            {
                UserId = finish.Room.PlayerXId,
                OpponentUserId = finish.Room.PlayerOId,
                Mode = MatchMode.Online,
                Symbol = Player.X,
                Result = finish.WinnerId == finish.Room.PlayerXId ? MatchResult.Win : MatchResult.Loss,
                Moves = csv,
                PlayedAtUtc = now,
            }, ct);
            await matches.AddAsync(new Match
            {
                UserId = finish.Room.PlayerOId,
                OpponentUserId = finish.Room.PlayerXId,
                Mode = MatchMode.Online,
                Symbol = Player.O,
                Result = finish.WinnerId == finish.Room.PlayerOId ? MatchResult.Win : MatchResult.Loss,
                Moves = csv,
                PlayedAtUtc = now,
            }, ct);
        }
        if (sweep.ForfeitFinishes.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        invitations.ExpirePending(clock);
    }
}

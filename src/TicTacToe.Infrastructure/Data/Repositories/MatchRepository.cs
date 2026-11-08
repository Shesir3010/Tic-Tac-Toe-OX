using Microsoft.EntityFrameworkCore;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Domain.DTOs.Matches;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Infrastructure.Data.Repositories;

public sealed class MatchRepository(ApplicationDbContext dbContext) : IMatchRepository
{
    public Task AddAsync(Match match, CancellationToken cancellationToken)
    {
        dbContext.Matches.Add(match);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<MatchHistoryItem>> GetHistoryAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var matches = await dbContext.Matches
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.PlayedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var opponentIds = matches
            .Where(m => m.OpponentUserId is not null)
            .Select(m => m.OpponentUserId!.Value)
            .Distinct()
            .ToList();

        var names = new Dictionary<Guid, string>();
        if (opponentIds.Count > 0)
        {
            names = await dbContext.Users
                .Where(u => opponentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? string.Empty, cancellationToken);
        }

        return matches.Select(m => new MatchHistoryItem(
            m.PlayedAtUtc,
            m.Mode,
            m.Difficulty,
            m.Symbol,
            m.OpponentUserId is null ? null : names.GetValueOrDefault(m.OpponentUserId.Value),
            m.Result,
            m.Moves)).ToList();
    }

    public async Task<PlayerStats> GetStatsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var played = await dbContext.Matches.CountAsync(m => m.UserId == userId, cancellationToken);
        var wins = await dbContext.Matches.CountAsync(m => m.UserId == userId && m.Result == MatchResult.Win, cancellationToken);
        var losses = await dbContext.Matches.CountAsync(m => m.UserId == userId && m.Result == MatchResult.Loss, cancellationToken);
        var draws = await dbContext.Matches.CountAsync(m => m.UserId == userId && m.Result == MatchResult.Draw, cancellationToken);
        return new PlayerStats(played, wins, losses, draws);
    }
}

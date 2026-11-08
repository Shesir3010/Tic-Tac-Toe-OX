using TicTacToe.Domain.DTOs.Matches;
using TicTacToe.Domain.Entities;

namespace TicTacToe.Application.Contracts.Repositories;

public interface IMatchRepository
{
    Task AddAsync(Match match, CancellationToken cancellationToken);

    Task<IReadOnlyList<MatchHistoryItem>> GetHistoryAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);

    Task<PlayerStats> GetStatsAsync(Guid userId, CancellationToken cancellationToken);
}

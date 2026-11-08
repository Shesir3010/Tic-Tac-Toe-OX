using TicTacToe.Domain.Entities;

namespace TicTacToe.Application.Contracts.Repositories;

public interface IMatchRepository
{
    Task AddAsync(Match match, CancellationToken cancellationToken);
}

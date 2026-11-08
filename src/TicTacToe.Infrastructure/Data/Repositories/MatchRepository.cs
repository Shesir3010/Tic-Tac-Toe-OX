using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Domain.Entities;

namespace TicTacToe.Infrastructure.Data.Repositories;

public sealed class MatchRepository(ApplicationDbContext dbContext) : IMatchRepository
{
    public Task AddAsync(Match match, CancellationToken cancellationToken)
    {
        dbContext.Matches.Add(match);
        return Task.CompletedTask;
    }
}

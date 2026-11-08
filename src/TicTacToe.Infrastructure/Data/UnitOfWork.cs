using TicTacToe.Application.Contracts;
using TicTacToe.Infrastructure.Data;

namespace TicTacToe.Infrastructure.Data;

public sealed class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}

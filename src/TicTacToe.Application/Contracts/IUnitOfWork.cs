namespace TicTacToe.Application.Contracts;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

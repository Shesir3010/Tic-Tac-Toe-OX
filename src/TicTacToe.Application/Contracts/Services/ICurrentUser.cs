namespace TicTacToe.Application.Contracts.Services;

/// <summary>Caller identity from Identity cookie claims (never from payload).</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}

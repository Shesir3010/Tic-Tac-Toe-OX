namespace TicTacToe.Application.Contracts.Services;

/// <summary>Identity seam (ADR-0004). Auth is deliberately not CQRS — framework calls, not domain use cases.</summary>
public interface IAccountService
{
    Task<AccountRegistrationResult> RegisterAsync(string username, string email, string password, CancellationToken cancellationToken);

    Task<AccountLoginResult> LoginAsync(string usernameOrEmail, string password, CancellationToken cancellationToken);

    Task LogoutAsync();
}

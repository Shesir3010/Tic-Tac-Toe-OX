namespace TicTacToe.Application.Contracts.Services;

/// <summary>Registration errors as (code, description) so Application never references ASP.NET Identity.</summary>
public sealed record AccountRegistrationResult(bool Succeeded, IReadOnlyList<(string Code, string Description)> Errors)
{
    public static AccountRegistrationResult Success() => new(true, []);
    public static AccountRegistrationResult Failed(IEnumerable<(string, string)> errors) => new(false, errors.ToList());
}

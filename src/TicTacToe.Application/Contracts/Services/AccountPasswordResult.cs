namespace TicTacToe.Application.Contracts.Services;

/// <summary>Account password result — WrongPassword maps to Auth.WrongPassword.</summary>
public sealed record AccountPasswordResult(bool Succeeded, string? ErrorMessage, string? ErrorCode = null)
{
    public static AccountPasswordResult Success() => new(true, null);
    public static AccountPasswordResult WrongPassword(string message = "Current password is incorrect.") => new(false, message, "Auth.WrongPassword");
    public static AccountPasswordResult Failed(string message, string code = "Validation.Error") => new(false, message, code);
}

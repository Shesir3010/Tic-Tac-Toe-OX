namespace TicTacToe.Application.Contracts.Services;

/// <summary>Result of a login attempt. LockedOut is friendly copy, never user-enumerating.</summary>
public sealed record AccountLoginResult(bool IsSuccess, bool IsLockedOut, string? ErrorMessage);

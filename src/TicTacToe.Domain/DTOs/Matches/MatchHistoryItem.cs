using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Domain.DTOs.Matches;

/// <summary>One history row (newest first). OpponentName null = "Deleted player" (set-null on delete).</summary>
public sealed record MatchHistoryItem(
    DateTimeOffset PlayedAtUtc,
    MatchMode Mode,
    Difficulty? Difficulty,
    Player Symbol,
    string? OpponentName,
    MatchResult Result,
    string Moves);

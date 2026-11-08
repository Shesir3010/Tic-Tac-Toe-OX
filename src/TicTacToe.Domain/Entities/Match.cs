using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Domain.Entities;

/// <summary>One saved result per player per finished game. Insert-once, never updated (FR-26).</summary>
public class Match
{
    public Match()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? OpponentUserId { get; set; }

    public MatchMode Mode { get; set; }

    public Difficulty? Difficulty { get; set; }

    public Player Symbol { get; set; }

    public MatchResult Result { get; set; }

    public string Moves { get; set; } = string.Empty;

    public DateTimeOffset PlayedAtUtc { get; set; }
}

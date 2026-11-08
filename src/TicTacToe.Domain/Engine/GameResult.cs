namespace TicTacToe.Domain.Engine;

/// <summary>Produced once by <see cref="Rules.Evaluate"/>; value-equal including the winning line.</summary>
public sealed class GameResult : IEquatable<GameResult>
{
    public GameResult(GameOutcome outcome, IReadOnlyList<int>? winningLine)
    {
        Outcome = outcome;
        WinningLine = winningLine;
    }

    public GameOutcome Outcome { get; }

    public IReadOnlyList<int>? WinningLine { get; }

    public bool IsFinished => Outcome != GameOutcome.InProgress;

    public bool Equals(GameResult? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Outcome != other.Outcome)
        {
            return false;
        }

        if (WinningLine is null && other.WinningLine is null)
        {
            return true;
        }

        if (WinningLine is null || other.WinningLine is null)
        {
            return false;
        }

        return WinningLine.SequenceEqual(other.WinningLine);
    }

    public override bool Equals(object? obj) => Equals(obj as GameResult);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Outcome);
        if (WinningLine is not null)
        {
            foreach (var cell in WinningLine)
            {
                hash.Add(cell);
            }
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(GameResult? left, GameResult? right) => left?.Equals(right) ?? right is null;

    public static bool operator !=(GameResult? left, GameResult? right) => !(left == right);
}

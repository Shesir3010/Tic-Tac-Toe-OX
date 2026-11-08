namespace TicTacToe.Domain.Contracts;

/// <summary>The single randomness seam in Domain (R-A4). Production uses SystemRandomSource; tests inject a fake.</summary>
public interface IRandomSource
{
    /// <returns>A value in [0, maxExclusive).</returns>
    int Next(int maxExclusive);
}

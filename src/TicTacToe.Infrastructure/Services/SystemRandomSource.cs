using TicTacToe.Domain.Contracts;

namespace TicTacToe.Infrastructure.Services;

/// <summary>Production IRandomSource (thread-safe). Seeded fakes are used in tests only.</summary>
public sealed class SystemRandomSource : IRandomSource
{
    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Max must be positive.");
        }

        return Random.Shared.Next(maxExclusive);
    }
}

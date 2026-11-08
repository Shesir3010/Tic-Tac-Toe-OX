using TicTacToe.Domain.Contracts;

namespace TicTacToe.Domain.Engine.Ai;

/// <summary>FR-09 Easy: uniform random open cell via injected IRandomSource.</summary>
public sealed class EasyAi : IComputerPlayer
{
    private readonly IRandomSource _random;

    public EasyAi(Player mark, IRandomSource random)
    {
        Mark = mark;
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public Player Mark { get; }

    public int ChooseMove(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var open = new List<int>();
        for (var i = 0; i < 9; i++)
        {
            if (board[i] is null)
            {
                open.Add(i);
            }
        }

        if (open.Count == 0)
        {
            throw new InvalidOperationException("No legal moves.");
        }

        return open[_random.Next(open.Count)];
    }
}

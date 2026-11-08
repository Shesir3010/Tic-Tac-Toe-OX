namespace TicTacToe.Domain.Engine.Ai;

/// <summary>FR-09 Medium: win → block → center → corner → any. Deterministic.</summary>
public sealed class MediumAi : IComputerPlayer
{
    private static readonly int[] Corners = [0, 2, 6, 8];

    public MediumAi(Player mark) => Mark = mark;

    public Player Mark { get; }

    public int ChooseMove(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var open = OpenCells(board);
        if (open.Count == 0)
        {
            throw new InvalidOperationException("No legal moves.");
        }

        var winner = Mark;
        var opponent = Mark == Player.X ? Player.O : Player.X;

        if (FindWinningMove(board, open, winner) is { } winning)
        {
            return winning;
        }

        if (FindWinningMove(board, open, opponent) is { } blocking)
        {
            return blocking;
        }

        if (open.Contains(4))
        {
            return 4;
        }

        foreach (var corner in Corners)
        {
            if (open.Contains(corner))
            {
                return corner;
            }
        }

        return open[0];
    }

    private static List<int> OpenCells(Board board)
    {
        var open = new List<int>();
        for (var i = 0; i < 9; i++)
        {
            if (board[i] is null)
            {
                open.Add(i);
            }
        }

        return open;
    }

    private static int? FindWinningMove(Board board, List<int> open, Player player)
    {
        foreach (var cell in open)
        {
            var next = board.Move(cell, player);
            var outcome = Rules.Evaluate(next).Outcome;
            if (player == Player.X && outcome == GameOutcome.XWins)
            {
                return cell;
            }

            if (player == Player.O && outcome == GameOutcome.OWins)
            {
                return cell;
            }
        }

        return null;
    }
}

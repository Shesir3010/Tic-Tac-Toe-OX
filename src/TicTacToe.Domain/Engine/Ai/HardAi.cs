namespace TicTacToe.Domain.Engine.Ai;

/// <summary>FR-09 Hard: exhaustive minimax (memoized), prefers fastest win, never loses.</summary>
public sealed class HardAi : IComputerPlayer
{
    public HardAi(Player mark) => Mark = mark;

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

        var memo = new Dictionary<string, int>();
        var bestScore = int.MinValue;
        var bestMove = open[0];

        foreach (var cell in open)
        {
            var score = Minimax(board.Move(cell, Mark), 1, memo);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = cell;
            }
        }

        return bestMove;
    }

    private int Minimax(Board board, int depth, Dictionary<string, int> memo)
    {
        var result = Rules.Evaluate(board);
        if (result.IsFinished)
        {
            return Score(result.Outcome, depth);
        }

        var key = BoardKey(board);
        if (memo.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var isSelfTurn = TurnOf(board) == Mark;
        var best = isSelfTurn ? int.MinValue : int.MaxValue;

        for (var i = 0; i < 9; i++)
        {
            if (board[i] is not null)
            {
                continue;
            }

            var next = board.Move(i, TurnOf(board));
            var score = Minimax(next, depth + 1, memo);
            best = isSelfTurn ? Math.Max(best, score) : Math.Min(best, score);
        }

        memo[key] = best;
        return best;
    }

    private int Score(GameOutcome outcome, int depth)
    {
        if (Mark == Player.X && outcome == GameOutcome.XWins)
        {
            return 10 - depth;
        }

        if (Mark == Player.O && outcome == GameOutcome.OWins)
        {
            return 10 - depth;
        }

        if (Mark == Player.X && outcome == GameOutcome.OWins)
        {
            return depth - 10;
        }

        if (Mark == Player.O && outcome == GameOutcome.XWins)
        {
            return depth - 10;
        }

        return 0;
    }

    private static Player TurnOf(Board board)
    {
        var xCount = 0;
        var oCount = 0;
        foreach (var cell in board.Cells)
        {
            if (cell == Player.X)
            {
                xCount++;
            }
            else if (cell == Player.O)
            {
                oCount++;
            }
        }

        return xCount == oCount ? Player.X : Player.O;
    }

    private static string BoardKey(Board board)
    {
        var chars = new char[9];
        for (var i = 0; i < 9; i++)
        {
            chars[i] = board[i] switch
            {
                Player.X => 'X',
                Player.O => 'O',
                _ => '.',
            };
        }

        return new string(chars);
    }
}

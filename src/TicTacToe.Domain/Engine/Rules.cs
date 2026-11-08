namespace TicTacToe.Domain.Engine;

/// <summary>
/// Pure server-authoritative rules (E1-E6). The single source of legality, turn order and end-of-game
/// detection. Every mutation path (JSON endpoint, hub commands, cleanup forfeits, AI) goes through here.
/// </summary>
public static class Rules
{
    private static readonly int[][] WinningLines =
    [
        [0, 1, 2],
        [3, 4, 5],
        [6, 7, 8],
        [0, 3, 6],
        [1, 4, 7],
        [2, 5, 8],
        [0, 4, 8],
        [2, 4, 6],
    ];

    public static EngineResult ValidateMove(Board board, int index, Player mark, Player turn)
    {
        ArgumentNullException.ThrowIfNull(board);

        if (index < 0 || index > 8)
        {
            return EngineResult.Fail(EngineFailure.InvalidCell);
        }

        if (Evaluate(board).IsFinished)
        {
            return EngineResult.Fail(EngineFailure.AlreadyOver);
        }

        if (board[index] is not null)
        {
            return EngineResult.Fail(EngineFailure.CellOccupied);
        }

        if (mark != turn)
        {
            return EngineResult.Fail(EngineFailure.NotYourTurn);
        }

        return EngineResult.Success();
    }

    public static Board ApplyMove(Board board, int index, Player mark)
    {
        ArgumentNullException.ThrowIfNull(board);

        var turn = TurnOf(board);
        var validation = ValidateMove(board, index, mark, turn);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Illegal move: {validation.Failure}.");
        }

        return board.Move(index, mark);
    }

    public static GameResult Evaluate(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);

        foreach (var line in WinningLines)
        {
            var first = board[line[0]];
            if (first is not null && first == board[line[1]] && first == board[line[2]])
            {
                var outcome = first == Player.X ? GameOutcome.XWins : GameOutcome.OWins;
                return new GameResult(outcome, line);
            }
        }

        if (board.Cells.All(cell => cell is not null))
        {
            return new GameResult(GameOutcome.Draw, null);
        }

        return new GameResult(GameOutcome.InProgress, null);
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
}

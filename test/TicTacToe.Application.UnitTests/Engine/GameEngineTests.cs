using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Engine;

namespace TicTacToe.Application.UnitTests.Engine;

// E1-E6 per ADR-0006. AAA + MethodName_Scenario_ExpectedResult (test-strategy.md).
[TestFixture]
public sealed class GameEngineTests
{
    [Test]
    public void Board_Empty_HasNineEmptyCellsAndEquality()
    {
        var board = Board.Empty();

        // Arrange / Act / Assert
        board.Cells.Count.ShouldBe(9);
        board.Cells.ShouldAllBe(cell => cell == null);
        board.ShouldBe(Board.Empty());
        (board == Board.Empty()).ShouldBeTrue();
        (board != Board.Empty()).ShouldBeFalse();
        board.GetHashCode().ShouldBe(Board.Empty().GetHashCode());
    }

    [Test]
    public void Board_Ctor_Null_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new Board(null!));
    }

    [Test]
    public void Board_Ctor_WrongLength_Throws()
    {
        Should.Throw<ArgumentException>(() => new Board(new Player?[8]));
    }

    [Test]
    public void Board_Move_Valid_ReturnsNewBoard_AndLeavesOldUnchanged()
    {
        var empty = Board.Empty();

        var next = empty.Move(4, Player.X);

        next[4].ShouldBe(Player.X);
        empty[4].ShouldBeNull();
        next.ShouldNotBeSameAs(empty);
    }

    [TestCase(-1)]
    [TestCase(9)]
    public void Board_Move_OutOfRange_Throws(int index)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Board.Empty().Move(index, Player.X));
    }

    [Test]
    public void Board_Move_Occupied_Throws()
    {
        var board = Board.Empty().Move(0, Player.X);

        Should.Throw<InvalidOperationException>(() => board.Move(0, Player.O));
    }

    [Test]
    public void Board_Equals_NullAndDifferent_ReturnFalse()
    {
        var board = Board.Empty();

        board.Equals(null).ShouldBeFalse();
        board.Equals(default(object)).ShouldBeFalse();
        (board == null).ShouldBeFalse();
        (null == board).ShouldBeFalse();
        (null == null).ShouldBeTrue();

        var different = Board.Empty().Move(0, Player.X);
        board.Equals(different).ShouldBeFalse();
        (board == different).ShouldBeFalse();
        (board != different).ShouldBeTrue();
    }

    [Test]
    public void ValidateMove_Valid_ReturnsSuccess()
    {
        var result = Rules.ValidateMove(Board.Empty(), 4, Player.X, Player.X);

        result.IsValid.ShouldBeTrue();
        result.Failure.ShouldBeNull();
        result.ShouldBe(EngineResult.Success());
    }

    [TestCase(-1)]
    [TestCase(9)]
    public void ValidateMove_OutOfRange_ReturnsInvalidCell(int index)
    {
        var result = Rules.ValidateMove(Board.Empty(), index, Player.X, Player.X);

        result.IsValid.ShouldBeFalse();
        result.Failure.ShouldBe(EngineFailure.InvalidCell);
        result.ShouldBe(EngineResult.Fail(EngineFailure.InvalidCell));
    }

    [Test]
    public void ValidateMove_Occupied_ReturnsCellOccupied()
    {
        var board = Board.Empty().Move(0, Player.X);

        var result = Rules.ValidateMove(board, 0, Player.O, Player.O);

        result.Failure.ShouldBe(EngineFailure.CellOccupied);
    }

    [Test]
    public void ValidateMove_WrongTurn_ReturnsNotYourTurn()
    {
        var result = Rules.ValidateMove(Board.Empty(), 0, Player.O, Player.X);

        result.Failure.ShouldBe(EngineFailure.NotYourTurn);
    }

    [Test]
    public void ValidateMove_AfterWin_ReturnsAlreadyOver()
    {
        var board = Board.Empty()
            .Move(0, Player.X)
            .Move(3, Player.O)
            .Move(1, Player.X)
            .Move(4, Player.O)
            .Move(2, Player.X); // X wins top row

        var result = Rules.ValidateMove(board, 5, Player.O, Player.O);

        result.Failure.ShouldBe(EngineFailure.AlreadyOver);
    }

    [Test]
    public void ValidateMove_AfterDraw_ReturnsAlreadyOver()
    {
        var board = DrawBoard();

        var result = Rules.ValidateMove(board, 0, Player.X, Player.X);

        result.Failure.ShouldBe(EngineFailure.AlreadyOver);
    }

    [Test]
    public void ValidateMove_NullBoard_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Rules.ValidateMove(null!, 0, Player.X, Player.X));
    }

    [Test]
    public void ApplyMove_Valid_ReturnsNewBoard()
    {
        var next = Rules.ApplyMove(Board.Empty(), 4, Player.X);

        next[4].ShouldBe(Player.X);
    }

    [Test]
    public void ApplyMove_Illegal_Throws()
    {
        Should.Throw<InvalidOperationException>(() => Rules.ApplyMove(Board.Empty(), 9, Player.X));
    }

    [Test]
    public void ApplyMove_WrongTurn_Throws()
    {
        // X must start; O cannot move first.
        Should.Throw<InvalidOperationException>(() => Rules.ApplyMove(Board.Empty(), 0, Player.O));
    }

    [Test]
    public void ApplyMove_DoubleMove_Throws()
    {
        // E2: after X, it is O's turn — X cannot move twice.
        var board = Board.Empty().Move(0, Player.X);

        Should.Throw<InvalidOperationException>(() => Rules.ApplyMove(board, 1, Player.X));
    }

    [Test]
    public void ApplyMove_NullBoard_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Rules.ApplyMove(null!, 0, Player.X));
    }

    [Test]
    public void ApplyMove_XFirst_EnforcedAcrossSequence()
    {
        // E2 full sequence: X, O, X, O is legal; finished board then rejects (E5).
        var board = Board.Empty();
        board = Rules.ApplyMove(board, 0, Player.X);
        board = Rules.ApplyMove(board, 4, Player.O);
        board = Rules.ApplyMove(board, 1, Player.X);
        board = Rules.ApplyMove(board, 5, Player.O);
        board = Rules.ApplyMove(board, 2, Player.X);

        Rules.Evaluate(board).Outcome.ShouldBe(GameOutcome.XWins);
        Should.Throw<InvalidOperationException>(() => Rules.ApplyMove(board, 6, Player.O));
    }

    [TestCase(0, 1, 2)]
    [TestCase(3, 4, 5)]
    [TestCase(6, 7, 8)]
    [TestCase(0, 3, 6)]
    [TestCase(1, 4, 7)]
    [TestCase(2, 5, 8)]
    [TestCase(0, 4, 8)]
    [TestCase(2, 4, 6)]
    public void Evaluate_XLine_ReturnsXWins(int a, int b, int c)
    {
        // Fill other cells to block O from also winning on the opponent line when needed.
        var cells = WinningCellsFor(a, b, c, Player.X);
        var board = BoardWith(cells);

        var result = Rules.Evaluate(board);

        result.Outcome.ShouldBe(GameOutcome.XWins);
        result.IsFinished.ShouldBeTrue();
        result.WinningLine.ShouldNotBeNull();
        result.WinningLine!.ShouldBe(new[] { a, b, c });
    }

    [TestCase(0, 1, 2)]
    [TestCase(3, 4, 5)]
    [TestCase(6, 7, 8)]
    [TestCase(0, 3, 6)]
    [TestCase(1, 4, 7)]
    [TestCase(2, 5, 8)]
    [TestCase(0, 4, 8)]
    [TestCase(2, 4, 6)]
    public void Evaluate_OLine_ReturnsOWins(int a, int b, int c)
    {
        var cells = WinningCellsFor(a, b, c, Player.O);
        // O cannot win first; add an X move on a safe cell so the board is reachable-ish but still O line.
        cells[SafeFiller(cells)] = Player.X;
        var board = BoardWith(cells);

        var result = Rules.Evaluate(board);

        result.Outcome.ShouldBe(GameOutcome.OWins);
        result.WinningLine!.ShouldBe(new[] { a, b, c });
    }

    [Test]
    public void Evaluate_FullNoLine_ReturnsDraw()
    {
        var result = Rules.Evaluate(DrawBoard());

        result.Outcome.ShouldBe(GameOutcome.Draw);
        result.IsFinished.ShouldBeTrue();
        result.WinningLine.ShouldBeNull();
    }

    [Test]
    public void Evaluate_Empty_ReturnsInProgress()
    {
        var result = Rules.Evaluate(Board.Empty());

        result.Outcome.ShouldBe(GameOutcome.InProgress);
        result.IsFinished.ShouldBeFalse();
        result.WinningLine.ShouldBeNull();
    }

    [Test]
    public void Evaluate_NullBoard_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Rules.Evaluate(null!));
    }

    [Test]
    public void Evaluate_SameState_Twice_ReturnsEqualResults()
    {
        // E6 determinism, including value equality of GameResult.
        var board = Board.Empty().Move(0, Player.X).Move(4, Player.O).Move(1, Player.X);

        var first = Rules.Evaluate(board);
        var second = Rules.Evaluate(board);

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
        (first == second).ShouldBeTrue();
        (first != second).ShouldBeFalse();
    }

    [Test]
    public void GameResult_GetHashCode_WithAndWithoutLine_Unique()
    {
        var win = new GameResult(GameOutcome.XWins, new[] { 0, 1, 2 });
        var winCopy = new GameResult(GameOutcome.XWins, new[] { 0, 1, 2 });
        var draw = new GameResult(GameOutcome.Draw, null);

        win.GetHashCode().ShouldBe(winCopy.GetHashCode());
        win.GetHashCode().ShouldNotBe(draw.GetHashCode());
    }

    [Test]
    public void GameResult_Equality_CoversAllBranches()
    {
        var win = new GameResult(GameOutcome.XWins, new[] { 0, 1, 2 });
        var winCopy = new GameResult(GameOutcome.XWins, new[] { 0, 1, 2 });
        var winOtherLine = new GameResult(GameOutcome.XWins, new[] { 3, 4, 5 });
        var draw = new GameResult(GameOutcome.Draw, null);
        var inProgress = new GameResult(GameOutcome.InProgress, null);
        GameResult? nullResult = null;

        win.Equals(winCopy).ShouldBeTrue();
        win.Equals(nullResult).ShouldBeFalse();
        win.Equals(default(object)).ShouldBeFalse();
        win.Equals(winOtherLine).ShouldBeFalse();
        win.Equals(draw).ShouldBeFalse();
        draw.Equals(inProgress).ShouldBeFalse();
        draw.Equals(new GameResult(GameOutcome.Draw, null)).ShouldBeTrue();
        draw.Equals(new GameResult(GameOutcome.Draw, new[] { 0, 1, 2 })).ShouldBeFalse();
        (win == nullResult).ShouldBeFalse();
        (nullResult == win).ShouldBeFalse();
        (nullResult == null).ShouldBeTrue();
        (win != winCopy).ShouldBeFalse();
    }

    private static Board DrawBoard()
    {
        // X O X / X O O / O X X — full, no line (verify: no 3-in-a-row).
        return new Board(
        [
            Player.X, Player.O, Player.X,
            Player.X, Player.O, Player.O,
            Player.O, Player.X, Player.X,
        ]);
    }

    private static Dictionary<int, Player?> WinningCellsFor(int a, int b, int c, Player winner)
    {
        var cells = new Dictionary<int, Player?>();
        for (var i = 0; i < 9; i++)
        {
            cells[i] = null;
        }

        cells[a] = winner;
        cells[b] = winner;
        cells[c] = winner;
        return cells;
    }

    private static int SafeFiller(Dictionary<int, Player?> cells)
    {
        for (var i = 0; i < 9; i++)
        {
            if (cells[i] is null)
            {
                return i;
            }
        }

        return 0;
    }

    private static Board BoardWith(Dictionary<int, Player?> cells)
    {
        var array = new Player?[9];
        for (var i = 0; i < 9; i++)
        {
            array[i] = cells[i];
        }

        return new Board(array);
    }
}

using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Engine.Ai;

namespace TicTacToe.Application.UnitTests.Engine.Ai;

[TestFixture]
public sealed class MediumAiTests
{
    [Test]
    public void ChooseMove_WinningMoveAvailable_TakesIt()
    {
        // X X . / O O . / . . . — X to move? Board: X0 O3 X1 O4 -> X to move (2 X, 2 O). X wins at 2.
        var board = new Board(
        [
            Player.X, Player.X, null,
            Player.O, Player.O, null,
            null, null, null,
        ]);

        var move = new MediumAi(Player.X).ChooseMove(board);

        move.ShouldBe(2);
        Rules.Evaluate(board.Move(move, Player.X)).Outcome.ShouldBe(GameOutcome.XWins);
    }

    [Test]
    public void ChooseMove_BlockingMoveRequired_Blocks()
    {
        // X X . / O . . / . . . — O to move must block 2.
        var board = new Board(
        [
            Player.X, Player.X, null,
            Player.O, null, null,
            null, null, null,
        ]);

        var move = new MediumAi(Player.O).ChooseMove(board);

        move.ShouldBe(2);
    }

    [Test]
    public void ChooseMove_NoWinBlock_TakesCenter()
    {
        var move = new MediumAi(Player.X).ChooseMove(Board.Empty());

        move.ShouldBe(4);
    }

    [Test]
    public void ChooseMove_CenterTaken_TakesFirstCorner()
    {
        var board = Board.Empty().Move(4, Player.O); // O took center, X to move

        var move = new MediumAi(Player.X).ChooseMove(board);

        move.ShouldBe(0);
    }

    [Test]
    public void ChooseMove_WinAvailableInCornersTakenBoard_TakesWin()
    {
        // X at 0, O at 2, X at 6, O at 4: X wins at 3 (0-3-6 column) — win beats center/corner.
        var board = new Board(
        [
            Player.X, null, Player.O,
            null, Player.O, null,
            Player.X, null, null,
        ]);

        var move = new MediumAi(Player.X).ChooseMove(board);

        move.ShouldBe(3);
        Rules.Evaluate(board.Move(move, Player.X)).Outcome.ShouldBe(GameOutcome.XWins);
    }

    [Test]
    public void ChooseMove_FullBoard_Throws()
    {
        var board = new Board(
        [
            Player.X, Player.O, Player.X,
            Player.X, Player.O, Player.O,
            Player.O, Player.X, Player.X,
        ]);

        Should.Throw<InvalidOperationException>(() => new MediumAi(Player.X).ChooseMove(board));
    }

    [Test]
    public void ChooseMove_NullBoard_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new MediumAi(Player.X).ChooseMove(null!));
    }

    [Test]
    public void Constructor_PreservesMark()
    {
        new MediumAi(Player.O).Mark.ShouldBe(Player.O);
    }
}

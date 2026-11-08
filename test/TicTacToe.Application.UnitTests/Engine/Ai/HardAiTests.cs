using System.Diagnostics;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Engine.Ai;

namespace TicTacToe.Application.UnitTests.Engine.Ai;

[TestFixture]
public sealed class HardAiTests
{
    [Test]
    public void ChooseMove_EmptyBoard_ChoosesDeterministicOptimalMove()
    {
        var first = new HardAi(Player.X).ChooseMove(Board.Empty());
        var second = new HardAi(Player.X).ChooseMove(Board.Empty());

        first.ShouldBe(second);
        first.ShouldBeGreaterThanOrEqualTo(0);
        first.ShouldBeLessThan(9);
    }

    [Test]
    public void ChooseMove_WinningMoveAvailable_TakesFastestWin()
    {
        var board = new Board(
        [
            Player.X, Player.X, null,
            Player.O, Player.O, null,
            null, null, null,
        ]);

        var move = new HardAi(Player.X).ChooseMove(board);

        move.ShouldBe(2);
    }

    [Test]
    public void ChooseMove_BlockRequired_Blocks()
    {
        var board = new Board(
        [
            Player.X, Player.X, null,
            Player.O, null, null,
            null, null, null,
        ]);

        var move = new HardAi(Player.O).ChooseMove(board);

        move.ShouldBe(2);
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

        Should.Throw<InvalidOperationException>(() => new HardAi(Player.X).ChooseMove(board));
    }

    [Test]
    public void ChooseMove_NullBoard_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new HardAi(Player.X).ChooseMove(null!));
    }

    [Test]
    public void Constructor_PreservesMark()
    {
        new HardAi(Player.O).Mark.ShouldBe(Player.O);
    }

    [Test]
    public void ChooseMove_ReplyIsFast_Under500ms()
    {
        var stopwatch = Stopwatch.StartNew();

        new HardAi(Player.X).ChooseMove(Board.Empty());

        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(500);
    }

    [Test]
    public void HardAsX_NeverLoses_ExhaustiveSweep()
    {
        var losses = 0;
        var games = 0;

        void Play(Board board)
        {
            var result = Rules.Evaluate(board);
            if (result.IsFinished)
            {
                games++;
                if (result.Outcome == GameOutcome.OWins)
                {
                    losses++;
                }

                return;
            }

            var turn = TurnOf(board);
            if (turn == Player.X)
            {
                var move = new HardAi(Player.X).ChooseMove(board);
                board[move].ShouldBeNull();
                Play(board.Move(move, Player.X));
            }
            else
            {
                for (var i = 0; i < 9; i++)
                {
                    if (board[i] is null)
                    {
                        Play(board.Move(i, Player.O));
                    }
                }
            }
        }

        Play(Board.Empty());

        games.ShouldBeGreaterThan(0);
        losses.ShouldBe(0);
    }

    [Test]
    public void HardAsO_NeverLoses_ExhaustiveSweep()
    {
        var losses = 0;
        var games = 0;

        void Play(Board board)
        {
            var result = Rules.Evaluate(board);
            if (result.IsFinished)
            {
                games++;
                if (result.Outcome == GameOutcome.XWins)
                {
                    losses++;
                }

                return;
            }

            var turn = TurnOf(board);
            if (turn == Player.O)
            {
                var move = new HardAi(Player.O).ChooseMove(board);
                board[move].ShouldBeNull();
                Play(board.Move(move, Player.O));
            }
            else
            {
                for (var i = 0; i < 9; i++)
                {
                    if (board[i] is null)
                    {
                        Play(board.Move(i, Player.X));
                    }
                }
            }
        }

        Play(Board.Empty());

        games.ShouldBeGreaterThan(0);
        losses.ShouldBe(0);
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

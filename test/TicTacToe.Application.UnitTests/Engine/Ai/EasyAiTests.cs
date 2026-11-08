using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Contracts;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Engine.Ai;

namespace TicTacToe.Application.UnitTests.Engine.Ai;

[TestFixture]
public sealed class EasyAiTests
{
    [Test]
    public void ChooseMove_EmptyBoard_ReturnsSeededCell()
    {
        var ai = new EasyAi(Player.X, new SequenceRandom(2));

        ai.ChooseMove(Board.Empty()).ShouldBe(2);
    }

    [Test]
    public void ChooseMove_PartialBoard_ReturnsOnlyOpenCell()
    {
        var board = Board.Empty().Move(0, Player.X).Move(4, Player.O);

        var move = new EasyAi(Player.O, new SequenceRandom(0)).ChooseMove(board);

        move.ShouldBe(1); // first open cell
        board[move].ShouldBeNull();
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

        Should.Throw<InvalidOperationException>(() => new EasyAi(Player.X, new SequenceRandom(0)).ChooseMove(board));
    }

    [Test]
    public void Ctor_NullRandom_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new EasyAi(Player.X, null!));
    }

    [Test]
    public void ChooseMove_NullBoard_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new EasyAi(Player.X, new SequenceRandom(0)).ChooseMove(null!));
    }

    [Test]
    public void Constructor_PreservesMark()
    {
        new EasyAi(Player.O, new SequenceRandom(0)).Mark.ShouldBe(Player.O);
    }

    private sealed class SequenceRandom(int value) : IRandomSource
    {
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            return Math.Min(value, maxExclusive - 1);
        }
    }
}

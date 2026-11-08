using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;
using TicTacToe.Infrastructure.Services;

namespace TicTacToe.Application.UnitTests.Infrastructure;

[TestFixture]
public sealed class ComputerPlayerFactoryTests
{
    [Test]
    public void Create_Easy_ReturnsEasyPreservingMark()
    {
        var player = new ComputerPlayerFactory(new StubRandom()).Create(Difficulty.Easy, Player.O);

        player.Mark.ShouldBe(Player.O);
        player.ChooseMove(Board.Empty()).ShouldBe(0);
    }

    [Test]
    public void Create_Medium_ReturnsMedium()
    {
        var player = new ComputerPlayerFactory(new StubRandom()).Create(Difficulty.Medium, Player.X);

        player.Mark.ShouldBe(Player.X);
        player.ChooseMove(Board.Empty()).ShouldBe(4);
    }

    [Test]
    public void Create_Hard_ReturnsHard()
    {
        var player = new ComputerPlayerFactory(new StubRandom()).Create(Difficulty.Hard, Player.X);

        player.Mark.ShouldBe(Player.X);
        player.ChooseMove(Board.Empty()).ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void Create_Unknown_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new ComputerPlayerFactory(new StubRandom()).Create((Difficulty)99, Player.X));
    }

    [Test]
    public void Ctor_NullRandom_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new ComputerPlayerFactory(null!));
    }

    private sealed class StubRandom : TicTacToe.Domain.Contracts.IRandomSource
    {
        public int Next(int maxExclusive) => 0;
    }
}

using Moq;
using MatchEntity = TicTacToe.Domain.Entities.Match;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Features.Matches.History.Command;
using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.UnitTests.Features.Matches.History.Command;

[TestFixture]
public sealed class SaveMatchCommandHandlerTests
{
    private static readonly Guid TestUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset FixedNow = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    // X wins top row: X0 O3 X1 O4 X2
    private static readonly int[] XWins = [0, 3, 1, 4, 2];

    [Test]
    public async Task Handle_HonestWin_WritesExactlyOneRow()
    {
        var (handler, repo, uow) = Build();

        var result = await handler.Handle(
            new SaveMatchCommand(XWins, MatchResult.Win, Player.X, GameMode.Local, null, null), default);

        result.IsSuccess.ShouldBeTrue();
        repo.Verify(x => x.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.AddAsync(
            It.Is<MatchEntity>(m =>
                m.UserId == TestUser && m.Result == MatchResult.Win && m.Symbol == Player.X &&
                m.Moves == "0,3,1,4,2" && m.PlayedAtUtc == FixedNow),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_TamperedResult_ReturnsForged_AndWritesNothing()
    {
        var (handler, repo, uow) = Build();

        var result = await handler.Handle(
            new SaveMatchCommand(XWins, MatchResult.Loss, Player.X, GameMode.Local, null, null), default);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Game.ForgedResult");
        repo.Verify(x => x.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_IllegalMove_ReturnsForged()
    {
        var (handler, repo, _) = Build();
        var moves = new[] { 0, 0, 1, 4, 2 }; // O replays 0, occupied

        var result = await handler.Handle(
            new SaveMatchCommand(moves, MatchResult.Win, Player.X, GameMode.Local, null, null), default);

        result.Error!.Code.ShouldBe("Game.ForgedResult");
        repo.Verify(x => x.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_Unfinished_ReturnsForged()
    {
        var (handler, repo, _) = Build();

        var result = await handler.Handle(
            new SaveMatchCommand([4], MatchResult.Win, Player.X, GameMode.Local, null, null), default);

        result.Error!.Code.ShouldBe("Game.ForgedResult");
        repo.Verify(x => x.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_Guest_ReturnsValidation_AndWritesNothing()
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(x => x.UserId).Returns((Guid?)null);
        var (handler, repo, _) = Build(user);

        var result = await handler.Handle(
            new SaveMatchCommand(XWins, MatchResult.Win, Player.X, GameMode.Local, null, null), default);

        result.Error!.Code.ShouldBe("Validation.Error");
        repo.Verify(x => x.AddAsync(It.IsAny<MatchEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_ComputerMode_StoresDifficulty()
    {
        var (handler, repo, _) = Build();

        var result = await handler.Handle(
            new SaveMatchCommand(XWins, MatchResult.Win, Player.X, GameMode.Computer, Difficulty.Hard, null), default);

        result.IsSuccess.ShouldBeTrue();
        repo.Verify(x => x.AddAsync(
            It.Is<MatchEntity>(m => m.Mode == MatchMode.Computer && m.Difficulty == Difficulty.Hard),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_DrawClaimedCorrectly_SavesDraw()
    {
        var (handler, repo, _) = Build();
        // Draw board sequence producing DrawBoard pattern: X0 O1 X2 O4 X3 O5 X7 O6 X8
        // Verify with engine in test setup: 0,1,2,4,3,5,7,6,8 gives no win.
        var moves = new[] { 0, 1, 2, 4, 3, 5, 7, 6, 8 };

        var result = await handler.Handle(
            new SaveMatchCommand(moves, MatchResult.Draw, Player.X, GameMode.Local, null, null), default);

        result.IsSuccess.ShouldBeTrue();
        repo.Verify(x => x.AddAsync(
            It.Is<MatchEntity>(m => m.Result == MatchResult.Draw),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (SaveMatchCommandHandler, Mock<IMatchRepository>, Mock<IUnitOfWork>) Build(Mock<ICurrentUser>? userMock = null)
    {
        var user = userMock ?? new Mock<ICurrentUser>();
        if (userMock is null)
        {
            user.Setup(x => x.UserId).Returns(TestUser);
        }
        var repo = new Mock<IMatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new FakeClock(FixedNow);
        return (new SaveMatchCommandHandler(user.Object, repo.Object, uow.Object, clock), repo, uow);
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}


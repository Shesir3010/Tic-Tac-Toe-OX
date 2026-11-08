using Moq;
using MatchEntity = TicTacToe.Domain.Entities.Match;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Features.Matches.History.Query;
using TicTacToe.Domain.DTOs.Matches;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.UnitTests.Features.Matches.History.Query;

[TestFixture]
public sealed class GetHistoryQueryHandlerTests
{
    private static readonly Guid TestUser = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public async Task Handle_User_ReturnsPagedItems()
    {
        var expected = new[]
        {
            new MatchHistoryItem(DateTimeOffset.Now, MatchMode.Local, null, Player.X, null, MatchResult.Win, "0,3,1,4,2"),
            new MatchHistoryItem(DateTimeOffset.Now.AddMinutes(-5), MatchMode.Computer, Difficulty.Hard, Player.O, "opponent1", MatchResult.Loss, "4,0,1,8"),
        };
        var (handler, repo) = Build(expected, TestUser);

        var result = await handler.Handle(new GetHistoryQuery(1, 10), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
        repo.Verify(x => x.GetHistoryAsync(TestUser, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_Guest_ReturnsValidation()
    {
        var (handler, repo) = Build([], userId: null);

        var result = await handler.Handle(new GetHistoryQuery(), default);

        result.Error!.Code.ShouldBe("Validation.Error");
        repo.Verify(x => x.GetHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_PageZero_ClampsToOne()
    {
        var (handler, repo) = Build([], TestUser);

        await handler.Handle(new GetHistoryQuery(0, 10), default);

        repo.Verify(x => x.GetHistoryAsync(TestUser, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (GetHistoryQueryHandler, Mock<IMatchRepository>) Build(IReadOnlyList<MatchHistoryItem> items, Guid? userId)
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(x => x.UserId).Returns(userId);
        var repo = new Mock<IMatchRepository>();
        repo.Setup(x => x.GetHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        return (new GetHistoryQueryHandler(user.Object, repo.Object), repo);
    }
}


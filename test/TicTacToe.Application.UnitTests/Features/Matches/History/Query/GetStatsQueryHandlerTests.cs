using Moq;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Features.Matches.History.Query;
using TicTacToe.Domain.DTOs.Matches;

namespace TicTacToe.Application.UnitTests.Features.Matches.History.Query;

[TestFixture]
public sealed class GetStatsQueryHandlerTests
{
    private static readonly Guid TestUser = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public async Task Handle_User_ReturnsComputedTotals()
    {
        var (handler, _) = Build(new PlayerStats(Played: 10, Wins: 6, Losses: 3, Draws: 1), TestUser);

        var result = await handler.Handle(new GetStatsQuery(), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Wins.ShouldBe(6);
        result.Value.Played.ShouldBe(10);
    }

    [Test]
    public async Task Handle_Guest_ReturnsValidation()
    {
        var (handler, _) = Build(new PlayerStats(0, 0, 0, 0), userId: null);

        var result = await handler.Handle(new GetStatsQuery(), default);

        result.Error!.Code.ShouldBe("Validation.Error");
    }

    private static (GetStatsQueryHandler, Mock<IMatchRepository>) Build(PlayerStats stats, Guid? userId)
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(x => x.UserId).Returns(userId);
        var repo = new Mock<IMatchRepository>();
        repo.Setup(x => x.GetStatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(stats);
        return (new GetStatsQueryHandler(user.Object, repo.Object), repo);
    }
}


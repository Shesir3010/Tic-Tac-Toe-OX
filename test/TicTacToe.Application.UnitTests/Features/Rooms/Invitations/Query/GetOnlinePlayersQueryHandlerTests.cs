using Moq;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Features.Rooms.Invitations.Query;
using TicTacToe.Domain.DTOs.Rooms;
using TicTacToe.Domain.Enums;

namespace TicTacToe.Application.UnitTests.Features.Rooms.Invitations.Query;

[TestFixture]
public sealed class GetOnlinePlayersQueryHandlerTests
{
    private static readonly Guid Me = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Test]
    public async Task Handle_User_ReturnsOnlineList()
    {
        var presence = new Mock<IPresenceTracker>();
        presence.Setup(x => x.GetOnline()).Returns([
            new OnlinePlayerDto(Me, "me", OnlineStatus.Available),
            new OnlinePlayerDto(Guid.NewGuid(), "opponent1", OnlineStatus.Available),
        ]);
        var user = new Mock<ICurrentUser>();
        user.Setup(x => x.UserId).Returns(Me);
        var invitations = new Mock<IInvitationManager>();
        invitations.Setup(x => x.GetPendingForUser(It.IsAny<Guid>())).Returns(Array.Empty<TicTacToe.Domain.Online.Invitation>());
        var handler = new GetOnlinePlayersQueryHandler(user.Object, presence.Object, invitations.Object, new Mock<IRoomStore>().Object);

        var result = await handler.Handle(new GetOnlinePlayersQuery(), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
    }

    [Test]
    public async Task Handle_Guest_ReturnsValidation()
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(x => x.UserId).Returns((Guid?)null);
        var handler = new GetOnlinePlayersQueryHandler(user.Object, new Mock<IPresenceTracker>().Object, new Mock<IInvitationManager>().Object, new Mock<IRoomStore>().Object);

        var result = await handler.Handle(new GetOnlinePlayersQuery(), default);

        result.Error!.Code.ShouldBe("Validation.Error");
    }
}

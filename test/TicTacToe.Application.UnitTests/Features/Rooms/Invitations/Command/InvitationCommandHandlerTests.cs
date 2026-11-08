using Moq;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Features.Rooms.Invitations.Command;
using TicTacToe.Domain.DTOs.Rooms;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Online;

namespace TicTacToe.Application.UnitTests.Features.Rooms.Invitations.Command;

[TestFixture]
public sealed class InvitationCommandHandlerTests
{
    private static readonly Guid Me = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Target = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void Send_GuardMapsCode()
    {
        var user = User();
        var invocations = new Mock<IInvitationManager>();
        invocations.Setup(x => x.Send(Me, Target, It.IsAny<TimeProvider>())).Returns((false, null, "Invite.Self"));
        var handler = new SendInvitationCommandHandler(user.Object, invocations.Object, new FakeClock(Now));

        var result = handler.Handle(new SendInvitationCommand(Target), default).GetAwaiter().GetResult();

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Invite.Self");
    }

    [Test]
    public void Send_OkReturnsWaiting()
    {
        var user = User();
        var pending = new Invitation { Id = Guid.NewGuid(), FromUserId = Me, ToUserId = Target };
        var invocations = new Mock<IInvitationManager>();
        invocations.Setup(x => x.Send(Me, Target, It.IsAny<TimeProvider>())).Returns((true, pending, ""));
        var handler = new SendInvitationCommandHandler(user.Object, invocations.Object, new FakeClock(Now));

        var result = handler.Handle(new SendInvitationCommand(Target), default).GetAwaiter().GetResult();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.InvitationId.ShouldBe(pending.Id);
        result.Value.ExpiresInSeconds.ShouldBe(30);
    }

    [Test]
    public void Cancel_OnlyInviter_Forbidden()
    {
        var user = User();
        var invocations = new Mock<IInvitationManager>();
        invocations.Setup(x => x.Cancel(It.IsAny<Guid>(), Me)).Returns((false, "Invite.Forbidden"));
        var handler = new CancelInvitationCommandHandler(user.Object, invocations.Object);

        var result = handler.Handle(new CancelInvitationCommand(Guid.NewGuid()), default).GetAwaiter().GetResult();

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Invite.Forbidden");
    }

    [Test]
    public void Respond_Accept_CreatesRoomOnce()
    {
        var user = User();
        var pending = new Invitation { Id = Guid.NewGuid(), FromUserId = Target, ToUserId = Me };
        var room = new RoomState(Guid.NewGuid(), RoomStatus.Playing, new Player?[9], Player.X, Target, Me, null);
        var invocations = new Mock<IInvitationManager>();
        invocations.Setup(x => x.Respond(pending.Id, Me, true, It.IsAny<TimeProvider>())).Returns((true, pending, ""));
        var rooms = new Mock<IRoomStore>();
        rooms.Setup(x => x.Create(Target, Me)).Returns((true, room, ""));
        var handler = new RespondToInvitationCommandHandler(user.Object, invocations.Object, rooms.Object, new FakeClock(Now));

        var result = handler.Handle(new RespondToInvitationCommand(pending.Id, true), default).GetAwaiter().GetResult();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(room.Id);
        result.Value.PlayerXId.ShouldBe(Target); // inviter = X
        rooms.Verify(x => x.Create(Target, Me), Times.Once);
    }

    [Test]
    public void Respond_Reject_NoRoom()
    {
        var user = User();
        var pending = new Invitation { Id = Guid.NewGuid(), FromUserId = Target, ToUserId = Me };
        var invocations = new Mock<IInvitationManager>();
        invocations.Setup(x => x.Respond(pending.Id, Me, false, It.IsAny<TimeProvider>())).Returns((true, pending, ""));
        var rooms = new Mock<IRoomStore>();
        var handler = new RespondToInvitationCommandHandler(user.Object, invocations.Object, rooms.Object, new FakeClock(Now));

        var result = handler.Handle(new RespondToInvitationCommand(pending.Id, false), default).GetAwaiter().GetResult();

        result.IsSuccess.ShouldBeTrue();
        rooms.Verify(x => x.Create(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    private static Mock<ICurrentUser> User()
    {
        var u = new Mock<ICurrentUser>();
        u.Setup(x => x.UserId).Returns(Me);
        return u;
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

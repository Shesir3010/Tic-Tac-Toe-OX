using Moq;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Application.Common.Results;
using TicTacToe.Application.Contracts;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Features.Rooms.Online.Command;
using TicTacToe.Application.Features.Rooms.Online.Query;
using TicTacToe.Domain.DTOs.Rooms;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Online;

namespace TicTacToe.Application.UnitTests.Features.Rooms.Online.Command;

[TestFixture]
public sealed class OnlineCommandHandlerTests
{
    private static readonly Guid XUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OUser = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private static Mock<ICurrentUser> User(Guid id)
    {
        var u = new Mock<ICurrentUser>();
        u.Setup(x => x.UserId).Returns(id);
        return u;
    }

    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Test]
    public async Task MakeMove_Finish_SavesBothRowsOnce()
    {
        var room = new RoomState(Guid.NewGuid(), RoomStatus.Finished, new Player?[9], null, XUser, OUser, null);
        var rooms = new Mock<IRoomStore>();
        rooms.Setup(x => x.ApplyMove(It.IsAny<Guid>(), XUser, 2))
             .Returns((room, "", new List<int> { 0, 3, 1, 4, 2 }.AsReadOnly(), GameOutcome.XWins, true));
        var repo = new Mock<IMatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        var handler = new MakeOnlineMoveCommandHandler(User(XUser).Object, rooms.Object, repo.Object, uow.Object, new FakeClock(Now));

        var result = await handler.Handle(new MakeOnlineMoveCommand(room.Id, 2), default);

        result.IsSuccess.ShouldBeTrue();
        repo.Verify(x => x.AddAsync(It.IsAny<TicTacToe.Domain.Entities.Match>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task MakeMove_NotParticipant_MapsCode()
    {
        var rooms = new Mock<IRoomStore>();
        rooms.Setup(x => x.ApplyMove(It.IsAny<Guid>(), XUser, 0))
             .Returns(((RoomState?)null, "Room.NotParticipant", Array.Empty<int>(), GameOutcome.InProgress, false));
        var handler = new MakeOnlineMoveCommandHandler(User(XUser).Object, rooms.Object, new Mock<IMatchRepository>().Object, new Mock<IUnitOfWork>().Object, new FakeClock(Now));

        var result = await handler.Handle(new MakeOnlineMoveCommand(Guid.NewGuid(), 0), default);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Room.NotParticipant");
    }

    [Test]
    public async Task Leave_Forfeit_SavesBoth()
    {
        var room = new RoomState(Guid.NewGuid(), RoomStatus.Finished, new Player?[9], null, XUser, OUser, null);
        var rooms = new Mock<IRoomStore>();
        rooms.Setup(x => x.Leave(It.IsAny<Guid>(), XUser)).Returns((room, "", true, new List<int> { 0 }.AsReadOnly(), GameOutcome.OWins));
        var repo = new Mock<IMatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        var handler = new LeaveRoomCommandHandler(User(XUser).Object, rooms.Object, repo.Object, uow.Object, new FakeClock(Now));

        var result = await handler.Handle(new LeaveRoomCommand(room.Id), default);

        result.IsSuccess.ShouldBeTrue();
        repo.Verify(x => x.AddAsync(It.IsAny<TicTacToe.Domain.Entities.Match>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RequestRematch_OneReady_ReturnsAlreadyOverWithRoom()
    {
        var room = new RoomState(Guid.NewGuid(), RoomStatus.Finished, new Player?[9], null, XUser, OUser, null);
        var rooms = new Mock<IRoomStore>();
        rooms.Setup(x => x.RequestRematch(It.IsAny<Guid>(), XUser)).Returns((room, "Game.AlreadyOver"));
        var handler = new RequestRematchCommandHandler(User(XUser).Object, rooms.Object);

        var result = await handler.Handle(new RequestRematchCommand(room.Id), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(room.Id);
    }

    [Test]
    public async Task GetRoom_NotParticipant_MapsCode()
    {
        var room = new RoomState(Guid.NewGuid(), RoomStatus.Playing, new Player?[9], Player.X, XUser, OUser, null);
        var rooms = new Mock<IRoomStore>();
        rooms.Setup(x => x.Get(It.IsAny<Guid>())).Returns(room);
        var other = Guid.NewGuid();
        var handler = new GetRoomQueryHandler(User(other).Object, rooms.Object, new FakeClock(Now));

        var result = await handler.Handle(new TicTacToe.Application.Features.Rooms.Online.Query.GetRoomQuery(room.Id), default);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Room.NotParticipant");
    }
}

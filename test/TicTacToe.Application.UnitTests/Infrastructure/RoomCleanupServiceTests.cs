using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Application.Contracts;
using TicTacToe.Application.Contracts.Repositories;
using TicTacToe.Application.Contracts.Services;
using TicTacToe.Application.Settings;
using TicTacToe.Domain.DTOs.Rooms;
using TicTacToe.Domain.Engine;
using TicTacToe.Domain.Online;
using TicTacToe.Infrastructure.Services;

namespace TicTacToe.Application.UnitTests.Infrastructure;

/// <summary>M11 U-13: cleanup Tick saves forfeits once, orphans close without saves.</summary>
[TestFixture]
public sealed class RoomCleanupServiceTests
{
    private static readonly Guid XUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OUser = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset T0 = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static (RoomCleanupService service, Mock<IMatchRepository> repo, Mock<IUnitOfWork> uow) Build(Mock<IRoomStore> rooms)
    {
        var repo = new Mock<IMatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        var services = new ServiceCollection();
        services.AddScoped<IMatchRepository>(_ => repo.Object);
        services.AddScoped<IUnitOfWork>(_ => uow.Object);
        var provider = services.BuildServiceProvider();
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(x => x.CreateScope()).Returns(provider.CreateScope());
        var service = new RoomCleanupService(factory.Object, rooms.Object, new Mock<IInvitationManager>().Object, new FakeClock(T0), Options.Create(new RoomSettings()), Microsoft.Extensions.Logging.Abstractions.NullLogger<RoomCleanupService>.Instance);
        return (service, repo, uow);
    }

    [Test]
    public async Task Tick_Forfeit_SavesBothRowsOnce()
    {
        var room = new RoomState(Guid.NewGuid(), RoomStatus.Playing, new Player?[9], Player.X, XUser, OUser, null);
        var rooms = new Mock<IRoomStore>();
        rooms.Setup(x => x.SweepExpired(It.IsAny<TimeProvider>(), 60)).Returns(new RoomSweepResult(
            [new RoomFinish(room, [0], OUser, XUser)], []));
        var (service, repo, uow) = Build(rooms);

        await service.TickAsync(default);

        repo.Verify(x => x.AddAsync(It.IsAny<TicTacToe.Domain.Entities.Match>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Tick_Orphan_NoSaves()
    {
        var room = new RoomState(Guid.NewGuid(), RoomStatus.Closed, new Player?[9], null, XUser, OUser, null);
        var rooms = new Mock<IRoomStore>();
        rooms.Setup(x => x.SweepExpired(It.IsAny<TimeProvider>(), 60)).Returns(new RoomSweepResult([], [room]));
        var (service, repo, uow) = Build(rooms);

        await service.TickAsync(default);

        repo.Verify(x => x.AddAsync(It.IsAny<TicTacToe.Domain.Entities.Match>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

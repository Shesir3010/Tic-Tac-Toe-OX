using System.Collections.Concurrent;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Domain.Engine;
using TicTacToe.Infrastructure.Services;

namespace TicTacToe.Application.UnitTests.Infrastructure;

/// <summary>M11 U-11/U-12: per-room locks, 10-race basics, grace fake time, cleanup sweep.</summary>
[TestFixture]
public sealed class RoomStoreTests
{
    private static readonly Guid XUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OUser = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset T0 = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Test]
    public void ApplyMove_TwoWriters_SameCell_ExactlyOneWins()
    {
        var store = new InMemoryRoomStore();
        var (ok, room, _) = store.Create(XUser, OUser);
        ok.ShouldBeTrue();

        var winners = new ConcurrentBag<string>();
        var tasks = new[]
        {
            Task.Run(() => winners.Add(store.ApplyMove(room!.Id, XUser, 0).errorCode is var c ? (c == "" ? "ok" : c) : "")),
            Task.Run(() => winners.Add(store.ApplyMove(room!.Id, XUser, 0).errorCode is var c ? (c == "" ? "ok" : c) : "")),
        };
        Task.WaitAll(tasks);
        winners.Count(w => w == "ok").ShouldBe(1);
    }

    [Test]
    public void ApplyMove_NotYourTurn_Fails()
    {
        var store = new InMemoryRoomStore();
        var (ok, room, _) = store.Create(XUser, OUser);
        ok.ShouldBeTrue();
        var (r, code, _, _, _) = store.ApplyMove(room!.Id, OUser, 0);
        r.ShouldBeNull();
        code.ShouldBe("Game.NotYourTurn");
    }

    [Test]
    public void Leave_AtMidGame_OpponentWinsOutcome()
    {
        var store = new InMemoryRoomStore();
        var (ok, room, _) = store.Create(XUser, OUser);
        ok.ShouldBeTrue();
        var (r, code, forfeit, _, outcome) = store.Leave(room!.Id, XUser);
        code.ShouldBe("");
        forfeit.ShouldBeTrue();
        outcome.ShouldBe(GameOutcome.OWins);
        r!.Status.ToString().ShouldBe("Finished");
    }

    [Test]
    public void RequestRematch_OneReady_StaysFinished_BothReady_Resets()
    {
        var store = new InMemoryRoomStore();
        var (ok, room, _) = store.Create(XUser, OUser);
        ok.ShouldBeTrue();
        // Finish quickly: X0 O3 X1 O4 X2
        store.ApplyMove(room!.Id, XUser, 0);
        store.ApplyMove(room.Id, OUser, 3);
        store.ApplyMove(room.Id, XUser, 1);
        store.ApplyMove(room.Id, OUser, 4);
        store.ApplyMove(room.Id, XUser, 2);

        var (r1, c1) = store.RequestRematch(room.Id, XUser);
        c1.ShouldBe("Game.AlreadyOver");
        r1!.Status.ToString().ShouldBe("Finished");
        var (r2, c2) = store.RequestRematch(room.Id, OUser);
        c2.ShouldBe("");
        r2!.Status.ToString().ShouldBe("Playing");
        r2.Board.ShouldBe(new Player?[9]);
    }

    [Test]
    public void Sweep_OnePastGrace_Forfeit_BothPast_OrphanClose_NoSaves()
    {
        var store = new InMemoryRoomStore();
        var (_, room, _) = store.Create(XUser, OUser);
        var deadline = T0.AddSeconds(60);
        store.MarkDisconnected(room!.Id, XUser, deadline);
        var sweep1 = store.SweepExpired(new FakeClock(T0.AddSeconds(30)), 60);
        sweep1.ForfeitFinishes.Count.ShouldBe(0);
        var sweep2 = store.SweepExpired(new FakeClock(T0.AddSeconds(61)), 60);
        sweep2.ForfeitFinishes.Count.ShouldBe(1);
        sweep2.ForfeitFinishes[0].WinnerId.ShouldBe(OUser);

        var store2 = new InMemoryRoomStore();
        var (_, room2, _) = store2.Create(XUser, OUser);
        store2.MarkDisconnected(room2!.Id, XUser, deadline);
        store2.MarkDisconnected(room2.Id, OUser, deadline);
        var sweep3 = store2.SweepExpired(new FakeClock(T0.AddSeconds(61)), 60);
        sweep3.ForfeitFinishes.Count.ShouldBe(0);
        sweep3.OrphanCloses.Count.ShouldBe(1);
    }
}

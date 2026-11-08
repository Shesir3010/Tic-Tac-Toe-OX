using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using TicTacToe.Application.Settings;
using TicTacToe.Infrastructure.Services;

namespace TicTacToe.Application.UnitTests.Infrastructure;

/// <summary>M10 U-4: guards, TTL, cooldown with fake time.</summary>
[TestFixture]
public sealed class InvitationManagerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid A = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private InvitationManager Manager(FakeClock clock, int countB = 1, int countA = 1)
    {
        var presence = new PresenceTracker();
        for (var i = 0; i < countB; i++) presence.AddUser(B, "B");
        for (var i = 0; i < countA; i++) presence.AddUser(A, "A");
        return new InvitationManager(presence, Options.Create(new RoomSettings { InvitationTtlSeconds = 30, CooldownSeconds = 30 }));
    }

    [Test]
    public void Send_Self_InviteSelf()
    {
        var manager = Manager(new FakeClock(T0));
        manager.Send(A, A, new FakeClock(T0)).errorCode.ShouldBe("Invite.Self");
    }

    [Test]
    public void Send_Offline_InviteOffline()
    {
        var presence = new PresenceTracker();
        presence.AddUser(A, "A");
        var manager = new InvitationManager(presence, Options.Create(new RoomSettings()));

        manager.Send(A, B, new FakeClock(T0)).errorCode.ShouldBe("Invite.Offline");
    }

    [Test]
    public void Send_Busy_InviteBusy()
    {
        var clock = new FakeClock(T0);
        var manager = Manager(clock);
        var c = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var presence = new PresenceTracker();
        presence.AddUser(A, "A"); presence.AddUser(B, "B"); presence.AddUser(c, "C");
        var m2 = new InvitationManager(presence, Options.Create(new RoomSettings()));
        m2.Send(A, B, clock).ok.ShouldBeTrue();

        m2.Send(c, B, clock).errorCode.ShouldBe("Invite.Busy");
    }

    [Test]
    public void Send_AlreadyPending_InviteAlreadyPending()
    {
        var clock = new FakeClock(T0);
        var presence = new PresenceTracker();
        var c = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        presence.AddUser(A, "A"); presence.AddUser(B, "B"); presence.AddUser(c, "C");
        var manager = new InvitationManager(presence, Options.Create(new RoomSettings()));
        manager.Send(A, B, clock).ok.ShouldBeTrue();

        manager.Send(A, c, clock).errorCode.ShouldBe("Invite.AlreadyPending");
    }

    [Test]
    public void Reject_Cooldown_BlocksReInvite()
    {
        var clock = new FakeClock(T0);
        var manager = Manager(clock);
        var (_, inv, _) = manager.Send(A, B, clock);
        manager.Respond(inv!.Id, B, false, clock);

        manager.Send(A, B, clock).errorCode.ShouldBe("Invite.Cooldown");

        // After 31s cooldown lifts.
        var later = new FakeClock(T0.AddSeconds(31));
        manager.Send(A, B, later).ok.ShouldBeTrue();
    }

    [Test]
    public void ExpirePending_AfterTtl_Expires()
    {
        var clock = new FakeClock(T0);
        var manager = Manager(clock);
        var (_, inv, _) = manager.Send(A, B, clock);

        manager.ExpirePending(new FakeClock(T0.AddSeconds(31)));

        manager.Respond(inv!.Id, B, true, new FakeClock(T0.AddSeconds(32))).errorCode.ShouldBe("Room.Expired");
    }

    [Test]
    public void Cancel_OnlyInviter()
    {
        var clock = new FakeClock(T0);
        var manager = Manager(clock);
        var (_, inv, _) = manager.Send(A, B, clock);

        manager.Cancel(inv!.Id, B).errorCode.ShouldBe("Invite.Forbidden");
        manager.Cancel(inv.Id, A).ok.ShouldBeTrue();
    }

    [Test]
    public void CancelOffline_RemovesPending()
    {
        var clock = new FakeClock(T0);
        var manager = Manager(clock);
        var (_, inv, _) = manager.Send(A, B, clock);

        manager.CancelForOffline(B);

        manager.Respond(inv!.Id, B, true, clock).errorCode.ShouldBe("Room.Expired");
    }

    [Test]
    public void Respond_ExactlyOnce_SecondFails()
    {
        var clock = new FakeClock(T0);
        var manager = Manager(clock);
        var (_, inv, _) = manager.Send(A, B, clock);

        manager.Respond(inv!.Id, B, true, clock).ok.ShouldBeTrue();
        manager.Respond(inv.Id, B, true, clock).errorCode.ShouldBe("Room.Expired");
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

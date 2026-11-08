using NUnit.Framework;
using Shouldly;
using TicTacToe.Infrastructure.Services;

namespace TicTacToe.Application.UnitTests.Infrastructure;

[TestFixture]
public sealed class PresenceTrackerTests
{
    [Test]
    public void AddRemove_CountFlipsOnline()
    {
        var tracker = new PresenceTracker();
        var id = Guid.NewGuid();

        tracker.AddUser(id, "rafi");
        tracker.ConnectionCount(id).ShouldBe(1);

        tracker.AddUser(id, "rafi"); // second tab
        tracker.ConnectionCount(id).ShouldBe(2);

        tracker.RemoveUser(id);
        tracker.ConnectionCount(id).ShouldBe(1);

        tracker.RemoveUser(id);
        tracker.ConnectionCount(id).ShouldBe(0);
        tracker.GetOnline().ShouldBeEmpty();
    }

    [Test]
    public void GetOnline_ReturnsAvailableSortedByName()
    {
        var tracker = new PresenceTracker();
        tracker.AddUser(Guid.NewGuid(), "rafi");
        tracker.AddUser(Guid.NewGuid(), "sara");

        var online = tracker.GetOnline();

        online.Count.ShouldBe(2);
        online[0].DisplayName.ShouldBe("rafi");
        online[1].DisplayName.ShouldBe("sara");
        online.ShouldAllBe(p => p.Status.ToString() == "Available");
    }

    [Test]
    public void RemoveUnknown_IsSafe()
    {
        var tracker = new PresenceTracker();

        tracker.RemoveUser(Guid.NewGuid());

        tracker.GetOnline().ShouldBeEmpty();
    }
}

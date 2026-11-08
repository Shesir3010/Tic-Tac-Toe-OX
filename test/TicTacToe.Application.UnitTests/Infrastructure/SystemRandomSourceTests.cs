using NUnit.Framework;
using Shouldly;
using TicTacToe.Infrastructure.Services;

namespace TicTacToe.Application.UnitTests.Infrastructure;

[TestFixture]
public sealed class SystemRandomSourceTests
{
    [TestCase(1)]
    [TestCase(9)]
    [TestCase(100)]
    public void Next_Positive_ReturnsInRange(int maxExclusive)
    {
        var value = new SystemRandomSource().Next(maxExclusive);

        value.ShouldBeGreaterThanOrEqualTo(0);
        value.ShouldBeLessThan(maxExclusive);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Next_NonPositive_Throws(int maxExclusive)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new SystemRandomSource().Next(maxExclusive));
    }
}

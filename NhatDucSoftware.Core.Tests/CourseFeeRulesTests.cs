using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Tests;

public class CourseFeeRulesTests
{
    private static readonly DateTime Today = new(2026, 9, 21);

    [Fact]
    public void MinEffectiveFrom_IsFirstDayOfCurrentMonth()
    {
        Assert.Equal(new DateTime(2026, 9, 1), CourseFeeRules.MinEffectiveFrom(Today));
    }

    [Fact]
    public void EnsureEffectiveFromAllowed_AcceptsFifteenthOfCurrentMonth()
    {
        CourseFeeRules.EnsureEffectiveFromAllowed(new DateTime(2026, 9, 15), Today);
    }

    [Fact]
    public void EnsureEffectiveFromAllowed_RejectsPreviousMonth()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => CourseFeeRules.EnsureEffectiveFromAllowed(new DateTime(2026, 8, 31), Today));

        Assert.Contains("01/09/2026", ex.Message);
    }

    [Fact]
    public void ResolveFee_UsesNewPriceFromEffectiveDateInclusive()
    {
        var history = new List<CourseFeeHistoryEntry>
        {
            new() { Id = 1, TuitionFee = 35_000m, EffectiveFrom = new DateTime(1900, 1, 1) },
            new() { Id = 2, TuitionFee = 40_000m, EffectiveFrom = new DateTime(2026, 9, 15) }
        };

        Assert.Equal(35_000m, CourseFeeRules.ResolveFee(history, new DateTime(2026, 9, 14), 0m));
        Assert.Equal(40_000m, CourseFeeRules.ResolveFee(history, new DateTime(2026, 9, 15), 0m));
        Assert.Equal(40_000m, CourseFeeRules.ResolveFee(history, new DateTime(2026, 9, 21), 0m));
    }
}

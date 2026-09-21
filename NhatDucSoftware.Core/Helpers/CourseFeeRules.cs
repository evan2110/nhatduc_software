using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Helpers;

public static class CourseFeeRules
{
    public static DateTime MinEffectiveFrom(DateTime today) =>
        new(today.Year, today.Month, 1);

    public static void EnsureEffectiveFromAllowed(DateTime effectiveFrom, DateTime today)
    {
        var min = MinEffectiveFrom(today);
        if (effectiveFrom.Date < min)
        {
            throw new InvalidOperationException(
                $"Ngày hiệu lực không được trước {min:dd/MM/yyyy}.");
        }
    }

    public static decimal ResolveFee(
        IReadOnlyList<CourseFeeHistoryEntry> history,
        DateTime sessionDate,
        decimal fallbackFee)
    {
        var match = history
            .Where(h => h.EffectiveFrom.Date <= sessionDate.Date)
            .OrderByDescending(h => h.EffectiveFrom.Date)
            .ThenByDescending(h => h.Id)
            .FirstOrDefault();

        return match?.TuitionFee ?? fallbackFee;
    }
}

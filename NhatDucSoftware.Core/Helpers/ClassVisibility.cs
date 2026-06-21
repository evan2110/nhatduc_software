using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Helpers;

public static class ClassVisibility
{
    public static bool IsVisibleForWeek(ClassInfo classInfo, DateTime weekMonday) =>
        IsVisibleForWeek(classInfo.Status, classInfo.InactiveFromWeekStart, weekMonday);

    public static bool IsVisibleForDate(ClassInfo classInfo, DateTime date) =>
        IsVisibleForWeek(classInfo, ClassWeeklySchedule.GetMondayOfWeek(date));

    public static bool IsVisibleForWeek(string status, string? inactiveFromWeekStart, DateTime weekMonday)
    {
        if (!string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(inactiveFromWeekStart))
        {
            return false;
        }

        var inactiveMonday = DateTime.Parse(inactiveFromWeekStart).Date;
        return ClassWeeklySchedule.GetMondayOfWeek(weekMonday) < inactiveMonday;
    }
}

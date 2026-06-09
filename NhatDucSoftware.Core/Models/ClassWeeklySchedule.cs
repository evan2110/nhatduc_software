namespace NhatDucSoftware.Core.Models;

public class ClassWeeklySchedule
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string WeekStartDate { get; set; } = string.Empty; // Monday of the week (yyyy-MM-dd)
    public int DayOfWeek { get; set; } // 0=Monday, 1=Tuesday, ..., 6=Sunday
    public int ShiftNumber { get; set; } // 1-5

    public static string GetDayName(int day) => day switch
    {
        0 => "Thứ 2",
        1 => "Thứ 3",
        2 => "Thứ 4",
        3 => "Thứ 5",
        4 => "Thứ 6",
        5 => "Thứ 7",
        6 => "Chủ nhật",
        _ => ""
    };

    public static DateTime GetMondayOfWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - System.DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }
}

namespace NhatDucSoftware.Core.Models;

public class TeacherTimesheet
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public DateTime WorkDate { get; set; }
    public int ShiftNumber { get; set; } // 1-5
    public bool IsPresent { get; set; }
    public string? Note { get; set; }

    // Computed
    public string TeacherName { get; set; } = string.Empty;

    public static readonly Dictionary<int, (TimeSpan Start, TimeSpan End)> Shifts = new()
    {
        { 1, (new TimeSpan(7, 30, 0), new TimeSpan(9, 0, 0)) },
        { 2, (new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0)) },
        { 3, (new TimeSpan(14, 0, 0), new TimeSpan(15, 30, 0)) },
        { 4, (new TimeSpan(15, 30, 0), new TimeSpan(17, 0, 0)) },
        { 5, (new TimeSpan(17, 30, 0), new TimeSpan(19, 0, 0)) },
    };

    public const decimal DefaultPayPerShift = 100_000;

    public const decimal PayPerShift = DefaultPayPerShift;

    public static string GetShiftDescription(int shiftNumber)
    {
        if (!Shifts.TryGetValue(shiftNumber, out var shift))
            return "N/A";
        return $"Ca {shiftNumber}: {shift.Start:hh\\:mm} - {shift.End:hh\\:mm}";
    }
}

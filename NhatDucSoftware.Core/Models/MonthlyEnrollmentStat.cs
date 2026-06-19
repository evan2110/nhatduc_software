namespace NhatDucSoftware.Core.Models;

public class MonthlyEnrollmentStat
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int ClassCount { get; set; }
}

namespace NhatDucSoftware.Core.Models;

public class RevenueByMonthStat
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
}

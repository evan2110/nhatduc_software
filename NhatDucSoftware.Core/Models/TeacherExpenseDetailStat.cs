namespace NhatDucSoftware.Core.Models;

public class TeacherExpenseDetailStat
{
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

namespace NhatDucSoftware.Core.Models;

public class ReportSummary
{
    public int TotalStudents { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ActiveClasses { get; set; }

    /// <summary>Học phí từ điểm danh (sau giảm) theo năm đang chọn.</summary>
    public decimal TotalTuitionEarned { get; set; }

    /// <summary>Lương giáo viên (sau điều chỉnh) theo năm đang chọn.</summary>
    public decimal TotalExpense { get; set; }
}

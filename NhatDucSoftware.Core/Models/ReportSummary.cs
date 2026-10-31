namespace NhatDucSoftware.Core.Models;

public class ReportSummary
{
    public int TotalStudents { get; set; }

    /// <summary>Học phí đã thu vào sổ RevenueLedger, chưa gồm khoản thu thủ công.</summary>
    public decimal TuitionCollected { get; set; }

    /// <summary>Học phí đã thu cộng các khoản thu thủ công.</summary>
    public decimal TotalRevenue { get; set; }
    public int ActiveClasses { get; set; }

    /// <summary>Học phí từ điểm danh (sau giảm) theo năm đang chọn.</summary>
    public decimal TotalTuitionEarned { get; set; }

    /// <summary>Lương giáo viên (sau điều chỉnh) theo năm đang chọn.</summary>
    public decimal TotalSalaryExpense { get; set; }

    /// <summary>Chi tiêu thủ công (sổ Expenses) theo năm đang chọn.</summary>
    public decimal TotalManualExpense { get; set; }

    /// <summary>Tổng chi = Chi lương + Chi khác theo năm đang chọn.</summary>
    public decimal TotalExpense { get; set; }
}

namespace NhatDucSoftware.Core.Models;

public class CenterAttendanceExportRow
{
    public DateTime SessionDate { get; set; }
    public string ClassName { get; set; } = "";
    public int ShiftNumber { get; set; }
    public string StudentName { get; set; } = "";
    public string Status { get; set; } = "";
    public string TeacherName { get; set; } = "";
}

public class CenterTimesheetExportRow
{
    public DateTime WorkDate { get; set; }
    public string TeacherName { get; set; } = "";
    public int ShiftNumber { get; set; }
    public bool IsPresent { get; set; }
    public string? Note { get; set; }
    public decimal ShiftPay { get; set; }
}

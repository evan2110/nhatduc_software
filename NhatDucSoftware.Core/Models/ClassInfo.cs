namespace NhatDucSoftware.Core.Models;

public class ClassInfo
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public int? TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int CurrentSize { get; set; }
    public string Status { get; set; } = "Active";
    public string? InactiveFromWeekStart { get; set; }
}

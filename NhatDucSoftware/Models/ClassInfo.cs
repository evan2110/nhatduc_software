namespace NhatDucSoftware.Models;

public class ClassInfo
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int CurrentSize { get; set; }
    public string Status { get; set; } = "Active";
}

namespace NhatDucSoftware.Core.Models;

public class TeacherClassPaySetting
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal PayPerShift { get; set; } = TeacherTimesheet.DefaultPayPerShift;
    public bool IsConfigured { get; set; }
}

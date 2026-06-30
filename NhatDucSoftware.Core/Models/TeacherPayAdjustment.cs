namespace NhatDucSoftware.Core.Models;

public class TeacherPayAdjustment
{
    public long Id { get; set; }
    public int TeacherId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int ShiftCount { get; set; }
    public decimal PayPerShift { get; set; }
    public string? Note { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

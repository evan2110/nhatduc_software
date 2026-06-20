namespace NhatDucSoftware.Core.Models;

public class Teacher
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? Qualification { get; set; }
    /// <summary>JSON array of subject names, e.g. ["Toán 10","Lý 11"]</summary>
    public string? TeachingSubjects { get; set; }
}

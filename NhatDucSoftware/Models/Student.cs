namespace NhatDucSoftware.Models;

public class Student
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int? BirthYear { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = "Active";
}

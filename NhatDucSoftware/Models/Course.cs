namespace NhatDucSoftware.Models;

public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "JP";
    public decimal TuitionFee { get; set; }
    public int DurationHours { get; set; }
    public string Status { get; set; } = "Active";
}

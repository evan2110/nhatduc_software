namespace NhatDucSoftware.Core.Models;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TuitionFee { get; set; }
    public string Status { get; set; } = "Active";
}

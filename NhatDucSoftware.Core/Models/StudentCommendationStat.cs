namespace NhatDucSoftware.Core.Models;

public class StudentCommendationStat
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string ValueLabel { get; set; } = string.Empty;
}

namespace NhatDucSoftware.Core.Models;

public class StudentBalanceHistory
{
    public long Id { get; set; }
    public int StudentId { get; set; }
    public decimal OldBalance { get; set; }
    public decimal NewBalance { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UpdatedBy { get; set; }
    public string UpdatedByName { get; set; } = string.Empty;
}

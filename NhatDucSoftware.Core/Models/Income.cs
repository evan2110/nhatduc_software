namespace NhatDucSoftware.Core.Models;

public class Income
{
    public int Id { get; set; }
    public string IncomeDate { get; set; } = string.Empty;
    public string IncomeType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public string? CollectedBy { get; set; }
    public int? StudentCount { get; set; }
    public decimal? PricePerLesson { get; set; }
    public decimal? SalaryPerLesson { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
}

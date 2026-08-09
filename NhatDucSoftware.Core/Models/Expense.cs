namespace NhatDucSoftware.Core.Models;

public class Expense
{
    public int Id { get; set; }
    public string ExpenseDate { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? PaidBy { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? AttachmentFileId { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentUrl { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
}

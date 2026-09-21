namespace NhatDucSoftware.Core.Models;

public sealed class CourseFeeHistoryEntry
{
    public int Id { get; set; }
    public decimal TuitionFee { get; set; }
    public DateTime EffectiveFrom { get; set; }
}

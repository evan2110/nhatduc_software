namespace NhatDucSoftware.Core.Services;

public static class PaymentAllocator
{
    public static List<PaymentClassSlice> Allocate(
        decimal amount,
        IReadOnlyList<ClassCollectibleRow> classes)
    {
        if (amount <= 0)
        {
            return new List<PaymentClassSlice>();
        }

        var eligible = classes
            .Where(c => !c.IsFinalized && c.Remaining > 0)
            .OrderBy(c => c.ClassName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.ClassId)
            .ToList();

        var remainingAmount = amount;
        var slices = new List<PaymentClassSlice>();

        foreach (var row in eligible)
        {
            if (remainingAmount <= 0)
            {
                break;
            }

            var allocated = Math.Min(remainingAmount, row.Remaining);
            if (allocated <= 0)
            {
                continue;
            }

            slices.Add(new PaymentClassSlice
            {
                ClassId = row.ClassId,
                ClassName = row.ClassName,
                Amount = allocated
            });
            remainingAmount -= allocated;
        }

        return slices;
    }
}

public sealed class ClassCollectibleRow
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal Remaining { get; set; }
    public bool IsFinalized { get; set; }
}

public sealed class PaymentClassSlice
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

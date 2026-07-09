namespace NhatDucSoftware.Core.Services;

public static class TuitionDiscountAllocator
{
    public static decimal CalculateDiscountAmount(decimal totalDiscountable, decimal discountPercent)
    {
        if (totalDiscountable <= 0 || discountPercent <= 0)
        {
            return 0;
        }

        var percent = Math.Clamp(discountPercent, 0, 100);
        return Math.Round(totalDiscountable * percent / 100m, 0, MidpointRounding.AwayFromZero);
    }

    public static List<TuitionClassAllocation> Allocate(
        IReadOnlyList<TuitionClassGrossRow> classRows,
        decimal discountPercent)
    {
        var rowsWithBalance = classRows
            .Where(r => r.GrossAttendance + r.CarryOver > 0)
            .OrderBy(r => r.ClassName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.ClassId)
            .ToList();

        var totalDiscountable = rowsWithBalance.Sum(r => r.GrossAttendance + r.CarryOver);
        var totalDiscount = CalculateDiscountAmount(totalDiscountable, discountPercent);
        var remainingDiscount = totalDiscount;

        var allocationByClassId = new Dictionary<int, decimal>();
        foreach (var row in rowsWithBalance)
        {
            var discountableBase = row.GrossAttendance + row.CarryOver;
            var allocated = Math.Min(remainingDiscount, discountableBase);
            allocationByClassId[row.ClassId] = allocated;
            remainingDiscount -= allocated;
        }

        return classRows
            .OrderBy(r => r.ClassName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.ClassId)
            .Select(row =>
            {
                var discountAllocated = allocationByClassId.GetValueOrDefault(row.ClassId);
                var discountableBase = row.GrossAttendance + row.CarryOver;
                var netAttendance = Math.Max(0, row.GrossAttendance - Math.Min(discountAllocated, row.GrossAttendance));
                return new TuitionClassAllocation
                {
                    ClassId = row.ClassId,
                    ClassName = row.ClassName,
                    GrossAttendance = row.GrossAttendance,
                    DiscountAllocated = discountAllocated,
                    NetAttendance = netAttendance,
                    CarryOver = row.CarryOver
                };
            })
            .ToList();
    }
}

public sealed class TuitionClassGrossRow
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal GrossAttendance { get; set; }
    public decimal CarryOver { get; set; }
    public decimal DiscountableBase => GrossAttendance + CarryOver;
}

public sealed class TuitionClassAllocation
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal GrossAttendance { get; set; }
    public decimal DiscountAllocated { get; set; }
    public decimal NetAttendance { get; set; }
    public decimal CarryOver { get; set; }
    public decimal DiscountableBase => GrossAttendance + CarryOver;
    public decimal TotalDue => Math.Max(0, DiscountableBase - DiscountAllocated);
}

public sealed class StudentTuitionDiscountInfo
{
    public decimal DiscountPercent { get; set; }
    public string Note { get; set; } = string.Empty;
}

public sealed class StudentTuitionDiscountPreview
{
    public int StudentId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal DiscountPercent { get; set; }
    public string Note { get; set; } = string.Empty;
    public decimal TotalGrossAttendance { get; set; }
    public decimal TotalCarryOver { get; set; }
    public decimal TotalDiscountableBase { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal TotalDueAfterDiscount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemainingAfterDiscount { get; set; }
    public List<TuitionClassAllocation> ClassAllocations { get; set; } = new();
    public bool IsLocked { get; set; }
}

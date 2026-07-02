namespace NhatDucSoftware.Core.Services;

public static class TuitionDiscountAllocator
{
    public static decimal CalculateDiscountAmount(decimal totalGrossAttendance, decimal discountPercent)
    {
        if (totalGrossAttendance <= 0 || discountPercent <= 0)
        {
            return 0;
        }

        var percent = Math.Clamp(discountPercent, 0, 100);
        return Math.Round(totalGrossAttendance * percent / 100m, 0, MidpointRounding.AwayFromZero);
    }

    public static List<TuitionClassAllocation> Allocate(
        IReadOnlyList<TuitionClassGrossRow> classRows,
        decimal discountPercent)
    {
        var rowsWithAttendance = classRows
            .Where(r => r.GrossAttendance > 0)
            .OrderBy(r => r.ClassName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.ClassId)
            .ToList();

        var totalGross = rowsWithAttendance.Sum(r => r.GrossAttendance);
        var totalDiscount = CalculateDiscountAmount(totalGross, discountPercent);
        var remainingDiscount = totalDiscount;

        var allocationByClassId = new Dictionary<int, decimal>();
        for (var i = 0; i < rowsWithAttendance.Count; i++)
        {
            var row = rowsWithAttendance[i];
            decimal allocated;
            if (i == rowsWithAttendance.Count - 1)
            {
                allocated = Math.Min(remainingDiscount, row.GrossAttendance);
            }
            else
            {
                allocated = Math.Min(remainingDiscount, row.GrossAttendance);
            }

            allocationByClassId[row.ClassId] = allocated;
            remainingDiscount -= allocated;
        }

        return classRows
            .OrderBy(r => r.ClassName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.ClassId)
            .Select(row =>
            {
                var discountAllocated = allocationByClassId.GetValueOrDefault(row.ClassId);
                var netAttendance = Math.Max(0, row.GrossAttendance - discountAllocated);
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
}

public sealed class TuitionClassAllocation
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal GrossAttendance { get; set; }
    public decimal DiscountAllocated { get; set; }
    public decimal NetAttendance { get; set; }
    public decimal CarryOver { get; set; }
    public decimal TotalDue => NetAttendance + CarryOver;
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
    public decimal TotalDiscountAmount { get; set; }
    public decimal TotalDueAfterDiscount { get; set; }
    public List<TuitionClassAllocation> ClassAllocations { get; set; } = new();
    public bool IsLocked { get; set; }
}

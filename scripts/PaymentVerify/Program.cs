using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Services;

DbContext.Configure();

var month = DateTime.Now.Month;
var year = DateTime.Now.Year;
var paymentService = new PaymentService();

var summary = paymentService.GetClassPaymentSummary(0, month, year);
Console.WriteLine($"=== Payment verify {month:D2}/{year} (Tất cả) ===");
Console.WriteLine($"Tổng cần đóng: {summary.TotalDue:N0}");
Console.WriteLine($"Tổng đã đóng:  {summary.TotalPaid:N0}");
Console.WriteLine($"Tổng còn lại:  {summary.TotalRemaining:N0}");
Console.WriteLine();

var rows = paymentService.GetPaymentListByClassMonthYear(0, month, year)
    .Where(r => r.StudentId > 0)
    .Take(5)
    .ToList();

var mismatchCount = 0;
foreach (var row in rows)
{
    var total = paymentService.GetTotalTuitionByStudentInClassMonthYear(row.StudentId, 0, month, year);
    var paid = paymentService.GetPaidAmountByStudentMonthYear(row.StudentId, month, year, 0);
    var remaining = Math.Max(0, total - paid);
    var collectible = paymentService.GetStudentCollectibleRemaining(row.StudentId, month, year);
    var breakdown = paymentService.GetStudentTuitionBreakdownByClassMonthYear(row.StudentId, month, year);
    var breakdownRemaining = breakdown.Sum(b => b.Remaining);
    var discount = paymentService.GetStudentTuitionDiscount(row.StudentId, month, year);
    var preview = paymentService.GetStudentTuitionDiscountPreview(row.StudentId, month, year);

    var ok = remaining == breakdownRemaining;
    if (!ok)
    {
        mismatchCount++;
    }

    Console.WriteLine($"--- {row.HoVaTen} (id={row.StudentId}) {(ok ? "OK" : "MISMATCH")} ---");
    Console.WriteLine($"  Cần đóng: {total:N0} | Đã đóng: {paid:N0} | Còn lại: {remaining:N0} | Thu được: {collectible:N0}");
    Console.WriteLine($"  Σ breakdown còn lại: {breakdownRemaining:N0} | Giảm: {discount.DiscountPercent}%");
    if (preview.TotalDiscountableBase > 0)
    {
        Console.WriteLine($"  Tổng trước giảm: {preview.TotalDiscountableBase:N0} (gốc {preview.TotalGrossAttendance:N0} + nợ {preview.TotalCarryOver:N0}) -> giảm {preview.TotalDiscountAmount:N0} -> cần {preview.TotalDueAfterDiscount:N0}");
    }
    foreach (var item in breakdown.Where(b => b.TotalDue > 0 || b.Paid > 0 || b.Remaining > 0))
    {
        Console.WriteLine($"    {item.ClassName}: gốc={item.GrossAttendance:N0} giảm={item.DiscountAmount:N0} cần={item.TotalDue:N0} đã={item.Paid:N0} còn={item.Remaining:N0}");
    }

    if (collectible > 0)
    {
        var slices = PaymentAllocator.Allocate(100_000m, paymentService.GetStudentClassCollectibleRows(row.StudentId, month, year));
        var sliceTotal = slices.Sum(s => s.Amount);
        Console.WriteLine($"  Allocator test 100k -> {sliceTotal:N0} ({string.Join(", ", slices.Select(s => $"{s.ClassName}:{s.Amount:N0}"))})");
    }
}

Console.WriteLine();
Console.WriteLine(mismatchCount == 0
    ? "PASS: Tất cả học viên mẫu có breakdown khớp còn lại."
    : $"FAIL: {mismatchCount} học viên có breakdown không khớp.");

using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public sealed class TuitionYearReportData
{
    private readonly List<MonthlyAmountStat> _monthlyAmounts;
    private readonly Dictionary<int, List<ClassTuitionDetailStat>> _classDetailByMonth;
    private readonly List<ClassTuitionDetailStat> _yearClassDetail;

    private TuitionYearReportData(
        List<MonthlyAmountStat> monthlyAmounts,
        Dictionary<int, List<ClassTuitionDetailStat>> classDetailByMonth,
        List<ClassTuitionDetailStat> yearClassDetail)
    {
        _monthlyAmounts = monthlyAmounts;
        _classDetailByMonth = classDetailByMonth;
        _yearClassDetail = yearClassDetail;
    }

    public List<MonthlyAmountStat> GetMonthlyAmounts() => _monthlyAmounts;

    public List<ClassTuitionDetailStat> GetClassDetail(int? month = null) =>
        month is >= 1 and <= 12
            ? _classDetailByMonth.GetValueOrDefault(month.Value) ?? new List<ClassTuitionDetailStat>()
            : _yearClassDetail;

    public static TuitionYearReportData Load(int year)
    {
        var monthlyAmounts = new List<MonthlyAmountStat>();
        var classDetailByMonth = new Dictionary<int, List<ClassTuitionDetailStat>>();
        var yearClassTotals = new Dictionary<int, (string Name, decimal Amount)>();

        for (var month = 1; month <= 12; month++)
        {
            var batch = PaymentMonthBatch.Load(0, month, year);
            monthlyAmounts.Add(new MonthlyAmountStat
            {
                Month = month,
                MonthName = GetMonthName(month),
                Amount = batch.Summary.TotalAttendanceDue
            });

            var monthClassDetail = new List<ClassTuitionDetailStat>();
            foreach (var (classId, className, amount) in batch.GetClassNetAttendanceTotals())
            {
                monthClassDetail.Add(new ClassTuitionDetailStat
                {
                    ClassId = classId,
                    ClassName = className,
                    TotalAmount = amount
                });

                if (!yearClassTotals.TryGetValue(classId, out var yearTotal))
                {
                    yearClassTotals[classId] = (className, amount);
                }
                else
                {
                    yearClassTotals[classId] = (yearTotal.Name, yearTotal.Amount + amount);
                }
            }

            classDetailByMonth[month] = monthClassDetail
                .Where(x => x.TotalAmount > 0)
                .OrderByDescending(x => x.TotalAmount)
                .ThenBy(x => x.ClassName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var yearClassDetail = yearClassTotals
            .Select(kv => new ClassTuitionDetailStat
            {
                ClassId = kv.Key,
                ClassName = kv.Value.Name,
                TotalAmount = kv.Value.Amount
            })
            .Where(x => x.TotalAmount > 0)
            .OrderByDescending(x => x.TotalAmount)
            .ThenBy(x => x.ClassName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TuitionYearReportData(monthlyAmounts, classDetailByMonth, yearClassDetail);
    }

    private static string GetMonthName(int month) => month switch
    {
        1 => "Tháng 1",
        2 => "Tháng 2",
        3 => "Tháng 3",
        4 => "Tháng 4",
        5 => "Tháng 5",
        6 => "Tháng 6",
        7 => "Tháng 7",
        8 => "Tháng 8",
        9 => "Tháng 9",
        10 => "Tháng 10",
        11 => "Tháng 11",
        12 => "Tháng 12",
        _ => $"Tháng {month}"
    };
}

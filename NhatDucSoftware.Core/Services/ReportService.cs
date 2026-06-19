using System.Data.Common;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class ReportService
{
    private readonly TeacherTimesheetService _timesheetService = new();
    private readonly TeacherService _teacherService = new();

    public ReportSummary GetSummary()
    {
        var result = new ReportSummary();

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var studentCmd = connection.CreateCommand();
        studentCmd.CommandText = "SELECT COUNT(1) FROM Students;";
        result.TotalStudents = Convert.ToInt32(studentCmd.ExecuteScalar());

        using var revenueCmd = connection.CreateCommand();
        revenueCmd.CommandText = "SELECT COALESCE(SUM(Amount), 0) FROM RevenueLedger;";
        result.TotalRevenue = Convert.ToDecimal(revenueCmd.ExecuteScalar());

        using var classCmd = connection.CreateCommand();
        classCmd.CommandText = "SELECT COUNT(1) FROM Classes WHERE Status = 'Active';";
        result.ActiveClasses = Convert.ToInt32(classCmd.ExecuteScalar());

        return result;
    }

    public List<RevenueByYearStat> GetRevenueByYear()
    {
        var result = new List<RevenueByYearStat>();

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT CAST(LEFT(PaymentDate::text, 4) AS INTEGER) AS RevenueYear,
       COALESCE(SUM(Amount), 0) AS TotalRevenue
FROM RevenueLedger
WHERE LENGTH(PaymentDate::text) >= 4
  AND LEFT(PaymentDate::text, 4) ~ '^\d{4}$'
GROUP BY LEFT(PaymentDate::text, 4)
ORDER BY RevenueYear DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new RevenueByYearStat
            {
                Year = Convert.ToInt32(reader.GetValue(0)),
                TotalRevenue = ReadDecimal(reader, 1)
            });
        }

        return result;
    }

    public List<RevenueByMonthStat> GetRevenueByMonth(int year)
    {
        var result = new List<RevenueByMonthStat>();

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT CAST(SUBSTRING(PaymentDate::text FROM 6 FOR 2) AS INTEGER) AS Month,
       COALESCE(SUM(Amount), 0) AS TotalRevenue
FROM RevenueLedger
WHERE LEFT(PaymentDate::text, 4) = @yearText
  AND LENGTH(PaymentDate::text) >= 7
  AND SUBSTRING(PaymentDate::text FROM 6 FOR 2) ~ '^\d{2}$'
GROUP BY SUBSTRING(PaymentDate::text FROM 6 FOR 2)
ORDER BY Month;";
        command.Parameters.AddWithValue("@yearText", year.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var month = Convert.ToInt32(reader.GetValue(0));
            result.Add(new RevenueByMonthStat
            {
                Month = month,
                MonthName = GetMonthName(month),
                TotalRevenue = ReadDecimal(reader, 1)
            });
        }

        for (int i = 1; i <= 12; i++)
        {
            if (!result.Any(x => x.Month == i))
            {
                result.Add(new RevenueByMonthStat
                {
                    Month = i,
                    MonthName = GetMonthName(i),
                    TotalRevenue = 0
                });
            }
        }

        return result.OrderBy(x => x.Month).ToList();
    }

    public List<MonthlyAmountStat> GetExpenseByMonth(int year)
    {
        var teachers = _teacherService.GetAll();
        var result = CreateEmptyMonthlyAmounts();

        foreach (var teacher in teachers)
        {
            for (var month = 1; month <= 12; month++)
            {
                result[month - 1].Amount += _timesheetService.CalculateMonthlyPay(teacher.Id, year, month);
            }
        }

        return result;
    }

    public List<TeacherExpenseDetailStat> GetTeacherExpenseDetail(int year, int? month = null)
    {
        var teachers = _teacherService.GetAll();
        var result = new List<TeacherExpenseDetailStat>();

        foreach (var teacher in teachers)
        {
            decimal total;
            if (month is >= 1 and <= 12)
            {
                total = _timesheetService.CalculateMonthlyPay(teacher.Id, year, month.Value);
            }
            else
            {
                total = 0;
                for (var m = 1; m <= 12; m++)
                {
                    total += _timesheetService.CalculateMonthlyPay(teacher.Id, year, m);
                }
            }

            if (total > 0)
            {
                result.Add(new TeacherExpenseDetailStat
                {
                    TeacherId = teacher.Id,
                    TeacherName = teacher.FullName,
                    TotalAmount = total
                });
            }
        }

        return result.OrderByDescending(x => x.TotalAmount).ToList();
    }

    public List<MonthlyAmountStat> GetTuitionEarnedByMonth(int year)
    {
        var result = CreateEmptyMonthlyAmounts();

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT CAST(EXTRACT(MONTH FROM CAST(ats.SessionDate AS date)) AS INTEGER) AS Month,
       COALESCE(SUM(co.TuitionFee), 0) AS TotalAmount
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
INNER JOIN Classes c ON c.Id = ats.ClassId
INNER JOIN Courses co ON co.Id = c.CourseId
WHERE ar.Status = 'C'
  AND EXTRACT(YEAR FROM CAST(ats.SessionDate AS date)) = @year::numeric
GROUP BY EXTRACT(MONTH FROM CAST(ats.SessionDate AS date))
ORDER BY Month;";
        command.Parameters.AddWithValue("@year", year);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var month = Convert.ToInt32(reader.GetValue(0));
            result[month - 1].Amount = ReadDecimal(reader, 1);
        }

        return result;
    }

    public List<ClassTuitionDetailStat> GetClassTuitionDetail(int year, int? month = null)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT c.Id,
       c.ClassName,
       COALESCE(SUM(co.TuitionFee), 0) AS TotalAmount
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
INNER JOIN Classes c ON c.Id = ats.ClassId
INNER JOIN Courses co ON co.Id = c.CourseId
WHERE ar.Status = 'C'
  AND EXTRACT(YEAR FROM CAST(ats.SessionDate AS date)) = @year::numeric
  AND (@month = 0 OR EXTRACT(MONTH FROM CAST(ats.SessionDate AS date)) = @month::numeric)
GROUP BY c.Id, c.ClassName
HAVING COALESCE(SUM(co.TuitionFee), 0) > 0
ORDER BY TotalAmount DESC, c.ClassName ASC;";
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@month", month ?? 0);

        var result = new List<ClassTuitionDetailStat>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ClassTuitionDetailStat
            {
                ClassId = Convert.ToInt32(reader.GetValue(0)),
                ClassName = reader.GetString(1),
                TotalAmount = ReadDecimal(reader, 2)
            });
        }

        return result;
    }

    public List<MonthlyEnrollmentStat> GetEnrollmentByMonth(int year)
    {
        var result = new List<MonthlyEnrollmentStat>();
        for (var month = 1; month <= 12; month++)
        {
            result.Add(new MonthlyEnrollmentStat
            {
                Month = month,
                MonthName = GetMonthName(month),
                StudentCount = GetStudentCountByMonthEnd(year, month),
                ClassCount = GetActiveClassCountByMonth(year, month)
            });
        }

        return result;
    }

    private static int GetStudentCountByMonthEnd(int year, int month)
    {
        var endDate = $"{year:D4}-{month:D2}-{DateTime.DaysInMonth(year, month):D2}";

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(DISTINCT s.Id)
FROM Students s
WHERE s.Status = 'Active'
  AND LEFT(s.CreatedAt::text, 10) <= @endDate;";
        command.Parameters.AddWithValue("@endDate", endDate);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static int GetActiveClassCountByMonth(int year, int month)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(DISTINCT ats.ClassId)
FROM AttendanceSessions ats
INNER JOIN Classes c ON c.Id = ats.ClassId
WHERE c.Status = 'Active'
  AND EXTRACT(YEAR FROM CAST(ats.SessionDate AS date)) = @year::numeric
  AND EXTRACT(MONTH FROM CAST(ats.SessionDate AS date)) = @month::numeric;";
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@month", month);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static List<MonthlyAmountStat> CreateEmptyMonthlyAmounts()
    {
        return Enumerable.Range(1, 12)
            .Select(month => new MonthlyAmountStat
            {
                Month = month,
                MonthName = GetMonthName(month),
                Amount = 0
            })
            .ToList();
    }

    private static decimal ReadDecimal(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static string GetMonthName(int month)
    {
        return month switch
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
}

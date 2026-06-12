using System.Data.Common;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class ReportService
{
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

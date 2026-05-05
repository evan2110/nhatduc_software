using NhatDucSoftware.Data;
using NhatDucSoftware.Models;

namespace NhatDucSoftware.Services;

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
        revenueCmd.CommandText = "SELECT IFNULL(SUM(Amount), 0) FROM RevenueLedger;";
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
SELECT CAST(strftime('%Y', PaymentDate) AS INTEGER) AS RevenueYear,
       IFNULL(SUM(Amount), 0) AS TotalRevenue
FROM RevenueLedger
GROUP BY strftime('%Y', PaymentDate)
ORDER BY RevenueYear DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new RevenueByYearStat
            {
                Year = reader.GetInt32(0),
                TotalRevenue = Convert.ToDecimal(reader.GetDouble(1))
            });
        }

        return result;
    }
}

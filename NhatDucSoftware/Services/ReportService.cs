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
        revenueCmd.CommandText = "SELECT IFNULL(SUM(Amount), 0) FROM Payments;";
        result.TotalRevenue = Convert.ToDecimal(revenueCmd.ExecuteScalar());

        using var classCmd = connection.CreateCommand();
        classCmd.CommandText = "SELECT COUNT(1) FROM Classes WHERE Status = 'Active';";
        result.ActiveClasses = Convert.ToInt32(classCmd.ExecuteScalar());

        return result;
    }
}

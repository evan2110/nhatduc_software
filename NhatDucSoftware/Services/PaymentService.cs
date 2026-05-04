using NhatDucSoftware.Data;

namespace NhatDucSoftware.Services;

public class PaymentService
{
    public decimal GetTotalTuitionByStudent(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT IFNULL(SUM(c.TuitionFee), 0)
FROM StudentCourses sc
INNER JOIN Courses c ON c.Id = sc.CourseId
WHERE sc.StudentId = @studentId AND sc.Status = 'Active';";
        command.Parameters.AddWithValue("@studentId", studentId);

        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public decimal GetPaidAmount(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT IFNULL(SUM(Amount), 0) FROM Payments WHERE StudentId = @studentId;";
        command.Parameters.AddWithValue("@studentId", studentId);
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public void Collect(int studentId, decimal amount, int createdBy, string? note)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Payments(StudentId, ClassId, Amount, PaymentDate, Note, CreatedBy)
VALUES(@studentId, NULL, @amount, @date, @note, @createdBy);";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@amount", amount);
        command.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("@note", (object?)note ?? DBNull.Value);
        command.Parameters.AddWithValue("@createdBy", createdBy);
        command.ExecuteNonQuery();
    }
}

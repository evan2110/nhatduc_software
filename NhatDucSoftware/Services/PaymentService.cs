using NhatDucSoftware.Data;

namespace NhatDucSoftware.Services;

public class PaymentService
{
    public decimal GetTotalTuitionByStudent(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        // Tính: số buổi có mặt (C) của từng lớp × học phí khóa học tương ứng
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT IFNULL(SUM(attended * co.TuitionFee), 0)
FROM (
    SELECT cs.ClassId,
           (SELECT COUNT(*) FROM AttendanceRecords ar
            INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
            WHERE ar.StudentId = @studentId AND ats.ClassId = cs.ClassId AND ar.Status = 'C') AS attended
    FROM ClassStudents cs
    WHERE cs.StudentId = @studentId
) sub
INNER JOIN Classes c ON c.Id = sub.ClassId
INNER JOIN Courses co ON co.Id = c.CourseId;";
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

    /// <summary>
    /// Lấy thông tin điểm danh của học viên: tổng buổi, có mặt, vắng.
    /// </summary>
    public (int TotalSessions, int Attended, int Absent) GetAttendanceSummary(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT 
    COUNT(*) AS Total,
    SUM(CASE WHEN Status = 'C' THEN 1 ELSE 0 END) AS Attended,
    SUM(CASE WHEN Status = 'V' THEN 1 ELSE 0 END) AS Absent
FROM AttendanceRecords
WHERE StudentId = @studentId;";
        command.Parameters.AddWithValue("@studentId", studentId);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
        }
        return (0, 0, 0);
    }

    /// <summary>
    /// Lấy chi tiết các buổi học của học viên: ngày, lớp, trạng thái.
    /// </summary>
    public List<AttendanceDetailRow> GetAttendanceDetails(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ats.SessionDate, c.ClassName, ar.Status
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
INNER JOIN Classes c ON c.Id = ats.ClassId
WHERE ar.StudentId = @studentId
ORDER BY ats.SessionDate DESC;";
        command.Parameters.AddWithValue("@studentId", studentId);

        var results = new List<AttendanceDetailRow>();
        using var reader2 = command.ExecuteReader();
        while (reader2.Read())
        {
            results.Add(new AttendanceDetailRow
            {
                Ngay = reader2.GetString(0),
                Lop = reader2.GetString(1),
                TrangThai = reader2.GetString(2) == "C" ? "Có mặt" : "Vắng"
            });
        }
        return results;
    }
}

public class AttendanceDetailRow
{
    public string Ngay { get; set; } = "";
    public string Lop { get; set; } = "";
    public string TrangThai { get; set; } = "";
}

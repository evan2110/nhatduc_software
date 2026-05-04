using NhatDucSoftware.Data;

namespace NhatDucSoftware.Services;

public class AttendanceService
{
    public List<(int StudentId, string FullName)> GetStudentsByClass(int classId)
    {
        var result = new List<(int, string)>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT s.Id, s.FullName
FROM ClassStudents cs
INNER JOIN Students s ON s.Id = cs.StudentId
WHERE cs.ClassId = @classId
ORDER BY s.FullName;";
        command.Parameters.AddWithValue("@classId", classId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add((reader.GetInt32(0), reader.GetString(1)));
        }

        return result;
    }

    public void SaveAttendance(int classId, int teacherId, DateTime sessionDate, Dictionary<int, string> records)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        using var sessionCmd = connection.CreateCommand();
        sessionCmd.Transaction = transaction;
        sessionCmd.CommandText = @"INSERT INTO AttendanceSessions(ClassId, SessionDate, CreatedByTeacherId)
VALUES(@classId, @date, @teacherId);";
        sessionCmd.Parameters.AddWithValue("@classId", classId);
        sessionCmd.Parameters.AddWithValue("@date", sessionDate.ToString("yyyy-MM-dd"));
        sessionCmd.Parameters.AddWithValue("@teacherId", teacherId);
        sessionCmd.ExecuteNonQuery();

        long sessionId;
        using (var idCmd = connection.CreateCommand())
        {
            idCmd.Transaction = transaction;
            idCmd.CommandText = "SELECT last_insert_rowid();";
            sessionId = Convert.ToInt64(idCmd.ExecuteScalar());
        }

        foreach (var (studentId, status) in records)
        {
            using var recordCmd = connection.CreateCommand();
            recordCmd.Transaction = transaction;
            recordCmd.CommandText = @"INSERT INTO AttendanceRecords(SessionId, StudentId, Status)
VALUES(@sessionId, @studentId, @status);";
            recordCmd.Parameters.AddWithValue("@sessionId", sessionId);
            recordCmd.Parameters.AddWithValue("@studentId", studentId);
            recordCmd.Parameters.AddWithValue("@status", status);
            recordCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}

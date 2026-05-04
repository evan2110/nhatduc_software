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

    public Dictionary<int, string> GetAttendanceByClassAndDate(int classId, DateTime sessionDate)
    {
        var result = new Dictionary<int, string>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ar.StudentId, ar.Status
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
WHERE ats.Id = (
    SELECT Id
    FROM AttendanceSessions
    WHERE ClassId = @classId AND SessionDate = @date
    ORDER BY Id DESC
    LIMIT 1
);";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@date", sessionDate.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[reader.GetInt32(0)] = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        }

        return result;
    }

    public void SaveAttendance(int classId, int teacherId, DateTime sessionDate, Dictionary<int, string> records)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        long sessionId;
        using (var findSessionCmd = connection.CreateCommand())
        {
            findSessionCmd.Transaction = transaction;
            findSessionCmd.CommandText = @"
SELECT Id
FROM AttendanceSessions
WHERE ClassId = @classId AND SessionDate = @date
ORDER BY Id DESC
LIMIT 1;";
            findSessionCmd.Parameters.AddWithValue("@classId", classId);
            findSessionCmd.Parameters.AddWithValue("@date", sessionDate.ToString("yyyy-MM-dd"));

            var existingSessionId = findSessionCmd.ExecuteScalar();
            if (existingSessionId is not null && existingSessionId != DBNull.Value)
            {
                sessionId = Convert.ToInt64(existingSessionId);

                // Delete older duplicate sessions for same class+date
                using var deleteOldCmd = connection.CreateCommand();
                deleteOldCmd.Transaction = transaction;
                deleteOldCmd.CommandText = @"DELETE FROM AttendanceRecords WHERE SessionId IN (
    SELECT Id FROM AttendanceSessions WHERE ClassId = @classId AND SessionDate = @date AND Id <> @keepId
);
DELETE FROM AttendanceSessions WHERE ClassId = @classId AND SessionDate = @date AND Id <> @keepId;";
                deleteOldCmd.Parameters.AddWithValue("@classId", classId);
                deleteOldCmd.Parameters.AddWithValue("@date", sessionDate.ToString("yyyy-MM-dd"));
                deleteOldCmd.Parameters.AddWithValue("@keepId", sessionId);
                deleteOldCmd.ExecuteNonQuery();
            }
            else
            {
                using var sessionCmd = connection.CreateCommand();
                sessionCmd.Transaction = transaction;
                sessionCmd.CommandText = @"INSERT INTO AttendanceSessions(ClassId, SessionDate, CreatedByTeacherId)
VALUES(@classId, @date, @teacherId);";
                sessionCmd.Parameters.AddWithValue("@classId", classId);
                sessionCmd.Parameters.AddWithValue("@date", sessionDate.ToString("yyyy-MM-dd"));
                sessionCmd.Parameters.AddWithValue("@teacherId", teacherId);
                sessionCmd.ExecuteNonQuery();

                using var idCmd = connection.CreateCommand();
                idCmd.Transaction = transaction;
                idCmd.CommandText = "SELECT last_insert_rowid();";
                sessionId = Convert.ToInt64(idCmd.ExecuteScalar());
            }
        }

        foreach (var (studentId, status) in records)
        {
            using var recordCmd = connection.CreateCommand();
            recordCmd.Transaction = transaction;
            recordCmd.CommandText = @"INSERT INTO AttendanceRecords(SessionId, StudentId, Status)
VALUES(@sessionId, @studentId, @status)
ON CONFLICT(SessionId, StudentId) DO UPDATE SET Status = excluded.Status;";
            recordCmd.Parameters.AddWithValue("@sessionId", sessionId);
            recordCmd.Parameters.AddWithValue("@studentId", studentId);
            recordCmd.Parameters.AddWithValue("@status", (status ?? string.Empty).Trim());
            recordCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}

using NhatDucSoftware.Core.Data;

namespace NhatDucSoftware.Core.Services;

public class AttendanceService
{
    private readonly ClassScheduleService _scheduleService = new();

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

    public List<int> GetTeacherShiftsForClassOnDate(int classId, int teacherId, DateTime sessionDate)
    {
        return _scheduleService.GetTeacherShiftsForClassOnDate(classId, teacherId, sessionDate);
    }

    public List<int> GetShiftsForClassOnDate(int classId, DateTime sessionDate)
    {
        return _scheduleService.GetShiftsForClassOnDate(classId, sessionDate);
    }

    public Dictionary<int, string> GetAttendanceByClassDateAndShift(int classId, DateTime sessionDate, int shiftNumber)
    {
        var result = new Dictionary<int, string>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ar.StudentId, ar.Status
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
WHERE ats.ClassId = @classId
  AND ats.SessionDate = @date
  AND ats.ShiftNumber = @shift;";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@date", sessionDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@shift", shiftNumber);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[reader.GetInt32(0)] = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        }

        return result;
    }

    public (int TotalStudents, int RecordedStudents, bool IsComplete) GetAttendanceCompletionStatus(
        int classId, DateTime sessionDate, int shiftNumber)
    {
        var students = GetStudentsByClass(classId);
        if (students.Count == 0)
        {
            return (0, 0, true);
        }

        var attendance = GetAttendanceByClassDateAndShift(classId, sessionDate, shiftNumber);
        var recorded = students.Count(s =>
        {
            if (!attendance.TryGetValue(s.StudentId, out var status))
            {
                return false;
            }

            var normalized = status.Trim().ToUpperInvariant();
            return normalized is "C" or "V";
        });

        return (students.Count, recorded, recorded == students.Count);
    }

    public void SaveAttendance(int classId, int teacherId, DateTime sessionDate, int shiftNumber, Dictionary<int, string> records)
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
WHERE ClassId = @classId AND SessionDate = @date AND ShiftNumber = @shift
LIMIT 1;";
            findSessionCmd.Parameters.AddWithValue("@classId", classId);
            findSessionCmd.Parameters.AddWithValue("@date", sessionDate.ToString("yyyy-MM-dd"));
            findSessionCmd.Parameters.AddWithValue("@shift", shiftNumber);

            var existingSessionId = findSessionCmd.ExecuteScalar();
            if (existingSessionId is not null && existingSessionId != DBNull.Value)
            {
                sessionId = Convert.ToInt64(existingSessionId);
            }
            else
            {
                using var sessionCmd = connection.CreateCommand();
                sessionCmd.Transaction = transaction;
                sessionCmd.CommandText = @"INSERT INTO AttendanceSessions(ClassId, SessionDate, ShiftNumber, CreatedByTeacherId)
VALUES(@classId, @date, @shift, @teacherId)
RETURNING Id;";
                sessionCmd.Parameters.AddWithValue("@classId", classId);
                sessionCmd.Parameters.AddWithValue("@date", sessionDate.ToString("yyyy-MM-dd"));
                sessionCmd.Parameters.AddWithValue("@shift", shiftNumber);
                sessionCmd.Parameters.AddWithValue("@teacherId", teacherId);
                sessionId = Convert.ToInt64(sessionCmd.ExecuteScalar());
            }
        }

        foreach (var (studentId, status) in records)
        {
            using var recordCmd = connection.CreateCommand();
            recordCmd.Transaction = transaction;
            recordCmd.CommandText = @"INSERT INTO AttendanceRecords(SessionId, StudentId, Status)
VALUES(@sessionId, @studentId, @status)
ON CONFLICT(SessionId, StudentId) DO UPDATE SET Status = EXCLUDED.Status;";
            recordCmd.Parameters.AddWithValue("@sessionId", sessionId);
            recordCmd.Parameters.AddWithValue("@studentId", studentId);
            recordCmd.Parameters.AddWithValue("@status", (status ?? string.Empty).Trim());
            recordCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}

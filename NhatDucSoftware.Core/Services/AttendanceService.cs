using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class AttendanceService
{
    private readonly ClassScheduleService _scheduleService = new();

    public List<(int StudentId, string FullName)> GetStudentsByClass(int classId, DateTime? sessionDate = null)
    {
        var result = new List<(int, string)>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        var sql = @"
SELECT s.Id, s.FullName
FROM ClassStudents cs
INNER JOIN Students s ON s.Id = cs.StudentId
WHERE cs.ClassId = @classId";
        if (sessionDate.HasValue)
        {
            sql += @"
  AND DATE(CAST(cs.JoinedDate AS timestamp)) <= @sessionDate::date";
            command.Parameters.AddWithValue("@sessionDate", sessionDate.Value.Date.ToString("yyyy-MM-dd"));
        }

        sql += @"
ORDER BY s.FullName;";
        command.CommandText = sql;
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
        var students = GetStudentsByClass(classId, sessionDate);
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

    public List<AttendanceRow> GetAttendanceRowsForClass(int classId, DateTime sessionDate, int shiftNumber)
    {
        var saved = GetAttendanceByClassDateAndShift(classId, sessionDate, shiftNumber);
        return GetStudentsByClass(classId, sessionDate)
            .Select(x => new AttendanceRow
            {
                StudentId = x.StudentId,
                StudentName = x.FullName,
                Status = saved.TryGetValue(x.StudentId, out var status) ? status : ""
            })
            .ToList();
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

    public List<CenterAttendanceExportRow> GetAttendanceByDateRange(DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date.ToString("yyyy-MM-dd");
        var to = toDate.Date.ToString("yyyy-MM-dd");
        var result = new List<CenterAttendanceExportRow>();

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ats.SessionDate,
       c.ClassName,
       ats.ShiftNumber,
       s.FullName,
       ar.Status,
       COALESCE(t.FullName, '')
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
INNER JOIN Classes c ON c.Id = ats.ClassId
INNER JOIN Students s ON s.Id = ar.StudentId
LEFT JOIN Teachers t ON t.Id = c.TeacherId
WHERE ats.SessionDate >= @fromDate
  AND ats.SessionDate <= @toDate
ORDER BY ats.SessionDate, c.ClassName, ats.ShiftNumber, s.FullName;";
        command.Parameters.AddWithValue("@fromDate", from);
        command.Parameters.AddWithValue("@toDate", to);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var status = reader.IsDBNull(4) ? "" : reader.GetString(4);
            result.Add(new CenterAttendanceExportRow
            {
                SessionDate = DateTime.Parse(reader.GetString(0)),
                ClassName = reader.GetString(1),
                ShiftNumber = reader.GetInt32(2),
                StudentName = reader.GetString(3),
                Status = status.Equals("C", StringComparison.OrdinalIgnoreCase) ? "Có mặt"
                    : status.Equals("V", StringComparison.OrdinalIgnoreCase) ? "Vắng"
                    : status,
                TeacherName = reader.GetString(5)
            });
        }

        return result;
    }
}

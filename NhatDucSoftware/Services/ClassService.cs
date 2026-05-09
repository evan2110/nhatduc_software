using NhatDucSoftware.Data;
using NhatDucSoftware.Models;

namespace NhatDucSoftware.Services;

public class ClassService
{
    public List<ClassInfo> GetAll()
    {
        var result = new List<ClassInfo>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT c.Id,
       c.ClassName,
       co.Name,
       IFNULL(t.FullName, ''),
       (SELECT COUNT(1) FROM ClassStudents cs WHERE cs.ClassId = c.Id) AS CurrentSize,
       c.Status
FROM Classes c
INNER JOIN Courses co ON co.Id = c.CourseId
LEFT JOIN Teachers t ON t.Id = c.TeacherId
ORDER BY c.Id ASC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ClassInfo
            {
                Id = reader.GetInt32(0),
                ClassName = reader.GetString(1),
                CourseCode = reader.GetString(2),
                TeacherName = reader.GetString(3),
                CurrentSize = reader.GetInt32(4),
                Status = reader.GetString(5)
            });
        }

        return result;
    }

    public List<ClassInfo> GetClassesByTeacher(int teacherId)
    {
        var result = new List<ClassInfo>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT c.Id, c.ClassName, co.Name, IFNULL(t.FullName, ''),
       (SELECT COUNT(1) FROM ClassStudents cs WHERE cs.ClassId = c.Id),
       c.Status
FROM Classes c
INNER JOIN Courses co ON co.Id = c.CourseId
LEFT JOIN Teachers t ON t.Id = c.TeacherId
WHERE c.TeacherId = @teacherId
ORDER BY c.Id ASC;";
        command.Parameters.AddWithValue("@teacherId", teacherId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ClassInfo
            {
                Id = reader.GetInt32(0),
                ClassName = reader.GetString(1),
                CourseCode = reader.GetString(2),
                TeacherName = reader.GetString(3),
                CurrentSize = reader.GetInt32(4),
                Status = reader.GetString(5)
            });
        }

        return result;
    }

    private static int GetNextAvailableId(Microsoft.Data.Sqlite.SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            WITH RECURSIVE seq(n) AS (
                SELECT 1
                UNION ALL
                SELECT n + 1 FROM seq WHERE n < (SELECT COALESCE(MAX(Id), 0) + 1 FROM Classes)
            )
            SELECT MIN(n) FROM seq WHERE n NOT IN (SELECT Id FROM Classes);";
        var result = cmd.ExecuteScalar();
        return result is long l ? (int)l : 1;
    }

    public void AddClass(string className, int courseId, int? teacherId, string status)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        var nextId = GetNextAvailableId(connection);

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Classes(Id, ClassName, CourseId, TeacherId, MaxSize, Status)
VALUES(@id, @name, @courseId, @teacherId, 999, @status);";
        command.Parameters.AddWithValue("@id", nextId);
        command.Parameters.AddWithValue("@name", className);
        command.Parameters.AddWithValue("@courseId", courseId);
        command.Parameters.AddWithValue("@teacherId", (object?)teacherId ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", status);
        command.ExecuteNonQuery();
    }

    public void UpdateClass(int classId, string className, int courseId, int? teacherId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE Classes SET ClassName = @name, CourseId = @courseId, TeacherId = @teacherId WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", classId);
        command.Parameters.AddWithValue("@name", className);
        command.Parameters.AddWithValue("@courseId", courseId);
        command.Parameters.AddWithValue("@teacherId", (object?)teacherId ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public void DeleteClass(int classId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using (var clearAttendanceRecords = connection.CreateCommand())
        {
            clearAttendanceRecords.Transaction = transaction;
            clearAttendanceRecords.CommandText = @"
DELETE FROM AttendanceRecords
WHERE SessionId IN (SELECT Id FROM AttendanceSessions WHERE ClassId = @id);";
            clearAttendanceRecords.Parameters.AddWithValue("@id", classId);
            clearAttendanceRecords.ExecuteNonQuery();
        }

        using (var clearAttendanceSessions = connection.CreateCommand())
        {
            clearAttendanceSessions.Transaction = transaction;
            clearAttendanceSessions.CommandText = "DELETE FROM AttendanceSessions WHERE ClassId = @id;";
            clearAttendanceSessions.Parameters.AddWithValue("@id", classId);
            clearAttendanceSessions.ExecuteNonQuery();
        }

        using (var clearEvaluations = connection.CreateCommand())
        {
            clearEvaluations.Transaction = transaction;
            clearEvaluations.CommandText = "DELETE FROM StudentEvaluations WHERE ClassId = @id;";
            clearEvaluations.Parameters.AddWithValue("@id", classId);
            clearEvaluations.ExecuteNonQuery();
        }

        using (var clearStudents = connection.CreateCommand())
        {
            clearStudents.Transaction = transaction;
            clearStudents.CommandText = "DELETE FROM ClassStudents WHERE ClassId = @id;";
            clearStudents.Parameters.AddWithValue("@id", classId);
            clearStudents.ExecuteNonQuery();
        }

        using (var clearSchedules = connection.CreateCommand())
        {
            clearSchedules.Transaction = transaction;
            clearSchedules.CommandText = "DELETE FROM ClassSchedules WHERE ClassId = @id;";
            clearSchedules.Parameters.AddWithValue("@id", classId);
            clearSchedules.ExecuteNonQuery();
        }

        using (var clearWeeklySchedules = connection.CreateCommand())
        {
            clearWeeklySchedules.Transaction = transaction;
            clearWeeklySchedules.CommandText = "DELETE FROM ClassWeeklySchedules WHERE ClassId = @id;";
            clearWeeklySchedules.Parameters.AddWithValue("@id", classId);
            clearWeeklySchedules.ExecuteNonQuery();
        }

        using (var detachPayments = connection.CreateCommand())
        {
            detachPayments.Transaction = transaction;
            detachPayments.CommandText = "UPDATE Payments SET ClassId = NULL WHERE ClassId = @id;";
            detachPayments.Parameters.AddWithValue("@id", classId);
            detachPayments.ExecuteNonQuery();
        }

        using (var clearFinalizations = connection.CreateCommand())
        {
            clearFinalizations.Transaction = transaction;
            clearFinalizations.CommandText = "DELETE FROM PaymentFinalizations WHERE ClassId = @id;";
            clearFinalizations.Parameters.AddWithValue("@id", classId);
            clearFinalizations.ExecuteNonQuery();
        }

        using (var clearCarryOvers = connection.CreateCommand())
        {
            clearCarryOvers.Transaction = transaction;
            clearCarryOvers.CommandText = "DELETE FROM PaymentCarryOvers WHERE ClassId = @id;";
            clearCarryOvers.Parameters.AddWithValue("@id", classId);
            clearCarryOvers.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Classes WHERE Id = @id;";
            command.Parameters.AddWithValue("@id", classId);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public List<Teacher> GetTeachers()
    {
        var result = new List<Teacher>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, FullName FROM Teachers WHERE Status = 'Active' ORDER BY FullName;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Teacher
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1)
            });
        }

        return result;
    }

    public void AddStudentToClass(int classId, int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO ClassStudents(ClassId, StudentId, JoinedDate)
VALUES(@classId, @studentId, @joinedDate);";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@joinedDate", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }

    public void RemoveStudentFromClass(int classId, int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ClassStudents WHERE ClassId = @classId AND StudentId = @studentId;";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@studentId", studentId);
        command.ExecuteNonQuery();
    }

    public List<(int StudentId, string FullName)> GetStudentsInClass(int classId)
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
ORDER BY s.Id ASC;";
        command.Parameters.AddWithValue("@classId", classId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add((reader.GetInt32(0), reader.GetString(1)));
        }

        return result;
    }

    public int? GetTeacherIdByClass(int classId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT TeacherId FROM Classes WHERE Id = @classId LIMIT 1;";
        command.Parameters.AddWithValue("@classId", classId);

        var value = command.ExecuteScalar();
        if (value is null || value == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt32(value);
    }
}

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
       c.CourseId,
       co.Code,
       c.TeacherId,
       IFNULL(t.FullName, ''),
       c.MaxSize,
       (SELECT COUNT(1) FROM ClassStudents cs WHERE cs.ClassId = c.Id) AS CurrentSize,
       c.Status,
       IFNULL((SELECT GROUP_CONCAT(DayOfWeek || ' ' || StartTime || '-' || EndTime, '; ')
               FROM ClassSchedules s WHERE s.ClassId = c.Id), '') AS ScheduleText
FROM Classes c
INNER JOIN Courses co ON co.Id = c.CourseId
LEFT JOIN Teachers t ON t.Id = c.TeacherId
ORDER BY c.Id DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ClassInfo
            {
                Id = reader.GetInt32(0),
                ClassName = reader.GetString(1),
                CourseId = reader.GetInt32(2),
                CourseCode = reader.GetString(3),
                TeacherId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                TeacherName = reader.GetString(5),
                MaxSize = reader.GetInt32(6),
                CurrentSize = reader.GetInt32(7),
                Status = reader.GetString(8),
                ScheduleText = reader.GetString(9)
            });
        }

        return result;
    }

    public void AddClass(string className, int courseId, int? teacherId, int maxSize, string status)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Classes(ClassName, CourseId, TeacherId, MaxSize, Status)
VALUES(@name, @courseId, @teacherId, @maxSize, @status);";
        command.Parameters.AddWithValue("@name", className);
        command.Parameters.AddWithValue("@courseId", courseId);
        command.Parameters.AddWithValue("@teacherId", (object?)teacherId ?? DBNull.Value);
        command.Parameters.AddWithValue("@maxSize", maxSize);
        command.Parameters.AddWithValue("@status", status);
        command.ExecuteNonQuery();
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

        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = @"
SELECT (SELECT MaxSize FROM Classes WHERE Id = @classId) AS MaxSize,
       (SELECT COUNT(1) FROM ClassStudents WHERE ClassId = @classId) AS CurrentSize;";
        checkCmd.Parameters.AddWithValue("@classId", classId);

        using var reader = checkCmd.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("Không tìm thấy lớp.");
        }

        var maxSize = reader.GetInt32(0);
        var currentSize = reader.GetInt32(1);
        if (currentSize >= maxSize)
        {
            throw new InvalidOperationException("Lớp đã đủ sĩ số tối đa.");
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO ClassStudents(ClassId, StudentId, JoinedDate)
VALUES(@classId, @studentId, @joinedDate);";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@joinedDate", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }
}

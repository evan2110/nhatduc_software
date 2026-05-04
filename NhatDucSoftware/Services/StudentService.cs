using Microsoft.Data.Sqlite;
using NhatDucSoftware.Data;
using NhatDucSoftware.Models;

namespace NhatDucSoftware.Services;

public class StudentService
{
    public List<Student> GetAll()
    {
        var result = new List<Student>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, FullName, Phone, Email, Status FROM Students ORDER BY Id DESC;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Student
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Phone = reader.GetString(2),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                Status = reader.GetString(4)
            });
        }

        return result;
    }

    public void Add(Student student)
    {
        if (PhoneExists(student.Phone))
        {
            throw new InvalidOperationException("Số điện thoại đã tồn tại.");
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Students(FullName, Phone, Email, Language, Status, CreatedAt)
VALUES(@name, @phone, @mail, '', @status, @createdAt);";
        command.Parameters.AddWithValue("@name", student.FullName);
        command.Parameters.AddWithValue("@phone", student.Phone);
        command.Parameters.AddWithValue("@mail", (object?)student.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", student.Status);
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }

    public void Update(Student student)
    {
        if (PhoneExists(student.Phone, student.Id))
        {
            throw new InvalidOperationException("Số điện thoại đã tồn tại.");
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE Students
SET FullName = @name, Phone = @phone, Email = @mail, Status = @status
WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", student.Id);
        command.Parameters.AddWithValue("@name", student.FullName);
        command.Parameters.AddWithValue("@phone", student.Phone);
        command.Parameters.AddWithValue("@mail", (object?)student.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", student.Status);
        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Students WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    public void AssignCourse(int studentId, int courseId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(1) FROM StudentCourses WHERE StudentId = @studentId AND CourseId = @courseId AND Status = 'Active';";
        checkCmd.Parameters.AddWithValue("@studentId", studentId);
        checkCmd.Parameters.AddWithValue("@courseId", courseId);

        if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO StudentCourses(StudentId, CourseId, EnrollDate, Status)
VALUES(@studentId, @courseId, @date, 'Active');";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@courseId", courseId);
        command.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }

    private bool PhoneExists(string phone, int? ignoreId = null)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = ignoreId.HasValue
            ? "SELECT COUNT(1) FROM Students WHERE Phone = @phone AND Id <> @id;"
            : "SELECT COUNT(1) FROM Students WHERE Phone = @phone;";
        command.Parameters.AddWithValue("@phone", phone);
        if (ignoreId.HasValue)
        {
            command.Parameters.AddWithValue("@id", ignoreId.Value);
        }

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}

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
        command.CommandText = "SELECT Id, FullName, Phone, Email, BirthYear, Address, Status FROM Students ORDER BY Id ASC;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Student
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Phone = reader.GetString(2),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                BirthYear = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Address = reader.IsDBNull(5) ? null : reader.GetString(5),
                Status = reader.GetString(6)
            });
        }

        return result;
    }

    public void Add(Student student)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Students(FullName, Phone, Email, BirthYear, Address, Language, Status, CreatedAt)
VALUES(@name, @phone, @mail, @birthYear, @address, '', @status, @createdAt);";
        command.Parameters.AddWithValue("@name", student.FullName);
        command.Parameters.AddWithValue("@phone", student.Phone);
        command.Parameters.AddWithValue("@mail", (object?)student.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@birthYear", (object?)student.BirthYear ?? DBNull.Value);
        command.Parameters.AddWithValue("@address", (object?)student.Address ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", student.Status);
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }

    public void Update(Student student)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE Students
SET FullName = @name, Phone = @phone, Email = @mail, BirthYear = @birthYear, Address = @address, Status = @status
WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", student.Id);
        command.Parameters.AddWithValue("@name", student.FullName);
        command.Parameters.AddWithValue("@phone", student.Phone);
        command.Parameters.AddWithValue("@mail", (object?)student.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@birthYear", (object?)student.BirthYear ?? DBNull.Value);
        command.Parameters.AddWithValue("@address", (object?)student.Address ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", student.Status);
        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        void Exec(string sql)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        Exec("DELETE FROM StudentEvaluations WHERE StudentId = @id;");
        Exec("DELETE FROM AttendanceRecords WHERE StudentId = @id;");
        Exec("DELETE FROM Payments WHERE StudentId = @id;");
        Exec("DELETE FROM ClassStudents WHERE StudentId = @id;");
        Exec("DELETE FROM StudentCourses WHERE StudentId = @id;");
        Exec("DELETE FROM Students WHERE Id = @id;");

        transaction.Commit();
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
}

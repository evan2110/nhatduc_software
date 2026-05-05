using NhatDucSoftware.Data;
using NhatDucSoftware.Models;

namespace NhatDucSoftware.Services;

public class CourseService
{
    public List<Course> GetAll()
    {
        var result = new List<Course>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, TuitionFee, Status FROM Courses ORDER BY Id ASC;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Course
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                TuitionFee = Convert.ToDecimal(reader.GetDouble(2)),
                Status = reader.GetString(3)
            });
        }

        return result;
    }

    public void Add(Course course)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(1) FROM Courses WHERE Code = @code;";
        checkCmd.Parameters.AddWithValue("@code", course.Name);
        if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
        {
            throw new InvalidOperationException($"Khóa học với mã '{course.Name}' đã tồn tại.");
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Courses(Code, Name, Language, TuitionFee, DurationHours, Status)
VALUES(@code, @name, '', @fee, 90, @status);";
        command.Parameters.AddWithValue("@code", course.Name);
        command.Parameters.AddWithValue("@name", course.Name);
        command.Parameters.AddWithValue("@fee", course.TuitionFee);
        command.Parameters.AddWithValue("@status", course.Status);
        command.ExecuteNonQuery();
    }

    public void Update(Course course)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(1) FROM Courses WHERE Code = @code AND Id <> @id;";
        checkCmd.Parameters.AddWithValue("@code", course.Name);
        checkCmd.Parameters.AddWithValue("@id", course.Id);
        if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
        {
            throw new InvalidOperationException($"Khóa học với mã '{course.Name}' đã tồn tại.");
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE Courses SET Name = @name, Code = @code, TuitionFee = @fee, Status = @status WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", course.Id);
        command.Parameters.AddWithValue("@code", course.Name);
        command.Parameters.AddWithValue("@name", course.Name);
        command.Parameters.AddWithValue("@fee", course.TuitionFee);
        command.Parameters.AddWithValue("@status", course.Status);
        command.ExecuteNonQuery();
    }
}

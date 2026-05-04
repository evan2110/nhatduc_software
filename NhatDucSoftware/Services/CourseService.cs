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
        command.CommandText = "SELECT Id, Code, Name, Language, TuitionFee, DurationHours, Status FROM Courses ORDER BY Code;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Course
            {
                Id = reader.GetInt32(0),
                Code = reader.GetString(1),
                Name = reader.GetString(2),
                Language = reader.GetString(3),
                TuitionFee = Convert.ToDecimal(reader.GetDouble(4)),
                DurationHours = reader.GetInt32(5),
                Status = reader.GetString(6)
            });
        }

        return result;
    }

    public void Add(Course course)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Courses(Code, Name, Language, TuitionFee, DurationHours, Status)
VALUES(@code, @name, @lang, @fee, @duration, @status);";
        command.Parameters.AddWithValue("@code", course.Code);
        command.Parameters.AddWithValue("@name", course.Name);
        command.Parameters.AddWithValue("@lang", course.Language);
        command.Parameters.AddWithValue("@fee", course.TuitionFee);
        command.Parameters.AddWithValue("@duration", course.DurationHours);
        command.Parameters.AddWithValue("@status", course.Status);
        command.ExecuteNonQuery();
    }
}

using System.Data;
using Npgsql;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class CourseService
{
    private static readonly DateTime BaseFeeEffectiveFrom = new(1900, 1, 1);

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
        ValidateCourse(course);

        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.Transaction = transaction;
            checkCmd.CommandText = "SELECT COUNT(1) FROM Courses WHERE Code = @code;";
            checkCmd.Parameters.AddWithValue("@code", course.Name);
            if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
            {
                throw new InvalidOperationException($"Kh�a h?c v?i m? '{course.Name}' �? t?n t?i.");
            }
        }

        int courseId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO Courses(Code, Name, Language, TuitionFee, DurationHours, Status)
VALUES(@code, @name, '', @fee, 90, @status)
RETURNING Id;";
            command.Parameters.AddWithValue("@code", course.Name);
            command.Parameters.AddWithValue("@name", course.Name);
            command.Parameters.AddWithValue("@fee", course.TuitionFee);
            command.Parameters.AddWithValue("@status", course.Status);
            courseId = Convert.ToInt32(command.ExecuteScalar());
        }

        InsertFeeHistory(connection, transaction, courseId, course.TuitionFee, BaseFeeEffectiveFrom);
        transaction.Commit();
    }

    public void Update(Course course, DateTime? feeEffectiveFrom = null)
    {
        ValidateCourse(course);

        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        decimal currentFee;
        using (var currentCmd = connection.CreateCommand())
        {
            currentCmd.Transaction = transaction;
            currentCmd.CommandText = "SELECT TuitionFee FROM Courses WHERE Id = @id;";
            currentCmd.Parameters.AddWithValue("@id", course.Id);
            var current = currentCmd.ExecuteScalar();
            if (current is null || current is DBNull)
            {
                throw new InvalidOperationException("Không tìm thấy khóa học.");
            }

            currentFee = Convert.ToDecimal(current);
        }

        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.Transaction = transaction;
            checkCmd.CommandText = "SELECT COUNT(1) FROM Courses WHERE Code = @code AND Id <> @id;";
            checkCmd.Parameters.AddWithValue("@code", course.Name);
            checkCmd.Parameters.AddWithValue("@id", course.Id);
            if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
            {
                throw new InvalidOperationException($"Kh�a h?c v?i m? '{course.Name}' �? t?n t?i.");
            }
        }

        var feeChanged = currentFee != course.TuitionFee;
        var listedFee = currentFee;
        if (feeChanged)
        {
            if (feeEffectiveFrom is null)
            {
                throw new InvalidOperationException("Hãy chọn ngày hiệu lực khi đổi học phí.");
            }

            var today = VietnamDate.Today;
            var effectiveFrom = feeEffectiveFrom.Value.Date;
            CourseFeeRules.EnsureEffectiveFromAllowed(effectiveFrom, today);
            EnsureMonthNotFinalized(connection, transaction, course.Id, effectiveFrom);
            InsertFeeHistory(connection, transaction, course.Id, course.TuitionFee, effectiveFrom);
            if (effectiveFrom <= today)
            {
                listedFee = course.TuitionFee;
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"UPDATE Courses SET Name = @name, Code = @code, TuitionFee = @fee, Status = @status WHERE Id = @id;";
            command.Parameters.AddWithValue("@id", course.Id);
            command.Parameters.AddWithValue("@code", course.Name);
            command.Parameters.AddWithValue("@name", course.Name);
            command.Parameters.AddWithValue("@fee", listedFee);
            command.Parameters.AddWithValue("@status", course.Status);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static void InsertFeeHistory(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int courseId,
        decimal tuitionFee,
        DateTime effectiveFrom)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
INSERT INTO CourseFeeHistory(CourseId, TuitionFee, EffectiveFrom, CreatedAt)
VALUES(@courseId, @fee, @from, @createdAt)
ON CONFLICT(CourseId, EffectiveFrom)
DO UPDATE SET TuitionFee = EXCLUDED.TuitionFee;";
        command.Parameters.AddWithValue("@courseId", courseId);
        command.Parameters.AddWithValue("@fee", tuitionFee);
        command.Parameters.AddWithValue("@from", effectiveFrom.Date);
        command.Parameters["@from"].DbType = DbType.Date;
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }

    private static void EnsureMonthNotFinalized(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int courseId,
        DateTime effectiveFrom)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
SELECT COUNT(*)
FROM PaymentFinalizations pf
INNER JOIN Classes cl ON cl.Id = pf.ClassId
WHERE cl.CourseId = @courseId
  AND pf.Month = @month
  AND pf.Year = @year;";
        command.Parameters.AddWithValue("@courseId", courseId);
        command.Parameters.AddWithValue("@month", effectiveFrom.Month);
        command.Parameters.AddWithValue("@year", effectiveFrom.Year);
        if (Convert.ToInt32(command.ExecuteScalar()) > 0)
        {
            throw new InvalidOperationException(
                $"Không thể áp dụng giá mới cho tháng {effectiveFrom:MM/yyyy} vì đã có lớp thuộc khóa này được chốt số liệu.");
        }
    }

    private static void ValidateCourse(Course course)
    {
        if (string.IsNullOrWhiteSpace(course.Name))
        {
            throw new InvalidOperationException("T�n kh�a h?c kh�ng ???c ?? tr?ng.");
        }
    }
}

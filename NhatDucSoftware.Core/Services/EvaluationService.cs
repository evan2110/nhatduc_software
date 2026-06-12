using System.Data.Common;
using NhatDucSoftware.Core.Data;

namespace NhatDucSoftware.Core.Services;

public class EvaluationService
{
    public void Save(int studentId, int classId, int teacherId, decimal? score, string? comment)
    {
        Save(studentId, classId, teacherId, score, comment, DateTime.UtcNow.Year, DateTime.UtcNow.Month);
    }

    public void Save(int studentId, int classId, int teacherId, decimal? score, string? comment, int year, int month)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO StudentEvaluations(StudentId, ClassId, TeacherId, Score, Comment, CreatedAt)
VALUES(@studentId, @classId, @teacherId, @score, @comment, @createdAt);";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@score", (object?)score ?? DBNull.Value);
        command.Parameters.AddWithValue("@comment", (object?)comment ?? DBNull.Value);

        var createdAt = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        command.Parameters.AddWithValue("@createdAt", createdAt.ToString("o"));
        command.ExecuteNonQuery();
    }

    public List<StudentEvaluationRow> GetByStudent(int studentId)
    {
        var results = new List<StudentEvaluationRow>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COALESCE(c.ClassName, ''),
       COALESCE(t.FullName, ''),
       se.Score,
       se.Comment,
       se.CreatedAt
FROM StudentEvaluations se
LEFT JOIN Classes c ON c.Id = se.ClassId
LEFT JOIN Teachers t ON t.Id = se.TeacherId
WHERE se.StudentId = @studentId
ORDER BY se.CreatedAt DESC;";
        command.Parameters.AddWithValue("@studentId", studentId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapRow(reader));
        }

        return results;
    }

    public List<StudentEvaluationRow> GetByStudentInMonth(int studentId, int year, int month)
    {
        if (month is < 1 or > 12)
        {
            return new List<StudentEvaluationRow>();
        }

        var results = new List<StudentEvaluationRow>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COALESCE(c.ClassName, ''),
       COALESCE(t.FullName, ''),
       se.Score,
       se.Comment,
       se.CreatedAt
FROM StudentEvaluations se
LEFT JOIN Classes c ON c.Id = se.ClassId
LEFT JOIN Teachers t ON t.Id = se.TeacherId
WHERE se.StudentId = @studentId
  AND LEFT(se.CreatedAt::text, 7) = @yearMonth
ORDER BY se.CreatedAt DESC;";

        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@yearMonth", $"{year}-{month:D2}");

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapRow(reader));
        }

        return results;
    }

    private static StudentEvaluationRow MapRow(DbDataReader reader)
    {
        return new StudentEvaluationRow
        {
            Lop = ReadString(reader, 0),
            GiaoVien = ReadString(reader, 1),
            Diem = ReadScore(reader, 2),
            NhanXet = ReadString(reader, 3),
            Ngay = ReadDate(reader, 4)
        };
    }

    private static string ReadString(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return "";
        }

        return Convert.ToString(reader.GetValue(ordinal)) ?? "";
    }

    private static string ReadScore(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return "";
        }

        return reader.GetValue(ordinal) switch
        {
            decimal d => d.ToString("0.#"),
            double d => d.ToString("0.#"),
            float f => f.ToString("0.#"),
            int i => i.ToString(),
            long l => l.ToString(),
            _ => Convert.ToString(reader.GetValue(ordinal)) ?? ""
        };
    }

    private static string ReadDate(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return "";
        }

        var value = reader.GetValue(ordinal);
        if (value is DateTime dt)
        {
            return dt.ToLocalTime().ToString("dd/MM/yyyy");
        }

        var text = Convert.ToString(value) ?? "";
        if (DateTime.TryParse(text, out var parsed))
        {
            return parsed.ToLocalTime().ToString("dd/MM/yyyy");
        }

        return text.Length > 10 ? text[..10] : text;
    }
}

public class StudentEvaluationRow
{
    public string Lop { get; set; } = "";
    public string GiaoVien { get; set; } = "";
    public string Diem { get; set; } = "";
    public string NhanXet { get; set; } = "";
    public string Ngay { get; set; } = "";
}

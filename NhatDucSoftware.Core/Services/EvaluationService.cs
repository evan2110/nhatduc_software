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
SELECT c.ClassName, t.FullName, se.Score, se.Comment, se.CreatedAt
FROM StudentEvaluations se
INNER JOIN Classes c ON c.Id = se.ClassId
INNER JOIN Teachers t ON t.Id = se.TeacherId
WHERE se.StudentId = @studentId
ORDER BY se.CreatedAt DESC;";
        command.Parameters.AddWithValue("@studentId", studentId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new StudentEvaluationRow
            {
                Lop = reader.GetString(0),
                GiaoVien = reader.GetString(1),
                Diem = reader.IsDBNull(2) ? "" : reader.GetDecimal(2).ToString("0.#"),
                NhanXet = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Ngay = reader.IsDBNull(4) ? "" : reader.GetString(4).Length > 10 ? reader.GetString(4)[..10] : reader.GetString(4)
            });
        }

        return results;
    }

    public List<StudentEvaluationRow> GetByStudentInMonth(int studentId, int year, int month)
    {
        var results = new List<StudentEvaluationRow>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT c.ClassName, t.FullName, se.Score, se.Comment, se.CreatedAt
FROM StudentEvaluations se
INNER JOIN Classes c ON c.Id = se.ClassId
INNER JOIN Teachers t ON t.Id = se.TeacherId
WHERE se.StudentId = @studentId
  AND se.CreatedAt >= @fromDate
  AND se.CreatedAt < @toDate
ORDER BY se.CreatedAt DESC;";
        
        var fromDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = fromDate.AddMonths(1);

        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@fromDate", fromDate.ToString("o"));
        command.Parameters.AddWithValue("@toDate", toDate.ToString("o"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new StudentEvaluationRow
            {
                Lop = reader.GetString(0),
                GiaoVien = reader.GetString(1),
                Diem = reader.IsDBNull(2) ? "" : reader.GetDecimal(2).ToString("0.#"),
                NhanXet = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Ngay = reader.IsDBNull(4) ? "" : reader.GetString(4).Length > 10 ? reader.GetString(4)[..10] : reader.GetString(4)
            });
        }

        return results;
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

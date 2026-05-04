using NhatDucSoftware.Data;

namespace NhatDucSoftware.Services;

public class EvaluationService
{
    public void Save(int studentId, int classId, int teacherId, decimal? score, string? comment)
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
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }
}

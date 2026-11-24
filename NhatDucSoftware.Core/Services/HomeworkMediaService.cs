using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Services;

public sealed record HomeworkMediaFile(byte[] Data, string ContentType);

// Images placed inside a homework prompt. They are served by an unguessable token so the public homework page can show them.
public class HomeworkMediaService
{
    public string SaveImage(int teacherId, byte[] data)
    {
        if (!HomeworkMediaRules.TryAcceptImage(data, out var contentType, out var error))
        {
            throw new InvalidOperationException(error);
        }

        var token = HomeworkMediaRules.CreateToken();
        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO HomeworkMedia (Token, TeacherId, ContentType, Data, CreatedAt) VALUES (@token, @teacherId, @contentType, @data, @createdAt);";
        command.Parameters.AddWithValue("@token", token);
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@contentType", contentType);
        command.Parameters.AddWithValue("@data", data);
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
        return HomeworkMediaRules.PathFor(token);
    }

    public HomeworkMediaFile? Get(string? token)
    {
        if (!HomeworkMediaRules.IsToken(token))
        {
            return null;
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Data, ContentType FROM HomeworkMedia WHERE Token = @token;";
        command.Parameters.AddWithValue("@token", token!);
        using var reader = command.ExecuteReader();
        if (!reader.Read() || reader.IsDBNull(0))
        {
            return null;
        }

        return new HomeworkMediaFile((byte[])reader.GetValue(0), reader.GetString(1));
    }

    // Images the prompt shows, in order, for Gemini. Stops at maxCount.
    public static List<HomeworkMediaFile> LoadForPrompt(string? prompt, int maxCount)
    {
        var result = new List<HomeworkMediaFile>();
        var tokens = HomeworkMediaRules.TokensIn(prompt).Take(maxCount).ToList();
        if (tokens.Count == 0)
        {
            return result;
        }

        var service = new HomeworkMediaService();
        foreach (var token in tokens)
        {
            var file = service.Get(token);
            if (file is not null)
            {
                result.Add(file);
            }
        }

        return result;
    }

    public static void DeleteForPrompt(int teacherId, string? prompt)
    {
        var tokens = HomeworkMediaRules.TokensIn(prompt);
        if (tokens.Count == 0)
        {
            return;
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM HomeworkMedia WHERE TeacherId = @teacherId AND Token = ANY(@tokens);";
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@tokens", tokens.ToArray());
        command.ExecuteNonQuery();
    }
}

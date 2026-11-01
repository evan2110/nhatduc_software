using System.Globalization;
using NhatDucSoftware.Core.Data;

namespace NhatDucSoftware.Core.Services;

public static class ZaloChatSql
{
    public const string CreateTable = "CREATE TABLE IF NOT EXISTS ZaloChats (ChatId TEXT PRIMARY KEY, ChatType TEXT NOT NULL, UpdatedAt TEXT NOT NULL);";
    public const string Remember = "INSERT INTO ZaloChats(ChatId, ChatType, UpdatedAt) VALUES(@chatId, @chatType, @updatedAt) ON CONFLICT (ChatId) DO UPDATE SET ChatType = EXCLUDED.ChatType, UpdatedAt = EXCLUDED.UpdatedAt;";
    public const string AddClassColumn = "ALTER TABLE Classes ADD COLUMN IF NOT EXISTS ZaloChatId TEXT;";
    public const string LinkClass = "UPDATE Classes SET ZaloChatId = @chatId WHERE Id = @classId;";
    public const string UnlinkByChat = "UPDATE Classes SET ZaloChatId = NULL WHERE ZaloChatId = @chatId RETURNING COALESCE(ClassName, '');";
    public const string GetChatId = "SELECT ZaloChatId FROM Classes WHERE Id = @id;";
}

public sealed class ZaloChatService
{
    public void Remember(string chatId, string chatType)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = ZaloChatSql.Remember;
        command.Parameters.AddWithValue("@chatId", chatId);
        command.Parameters.AddWithValue("@chatType", chatType);
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }

    public void LinkClass(int classId, string chatId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = ZaloChatSql.LinkClass;
        command.Parameters.AddWithValue("@chatId", chatId);
        command.Parameters.AddWithValue("@classId", classId);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<string> UnlinkByChat(string chatId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = ZaloChatSql.UnlinkByChat;
        command.Parameters.AddWithValue("@chatId", chatId);
        var names = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (!reader.IsDBNull(0))
            {
                var name = reader.GetString(0).Trim();
                if (name.Length > 0)
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    public string? GetChatId(int classId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = ZaloChatSql.GetChatId;
        command.Parameters.AddWithValue("@id", classId);
        var value = command.ExecuteScalar();
        if (value is null || value == DBNull.Value)
        {
            return null;
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}

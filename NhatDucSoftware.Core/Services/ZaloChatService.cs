using NhatDucSoftware.Core.Data;

namespace NhatDucSoftware.Core.Services;

public static class ZaloChatSql
{
    public const string CreateTable = "CREATE TABLE IF NOT EXISTS ZaloChats (ChatId TEXT PRIMARY KEY, ChatType TEXT NOT NULL, UpdatedAt TEXT NOT NULL);";
    public const string Remember = "INSERT INTO ZaloChats(ChatId, ChatType, UpdatedAt) VALUES(@chatId, @chatType, @updatedAt) ON CONFLICT (ChatId) DO UPDATE SET ChatType = EXCLUDED.ChatType, UpdatedAt = EXCLUDED.UpdatedAt;";
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
}

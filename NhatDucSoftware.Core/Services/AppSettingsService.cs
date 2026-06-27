namespace NhatDucSoftware.Core.Services;

public static class AppSettingsService
{
    public static string? Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        using var connection = Data.DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT SettingValue
FROM AppSettings
WHERE SettingKey = @key
LIMIT 1;";
        command.Parameters.AddWithValue("@key", key.Trim());

        var value = command.ExecuteScalar();
        return value is null or DBNull ? null : Convert.ToString(value)?.Trim();
    }

    public static void Upsert(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required.", nameof(key));
        }

        using var connection = Data.DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO AppSettings (SettingKey, SettingValue, UpdatedAt)
VALUES (@key, @value, @updatedAt)
ON CONFLICT (SettingKey)
DO UPDATE SET SettingValue = EXCLUDED.SettingValue, UpdatedAt = EXCLUDED.UpdatedAt;";
        command.Parameters.AddWithValue("@key", key.Trim());
        command.Parameters.AddWithValue("@value", value);
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }
}

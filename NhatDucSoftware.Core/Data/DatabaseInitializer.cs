namespace NhatDucSoftware.Core.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using (var ping = connection.CreateCommand())
        {
            ping.CommandText = "SELECT 1;";
            ping.ExecuteScalar();
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS StudentEvaluations (
    Id BIGSERIAL PRIMARY KEY,
    StudentId BIGINT NOT NULL,
    ClassId BIGINT NOT NULL,
    TeacherId BIGINT NOT NULL,
    Score NUMERIC(5,2) NULL,
    Comment TEXT NULL,
    CreatedAt TEXT NOT NULL
);";
        command.ExecuteNonQuery();
    }
}

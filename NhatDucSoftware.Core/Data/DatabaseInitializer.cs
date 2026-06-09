namespace NhatDucSoftware.Core.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1;";
        command.ExecuteScalar();
    }
}

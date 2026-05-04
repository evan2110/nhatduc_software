using Microsoft.Data.Sqlite;

namespace NhatDucSoftware.Data;

public static class DbContext
{
    public static string ConnectionString => $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nhatduc.db")}";

    public static SqliteConnection CreateConnection()
    {
        return new SqliteConnection(ConnectionString);
    }
}

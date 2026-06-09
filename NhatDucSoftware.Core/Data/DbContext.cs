using Npgsql;

namespace NhatDucSoftware.Core.Data;

public static class DbContext
{
    private const string DefaultHost = "aws-1-ap-southeast-1.pooler.supabase.com";
    private const int DefaultPort = 5432;
    private const string DefaultDatabase = "postgres";
    private const string DefaultUsername = "postgres.zquukhtkppckbwzdiigb";

    private static string? _connectionString;

    public static void Configure(string? connectionString = null, string? password = null)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _connectionString = connectionString;
            return;
        }

        var dbPassword = password
            ?? Environment.GetEnvironmentVariable("SUPABASE_DB_PASSWORD")
            ?? "@Donhatduc2001";

        if (string.IsNullOrWhiteSpace(dbPassword))
        {
            throw new InvalidOperationException("Thiếu mật khẩu database. Cấu hình ConnectionStrings:Default hoặc SUPABASE_DB_PASSWORD.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = DefaultHost,
            Port = DefaultPort,
            Database = DefaultDatabase,
            Username = DefaultUsername,
            Password = dbPassword,
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };

        _connectionString = builder.ConnectionString;
    }

    public static string ConnectionString
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                Configure();
            }

            return _connectionString!;
        }
    }

    public static NpgsqlConnection CreateConnection() => new(ConnectionString);
}

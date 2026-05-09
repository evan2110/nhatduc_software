using Npgsql;

namespace NhatDucSoftware.Data;

public static class DbContext
{
    private const string Host = "aws-1-ap-southeast-1.pooler.supabase.com";
    private const int Port = 5432;
    private const string Database = "postgres";
    private const string Username = "postgres.zquukhtkppckbwzdiigb";

    public static string ConnectionString
    {
        get
        {
            var password = "@Donhatduc2001";
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Thiếu biến môi trường SUPABASE_DB_PASSWORD để kết nối Supabase.");
            }

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = Host,
                Port = Port,
                Database = Database,
                Username = Username,
                Password = password,
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };

            return builder.ConnectionString;
        }
    }

    public static NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }
}

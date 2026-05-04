using Microsoft.Data.Sqlite;
using NhatDucSoftware.Data;
using NhatDucSoftware.Models;

namespace NhatDucSoftware.Services;

public class AuthService
{
    public AuthenticatedUser? Login(string username, string password)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, Username, Role, TeacherId
FROM Users
WHERE Username = @username AND PasswordHash = @password;";
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@password", password);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new AuthenticatedUser
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            Role = reader.GetString(2),
            TeacherId = reader.IsDBNull(3) ? null : reader.GetInt32(3)
        };
    }
}

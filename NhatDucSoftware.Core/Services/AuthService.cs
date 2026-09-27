using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class AuthService
{
    public AuthenticatedUser? Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();

        int id;
        string storedUsername;
        string role;
        int? teacherId;
        string storedPassword;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT Id, Username, Role, TeacherId, PasswordHash FROM Users WHERE Username = @username LIMIT 1;";
            command.Parameters.AddWithValue("@username", username.Trim());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            id = reader.GetInt32(0);
            storedUsername = reader.GetString(1);
            role = reader.GetString(2);
            teacherId = reader.IsDBNull(3) ? null : reader.GetInt32(3);
            storedPassword = reader.GetString(4);
        }

        if (!PasswordHasher.Verify(storedPassword, password, out var needsUpgrade))
        {
            return null;
        }

        if (needsUpgrade)
        {
            using var update = connection.CreateCommand();
            update.CommandText = "UPDATE Users SET PasswordHash = @hash WHERE Id = @id;";
            update.Parameters.AddWithValue("@hash", PasswordHasher.Hash(password));
            update.Parameters.AddWithValue("@id", id);
            update.ExecuteNonQuery();
        }

        return new AuthenticatedUser
        {
            Id = id,
            Username = storedUsername,
            Role = role,
            TeacherId = teacherId
        };
    }
}

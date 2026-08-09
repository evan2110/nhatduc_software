using Npgsql;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class TeacherService
{
    public List<Teacher> GetAll()
    {
        var result = new List<Teacher>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, FullName, Phone, Email, Status FROM Teachers ORDER BY Id ASC;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Teacher
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                Status = reader.GetString(4)
            });
        }

        return result;
    }

    public TeacherAccountInfo? GetAccountInfo(int teacherId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"SELECT Username, PasswordHash
FROM Users
WHERE TeacherId = @teacherId
LIMIT 1;";
        command.Parameters.AddWithValue("@teacherId", teacherId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new TeacherAccountInfo
        {
            Username = reader.GetString(0),
            Password = reader.GetString(1)
        };
    }

    public void UpdatePassword(int teacherId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new InvalidOperationException("Mật khẩu không được để trống.");
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE Users
SET PasswordHash = @password
WHERE TeacherId = @teacherId;";
        command.Parameters.AddWithValue("@password", newPassword.Trim());
        command.Parameters.AddWithValue("@teacherId", teacherId);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Không tìm thấy tài khoản đăng nhập của giáo viên này.");
        }
    }

    /// <summary>
    /// Thêm giáo viên mới và tự động tạo tài khoản đăng nhập.
    /// Username = tên viết thường không dấu, Password mặc định = "123456".
    /// </summary>
    private static int GetNextAvailableId(NpgsqlConnection connection, NpgsqlTransaction? transaction = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = @"
            WITH RECURSIVE seq(n) AS (
                SELECT 1
                UNION ALL
                SELECT n + 1 FROM seq WHERE n < (SELECT COALESCE(MAX(Id), 0) + 1 FROM Teachers)
            )
            SELECT MIN(n) FROM seq WHERE n NOT IN (SELECT Id FROM Teachers);";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public (string Username, string Password) Add(Teacher teacher)
    {
        ValidateTeacher(teacher);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        if (FullNameExists(connection, teacher.FullName, transaction))
        {
            throw new InvalidOperationException(
                $"Giáo viên với họ tên '{teacher.FullName.Trim()}' đã tồn tại. Không thể thêm trùng tên.");
        }

        var nextId = GetNextAvailableId(connection, transaction);

        // Insert teacher with explicit Id
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"INSERT INTO Teachers(Id, FullName, Phone, Email, Status)
VALUES(@id, @name, @phone, @email, @status);";
        command.Parameters.AddWithValue("@id", nextId);
        command.Parameters.AddWithValue("@name", teacher.FullName);
        command.Parameters.AddWithValue("@phone", (object?)teacher.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@email", (object?)teacher.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", teacher.Status);
        command.ExecuteNonQuery();

        teacher.Id = nextId;

        // Generate username from full name
        var username = GenerateUsername(connection, transaction, teacher.FullName);
        var defaultPassword = "123456";

        // Create user account linked to teacher
        using var userCmd = connection.CreateCommand();
        userCmd.Transaction = transaction;
        userCmd.CommandText = @"INSERT INTO Users(Username, PasswordHash, Role, TeacherId)
VALUES(@username, @password, 'Teacher', @teacherId);";
        userCmd.Parameters.AddWithValue("@username", username);
        userCmd.Parameters.AddWithValue("@password", defaultPassword);
        userCmd.Parameters.AddWithValue("@teacherId", nextId);
        userCmd.ExecuteNonQuery();

        transaction.Commit();

        return (username, defaultPassword);
    }

    private string GenerateUsername(NpgsqlConnection connection, NpgsqlTransaction transaction, string fullName)
    {
        // Convert Vietnamese name to lowercase ASCII without diacritics
        var normalized = RemoveDiacritics(fullName).ToLower().Trim();
        // Take last word as base (e.g. "Nguyen Van An" -> "an")
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var baseName = parts.Length > 0 ? parts[^1] : "teacher";

        // Add initials of other parts (e.g. "nvan")
        var prefix = "";
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i].Length > 0) prefix += parts[i][0];
        }
        var baseUsername = prefix + baseName;

        // Ensure uniqueness
        var username = baseUsername;
        int counter = 1;
        while (UsernameExists(connection, transaction, username))
        {
            username = baseUsername + counter;
            counter++;
        }

        return username;
    }

    private bool UsernameExists(NpgsqlConnection connection, NpgsqlTransaction transaction, string username)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "SELECT COUNT(1) FROM Users WHERE Username = @u;";
        cmd.Parameters.AddWithValue("@u", username);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        // Handle special Vietnamese characters
        var result = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        result = result.Replace("đ", "d").Replace("Đ", "D");
        return result;
    }

    public void Update(Teacher teacher)
    {
        ValidateTeacher(teacher);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE Teachers
SET FullName = @name, Phone = @phone, Email = @email, Status = @status
WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", teacher.Id);
        command.Parameters.AddWithValue("@name", teacher.FullName);
        command.Parameters.AddWithValue("@phone", (object?)teacher.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@email", (object?)teacher.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", teacher.Status);
        command.ExecuteNonQuery();
    }

    public void Delete(int teacherId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var checkClass = connection.CreateCommand();
        checkClass.CommandText = "SELECT COUNT(1) FROM Classes WHERE TeacherId = @teacherId;";
        checkClass.Parameters.AddWithValue("@teacherId", teacherId);
        if (Convert.ToInt32(checkClass.ExecuteScalar()) > 0)
        {
            throw new InvalidOperationException("Giáo viên đang được gán lớp học, không thể xóa.");
        }

        using var transaction = connection.BeginTransaction();

        using var deleteUser = connection.CreateCommand();
        deleteUser.Transaction = transaction;
        deleteUser.CommandText = "DELETE FROM Users WHERE TeacherId = @teacherId;";
        deleteUser.Parameters.AddWithValue("@teacherId", teacherId);
        deleteUser.ExecuteNonQuery();

        using var deleteTeacher = connection.CreateCommand();
        deleteTeacher.Transaction = transaction;
        deleteTeacher.CommandText = "DELETE FROM Teachers WHERE Id = @id;";
        deleteTeacher.Parameters.AddWithValue("@id", teacherId);
        deleteTeacher.ExecuteNonQuery();

        transaction.Commit();
    }

    private static void ValidateTeacher(Teacher teacher)
    {
        if (string.IsNullOrWhiteSpace(teacher.FullName))
        {
            throw new InvalidOperationException("Họ tên không được để trống.");
        }
    }

    private static bool FullNameExists(
        System.Data.Common.DbConnection connection,
        string fullName,
        System.Data.Common.DbTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        if (transaction is not null)
        {
            command.Transaction = transaction;
        }

        command.CommandText = @"
SELECT COUNT(1)
FROM Teachers
WHERE LOWER(TRIM(FullName)) = LOWER(TRIM(@name));";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@name";
        parameter.Value = fullName.Trim();
        command.Parameters.Add(parameter);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}

public class TeacherAccountInfo
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

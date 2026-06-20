using System.Text.Json;
using Npgsql;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class TeacherProfileService
{
    private readonly ClassService _classService;

    public TeacherProfileService(ClassService classService)
    {
        _classService = classService;
    }

    public Teacher? GetProfile(int teacherId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, FullName, Phone, Email, Status, DateOfBirth, Address, Qualification, TeachingSubjects
FROM Teachers
WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", teacherId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return ReadTeacher(reader);
    }

    public void UpdateProfile(Teacher teacher)
    {
        if (string.IsNullOrWhiteSpace(teacher.FullName))
        {
            throw new InvalidOperationException("Họ tên không được để trống.");
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE Teachers
SET FullName = @name,
    Phone = @phone,
    Email = @email,
    DateOfBirth = @dob,
    Address = @address,
    Qualification = @qualification,
    TeachingSubjects = @subjects
WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", teacher.Id);
        command.Parameters.AddWithValue("@name", teacher.FullName.Trim());
        command.Parameters.AddWithValue("@phone", (object?)teacher.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@email", (object?)teacher.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@dob", teacher.DateOfBirth.HasValue
            ? teacher.DateOfBirth.Value.ToString("yyyy-MM-dd")
            : DBNull.Value);
        command.Parameters.AddWithValue("@address", (object?)teacher.Address ?? DBNull.Value);
        command.Parameters.AddWithValue("@qualification", (object?)teacher.Qualification ?? DBNull.Value);
        command.Parameters.AddWithValue("@subjects", (object?)teacher.TeachingSubjects ?? DBNull.Value);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Không tìm thấy giáo viên.");
        }
    }

    public List<string> GetAllSubjects(int teacherId)
    {
        var subjects = new List<string>();

        foreach (var cls in _classService.GetClassesByTeacher(teacherId))
        {
            if (!string.IsNullOrWhiteSpace(cls.CourseCode))
            {
                subjects.Add(cls.CourseCode.Trim());
            }

            if (!string.IsNullOrWhiteSpace(cls.ClassName))
            {
                subjects.Add(cls.ClassName.Trim());
            }
        }

        return subjects
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<string> ParseSubjects(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json)?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
        }
        catch
        {
            return json.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public static string SerializeSubjects(IEnumerable<string> subjects)
    {
        var list = subjects
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return JsonSerializer.Serialize(list);
    }

    public List<TeacherMaterial> GetMaterials(int teacherId, string? subjectName = null)
    {
        var result = new List<TeacherMaterial>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.Parameters.AddWithValue("@teacherId", teacherId);

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            command.CommandText = @"
SELECT Id, TeacherId, SubjectName, FileName, DriveFileId, DriveWebViewLink, UploadedAt
FROM TeacherMaterials
WHERE TeacherId = @teacherId
ORDER BY UploadedAt DESC;";
        }
        else
        {
            command.CommandText = @"
SELECT Id, TeacherId, SubjectName, FileName, DriveFileId, DriveWebViewLink, UploadedAt
FROM TeacherMaterials
WHERE TeacherId = @teacherId AND SubjectName = @subject
ORDER BY UploadedAt DESC;";
            command.Parameters.Add(new NpgsqlParameter("@subject", NpgsqlTypes.NpgsqlDbType.Text)
            {
                Value = subjectName
            });
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadMaterial(reader));
        }

        return result;
    }

    public void SaveMaterial(TeacherMaterial material)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO TeacherMaterials(TeacherId, SubjectName, FileName, DriveFileId, DriveWebViewLink, UploadedAt)
VALUES(@teacherId, @subject, @fileName, @fileId, @link, @uploadedAt);";
        command.Parameters.AddWithValue("@teacherId", material.TeacherId);
        command.Parameters.AddWithValue("@subject", material.SubjectName);
        command.Parameters.AddWithValue("@fileName", material.FileName);
        command.Parameters.AddWithValue("@fileId", material.DriveFileId);
        command.Parameters.AddWithValue("@link", (object?)material.DriveWebViewLink ?? DBNull.Value);
        command.Parameters.AddWithValue("@uploadedAt", material.UploadedAt.ToString("o"));
        command.ExecuteNonQuery();
    }

    private static Teacher ReadTeacher(NpgsqlDataReader reader)
    {
        DateTime? dob = null;
        if (!reader.IsDBNull(5))
        {
            var dobText = reader.GetString(5);
            if (DateTime.TryParse(dobText, out var parsed))
            {
                dob = parsed.Date;
            }
        }

        return new Teacher
        {
            Id = reader.GetInt32(0),
            FullName = reader.GetString(1),
            Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
            Email = reader.IsDBNull(3) ? null : reader.GetString(3),
            Status = reader.GetString(4),
            DateOfBirth = dob,
            Address = reader.IsDBNull(6) ? null : reader.GetString(6),
            Qualification = reader.IsDBNull(7) ? null : reader.GetString(7),
            TeachingSubjects = reader.IsDBNull(8) ? null : reader.GetString(8)
        };
    }

    private static TeacherMaterial ReadMaterial(NpgsqlDataReader reader)
    {
        var uploadedAt = DateTime.UtcNow;
        if (!reader.IsDBNull(6))
        {
            DateTime.TryParse(reader.GetString(6), out uploadedAt);
        }

        return new TeacherMaterial
        {
            Id = reader.GetInt64(0),
            TeacherId = reader.GetInt32(1),
            SubjectName = reader.GetString(2),
            FileName = reader.GetString(3),
            DriveFileId = reader.GetString(4),
            DriveWebViewLink = reader.IsDBNull(5) ? null : reader.GetString(5),
            UploadedAt = uploadedAt
        };
    }
}

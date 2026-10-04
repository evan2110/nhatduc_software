using System.Data.Common;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class AnnouncementService
{
    public const int MaxTitleLength = 200;
    public const int MaxContentLength = 20000;
    public const long MaxImageBytes = 4 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public List<Announcement> GetAll()
    {
        var result = new List<Announcement>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Content, (ImageData IS NOT NULL), CreatedAt, CreatedBy FROM Announcements ORDER BY Id DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadAnnouncement(reader));
        }

        return result;
    }

    public AnnouncementImage? GetImage(int id)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT ImageData, ImageContentType FROM Announcements WHERE Id = @id AND ImageData IS NOT NULL;";
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (!reader.Read() || reader.IsDBNull(0))
        {
            return null;
        }

        var contentType = reader.IsDBNull(1) ? "application/octet-stream" : reader.GetString(1);
        if (!AllowedImageTypes.Contains(contentType))
        {
            return null;
        }

        return new AnnouncementImage
        {
            Data = (byte[])reader.GetValue(0),
            ContentType = contentType
        };
    }

    public void Add(string title, string content, byte[]? imageData, string? imageContentType, string? imageFileName, string? createdBy)
    {
        title = title.Trim();
        content = AnnouncementHtml.Sanitize(content);
        Validate(title, content, imageData, imageContentType);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Announcements (Title, Content, ImageData, ImageContentType, ImageFileName, CreatedAt, CreatedBy) VALUES (@title, @content, @imageData, @imageContentType, @imageFileName, @createdAt, @createdBy);";
        command.Parameters.AddWithValue("@title", title);
        command.Parameters.AddWithValue("@content", content);
        command.Parameters.AddWithValue("@imageData", imageData is { Length: > 0 } ? imageData : DBNull.Value);
        command.Parameters.AddWithValue("@imageContentType", imageData is { Length: > 0 } ? imageContentType! : DBNull.Value);
        command.Parameters.AddWithValue("@imageFileName", imageData is { Length: > 0 } && !string.IsNullOrWhiteSpace(imageFileName) ? imageFileName.Trim() : DBNull.Value);
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("@createdBy", string.IsNullOrWhiteSpace(createdBy) ? DBNull.Value : createdBy.Trim());
        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Announcements WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);
        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Không tìm thấy bài thông báo.");
        }
    }

    private static void Validate(string title, string content, byte[]? imageData, string? imageContentType)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Tiêu đề không được để trống.");
        }

        if (title.Length > MaxTitleLength)
        {
            throw new InvalidOperationException($"Tiêu đề tối đa {MaxTitleLength} ký tự.");
        }

        if (AnnouncementHtml.IsBlank(content))
        {
            throw new InvalidOperationException("Nội dung không được để trống.");
        }

        if (content.Length > MaxContentLength)
        {
            throw new InvalidOperationException($"Nội dung tối đa {MaxContentLength} ký tự.");
        }

        if (imageData is not { Length: > 0 })
        {
            return;
        }

        if (imageData.Length > MaxImageBytes)
        {
            throw new InvalidOperationException("Hình ảnh quá lớn. Giới hạn 4MB.");
        }

        if (string.IsNullOrWhiteSpace(imageContentType) || !AllowedImageTypes.Contains(imageContentType))
        {
            throw new InvalidOperationException("Chỉ nhận hình JPG, PNG, WEBP hoặc GIF.");
        }
    }

    private static Announcement ReadAnnouncement(DbDataReader reader) => new()
    {
        Id = Convert.ToInt32(reader.GetValue(0)),
        Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
        Content = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
        HasImage = !reader.IsDBNull(3) && Convert.ToBoolean(reader.GetValue(3)),
        CreatedAt = reader.IsDBNull(4) ? string.Empty : reader.GetValue(4)?.ToString() ?? string.Empty,
        CreatedBy = reader.IsDBNull(5) ? null : reader.GetString(5)
    };
}

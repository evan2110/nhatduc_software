namespace NhatDucSoftware.Core.Models;

public class Announcement
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool HasImage { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
}

public class AnnouncementImage
{
    public byte[] Data { get; set; } = [];
    public string ContentType { get; set; } = "application/octet-stream";
}

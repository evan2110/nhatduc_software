namespace NhatDucSoftware.Core.Models;

public class TeacherMaterial
{
    public long Id { get; set; }
    public int TeacherId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DriveFileId { get; set; } = string.Empty;
    public string? DriveWebViewLink { get; set; }
    public DateTime UploadedAt { get; set; }
}

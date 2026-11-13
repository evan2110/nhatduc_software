namespace NhatDucSoftware.Core.Models;

public static class HomeworkKind
{
    public const string Written = "Written";
    public const string Speaking = "Speaking";
}

public sealed class HomeworkAssignment
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public int TeacherId { get; set; }
    public string Title { get; set; } = "";
    public DateTime AssignedOn { get; set; }
    public string Kind { get; set; } = HomeworkKind.Written;
    public string Prompt { get; set; } = "";
    public string? AnswerKeyText { get; set; }
    public string? AnswerKeyFileName { get; set; }
    public string? AnswerKeyDriveFileId { get; set; }
    public string? AnswerKeyDriveWebViewLink { get; set; }
    public string? DriveFolderId { get; set; }
    public string PublicToken { get; set; } = "";
}

public sealed record HomeworkPublicView(string ClassName, string Title, DateTime AssignedOn, string Kind, string Prompt);

public sealed class HomeworkSlipPreview
{
    public string StudentName { get; init; } = "";
    public string ClassName { get; init; } = "";
    public string Title { get; init; } = "";
    public bool HasPassword { get; init; }
}

public sealed class HomeworkSlipOpenResult
{
    public string? Error { get; init; }
    public HomeworkSlipDetail? Detail { get; init; }
}

public sealed class HomeworkSlipDetail
{
    public string StudentName { get; init; } = "";
    public string ClassName { get; init; } = "";
    public string Title { get; init; } = "";
    public decimal? Score { get; init; }
    public string AnswerKeyText { get; init; } = "";
    public string ShortComment { get; init; } = "";
    public string ReportText { get; init; } = "";
}

public sealed class HomeworkSubmissionRow
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public decimal? Score { get; set; }
    public string? ShortComment { get; set; }
    public string? ReportText { get; set; }
    public string? FileName { get; set; }
    public string? DriveWebViewLink { get; set; }
    public string? PendingFileName { get; set; }
    public string? SavedDriveFileId { get; set; }
    public string? PendingDriveFileId { get; set; }
    public string? WorkText { get; set; }
}

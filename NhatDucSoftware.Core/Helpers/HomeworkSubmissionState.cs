namespace NhatDucSoftware.Core.Helpers;

public readonly record struct HomeworkFileCommit(
    string? SavedFileName,
    string? SavedDriveFileId,
    string? SavedWebViewLink,
    string? DriveFileIdToDelete);

public static class HomeworkSubmissionState
{
    public static HomeworkFileCommit Commit(
        string? savedFileId,
        string? pendingFileId,
        string? pendingName,
        string? pendingLink,
        string? savedName,
        string? savedLink)
    {
        if (string.IsNullOrWhiteSpace(pendingFileId))
        {
            return new HomeworkFileCommit(savedName, savedFileId, savedLink, null);
        }

        var delete = string.Equals(savedFileId, pendingFileId, StringComparison.Ordinal)
            ? null
            : savedFileId;
        if (string.IsNullOrWhiteSpace(delete))
        {
            delete = null;
        }

        return new HomeworkFileCommit(pendingName, pendingFileId, pendingLink, delete);
    }
}

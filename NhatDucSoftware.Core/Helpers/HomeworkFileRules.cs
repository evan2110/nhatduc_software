namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkFileRules
{
    public const long MaxBytes = 50L * 1024L * 1024L;

    private static readonly string[] Speaking = ["mp3", "m4a", "wav", "mp4", "webm", "mov"];

    public static bool IsAccepted(string kind, string fileName, long size, out string error)
    {
        error = "";
        if (size > MaxBytes)
        {
            error = "File quá lớn. Giới hạn 50MB.";
            return false;
        }

        if (kind != "Speaking")
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                error = "Hãy chọn file bài làm.";
                return false;
            }

            return true;
        }

        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (!Speaking.Contains(ext))
        {
            error = "Bài nói chỉ nhận video hoặc ghi âm.";
            return false;
        }

        return true;
    }
}

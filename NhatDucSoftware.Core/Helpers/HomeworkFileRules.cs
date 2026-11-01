namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkFileRules
{
    public const long MaxBytes = 50L * 1024L * 1024L;

    private static readonly string[] Written = ["jpg", "jpeg", "png", "webp", "pdf"];
    private static readonly string[] Speaking = ["mp3", "m4a", "wav", "mp4", "webm", "mov"];

    public static bool IsAccepted(string kind, string fileName, long size, out string error)
    {
        error = "";
        if (size > MaxBytes)
        {
            error = "File quá lớn. Giới hạn 50MB.";
            return false;
        }

        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var allowed = kind == "Speaking" ? Speaking : Written;
        if (!allowed.Contains(ext))
        {
            error = kind == "Speaking"
                ? "Bài nói chỉ nhận video hoặc ghi âm."
                : "Bài viết chỉ nhận ảnh hoặc PDF.";
            return false;
        }

        return true;
    }
}

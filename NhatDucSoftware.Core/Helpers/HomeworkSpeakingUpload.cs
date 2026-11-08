namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkSpeakingUpload
{
    public const string MissingFile = "Hãy chọn video hoặc ghi âm.";

    public static bool TryAccept(string? fileName, long size, out string error)
    {
        if (string.IsNullOrWhiteSpace(fileName) || size <= 0)
        {
            error = MissingFile;
            return false;
        }

        if (!HomeworkFileRules.IsAccepted("Speaking", fileName, size, out error))
        {
            return false;
        }

        return GeminiInlineFileRules.IsWithinLimit(size, out error);
    }

    public static string Mime(string? fileName)
    {
        var ext = Path.GetExtension(fileName ?? "").TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "mp3" => "audio/mpeg",
            "m4a" => "audio/mp4",
            "wav" => "audio/wav",
            "mp4" => "video/mp4",
            "webm" => "video/webm",
            "mov" => "video/quicktime",
            _ => "application/octet-stream"
        };
    }
}

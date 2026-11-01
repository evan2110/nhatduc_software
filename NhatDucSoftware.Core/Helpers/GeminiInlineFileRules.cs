namespace NhatDucSoftware.Core.Helpers;

public static class GeminiInlineFileRules
{
    public const long MaxBytes = 15L * 1024L * 1024L;

    public const string TooLargeMessage = "File quá lớn để gửi Gemini. Giới hạn 15MB cho mỗi lần chấm.";

    public static bool IsWithinLimit(long rawBytes, out string error)
    {
        if (rawBytes > MaxBytes)
        {
            error = TooLargeMessage;
            return false;
        }

        error = "";
        return true;
    }
}

namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkGradingRules
{
    public static bool TryValidateCreate(string? title, string? prompt, string? kind, out string error)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(prompt))
        {
            error = "Nhập tên bài và đề bài.";
            return false;
        }

        if (kind is not ("Written" or "Speaking"))
        {
            error = "Chọn loại bài viết hoặc bài nói.";
            return false;
        }

        error = "";
        return true;
    }

    public static bool CanGrade(string kind, string? answerKeyText, string? answerKeyDriveFileId, out string error)
    {
        if (kind == "Written"
            && string.IsNullOrWhiteSpace(answerKeyText)
            && string.IsNullOrWhiteSpace(answerKeyDriveFileId))
        {
            error = "Bài viết cần đáp án trước khi chấm.";
            return false;
        }

        error = "";
        return true;
    }

    public static bool CanAccess(bool teachesClass, bool studentInClass, out string error)
    {
        if (!teachesClass)
        {
            error = "Bạn không dạy lớp này.";
            return false;
        }

        if (!studentInClass)
        {
            error = "Học viên không thuộc lớp này.";
            return false;
        }

        error = "";
        return true;
    }
}

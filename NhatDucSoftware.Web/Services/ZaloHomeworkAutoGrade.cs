using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Models;
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Web.Services;

public static class ZaloHomeworkAutoGrade
{
    public const string EmptyAnswer = "Hãy gửi đáp án ở dòng bên dưới tên học viên và tên bài.";
    public const string SpeakingNeedsTeacher = "Bài nói nộp video trên link bài tập.";
    public const string Failed = "Chưa chấm được bài. Hãy thử lại.";

    public static async Task<string?> TryReplyAsync(
        string? messageText,
        string chatId,
        ZaloChatService chats,
        HomeworkService homework,
        GeminiHomeworkClient gemini,
        GoogleDriveService drive,
        ILogger logger)
    {
        var reply = ZaloHomeworkReplyParser.Parse(messageText);
        if (reply is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(reply.Answer))
        {
            return EmptyAnswer;
        }

        try
        {
            var classes = chats.ListLinkedClasses(chatId)
                .Select(item => new ZaloHomeworkClass(
                    item.Id,
                    item.Name,
                    homework.ListClassStudents(item.Id),
                    homework.ListClassAssignments(item.Id)))
                .ToList();
            var (target, failure) = ZaloHomeworkMatch.Find(classes, reply.StudentName, reply.AssignmentTitle);
            if (target is null)
            {
                return failure;
            }

            if (target.Kind == HomeworkKind.Speaking)
            {
                return SpeakingNeedsTeacher;
            }

            var assignment = homework.GetAssignment(target.TeacherId, target.AssignmentId)
                ?? throw new InvalidOperationException("Không tìm thấy bài tập.");
            if (!HomeworkGradingRules.CanGrade(assignment.Kind, assignment.AnswerKeyText, assignment.AnswerKeyDriveFileId, out var gradeError))
            {
                return gradeError;
            }

            return await GradeWrittenAsync(target, reply.Answer, homework, gemini, drive);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Không chấm được bài Zalo trong nhóm {ChatId}.", chatId);
            return ex.Message;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không chấm được bài Zalo trong nhóm {ChatId}.", chatId);
            return Failed;
        }
    }

    public static async Task<string> GradeWrittenAsync(
        ZaloHomeworkTarget target,
        string answer,
        HomeworkService homework,
        GeminiHomeworkClient gemini,
        GoogleDriveService drive)
    {
        var assignment = homework.GetAssignment(target.TeacherId, target.AssignmentId)
            ?? throw new InvalidOperationException("Không tìm thấy bài tập.");
        if (!HomeworkGradingRules.CanGrade(assignment.Kind, assignment.AnswerKeyText, assignment.AnswerKeyDriveFileId, out var gradeError))
        {
            throw new InvalidOperationException(gradeError);
        }

        homework.SaveWorkText(target.TeacherId, target.AssignmentId, target.StudentId, answer);

        byte[]? answerKeyBytes = null;
        string? answerKeyMime = null;
        if (!string.IsNullOrWhiteSpace(assignment.AnswerKeyDriveFileId))
        {
            answerKeyBytes = await drive.DownloadFileAsync(assignment.AnswerKeyDriveFileId);
            answerKeyMime = MimeFromFileName(assignment.AnswerKeyFileName);
        }

        var draft = await gemini.GradeAsync(
            assignment.Kind,
            assignment.ClassName,
            assignment.Title,
            target.StudentName,
            assignment.Prompt,
            assignment.AnswerKeyText,
            answerKeyBytes,
            answerKeyMime,
            Array.Empty<byte>(),
            "text/plain",
            answer);
        var scoreText = draft.Score?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        var report = GeminiGradeParser.NormalizeReport(draft.ReportText, scoreText);
        await homework.SaveGrade(target.TeacherId, target.AssignmentId, target.StudentId, draft.Score, draft.ShortComment, report);
        return HomeworkResultMessage.Build(
            assignment.ClassName,
            assignment.Title,
            target.StudentName,
            draft.Score,
            draft.ShortComment,
            report);
    }

    public static async Task<string> GradeSpeakingAsync(
        ZaloHomeworkTarget target,
        byte[] media,
        string mime,
        HomeworkService homework,
        GeminiHomeworkClient gemini)
    {
        var assignment = homework.GetAssignment(target.TeacherId, target.AssignmentId)
            ?? throw new InvalidOperationException("Không tìm thấy bài tập.");
        if (!HomeworkGradingRules.CanGrade(assignment.Kind, assignment.AnswerKeyText, assignment.AnswerKeyDriveFileId, out var gradeError))
        {
            throw new InvalidOperationException(gradeError);
        }

        var draft = await gemini.GradeAsync(
            assignment.Kind,
            assignment.ClassName,
            assignment.Title,
            target.StudentName,
            assignment.Prompt,
            null,
            null,
            null,
            media,
            mime);
        var scoreText = draft.Score?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        var report = GeminiGradeParser.NormalizeReport(draft.ReportText, scoreText);
        await homework.SaveGrade(target.TeacherId, target.AssignmentId, target.StudentId, draft.Score, draft.ShortComment, report);
        return HomeworkResultMessage.Build(
            assignment.ClassName,
            assignment.Title,
            target.StudentName,
            draft.Score,
            draft.ShortComment,
            report);
    }

    private static string MimeFromFileName(string? fileName)
    {
        var ext = Path.GetExtension(fileName ?? "").TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            "pdf" => "application/pdf",
            "txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}

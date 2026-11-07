using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Models;
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Web.Services;

public sealed class HomeworkLinkSubmit
{
    private readonly HomeworkService _homework;
    private readonly GeminiHomeworkClient _gemini;
    private readonly GoogleDriveService _drive;
    private readonly ZaloBotClient _zalo;
    private readonly ZaloChatService _chats;
    private readonly ILogger<HomeworkLinkSubmit> _logger;

    public HomeworkLinkSubmit(
        HomeworkService homework,
        GeminiHomeworkClient gemini,
        GoogleDriveService drive,
        ZaloBotClient zalo,
        ZaloChatService chats,
        ILogger<HomeworkLinkSubmit> logger)
    {
        _homework = homework;
        _gemini = gemini;
        _drive = drive;
        _zalo = zalo;
        _chats = chats;
        _logger = logger;
    }

    public async Task<string> Submit(string? token, string? studentName, string? answer)
    {
        if (string.IsNullOrWhiteSpace(studentName))
        {
            return "Hãy nhập họ và tên học viên.";
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            return "Hãy nhập đáp án.";
        }

        var assignment = _homework.GetByPublicToken(token);
        if (assignment is null)
        {
            return "Không tìm thấy bài tập.";
        }

        if (assignment.Kind != HomeworkKind.Written)
        {
            return "Bài nói không nộp trên trang này.";
        }

        var (student, nameError) = ZaloHomeworkMatch.VerifyStudent(
            _homework.ListClassStudents(assignment.ClassId),
            studentName);
        if (student is null)
        {
            return nameError ?? ZaloHomeworkMatch.StudentNotInClass;
        }

        var target = new ZaloHomeworkTarget(
            assignment.ClassId,
            assignment.ClassName,
            student.Id,
            student.Name,
            assignment.Id,
            assignment.TeacherId,
            assignment.Kind);

        if (!HomeworkGradingRules.CanGrade(assignment.Kind, assignment.AnswerKeyText, assignment.AnswerKeyDriveFileId, out _))
        {
            _homework.SaveWorkText(target.TeacherId, target.AssignmentId, target.StudentId, answer);
            return "Đã nộp bài.";
        }

        string slip;
        try
        {
            slip = await ZaloHomeworkAutoGrade.GradeWrittenAsync(target, answer, _homework, _gemini, _drive);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không chấm được bài nộp từ link {Token}.", token);
            return "Đã lưu bài làm. Chưa chấm được.";
        }

        var chatId = _chats.GetChatId(assignment.ClassId);
        if (string.IsNullOrWhiteSpace(chatId))
        {
            return "Đã nộp bài. Lớp chưa gắn nhóm Zalo.";
        }

        try
        {
            await _zalo.SendTextToAsync(chatId, slip);
            return "Đã nộp bài. Phiếu đã gửi vào nhóm Zalo.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không gửi được phiếu bài nộp từ link vào nhóm Zalo.");
            return "Đã nộp bài. Zalo chưa nhận được phiếu.";
        }
    }
}

using System.Data.Common;
using System.Globalization;
using Npgsql;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class HomeworkService
{
    private readonly ClassService _classes;
    private readonly EvaluationService _evaluations;
    private readonly GoogleDriveService _drive;
    private readonly TeacherProfileService _teachers;

    public HomeworkService(
        ClassService classes,
        EvaluationService evaluations,
        GoogleDriveService drive,
        TeacherProfileService teachers)
    {
        _classes = classes;
        _evaluations = evaluations;
        _drive = drive;
        _teachers = teachers;
    }

    public int CreateAssignment(int teacherId, int classId, string title, DateTime assignedOn, string kind, string prompt, string? answerKeyText)
    {
        if (!HomeworkGradingRules.TryValidateCreate(title, prompt, kind, out var error))
        {
            throw new InvalidOperationException(error);
        }

        EnsureTeaches(teacherId, classId);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = HomeworkSql.InsertAssignment;
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@title", title.Trim());
        command.Parameters.AddWithValue("@assignedOn", assignedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@kind", kind);
        command.Parameters.AddWithValue("@prompt", prompt.Trim());
        command.Parameters.AddWithValue("@answerKeyText", string.IsNullOrWhiteSpace(answerKeyText) ? DBNull.Value : answerKeyText.Trim());
        command.Parameters.AddWithValue("@answerKeyFileName", DBNull.Value);
        command.Parameters.AddWithValue("@answerKeyDriveFileId", DBNull.Value);
        command.Parameters.AddWithValue("@answerKeyDriveWebViewLink", DBNull.Value);
        command.Parameters.AddWithValue("@driveFolderId", DBNull.Value);
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<HomeworkAssignment> ListForTeacher(int teacherId, int classId)
    {
        var result = new List<HomeworkAssignment>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = HomeworkSql.ListForTeacher;
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@classId", classId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapAssignment(reader));
        }

        return result;
    }

    public HomeworkAssignment? GetAssignment(int teacherId, int assignmentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = HomeworkSql.GetAssignment;
        command.Parameters.AddWithValue("@id", assignmentId);
        command.Parameters.AddWithValue("@teacherId", teacherId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapAssignment(reader) : null;
    }

    public List<HomeworkSubmissionRow> ListRows(int teacherId, int assignmentId)
    {
        var assignment = RequireAssignment(teacherId, assignmentId);

        var result = new List<HomeworkSubmissionRow>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = HomeworkSql.ListRows;
        command.Parameters.AddWithValue("@assignmentId", assignmentId);
        command.Parameters.AddWithValue("@classId", assignment.ClassId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new HomeworkSubmissionRow
            {
                StudentId = Convert.ToInt32(reader.GetValue(0)),
                StudentName = ReadString(reader, 1) ?? "",
                Score = reader.IsDBNull(2) ? null : Convert.ToDecimal(reader.GetValue(2), CultureInfo.InvariantCulture),
                ShortComment = ReadString(reader, 3),
                ReportText = ReadString(reader, 4),
                FileName = ReadString(reader, 5),
                DriveWebViewLink = ReadString(reader, 6),
                PendingFileName = ReadString(reader, 7),
                SavedDriveFileId = ReadString(reader, 8),
                PendingDriveFileId = ReadString(reader, 9)
            });
        }

        return result;
    }

    public void UpdateAnswerKeyText(int teacherId, int assignmentId, string? answerKeyText)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = HomeworkSql.UpdateAnswerKeyText;
        command.Parameters.AddWithValue("@answerKeyText", string.IsNullOrWhiteSpace(answerKeyText) ? DBNull.Value : answerKeyText.Trim());
        command.Parameters.AddWithValue("@id", assignmentId);
        command.Parameters.AddWithValue("@teacherId", teacherId);
        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Không tìm thấy bài tập.");
        }
    }

    public async Task ReplaceAnswerKeyFile(int teacherId, int assignmentId, Stream stream, string fileName, string contentType)
    {
        var assignment = RequireAssignment(teacherId, assignmentId);
        EnsureTeaches(teacherId, assignment.ClassId);

        var upload = await UploadAsync(teacherId, assignment, stream, fileName, contentType);
        SetDriveFolder(assignmentId, upload.AssignmentFolderId);

        using (var connection = DbContext.CreateConnection())
        {
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = HomeworkSql.ReplaceAnswerKeyFile;
            command.Parameters.AddWithValue("@fileName", fileName);
            command.Parameters.AddWithValue("@driveFileId", upload.FileId);
            command.Parameters.AddWithValue("@webViewLink", (object?)upload.WebViewLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@id", assignmentId);
            command.Parameters.AddWithValue("@teacherId", teacherId);
            command.ExecuteNonQuery();
        }

        if (!string.Equals(assignment.AnswerKeyDriveFileId, upload.FileId, StringComparison.Ordinal))
        {
            await _drive.DeleteFileAsync(assignment.AnswerKeyDriveFileId);
        }
    }

    public async Task StoreWrittenFile(int teacherId, int assignmentId, int studentId, Stream stream, string fileName, string contentType)
    {
        var assignment = RequireAssignment(teacherId, assignmentId);
        if (assignment.Kind != HomeworkKind.Written)
        {
            throw new InvalidOperationException("Chỉ bài viết mới tải file bài làm.");
        }

        var size = stream.CanSeek ? stream.Length : 0L;
        if (!HomeworkFileRules.IsAccepted(assignment.Kind, fileName, size, out var fileError))
        {
            throw new InvalidOperationException(fileError);
        }

        EnsureStudentAccess(teacherId, assignment, studentId);

        var existing = ListRows(teacherId, assignmentId).FirstOrDefault(r => r.StudentId == studentId);
        var oldPendingId = existing?.PendingDriveFileId;

        var upload = await UploadAsync(teacherId, assignment, stream, fileName, contentType);
        SetDriveFolder(assignmentId, upload.AssignmentFolderId);

        using (var connection = DbContext.CreateConnection())
        {
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = HomeworkSql.UpsertPendingFile;
            command.Parameters.AddWithValue("@assignmentId", assignmentId);
            command.Parameters.AddWithValue("@studentId", studentId);
            command.Parameters.AddWithValue("@pendingFileName", fileName);
            command.Parameters.AddWithValue("@pendingDriveFileId", upload.FileId);
            command.Parameters.AddWithValue("@pendingDriveWebViewLink", (object?)upload.WebViewLink ?? DBNull.Value);
            command.ExecuteNonQuery();
        }

        if (!string.IsNullOrWhiteSpace(oldPendingId)
            && !string.Equals(oldPendingId, upload.FileId, StringComparison.Ordinal))
        {
            await _drive.DeleteFileAsync(oldPendingId);
        }
    }

    public async Task SaveGrade(int teacherId, int assignmentId, int studentId, decimal? score, string? shortComment, string reportText)
    {
        var assignment = RequireAssignment(teacherId, assignmentId);
        EnsureStudentAccess(teacherId, assignment, studentId);

        if (!HomeworkGradingRules.CanGrade(assignment.Kind, assignment.AnswerKeyText, assignment.AnswerKeyDriveFileId, out var gradeError))
        {
            throw new InvalidOperationException(gradeError);
        }

        var gradedAt = DateTime.UtcNow.ToString("o");

        if (assignment.Kind == HomeworkKind.Speaking)
        {
            using var speakingConnection = DbContext.CreateConnection();
            speakingConnection.Open();

            using var speakingCommand = speakingConnection.CreateCommand();
            speakingCommand.CommandText = HomeworkSql.SaveSpeakingGrade;
            AddGradeParameters(speakingCommand, assignmentId, studentId, score, shortComment, reportText, gradedAt);
            speakingCommand.ExecuteNonQuery();
            return;
        }

        var existing = ListRows(teacherId, assignmentId).FirstOrDefault(r => r.StudentId == studentId);
        var commit = HomeworkSubmissionState.Commit(
            existing?.SavedDriveFileId,
            existing?.PendingDriveFileId,
            null,
            null,
            null,
            null);

        using (var connection = DbContext.CreateConnection())
        {
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = HomeworkSql.SaveWrittenGrade;
            AddGradeParameters(command, assignmentId, studentId, score, shortComment, reportText, gradedAt);
            if (command.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("Hãy tải bài làm trước khi lưu.");
            }
        }

        await _drive.DeleteFileAsync(commit.DriveFileIdToDelete);
    }

    public void PushToMonth(int teacherId, int assignmentId, int studentId)
    {
        var assignment = RequireAssignment(teacherId, assignmentId);
        EnsureStudentAccess(teacherId, assignment, studentId);

        var row = ListRows(teacherId, assignmentId).FirstOrDefault(r => r.StudentId == studentId);
        if (!HomeworkMonthlyPush.TryBuild(row?.Score, row?.ShortComment, assignment.AssignedOn, out var write, out var error))
        {
            throw new InvalidOperationException(error);
        }

        _evaluations.Save(studentId, assignment.ClassId, teacherId, write.Score, write.ShortComment, write.Year, write.Month);
    }

    private void EnsureTeaches(int teacherId, int classId)
    {
        var teaches = _classes.GetClassesByTeacher(teacherId).Any(c => c.Id == classId);
        if (!HomeworkGradingRules.CanAccess(teaches, true, out var error))
        {
            throw new InvalidOperationException(error);
        }
    }

    private void EnsureStudentAccess(int teacherId, HomeworkAssignment assignment, int studentId)
    {
        var teaches = _classes.GetClassesByTeacher(teacherId).Any(c => c.Id == assignment.ClassId);
        var inClass = IsStudentInClass(assignment.ClassId, studentId);
        if (!HomeworkGradingRules.CanAccess(teaches, inClass, out var error))
        {
            throw new InvalidOperationException(error);
        }
    }

    private HomeworkAssignment RequireAssignment(int teacherId, int assignmentId)
    {
        return GetAssignment(teacherId, assignmentId)
            ?? throw new InvalidOperationException("Không tìm thấy bài tập.");
    }

    private static bool IsStudentInClass(int classId, int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = HomeworkSql.StudentInClass;
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@studentId", studentId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private async Task<GoogleDriveHomeworkUpload> UploadAsync(int teacherId, HomeworkAssignment assignment, Stream stream, string fileName, string contentType)
    {
        var teacher = _teachers.GetProfile(teacherId)
            ?? throw new InvalidOperationException("Không tìm thấy giáo viên.");

        return await _drive.UploadHomeworkFileAsync(
            assignment.DriveFolderId,
            teacher.FullName,
            HomeworkDrivePath.SubjectFolder(assignment.CourseName, assignment.CourseCode, assignment.ClassName),
            HomeworkDrivePath.AssignmentFolder(assignment.ClassName, assignment.Title),
            stream,
            fileName,
            contentType);
    }

    private static void SetDriveFolder(int assignmentId, string driveFolderId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = HomeworkSql.SetDriveFolder;
        command.Parameters.AddWithValue("@driveFolderId", driveFolderId);
        command.Parameters.AddWithValue("@id", assignmentId);
        command.ExecuteNonQuery();
    }

    private static void AddGradeParameters(NpgsqlCommand command, int assignmentId, int studentId, decimal? score, string? shortComment, string reportText, string gradedAt)
    {
        var parameters = command.Parameters;
        parameters.AddWithValue("@score", (object?)score ?? DBNull.Value);
        parameters.AddWithValue("@shortComment", (object?)shortComment ?? DBNull.Value);
        parameters.AddWithValue("@reportText", reportText);
        parameters.AddWithValue("@gradedAt", gradedAt);
        parameters.AddWithValue("@assignmentId", assignmentId);
        parameters.AddWithValue("@studentId", studentId);
    }

    private static HomeworkAssignment MapAssignment(DbDataReader reader)
    {
        var assignedOnText = ReadString(reader, 7) ?? "";
        var assignedOn = DateTime.TryParseExact(assignedOnText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : DateTime.TryParse(assignedOnText, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed) ? parsed : default;

        return new HomeworkAssignment
        {
            Id = Convert.ToInt32(reader.GetValue(0)),
            ClassId = Convert.ToInt32(reader.GetValue(1)),
            ClassName = ReadString(reader, 2) ?? "",
            CourseName = ReadString(reader, 3) ?? "",
            CourseCode = ReadString(reader, 4) ?? "",
            TeacherId = Convert.ToInt32(reader.GetValue(5)),
            Title = ReadString(reader, 6) ?? "",
            AssignedOn = assignedOn,
            Kind = ReadString(reader, 8) ?? HomeworkKind.Written,
            Prompt = ReadString(reader, 9) ?? "",
            AnswerKeyText = ReadString(reader, 10),
            AnswerKeyFileName = ReadString(reader, 11),
            AnswerKeyDriveFileId = ReadString(reader, 12),
            AnswerKeyDriveWebViewLink = ReadString(reader, 13),
            DriveFolderId = ReadString(reader, 14)
        };
    }

    private static string? ReadString(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}

using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkSqlTests
{
    [Fact]
    public void Statements_AreSingleLineAndKeepPendingSeparateFromSaved()
    {
        Assert.Contains("UNIQUE (AssignmentId, StudentId)", HomeworkSchema.CreateTables);
        Assert.DoesNotContain("\n", HomeworkSql.InsertAssignment);
        Assert.DoesNotContain("\n", HomeworkSql.UpsertPendingFile);
        Assert.DoesNotContain("\n", HomeworkSql.SaveWrittenGrade);
        Assert.DoesNotContain("\n", HomeworkSql.SaveSpeakingGrade);
        Assert.Contains("PendingDriveFileId", HomeworkSql.UpsertPendingFile);
        Assert.Contains("COALESCE(PendingDriveFileId, DriveFileId)", HomeworkSql.SaveWrittenGrade);
        Assert.DoesNotContain("DriveFileId", HomeworkSql.SaveSpeakingGrade);
    }

    [Fact]
    public void SaveWrittenGradeTextOnly_IsSingleLineAndLeavesFilesAlone()
    {
        Assert.DoesNotContain("\n", HomeworkSql.SaveWrittenGradeTextOnly);
        Assert.DoesNotContain("\r", HomeworkSql.SaveWrittenGradeTextOnly);
        Assert.DoesNotContain("DriveFileId", HomeworkSql.SaveWrittenGradeTextOnly);
        Assert.DoesNotContain("Pending", HomeworkSql.SaveWrittenGradeTextOnly);
    }

    [Fact]
    public void AddedStatements_AreSingleLine()
    {
        Assert.DoesNotContain("\n", HomeworkSql.ListForTeacher);
        Assert.DoesNotContain("c.CourseCode", HomeworkSql.ListForTeacher);
        Assert.Contains("co.Code", HomeworkSql.ListForTeacher);
        Assert.DoesNotContain("\n", HomeworkSql.GetAssignment);
        Assert.DoesNotContain("c.CourseCode", HomeworkSql.GetAssignment);
        Assert.Contains("co.Code", HomeworkSql.GetAssignment);
        Assert.DoesNotContain("\n", HomeworkSql.ListRows);
        Assert.Contains("WorkText", HomeworkSql.ListRows);
        Assert.Contains("PublicToken", HomeworkSql.InsertAssignment);
        Assert.Contains("PublicToken", HomeworkSql.GetByPublicToken);
        Assert.DoesNotContain("\n", HomeworkSql.GetByPublicToken);
        Assert.DoesNotContain("\n", HomeworkSql.SetPublicToken);
        Assert.DoesNotContain("\n", HomeworkSql.UpsertWorkText);
        Assert.DoesNotContain("\n", HomeworkSql.ListClassStudents);
        Assert.DoesNotContain("\n", HomeworkSql.ListClassAssignments);
        Assert.Contains("ClassId = @classId", HomeworkSql.ListClassAssignments);
        Assert.DoesNotContain("\n", HomeworkSql.StudentInClass);
        Assert.DoesNotContain("\n", HomeworkSql.SetDriveFolder);
        Assert.DoesNotContain("\n", HomeworkSql.ReplaceAnswerKeyFile);
        Assert.DoesNotContain("\n", HomeworkSql.UpdateAnswerKeyText);
        Assert.DoesNotContain("\n", HomeworkSql.DeleteSubmissions);
        Assert.DoesNotContain("\n", HomeworkSql.DeleteAssignment);
        Assert.Contains("TeacherId = @teacherId", HomeworkSql.DeleteSubmissions);
        Assert.Contains("TeacherId = @teacherId", HomeworkSql.DeleteAssignment);
        Assert.Contains("HomeworkSubmissions", HomeworkSql.DeleteSubmissions);
        Assert.DoesNotContain("\n", HomeworkSql.ListDriveFileIds);
    }
}

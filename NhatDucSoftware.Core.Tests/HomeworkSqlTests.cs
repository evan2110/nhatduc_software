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
    public void AddedStatements_AreSingleLine()
    {
        Assert.DoesNotContain("\n", HomeworkSql.ListForTeacher);
        Assert.DoesNotContain("\n", HomeworkSql.GetAssignment);
        Assert.DoesNotContain("\n", HomeworkSql.ListRows);
        Assert.DoesNotContain("\n", HomeworkSql.StudentInClass);
        Assert.DoesNotContain("\n", HomeworkSql.SetDriveFolder);
        Assert.DoesNotContain("\n", HomeworkSql.ReplaceAnswerKeyFile);
        Assert.DoesNotContain("\n", HomeworkSql.UpdateAnswerKeyText);
    }
}

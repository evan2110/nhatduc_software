using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkGradingRulesTests
{
    [Fact]
    public void CanGrade_BlocksWrittenWorkWithoutAnAnswerKey()
    {
        Assert.False(HomeworkGradingRules.CanGrade("Written", "  ", null, out var error));
        Assert.Contains("đáp án", error, StringComparison.OrdinalIgnoreCase);
        Assert.True(HomeworkGradingRules.CanGrade("Written", "A", null, out _));
        Assert.True(HomeworkGradingRules.CanGrade("Written", null, "drive-1", out _));
        Assert.True(HomeworkGradingRules.CanGrade("Speaking", null, null, out _));
    }

    [Fact]
    public void CanAccess_RejectsAnotherClassOrStudent()
    {
        Assert.False(HomeworkGradingRules.CanAccess(false, true, out var classError));
        Assert.Contains("không dạy", classError);
        Assert.False(HomeworkGradingRules.CanAccess(true, false, out var studentError));
        Assert.Contains("không thuộc lớp", studentError);
    }

    [Fact]
    public void Commit_KeepsSavedFileUntilPendingIsPromoted()
    {
        var unchanged = HomeworkSubmissionState.Commit("saved", null, null, null, "old.pdf", "http://old");
        Assert.Equal("saved", unchanged.SavedDriveFileId);
        Assert.Null(unchanged.DriveFileIdToDelete);

        var promoted = HomeworkSubmissionState.Commit("saved", "pending", "new.pdf", "http://new", "old.pdf", "http://old");
        Assert.Equal("pending", promoted.SavedDriveFileId);
        Assert.Equal("saved", promoted.DriveFileIdToDelete);
    }

    [Fact]
    public void TryBuild_SendsScoreAndShortCommentForTheAssignmentMonth()
    {
        Assert.True(HomeworkMonthlyPush.TryBuild(8.67m, "Tốt", new DateTime(2026, 11, 3), out var write, out _));
        Assert.Equal(8.67m, write.Score);
        Assert.Equal("Tốt", write.ShortComment);
        Assert.Equal(2026, write.Year);
        Assert.Equal(11, write.Month);
        Assert.False(HomeworkMonthlyPush.TryBuild(null, "Tốt", new DateTime(2026, 11, 3), out _, out var error));
        Assert.Contains("lưu phiếu", error, StringComparison.OrdinalIgnoreCase);
    }
}

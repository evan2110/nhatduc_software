using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkDrivePathTests
{
    [Fact]
    public void SubjectFolder_UsesCourseName()
    {
        Assert.Equal("Tiếng Anh", HomeworkDrivePath.SubjectFolder("Tiếng Anh", "ENG", "Grade 2A"));
    }

    [Fact]
    public void SubjectFolder_FallsBackToCourseCodeThenClassName()
    {
        Assert.Equal("ENG", HomeworkDrivePath.SubjectFolder("  ", "ENG", "Grade 2A"));
        Assert.Equal("Grade 2A", HomeworkDrivePath.SubjectFolder(null, " ", "Grade 2A"));
    }

    [Fact]
    public void AssignmentFolder_JoinsClassAndTitle()
    {
        Assert.Equal("Grade 2A - Unit 3", HomeworkDrivePath.AssignmentFolder("Grade 2A", "Unit 3"));
    }

    [Fact]
    public void Announcement_OmitsAnswerKey()
    {
        var text = HomeworkAnnouncement.Build("TOÁN THCS LỚP 7", "Trả lời câu tiếng Anh", new DateTime(2026, 11, 7), "https://nhatduc.example/bai-tap/abc");
        Assert.Contains("BÀI TẬP", text);
        Assert.Contains("Lớp: TOÁN THCS LỚP 7", text);
        Assert.Contains("Bài: Trả lời câu tiếng Anh", text);
        Assert.Contains("Ngày giao: 07/11/2026", text);
        Assert.Contains("Đề bài: https://nhatduc.example/bai-tap/abc", text);
        Assert.DoesNotContain("đáp án", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicToken_IsEightHexCharacters()
    {
        var token = HomeworkPublicToken.Create();
        Assert.Equal(8, token.Length);
        Assert.Matches("^[0-9a-f]{8}$", token);
    }
}

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
        var text = HomeworkAnnouncement.Build("Grade 2A", "Unit 3", new DateTime(2026, 11, 1), "Viết 5 câu.");
        Assert.Contains("Grade 2A", text);
        Assert.Contains("Unit 3", text);
        Assert.Contains("01/11/2026", text);
        Assert.Contains("Viết 5 câu.", text);
        Assert.DoesNotContain("đáp án", text, StringComparison.OrdinalIgnoreCase);
    }
}

using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkFileRulesTests
{
    [Theory]
    [InlineData("Written", "bai.jpg")]
    [InlineData("Written", "bai.JPEG")]
    [InlineData("Written", "bai.png")]
    [InlineData("Written", "bai.webp")]
    [InlineData("Written", "bai.pdf")]
    [InlineData("Speaking", "noi.mp3")]
    [InlineData("Speaking", "noi.m4a")]
    [InlineData("Speaking", "noi.wav")]
    [InlineData("Speaking", "noi.mp4")]
    [InlineData("Speaking", "noi.webm")]
    [InlineData("Speaking", "noi.mov")]
    public void IsAccepted_AllowsListedTypes(string kind, string fileName)
    {
        Assert.True(HomeworkFileRules.IsAccepted(kind, fileName, 1024, out _));
    }

    [Theory]
    [InlineData("Written", "noi.mp4")]
    [InlineData("Speaking", "bai.pdf")]
    [InlineData("Written", "bai.docx")]
    public void IsAccepted_RejectsTheOtherKind(string kind, string fileName)
    {
        Assert.False(HomeworkFileRules.IsAccepted(kind, fileName, 1024, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void IsAccepted_RejectsOver50MbWithoutChangingMeaningOfTheLimit()
    {
        Assert.False(HomeworkFileRules.IsAccepted("Written", "bai.jpg", HomeworkFileRules.MaxBytes + 1, out var error));
        Assert.Contains("50MB", error);
    }
}

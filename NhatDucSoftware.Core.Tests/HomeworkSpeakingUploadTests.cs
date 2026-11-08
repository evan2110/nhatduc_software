using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkSpeakingUploadTests
{
    [Theory]
    [InlineData("noi.mp4", "video/mp4")]
    [InlineData("noi.webm", "video/webm")]
    [InlineData("noi.mov", "video/quicktime")]
    [InlineData("noi.mp3", "audio/mpeg")]
    [InlineData("noi.m4a", "audio/mp4")]
    [InlineData("noi.wav", "audio/wav")]
    public void TryAccept_AllowsASpeakingFileWithinTheGeminiLimit(string fileName, string mime)
    {
        Assert.True(HomeworkSpeakingUpload.TryAccept(fileName, 1024, out _));
        Assert.Equal(mime, HomeworkSpeakingUpload.Mime(fileName));
    }

    [Fact]
    public void TryAccept_RejectsAFileThatIsNotAudioOrVideo()
    {
        var ok = HomeworkSpeakingUpload.TryAccept("bai.pdf", 1024, out var error);

        Assert.False(ok);
        Assert.Equal("Bài nói chỉ nhận video hoặc ghi âm.", error);
    }

    [Fact]
    public void TryAccept_RejectsAFileOverTheGeminiLimit()
    {
        var ok = HomeworkSpeakingUpload.TryAccept("noi.mp4", GeminiInlineFileRules.MaxBytes + 1, out var error);

        Assert.False(ok);
        Assert.Equal(GeminiInlineFileRules.TooLargeMessage, error);
    }

    [Fact]
    public void TryAccept_RejectsAMissingFile()
    {
        Assert.False(HomeworkSpeakingUpload.TryAccept("", 1024, out var blank));
        Assert.Equal(HomeworkSpeakingUpload.MissingFile, blank);
        Assert.False(HomeworkSpeakingUpload.TryAccept("noi.mp4", 0, out var empty));
        Assert.Equal(HomeworkSpeakingUpload.MissingFile, empty);
    }
}

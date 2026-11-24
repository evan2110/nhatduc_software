using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkPromptHtmlTests
{
    private const string Token = "0123456789abcdef0123456789abcdef";

    [Fact]
    public void Sanitize_KeepsUploadedImagesAndEmbeddedVideos()
    {
        var html = $"<p>Look</p><img src=\"/homework/media/{Token}\" alt=\"cat\">"
            + "<iframe src=\"https://www.youtube.com/embed/abc\"></iframe>"
            + "<iframe src=\"https://drive.google.com/file/d/1AbC_d-9xyz/preview?embedded=true\"></iframe>";

        var clean = HomeworkPromptHtml.Sanitize(html);

        Assert.Contains($"src=\"/homework/media/{Token}\"", clean);
        Assert.Contains("https://www.youtube.com/embed/abc", clean);
        Assert.Contains("https://drive.google.com/file/d/1AbC_d-9xyz/preview", clean);
    }

    [Fact]
    public void Sanitize_DropsUnsafeSources()
    {
        var html = "<img src=\"javascript:alert(1)\"><img src=\"/homework/media/../secret\">"
            + "<iframe src=\"https://evil.example/embed\"></iframe>"
            + "<iframe src=\"https://drive.google.com/drive/folders/abc\"></iframe><script>alert(1)</script><p onclick=\"x()\">Hi</p>";

        var clean = HomeworkPromptHtml.Sanitize(html);

        Assert.DoesNotContain("javascript", clean);
        Assert.DoesNotContain("secret", clean);
        Assert.DoesNotContain("evil.example", clean);
        Assert.DoesNotContain("folders", clean);
        Assert.DoesNotContain("script", clean);
        Assert.DoesNotContain("onclick", clean);
        Assert.Contains("Hi", clean);
    }

    [Fact]
    public void Sanitize_TurnsOldPlainTextPromptsIntoSafeHtml()
    {
        Assert.Equal("Line 1<br>a &lt; b", HomeworkPromptHtml.Sanitize("Line 1\na < b"));
    }

    [Fact]
    public void IsBlank_CountsAnImageOrVideoAsContent()
    {
        Assert.True(HomeworkPromptHtml.IsBlank("<p><br></p>"));
        Assert.True(HomeworkPromptHtml.IsBlank("  "));
        Assert.False(HomeworkPromptHtml.IsBlank($"<p><img src=\"/homework/media/{Token}\"></p>"));
        Assert.False(HomeworkPromptHtml.IsBlank("<iframe src=\"https://www.youtube.com/embed/abc\"></iframe>"));
    }

    [Fact]
    public void ToPlainText_GivesGeminiReadableTextWithMediaMarkers()
    {
        var html = $"<p>Câu 1: Nhìn tranh &amp; trả lời.</p><p><img src=\"/homework/media/{Token}\"></p><ul><li>What?</li><li>Where?</li></ul>";

        var text = HomeworkPromptHtml.ToPlainText(html);

        Assert.Equal("Câu 1: Nhìn tranh & trả lời.\n[Hình ảnh]\n- What?\n- Where?", text);
        Assert.Equal("Plain prompt", HomeworkPromptHtml.ToPlainText("  Plain prompt "));
    }

    [Fact]
    public void TokensIn_FindsEachUploadedImageOnce()
    {
        var other = "fedcba9876543210fedcba9876543210";
        var html = $"<img src=\"/homework/media/{Token}\"><img src=\"/homework/media/{other}\"><img src=\"/homework/media/{Token}\">";

        Assert.Equal(new[] { Token, other }, HomeworkMediaRules.TokensIn(html));
        Assert.Empty(HomeworkMediaRules.TokensIn("no images"));
    }

    [Fact]
    public void TryAcceptImage_ChecksTheBytesNotTheName()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0 };
        Assert.True(HomeworkMediaRules.TryAcceptImage(png, out var type, out _));
        Assert.Equal("image/png", type);

        var html = "<html>"u8.ToArray();
        Assert.False(HomeworkMediaRules.TryAcceptImage(html, out _, out var error));
        Assert.Equal(HomeworkMediaRules.NotAnImage, error);

        var huge = new byte[HomeworkMediaRules.MaxImageBytes + 1];
        huge[0] = 0xFF; huge[1] = 0xD8; huge[2] = 0xFF;
        Assert.False(HomeworkMediaRules.TryAcceptImage(huge, out _, out var sizeError));
        Assert.Equal(HomeworkMediaRules.TooLarge, sizeError);
    }
}

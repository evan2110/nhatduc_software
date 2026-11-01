using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class GeminiInlineFileRulesTests
{
    [Fact]
    public void ExactlyFifteenMegabytes_IsAccepted()
    {
        var ok = GeminiInlineFileRules.IsWithinLimit(15L * 1024L * 1024L, out var error);

        Assert.True(ok);
        Assert.Equal("", error);
    }

    [Fact]
    public void OneByteOverFifteenMegabytes_IsRejectedWithVietnameseMessage()
    {
        var ok = GeminiInlineFileRules.IsWithinLimit(15L * 1024L * 1024L + 1, out var error);

        Assert.False(ok);
        Assert.Equal("File quá lớn để gửi Gemini. Giới hạn 15MB cho mỗi lần chấm.", error);
    }

    [Fact]
    public void SmallAndEmptyFiles_AreAccepted()
    {
        Assert.True(GeminiInlineFileRules.IsWithinLimit(0, out _));
        Assert.True(GeminiInlineFileRules.IsWithinLimit(1024, out _));
    }
}

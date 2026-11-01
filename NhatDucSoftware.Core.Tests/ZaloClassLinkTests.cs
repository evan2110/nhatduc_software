using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class ZaloClassLinkTests
{
    [Fact]
    public void Match_UsesTheClassNameInsideTheMention()
    {
        var classes = new[]
        {
            (Id: 1, Name: "TOÁN THCS LỚP 6"),
            (Id: 2, Name: "Grade 2A")
        };

        var match = ZaloClassLink.Match(classes, "@Bot Nhật Đức TOÁN THCS LỚP 6");

        Assert.NotNull(match);
        Assert.Equal(1, match.Value.Id);
        Assert.Equal("TOÁN THCS LỚP 6", match.Value.Name);
    }

    [Fact]
    public void Match_PrefersTheLongerClassName()
    {
        var classes = new[]
        {
            (Id: 1, Name: "Lớp 6"),
            (Id: 2, Name: "Lớp 6A")
        };

        var match = ZaloClassLink.Match(classes, "Lớp 6A");

        Assert.Equal(2, match?.Id);
    }

    [Fact]
    public void Match_ReturnsNullWhenNothingOrTheNameIsTooShort()
    {
        var classes = new[] { (Id: 1, Name: "A"), (Id: 2, Name: "Grade 2A") };

        Assert.Null(ZaloClassLink.Match(classes, "hello"));
        Assert.Null(ZaloClassLink.Match(classes, "  "));
        Assert.Null(ZaloClassLink.Match(classes, "A"));
    }

    [Theory]
    [InlineData("@Bot Nhật Đức Cancel")]
    [InlineData("cancel")]
    [InlineData("CANCEL lớp này")]
    public void IsCancel_AcceptsTheCancelWord(string text)
    {
        Assert.True(ZaloClassLink.IsCancel(text));
    }

    [Theory]
    [InlineData("TOÁN THCS LỚP 6")]
    [InlineData("cancellation")]
    [InlineData("")]
    public void IsCancel_IgnoresOtherText(string text)
    {
        Assert.False(ZaloClassLink.IsCancel(text));
    }
}

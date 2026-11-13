using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkSlipAccessTests
{
    [Fact]
    public void Password_MatchesTheIssuedPasswordWithoutChangingCase()
    {
        Assert.True(StudentSlipPassword.Matches("  mat-khau  ", "mat-khau"));
        Assert.False(StudentSlipPassword.Matches("Mat-khau", "mat-khau"));
        Assert.False(StudentSlipPassword.Matches("", "mat-khau"));
        Assert.False(StudentSlipPassword.Matches("mat-khau", " "));
    }

    [Fact]
    public void Token_IsSixteenHexCharacters()
    {
        var token = HomeworkSlipToken.Create();
        Assert.Equal(16, token.Length);
        Assert.Matches("^[0-9a-f]{16}$", token);
    }

    [Fact]
    public void Link_UsesThePublicSiteAndTheSlipPath()
    {
        Assert.Equal("https://nhatduc.example/p/abc", HomeworkSlipLink.For("https://nhatduc.example/", "abc"));
        Assert.Equal("/p/abc", HomeworkSlipLink.For("", "abc"));
    }
}

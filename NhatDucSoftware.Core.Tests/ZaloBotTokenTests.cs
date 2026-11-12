using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class ZaloBotTokenTests
{
    [Fact]
    public void Read_KeepsThePrimaryBotAndAddsTheExtraTokens()
    {
        var tokens = ZaloBotTokens.Read("token-a", "token-b, token-a\ntoken-c");

        Assert.Equal([("1", "token-a"), ("2", "token-b"), ("3", "token-c")], tokens);
    }

    [Fact]
    public void WebhookUrl_LeavesTheFirstBotOnTheExistingAddress()
    {
        Assert.Equal("https://nhatduc.example/api/zalo/webhook", ZaloBotTokens.WebhookUrl("https://nhatduc.example/api/zalo/webhook/", "1"));
        Assert.Equal("https://nhatduc.example/api/zalo/webhook/2", ZaloBotTokens.WebhookUrl("https://nhatduc.example/api/zalo/webhook", "2"));
    }
}

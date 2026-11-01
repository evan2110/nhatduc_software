using System.Net;
using System.Text;
using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Tests;

public class ZaloWebhookTests
{
    [Fact]
    public void Parse_ReadsTheGroupChatFromAWebhookEvent()
    {
        var update = ZaloWebhookEvent.Parse("""
            {
              "event_name": "message.text.received",
              "message": {
                "text": "Test bot",
                "chat": { "id": "group-1", "chat_type": "GROUP" }
              }
            }
            """);

        Assert.Equal("message.text.received", update.EventName);
        Assert.Equal("group-1", update.ChatId);
        Assert.Equal("GROUP", update.ChatType);
        Assert.Equal("Test bot", update.Text);
    }

    [Fact]
    public void Parse_ReadsAWrappedUpdateAndIgnoresABodyWithoutAChat()
    {
        var wrapped = ZaloWebhookEvent.Parse("""
            { "ok": true, "result": { "event_name": "message.text.received", "message": { "chat": { "id": "private-1", "chat_type": "PRIVATE" }, "text": "hello" } } }
            """);
        var empty = ZaloWebhookEvent.Parse("");
        var notJson = ZaloWebhookEvent.Parse("ping");

        Assert.Equal("private-1", wrapped.ChatId);
        Assert.Equal("PRIVATE", wrapped.ChatType);
        Assert.Null(empty.ChatId);
        Assert.Null(notJson.ChatId);
    }

    [Fact]
    public void Secret_AcceptsOnlyTheConfiguredToken()
    {
        Assert.True(ZaloWebhookSecret.Matches("mykey-abcyxz", "mykey-abcyxz"));
        Assert.False(ZaloWebhookSecret.Matches("mykey-abcyxz", "other-token"));
        Assert.False(ZaloWebhookSecret.Matches("", "mykey-abcyxz"));
    }

    [Fact]
    public async Task SetWebhookAsync_RegistersThePublicUrlAndSecret()
    {
        var handler = new RecordingHandler();
        var client = new ZaloBotClient(new HttpClient(handler), "bot-token", "owner-chat");

        await client.SetWebhookAsync("https://nhatduc.example/api/zalo/webhook", "mykey-abcyxz");

        Assert.Equal(
            "https://bot-api.zaloplatforms.com/botbot-token/setWebhook",
            handler.Requests[0]);
        Assert.Contains("\"url\":\"https://nhatduc.example/api/zalo/webhook\"", handler.Bodies[0]);
        Assert.Contains("\"secret_token\":\"mykey-abcyxz\"", handler.Bodies[0]);
    }

    [Fact]
    public async Task SendTextToAsync_SendsToTheChatFromTheEvent()
    {
        var handler = new RecordingHandler();
        var client = new ZaloBotClient(new HttpClient(handler), "bot-token", "owner-chat");

        await client.SendTextToAsync("group-1", "Bot đã nhận tin trong nhóm.");

        Assert.Contains("\"chat_id\":\"group-1\"", handler.Bodies[0]);
        Assert.DoesNotContain("owner-chat", handler.Bodies[0]);
    }

    [Fact]
    public void UnlinkByChat_ClearsOnlyTheMatchingGroup()
    {
        Assert.Contains("SET ZaloChatId = NULL", ZaloChatSql.UnlinkByChat);
        Assert.Contains("WHERE ZaloChatId = @chatId", ZaloChatSql.UnlinkByChat);
        Assert.DoesNotContain("\n", ZaloChatSql.UnlinkByChat);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Bodies { get; } = new();
        public List<string> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri?.AbsoluteUri ?? "");
            Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok":true,"result":{},"error_code":0}""", Encoding.UTF8, "application/json")
            };
        }
    }
}

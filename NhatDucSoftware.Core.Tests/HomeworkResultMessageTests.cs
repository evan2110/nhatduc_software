using System.Net;
using System.Text;
using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkResultMessageTests
{
    [Fact]
    public void Build_IncludesClassTitleStudentScoreCommentAndReport()
    {
        var text = HomeworkResultMessage.Build("Grade 2A", "Unit 3", "Ngân Hà", 8.5m, "Đọc rõ", "Sửa câu 2.");

        Assert.Contains("KẾT QUẢ BÀI TẬP", text);
        Assert.Contains("Lớp: Grade 2A", text);
        Assert.Contains("Bài: Unit 3", text);
        Assert.Contains("Học viên: Ngân Hà", text);
        Assert.Contains("Điểm: 8.5", text);
        Assert.Contains("Nhận xét: Đọc rõ", text);
        Assert.Contains("Sửa câu 2.", text);
        Assert.DoesNotContain("Đáp án", text);
    }

    [Fact]
    public void Build_OmitsBlankCommentAndShowsMissingScore()
    {
        var text = HomeworkResultMessage.Build("Grade 2A", "Unit 3", "Ngân Hà", null, "  ", "");

        Assert.Contains("Điểm: Chưa có điểm", text);
        Assert.DoesNotContain("Nhận xét:", text);
    }

    [Fact]
    public void Split_KeepsAMessageWithinTheZaloLimit()
    {
        var text = new string('a', 2000);

        var parts = HomeworkResultMessage.Split(text);

        Assert.Single(parts);
        Assert.Equal(2000, parts[0].Length);
    }

    [Fact]
    public void Split_BreaksOnANewlineBeforeCuttingAWord()
    {
        var line = new string('a', 1000);
        var text = line + "\n" + line + "\n" + line;

        var parts = HomeworkResultMessage.Split(text);

        Assert.Equal(3, parts.Count);
        Assert.All(parts, part => Assert.True(part.Length <= 2000));
        Assert.Equal(line, parts[0]);
        Assert.Equal(line, parts[2]);
    }

    [Fact]
    public void Split_CutsAt2000WhenThereIsNoNewline()
    {
        var text = new string('b', 2001);

        var parts = HomeworkResultMessage.Split(text);

        Assert.Equal(2, parts.Count);
        Assert.Equal(2000, parts[0].Length);
        Assert.Equal("b", parts[1]);
    }
}

public class ZaloBotClientTests
{
    [Fact]
    public async Task SendTextAsync_PostsEachChunkToTheOwnerChat()
    {
        var handler = new RecordingHandler();
        var client = new ZaloBotClient(new HttpClient(handler), "test-token", "chat-1");

        await client.SendTextAsync(new string('c', 2001));

        Assert.Equal(2, handler.Bodies.Count);
        Assert.All(handler.Requests, request => Assert.Equal(
            "https://bot-api.zaloplatforms.com/bottest-token/sendMessage",
            request));
        Assert.Contains("\"chat_id\":\"chat-1\"", handler.Bodies[0]);
        Assert.Equal(2000, ReadText(handler.Bodies[0]).Length);
        Assert.Equal("c", ReadText(handler.Bodies[1]));
    }

    [Fact]
    public async Task SendTextAsync_DoesNotCallZaloWhenTheBotIsNotConfigured()
    {
        var handler = new RecordingHandler();
        var client = new ZaloBotClient(new HttpClient(handler), "", "chat-1");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendTextAsync("Xin chào"));

        Assert.Equal("Chưa cấu hình Zalo Bot.", ex.Message);
        Assert.Empty(handler.Bodies);
    }

    [Fact]
    public async Task SendTextAsync_ReportsARejectedMessageWithoutTheToken()
    {
        var handler = new RecordingHandler
        {
            ResponseBody = """{"ok":false,"description":"The chat_id is invaild","error_code":410}"""
        };
        var client = new ZaloBotClient(new HttpClient(handler), "secret-token", "chat-1");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendTextAsync("Xin chào"));

        Assert.Equal("Zalo chưa nhận được tin kết quả.", ex.Message);
        Assert.DoesNotContain("secret-token", ex.Message);
    }

    private static string ReadText(string body)
    {
        using var document = System.Text.Json.JsonDocument.Parse(body);
        return document.RootElement.GetProperty("text").GetString() ?? "";
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Bodies { get; } = new();
        public List<string> Requests { get; } = new();
        public string ResponseBody { get; set; } = """{"ok":true,"result":{},"error_code":0}""";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri?.AbsoluteUri ?? "");
            Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}

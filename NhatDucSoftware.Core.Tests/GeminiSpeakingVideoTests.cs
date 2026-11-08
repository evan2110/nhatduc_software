using System.Net;
using System.Text;
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Tests;

public class GeminiSpeakingVideoTests
{
    [Fact]
    public async Task GradeAsync_UploadsAVideoBeforeGradingIt()
    {
        var handler = new VideoHandler();
        var client = new GeminiHomeworkClient(new HttpClient(handler), "test-key", "gemini-2.5-flash");

        var draft = await client.GradeAsync(
            "Speaking",
            "LỚP 7",
            "Luyện nói",
            "NGUYỄN VĂN HÀO",
            "Quay một video.",
            null,
            null,
            null,
            "video-bytes"u8.ToArray(),
            "video/mp4");

        Assert.Equal(8m, draft.Score);
        Assert.Contains("file_uri", handler.GradeBody);
        Assert.Contains("https://generativelanguage.googleapis.com/v1beta/files/abc", handler.GradeBody);
        Assert.DoesNotContain("inline_data", handler.GradeBody);
        Assert.Contains(HttpMethod.Delete, handler.Methods);
    }

    [Fact]
    public async Task GradeAsync_KeepsAWrittenImageInline()
    {
        var handler = new VideoHandler();
        var client = new GeminiHomeworkClient(new HttpClient(handler), "test-key", "gemini-2.5-flash");

        await client.GradeAsync(
            "Written",
            "LỚP 7",
            "Bài viết",
            "An",
            "Đề",
            "Đáp án",
            null,
            null,
            "image"u8.ToArray(),
            "image/png");

        Assert.Contains("inline_data", handler.GradeBody);
        Assert.DoesNotContain(HttpMethod.Delete, handler.Methods);
    }

    private sealed class VideoHandler : HttpMessageHandler
    {
        public string GradeBody { get; private set; } = "";
        public List<HttpMethod> Methods { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Methods.Add(request.Method);
            var uri = request.RequestUri?.AbsoluteUri ?? "";
            if (uri.Contains("/upload/v1beta/files", StringComparison.Ordinal) && !uri.Contains("upload_id=", StringComparison.Ordinal))
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
                response.Headers.TryAddWithoutValidation("X-Goog-Upload-URL", "https://generativelanguage.googleapis.com/upload/v1beta/files?upload_id=abc");
                return response;
            }

            if (uri.Contains("upload_id=abc", StringComparison.Ordinal))
            {
                return Json("""
                    {"file":{"name":"files/abc","uri":"https://generativelanguage.googleapis.com/v1beta/files/abc","state":"PROCESSING"}}
                    """);
            }

            if (request.Method == HttpMethod.Get && uri.EndsWith("/files/abc", StringComparison.Ordinal))
            {
                return Json("""
                    {"name":"files/abc","uri":"https://generativelanguage.googleapis.com/v1beta/files/abc","state":"ACTIVE"}
                    """);
            }

            if (request.Method == HttpMethod.Delete)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            GradeBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return Json("""
                {"candidates":[{"content":{"parts":[{"text":"{\"score\":\"8\",\"shortComment\":\"Rõ\",\"report\":\"PHIẾU CHẤM BÀI NÓI\"}"}]}}]}
                """);
        }

        private static HttpResponseMessage Json(string body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }
}

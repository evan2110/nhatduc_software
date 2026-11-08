using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Services;

public static class GeminiHomeworkRequest
{
    public static string Endpoint(string model) =>
        $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
}

public sealed class GeminiHomeworkClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiHomeworkClient>? _logger;

    public GeminiHomeworkClient(HttpClient http, string apiKey, string model, ILogger<GeminiHomeworkClient>? logger = null)
    {
        _logger = logger;
        _http = http;
        _apiKey = apiKey;
        _model = string.IsNullOrWhiteSpace(model) ? "gemini-2.5-flash" : model;
    }

    public async Task<GeminiGradeDraft> GradeAsync(
        string kind,
        string className,
        string title,
        string studentName,
        string prompt,
        string? answerKeyText,
        byte[]? answerKeyFile,
        string? answerKeyMime,
        byte[] studentFile,
        string studentMime,
        string? studentWorkText = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Chưa cấu hình GEMINI_API_KEY.");
        }

        if ((studentFile.Length > 0 && !GeminiInlineFileRules.IsWithinLimit(studentFile.LongLength, out var sizeError))
            || (answerKeyFile is not null && !GeminiInlineFileRules.IsWithinLimit(answerKeyFile.LongLength, out sizeError)))
        {
            throw new InvalidOperationException(sizeError);
        }

        if (studentFile.Length == 0 && string.IsNullOrWhiteSpace(studentWorkText))
        {
            throw new InvalidOperationException("Hãy nhập nội dung hoặc tải file bài làm trước khi chấm.");
        }

        var text = GeminiGradePrompt.Build(kind, className, title, studentName, prompt, answerKeyText, studentWorkText);
        var parts = new List<object> { new { text } };
        if (answerKeyFile is { Length: > 0 })
        {
            parts.Add(new { inline_data = new { mime_type = answerKeyMime, data = Convert.ToBase64String(answerKeyFile) } });
        }

        string? uploadedName = null;
        try
        {
            if (studentFile.Length > 0 && IsSpeakingMedia(studentMime))
            {
                var uploaded = await UploadMediaAsync(studentFile, studentMime, cancellationToken);
                uploadedName = uploaded.Name;
                var ready = await WaitUntilActiveAsync(uploaded, cancellationToken);
                parts.Add(new { file_data = new { mime_type = studentMime, file_uri = ready.Uri } });
            }
            else if (studentFile.Length > 0)
            {
                parts.Add(new { inline_data = new { mime_type = studentMime, data = Convert.ToBase64String(studentFile) } });
            }

            var body = JsonSerializer.Serialize(new { contents = new[] { new { parts } } });
            using var request = new HttpRequestMessage(HttpMethod.Post, GeminiHomeworkRequest.Endpoint(_model));
            request.Headers.Add("x-goog-api-key", _apiKey);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await _http.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Gemini returned HTTP {StatusCode}. Response body: {ResponseBody}", (int)response.StatusCode, payload);
                throw new InvalidOperationException("Gemini không chấm được bài. Hãy thử lại.");
            }

            using var document = JsonDocument.Parse(payload);
            var raw = document.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
            return GeminiGradeParser.Parse(raw);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(uploadedName))
            {
                try
                {
                    await DeleteMediaAsync(uploadedName);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Không xóa được file Gemini tạm.");
                }
            }
        }
    }

    private static bool IsSpeakingMedia(string? mime)
    {
        return mime != null
            && (mime.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                || mime.StartsWith("audio/", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<GeminiUploadedFile> UploadMediaAsync(byte[] media, string mime, CancellationToken cancellationToken)
    {
        using var start = new HttpRequestMessage(HttpMethod.Post, "https://generativelanguage.googleapis.com/upload/v1beta/files");
        start.Headers.Add("x-goog-api-key", _apiKey);
        start.Headers.Add("X-Goog-Upload-Protocol", "resumable");
        start.Headers.Add("X-Goog-Upload-Command", "start");
        start.Headers.Add("X-Goog-Upload-Header-Content-Length", media.Length.ToString());
        start.Headers.Add("X-Goog-Upload-Header-Content-Type", mime);
        start.Content = new StringContent("""{"file":{"display_name":"bai-noi"}}""", Encoding.UTF8, "application/json");
        using var started = await _http.SendAsync(start, cancellationToken);
        var startBody = await started.Content.ReadAsStringAsync(cancellationToken);
        if (!started.IsSuccessStatusCode || !started.Headers.TryGetValues("X-Goog-Upload-URL", out var uploadUrls))
        {
            _logger?.LogWarning("Gemini file upload start returned HTTP {StatusCode}. Response body: {ResponseBody}", (int)started.StatusCode, startBody);
            throw new InvalidOperationException("Gemini không chấm được bài. Hãy thử lại.");
        }

        var uploadUrl = uploadUrls.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(uploadUrl))
        {
            throw new InvalidOperationException("Gemini không chấm được bài. Hãy thử lại.");
        }

        using var upload = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        upload.Headers.Add("X-Goog-Upload-Offset", "0");
        upload.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
        upload.Content = new ByteArrayContent(media);
        upload.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);
        using var uploaded = await _http.SendAsync(upload, cancellationToken);
        var uploadedBody = await uploaded.Content.ReadAsStringAsync(cancellationToken);
        if (!uploaded.IsSuccessStatusCode || !GeminiUploadedFile.TryRead(uploadedBody, out var file))
        {
            _logger?.LogWarning("Gemini file upload returned HTTP {StatusCode}. Response body: {ResponseBody}", (int)uploaded.StatusCode, uploadedBody);
            throw new InvalidOperationException("Gemini không chấm được bài. Hãy thử lại.");
        }

        return file;
    }

    private async Task<GeminiUploadedFile> WaitUntilActiveAsync(GeminiUploadedFile file, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddMinutes(2);
        var current = file;
        var polled = false;
        while (!string.Equals(current.State, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(current.State, "FAILED", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Gemini không xử lý được video. Hãy thử lại.");
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new InvalidOperationException("Gemini chưa xử lý xong video. Hãy thử lại.");
            }

            if (polled)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            polled = true;
            current = await GetMediaAsync(current.Name, cancellationToken);
        }

        return current;
    }

    private async Task<GeminiUploadedFile> GetMediaAsync(string name, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://generativelanguage.googleapis.com/v1beta/" + name);
        request.Headers.Add("x-goog-api-key", _apiKey);
        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode || !GeminiUploadedFile.TryRead(body, out var file))
        {
            _logger?.LogWarning("Gemini file status returned HTTP {StatusCode}. Response body: {ResponseBody}", (int)response.StatusCode, body);
            throw new InvalidOperationException("Gemini không chấm được bài. Hãy thử lại.");
        }

        return file;
    }

    private async Task DeleteMediaAsync(string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "https://generativelanguage.googleapis.com/v1beta/" + name);
        request.Headers.Add("x-goog-api-key", _apiKey);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger?.LogWarning("Gemini file delete returned HTTP {StatusCode}. Response body: {ResponseBody}", (int)response.StatusCode, body);
        }
    }
}

public sealed record GeminiUploadedFile(string Name, string Uri, string State)
{
    public static bool TryRead(string json, out GeminiUploadedFile file)
    {
        file = new GeminiUploadedFile("", "", "");
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty("file", out var wrapped))
            {
                root = wrapped;
            }

            var name = root.TryGetProperty("name", out var nameNode) ? nameNode.GetString() ?? "" : "";
            var uri = root.TryGetProperty("uri", out var uriNode) ? uriNode.GetString() ?? "" : "";
            var state = root.TryGetProperty("state", out var stateNode) ? stateNode.GetString() ?? "" : "";
            if (name.Length == 0 || uri.Length == 0)
            {
                return false;
            }

            file = new GeminiUploadedFile(name, uri, state);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

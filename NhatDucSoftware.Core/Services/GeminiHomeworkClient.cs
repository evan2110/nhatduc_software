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
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Chưa cấu hình GEMINI_API_KEY.");
        }

        if (!GeminiInlineFileRules.IsWithinLimit(studentFile.LongLength, out var sizeError)
            || (answerKeyFile is not null && !GeminiInlineFileRules.IsWithinLimit(answerKeyFile.LongLength, out sizeError)))
        {
            throw new InvalidOperationException(sizeError);
        }

        var text = GeminiGradePrompt.Build(kind, className, title, studentName, prompt, answerKeyText);
        var parts = new List<object> { new { text } };
        if (answerKeyFile is { Length: > 0 })
        {
            parts.Add(new { inline_data = new { mime_type = answerKeyMime, data = Convert.ToBase64String(answerKeyFile) } });
        }

        parts.Add(new { inline_data = new { mime_type = studentMime, data = Convert.ToBase64String(studentFile) } });
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
}

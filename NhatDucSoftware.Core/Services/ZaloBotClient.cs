using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Services;

public static class ZaloBotRequest
{
    public static string SendMessageEndpoint(string token) =>
        $"https://bot-api.zaloplatforms.com/bot{token}/sendMessage";
}

public sealed class ZaloBotClient
{
    public const string NotConfiguredMessage = "Chưa cấu hình Zalo Bot.";
    public const string SendFailedMessage = "Zalo chưa nhận được tin kết quả.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _http;
    private readonly string _token;
    private readonly string _chatId;
    private readonly ILogger<ZaloBotClient>? _logger;

    public ZaloBotClient(HttpClient http, string token, string chatId, ILogger<ZaloBotClient>? logger = null)
    {
        _http = http;
        _token = token ?? "";
        _chatId = chatId ?? "";
        _logger = logger;
    }

    public async Task SendTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_token) || string.IsNullOrWhiteSpace(_chatId))
        {
            throw new InvalidOperationException(NotConfiguredMessage);
        }

        foreach (var chunk in HomeworkResultMessage.Split(text))
        {
            await SendChunkAsync(chunk, cancellationToken);
        }
    }

    private async Task SendChunkAsync(string text, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new { chat_id = _chatId, text }, JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, ZaloBotRequest.SendMessageEndpoint(_token));
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var description = "";
        var accepted = response.IsSuccessStatusCode && IsOk(body, out description);
        if (!accepted)
        {
            _logger?.LogWarning("Zalo sendMessage failed with HTTP {StatusCode}: {Description}", (int)response.StatusCode, description);
            throw new InvalidOperationException(SendFailedMessage);
        }
    }

    private static bool IsOk(string body, out string description)
    {
        description = "";
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("description", out var descriptionElement))
            {
                description = descriptionElement.GetString() ?? "";
            }

            return document.RootElement.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            description = "Phản hồi không phải JSON.";
            return false;
        }
    }
}

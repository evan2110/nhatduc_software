using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NhatDucSoftware.Core.Helpers;

public sealed class ZaloWebhookEvent
{
    public string EventName { get; init; } = "";
    public string? ChatId { get; init; }
    public string? ChatType { get; init; }
    public string? Text { get; init; }

    public static ZaloWebhookEvent Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ZaloWebhookEvent();
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("result", out var result)
                && result.ValueKind == JsonValueKind.Object
                && result.TryGetProperty("message", out _))
            {
                root = result;
            }

            string? chatId = null;
            string? chatType = null;
            string? text = null;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("message", out var message)
                && message.ValueKind == JsonValueKind.Object)
            {
                text = ReadString(message, "text");
                if (message.TryGetProperty("chat", out var chat) && chat.ValueKind == JsonValueKind.Object)
                {
                    chatId = ReadString(chat, "id");
                    chatType = ReadString(chat, "chat_type");
                }
            }

            return new ZaloWebhookEvent
            {
                EventName = root.ValueKind == JsonValueKind.Object ? ReadString(root, "event_name") ?? "" : "",
                ChatId = chatId,
                ChatType = chatType,
                Text = text
            };
        }
        catch (JsonException)
        {
            return new ZaloWebhookEvent();
        }
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}

public static class ZaloWebhookSecret
{
    public static bool Matches(string? expected, string? provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected ?? "");
        var providedBytes = Encoding.UTF8.GetBytes(provided ?? "");
        if (expectedBytes.Length == 0 || expectedBytes.Length != providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

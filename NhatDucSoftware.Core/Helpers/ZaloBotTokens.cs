namespace NhatDucSoftware.Core.Helpers;

public static class ZaloBotTokens
{
    public static IReadOnlyList<(string Key, string Token)> Read(string? primary, string? extras)
    {
        var tokens = new List<(string Key, string Token)>();
        Add(tokens, "1", primary);
        var next = 2;
        foreach (var part in Split(extras))
        {
            if (tokens.Any(item => string.Equals(item.Token, part, StringComparison.Ordinal)))
            {
                continue;
            }

            Add(tokens, next.ToString(), part);
            next++;
        }

        return tokens;
    }

    public static string WebhookUrl(string? baseUrl, string key)
    {
        var url = (baseUrl ?? "").Trim().TrimEnd('/');
        return key == "1" || url.Length == 0 ? url : url + "/" + key;
    }

    private static void Add(List<(string Key, string Token)> tokens, string key, string? token)
    {
        var value = (token ?? "").Trim();
        if (value.Length == 0 || tokens.Any(item => string.Equals(item.Token, value, StringComparison.Ordinal)))
        {
            return;
        }

        tokens.Add((key, value));
    }

    private static IEnumerable<string> Split(string? extras)
    {
        if (string.IsNullOrWhiteSpace(extras))
        {
            return Array.Empty<string>();
        }

        return extras.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

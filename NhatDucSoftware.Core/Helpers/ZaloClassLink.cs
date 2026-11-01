namespace NhatDucSoftware.Core.Helpers;

public readonly record struct ZaloClassMatch(int Id, string Name);

public static class ZaloClassLink
{
    public const int MinimumNameLength = 3;

    public static ZaloClassMatch? Match(IEnumerable<(int Id, string Name)> classes, string? messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return null;
        }

        var text = messageText.Trim();
        var bestLength = 0;
        var bestId = 0;
        var bestName = "";
        var ambiguous = false;
        foreach (var item in classes)
        {
            var name = item.Name?.Trim() ?? "";
            if (name.Length < MinimumNameLength || !text.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (name.Length > bestLength)
            {
                bestLength = name.Length;
                bestId = item.Id;
                bestName = name;
                ambiguous = false;
            }
            else if (name.Length == bestLength && !string.Equals(name, bestName, StringComparison.OrdinalIgnoreCase))
            {
                ambiguous = true;
            }
        }

        return ambiguous || bestLength == 0 ? null : new ZaloClassMatch(bestId, bestName);
    }
}

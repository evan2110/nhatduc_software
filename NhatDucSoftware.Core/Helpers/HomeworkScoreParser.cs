using System.Globalization;

namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkScoreParser
{
    public static bool TryParse(string? text, out decimal score)
    {
        score = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        if (parsed < 0 || parsed > 10)
        {
            return false;
        }

        score = Math.Round(parsed, 2, MidpointRounding.AwayFromZero);
        return true;
    }
}

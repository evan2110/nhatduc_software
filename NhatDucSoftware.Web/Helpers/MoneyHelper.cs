using System.Globalization;

namespace NhatDucSoftware.Web.Helpers;

public static class MoneyHelper
{
    private static readonly NumberFormatInfo MoneyNumberFormat = new()
    {
        NumberDecimalDigits = 0,
        NumberGroupSeparator = " "
    };

    public static string FormatCurrency(decimal amount) =>
        $"{amount.ToString("N0", MoneyNumberFormat)}đ";

    public static bool TryParseMoney(string? text, out decimal amount)
    {
        var normalized = (text ?? string.Empty)
            .Replace("đ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Trim();

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
}

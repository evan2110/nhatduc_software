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

    public static string FormatMoneyInput(decimal amount) =>
        amount.ToString("N0", new NumberFormatInfo
        {
            NumberDecimalDigits = 0,
            NumberGroupSeparator = "."
        });

    public static bool TryParseMoney(string? text, out decimal amount)
    {
        var normalized = (text ?? string.Empty)
            .Replace("đ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Trim();

        if (System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^\d{1,3}(\.\d{3})+$"))
        {
            normalized = normalized.Replace(".", string.Empty);
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
}

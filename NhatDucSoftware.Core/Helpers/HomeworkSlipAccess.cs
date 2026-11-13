using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkSlipToken
{
    public static string Create()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
    }
}

public static class StudentSlipPassword
{
    public const string MissingMessage = "Học viên chưa được cấp mật khẩu.";
    public const string WrongMessage = "Mật khẩu chưa đúng.";
    public const string NotFoundMessage = "Không tìm thấy phiếu.";
    public const string EnterMessage = "Hãy nhập mật khẩu.";

    public static string Create(string? fullName, int id)
    {
        var parts = (fullName ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var last = parts.Length == 0 ? "" : parts[^1];
        return Fold(last) + "@" + id.ToString(CultureInfo.InvariantCulture);
    }

    public static bool Matches(string? stored, string? entered)
    {
        var expected = (stored ?? "").Trim();
        var actual = (entered ?? "").Trim();
        if (expected.Length == 0 || actual.Length == 0)
        {
            return false;
        }

        return string.Equals(expected, actual, StringComparison.Ordinal);
    }

    private static string Fold(string value)
    {
        var lower = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(lower.Length);
        foreach (var character in lower)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character == 'đ' ? 'd' : character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

public static class HomeworkSlipLink
{
    public static string For(string? origin, string token)
    {
        var root = (origin ?? "").Trim().TrimEnd('/');
        var path = "/p/" + token.Trim();
        return root.Length == 0 ? path : root + path;
    }

    public static string SiteOrigin()
    {
        var configured = Environment.GetEnvironmentVariable("ZALO_WEBHOOK_URL");
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var webhook))
        {
            return "";
        }

        return webhook.GetLeftPart(UriPartial.Authority);
    }
}

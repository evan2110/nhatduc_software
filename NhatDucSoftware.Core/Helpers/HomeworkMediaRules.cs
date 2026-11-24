using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkMediaRules
{
    public const string PathPrefix = "/homework/media/";
    public const long MaxImageBytes = 5 * 1024 * 1024;
    public const string TooLarge = "Ảnh quá lớn. Giới hạn 5MB.";
    public const string NotAnImage = "Chỉ nhận ảnh JPG, PNG, WEBP hoặc GIF.";

    private static readonly Regex TokenPattern = new("^[a-f0-9]{32}$", RegexOptions.Compiled);
    private static readonly Regex PathPattern = new("^/homework/media/([a-f0-9]{32})$", RegexOptions.Compiled);
    private static readonly Regex PathInHtml = new("/homework/media/([a-f0-9]{32})", RegexOptions.Compiled);

    public static string CreateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    public static string PathFor(string token) => PathPrefix + token;

    public static bool IsToken(string? token) => token is not null && TokenPattern.IsMatch(token);

    public static bool IsMediaPath(string? value) => value is not null && PathPattern.IsMatch(value);

    public static IReadOnlyList<string> TokensIn(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return Array.Empty<string>();
        }

        return PathInHtml.Matches(html)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    // Trust the bytes, not the browser's claimed type.
    public static string? DetectImageType(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (data.Length >= 8 && data[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return "image/png";
        }

        if (data.Length >= 6 && data[0] == 'G' && data[1] == 'I' && data[2] == 'F' && data[3] == '8')
        {
            return "image/gif";
        }

        if (data.Length >= 12 && data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F'
            && data[8] == 'W' && data[9] == 'E' && data[10] == 'B' && data[11] == 'P')
        {
            return "image/webp";
        }

        return null;
    }

    public static bool TryAcceptImage(byte[]? data, out string contentType, out string error)
    {
        contentType = "";
        if (data is not { Length: > 0 })
        {
            error = NotAnImage;
            return false;
        }

        if (data.LongLength > MaxImageBytes)
        {
            error = TooLarge;
            return false;
        }

        var detected = DetectImageType(data);
        if (detected is null)
        {
            error = NotAnImage;
            return false;
        }

        contentType = detected;
        error = "";
        return true;
    }
}

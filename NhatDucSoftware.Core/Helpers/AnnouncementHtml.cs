using System.Net;
using System.Text.RegularExpressions;

namespace NhatDucSoftware.Core.Helpers;

public static class AnnouncementHtml
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "b", "em", "i", "u", "s", "ul", "ol", "li", "a", "blockquote", "div", "span", "h2", "h3"
    };

    public static bool IsBlank(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return true;
        }

        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = WebUtility.HtmlDecode(text).Replace('\u00a0', ' ');
        return string.IsNullOrWhiteSpace(text);
    }

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return "";
        }

        if (!html.Contains('<'))
        {
            return WebUtility.HtmlEncode(html).Replace("\r\n", "\n").Replace("\n", "<br>");
        }

        var withoutDangerous = Regex.Replace(
            html,
            @"<(script|style|iframe|object|embed|svg|math|form|textarea|button|input)\b[^>]*>.*?</\1>",
            "",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return Regex.Replace(withoutDangerous, @"</?([a-zA-Z0-9]+)([^>]*)>", match =>
        {
            var name = match.Groups[1].Value.ToLowerInvariant();
            if (!AllowedTags.Contains(name))
            {
                return "";
            }

            if (match.Value.StartsWith("</", StringComparison.Ordinal))
            {
                return $"</{name}>";
            }

            if (name == "br")
            {
                return "<br>";
            }

            if (name != "a")
            {
                return $"<{name}>";
            }

            var href = ExtractHref(match.Groups[2].Value);
            return href is null
                ? "<a>"
                : $"<a href=\"{href}\" rel=\"noopener noreferrer\" target=\"_blank\">";
        });
    }

    private static string? ExtractHref(string attributes)
    {
        var match = Regex.Match(attributes, @"href\s*=\s*([""'])(.*?)\1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            match = Regex.Match(attributes, @"href\s*=\s*([^\s>]+)", RegexOptions.IgnoreCase);
        }

        if (!match.Success)
        {
            return null;
        }

        var href = WebUtility.HtmlDecode(match.Groups[match.Groups.Count - 1].Value).Trim();
        if (href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return WebUtility.HtmlEncode(href);
        }

        return null;
    }
}

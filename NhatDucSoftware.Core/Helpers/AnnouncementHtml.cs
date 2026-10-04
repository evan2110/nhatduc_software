using System.Net;
using System.Text.RegularExpressions;

namespace NhatDucSoftware.Core.Helpers;

public static class AnnouncementHtml
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "div", "span", "strong", "b", "em", "i", "u", "s", "strike", "del", "sub", "sup",
        "ul", "ol", "li", "a", "blockquote", "h1", "h2", "h3", "h4", "h5", "h6", "pre",
        "table", "thead", "tbody", "tfoot", "tr", "td", "th", "hr", "img", "figure", "figcaption", "iframe"
    };

    private static readonly HashSet<string> AllowedCss = new(StringComparer.OrdinalIgnoreCase)
    {
        "color", "background-color", "font-size", "font-family", "text-align", "font-weight", "font-style",
        "text-decoration", "text-decoration-line", "width", "height", "vertical-align", "border",
        "border-collapse", "padding", "margin"
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
            @"<(script|style|object|embed|svg|math|form|textarea|button|input)\b[^>]*>.*?</\1>",
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

            return OpenTag(name, match.Groups[2].Value);
        });
    }

    private static string OpenTag(string name, string attributes)
    {
        if (name == "br")
        {
            return "<br>";
        }

        var parts = new List<string>();
        var style = SanitizeStyle(ExtractAttr(attributes, "style"));
        if (style is not null)
        {
            parts.Add($"style=\"{style}\"");
        }

        var className = SanitizeClass(ExtractAttr(attributes, "class"));
        if (className is not null)
        {
            parts.Add($"class=\"{className}\"");
        }

        if (name == "a")
        {
            var href = SafeUrl(ExtractAttr(attributes, "href"), allowMailto: true);
            if (href is not null)
            {
                parts.Add($"href=\"{href}\" rel=\"noopener noreferrer\" target=\"_blank\"");
            }
        }
        else if (name == "img")
        {
            var src = SafeUrl(ExtractAttr(attributes, "src"), allowMailto: false);
            if (src is null)
            {
                return "";
            }

            parts.Add($"src=\"{src}\"");
            var alt = ExtractAttr(attributes, "alt");
            if (!string.IsNullOrWhiteSpace(alt))
            {
                parts.Add($"alt=\"{WebUtility.HtmlEncode(alt)}\"");
            }
        }
        else if (name == "iframe")
        {
            var src = SafeVideoUrl(ExtractAttr(attributes, "src"));
            if (src is null)
            {
                return "";
            }

            return $"<iframe src=\"{src}\" allowfullscreen=\"allowfullscreen\"></iframe>";
        }
        else if (name is "td" or "th")
        {
            AddIntAttr(parts, attributes, "colspan");
            AddIntAttr(parts, attributes, "rowspan");
        }

        var attr = parts.Count == 0 ? "" : " " + string.Join(" ", parts);
        return $"<{name}{attr}>";
    }

    private static void AddIntAttr(List<string> parts, string attributes, string name)
    {
        var value = ExtractAttr(attributes, name);
        if (int.TryParse(value, out var number) && number is >= 1 and <= 20)
        {
            parts.Add($"{name}=\"{number}\"");
        }
    }

    private static string? ExtractAttr(string attributes, string name)
    {
        var match = Regex.Match(
            attributes,
            $@"\b{name}\s*=\s*([""'])(.*?)\1",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (match.Success)
        {
            return WebUtility.HtmlDecode(match.Groups[2].Value).Trim();
        }

        match = Regex.Match(attributes, $@"\b{name}\s*=\s*([^\s""'>]+)", RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : null;
    }

    private static string? SanitizeStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return null;
        }

        var kept = new List<string>();
        foreach (var declaration in style.Split(';'))
        {
            var index = declaration.IndexOf(':');
            if (index <= 0)
            {
                continue;
            }

            var property = declaration[..index].Trim().ToLowerInvariant();
            var value = declaration[(index + 1)..].Trim();
            if (!AllowedCss.Contains(property) || value.Length is 0 or > 120)
            {
                continue;
            }

            var lower = value.ToLowerInvariant();
            if (lower.Contains("url(", StringComparison.Ordinal) || lower.Contains("expression", StringComparison.Ordinal)
                || lower.Contains("javascript", StringComparison.Ordinal) || lower.Contains("@import", StringComparison.Ordinal))
            {
                continue;
            }

            value = value.Replace("\"", "").Replace("'", "").Replace("<", "").Replace(">", "");
            kept.Add($"{property}: {value}");
        }

        return kept.Count == 0 ? null : string.Join("; ", kept);
    }

    private static string? SanitizeClass(string? className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return null;
        }

        var kept = className
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => Regex.IsMatch(token, @"^(se-|__se__)[A-Za-z0-9_-]+$"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return kept.Length == 0 ? null : string.Join(" ", kept);
    }

    private static string? SafeUrl(string? value, bool allowMailto)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        if (value.Contains('<') || value.Contains('>') || value.Contains('\r') || value.Contains('\n'))
        {
            return null;
        }

        var allowed = value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || (allowMailto && value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase));
        return allowed ? WebUtility.HtmlEncode(value) : null;
    }

    private static string? SafeVideoUrl(string? value)
    {
        var encoded = SafeUrl(value, allowMailto: false);
        if (encoded is null || !Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var host = uri.Host;
        var allowed = host.Equals("www.youtube.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("www.youtube-nocookie.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("player.vimeo.com", StringComparison.OrdinalIgnoreCase);
        return allowed ? encoded : null;
    }
}

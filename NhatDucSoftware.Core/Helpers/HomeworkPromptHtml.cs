using System.Net;
using System.Text.RegularExpressions;

namespace NhatDucSoftware.Core.Helpers;

// Đề bài is stored as sanitized HTML. Older assignments hold plain text, which Sanitize turns into safe HTML.
public static class HomeworkPromptHtml
{
    public const int MaxLength = 100000;
    public const string ImageMarker = "[Hình ảnh]";
    public const string VideoMarker = "[Video]";

    public static string Sanitize(string? prompt) => AnnouncementHtml.Sanitize(prompt);

    public static bool IsBlank(string? prompt) => AnnouncementHtml.IsBlank(prompt);

    // Text Gemini reads for the prompt; images travel as separate parts.
    public static string ToPlainText(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return "";
        }

        if (!Regex.IsMatch(prompt, @"</?[a-zA-Z]"))
        {
            return prompt.Trim();
        }

        var text = Regex.Replace(prompt, @"<(script|style)\b[^>]*>.*?</\1>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        text = Regex.Replace(text, @"<img\b[^>]*>", " " + ImageMarker + " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<(iframe|video)\b[^>]*>", " " + VideoMarker + " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</(p|div|li|h[1-6]|tr|blockquote|pre|figure)>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<li\b[^>]*>", "- ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", "");
        text = WebUtility.HtmlDecode(text).Replace(' ', ' ');

        var lines = text
            .Split('\n')
            .Select(line => Regex.Replace(line, @"[ \t]+", " ").Trim())
            .Where(line => line.Length > 0);
        return string.Join("\n", lines);
    }
}

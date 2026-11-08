using System.Globalization;

namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkResultMessage
{
    public const int MaxChunkLength = 2000;

    public static string Build(string className, string title, string studentName, decimal? score, string? shortComment, string? report)
    {
        var slip = WithoutWorkDetail(report);
        if (slip.Length > 0)
        {
            return slip;
        }

        var scoreText = score is null
            ? "Chưa có điểm"
            : score.Value.ToString("0.##", CultureInfo.InvariantCulture);
        var lines = new List<string>
        {
            $"Lớp: {className.Trim()}",
            $"Bài: {title.Trim()}",
            $"Học viên: {studentName.Trim()}",
            $"Điểm: {scoreText}"
        };
        if (!string.IsNullOrWhiteSpace(shortComment))
        {
            lines.Add($"Nhận xét: {shortComment.Trim()}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static string WithoutWorkDetail(string? report)
    {
        var text = (report ?? "").Replace("\r\n", "\n", StringComparison.Ordinal);
        var detail = FindDetailStart(text);
        if (detail < 0)
        {
            return text.Trim();
        }

        var comment = FindLine(text, "Nhận xét chung", detail);
        var head = text[..detail].TrimEnd();
        if (comment < 0)
        {
            return head;
        }

        var tail = text[comment..].Trim();
        return head.Length == 0 ? tail : head + "\n\n" + tail;
    }

    private static int FindDetailStart(string text)
    {
        var named = FindLine(text, "Chi tiết");
        if (named >= 0)
        {
            return named;
        }

        if (FindLine(text, "Đáp án đúng") < 0)
        {
            return -1;
        }

        return FindNumberedLine(text);
    }

    private static int FindNumberedLine(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (i > 0 && text[i - 1] != '\n')
            {
                continue;
            }

            var end = i;
            while (end < text.Length && char.IsDigit(text[end]))
            {
                end++;
            }

            if (end > i && end + 1 < text.Length && (text[end] == '.' || text[end] == ')') && text[end + 1] == ' ')
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindLine(string text, string heading, int start = 0)
    {
        var index = start;
        while (index < text.Length)
        {
            var found = text.IndexOf(heading, index, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                return -1;
            }

            if (found == 0 || text[found - 1] == '\n')
            {
                return found;
            }

            index = found + heading.Length;
        }

        return -1;
    }

    public static IReadOnlyList<string> Split(string text)
    {
        var parts = new List<string>();
        var rest = (text ?? "").Replace("\r\n", "\n", StringComparison.Ordinal);
        while (rest.Length > MaxChunkLength)
        {
            var cut = rest.LastIndexOf('\n', MaxChunkLength - 1);
            if (cut < 1)
            {
                cut = MaxChunkLength;
            }

            var part = rest[..cut].TrimEnd('\n');
            if (part.Length > 0)
            {
                parts.Add(part);
            }

            rest = rest[cut..].TrimStart('\n');
        }

        if (rest.Length > 0)
        {
            parts.Add(rest);
        }

        return parts;
    }
}

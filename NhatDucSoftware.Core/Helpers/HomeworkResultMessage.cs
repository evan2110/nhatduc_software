using System.Globalization;

namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkResultMessage
{
    public const int MaxChunkLength = 2000;

    public static string Build(string className, string title, string studentName, decimal? score, string? shortComment, string? report)
    {
        var scoreText = score is null
            ? "Chưa có điểm"
            : score.Value.ToString("0.##", CultureInfo.InvariantCulture);
        var lines = new List<string>
        {
            "KẾT QUẢ BÀI TẬP",
            $"Lớp: {className.Trim()}",
            $"Bài: {title.Trim()}",
            $"Học viên: {studentName.Trim()}",
            $"Điểm: {scoreText}"
        };
        if (!string.IsNullOrWhiteSpace(shortComment))
        {
            lines.Add($"Nhận xét: {shortComment.Trim()}");
        }

        lines.Add("");
        lines.Add((report ?? "").Trim());
        return string.Join(Environment.NewLine, lines).TrimEnd();
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

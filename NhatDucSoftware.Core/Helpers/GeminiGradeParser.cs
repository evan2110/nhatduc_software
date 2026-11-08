using System.Globalization;
using System.Text;
using System.Text.Json;

namespace NhatDucSoftware.Core.Helpers;

public sealed class GeminiGradeDraft
{
    public decimal? Score { get; init; }
    public string ShortComment { get; init; } = "";
    public string ReportText { get; init; } = "";
    public bool ScoreMissing { get; init; }
}

public static class GeminiGradePrompt
{
    public static string Build(string kind, string className, string title, string studentName, string prompt, string? answerKeyText, string? studentWorkText = null)
    {
        var shared = $"Trả về JSON thuần với các khóa score, shortComment, report. score là điểm trên thang 10 hoặc chuỗi rỗng nếu không chấm được. Học viên: {studentName}. Lớp: {className}. Bài: {title}. Đề: {prompt}.";
        if (!string.IsNullOrWhiteSpace(studentWorkText))
        {
            shared += " Bài làm dạng chữ: " + studentWorkText.Trim();
        }
        if (kind == "Speaking")
        {
            return shared + " Đây là bài nói. report là chuỗi văn bản thuần, không phải object JSON. report bắt đầu bằng dòng PHIẾU CHẤM BÀI NÓI, rồi học viên, lớp, bài và điểm. Tiếp theo mỗi mục một đoạn tiếng Việt: Phát âm, Độ trôi chảy, Ngữ pháp, Từ vựng, Nội dung. Các mục bắt buộc là phát âm, độ trôi chảy, ngữ pháp, từ vựng, nội dung. Dẫn ví dụ bằng dấu ngoặc kép bình thường.";
        }

        return shared + " Đây là bài viết. Đối chiếu với đáp án: " + (answerKeyText ?? "") + ". report bắt đầu bằng dòng PHIẾU CHẤM & SỬA BÀI, rồi tên học viên, lớp, tên bài, điểm. Phần từng câu đứng sau đúng một dòng Chi tiết bài làm, gồm bài làm, đáp án đúng và câu sửa. Kết thúc bằng một dòng riêng bắt đầu bằng Nhận xét chung.";
    }
}

public static class GeminiGradeParser
{
    public static GeminiGradeDraft Parse(string responseText)
    {
        var json = responseText.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            json = json.Trim('`');
            if (json.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                json = json[4..];
            }

            json = json.Trim();
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var scoreText = ReadJsonText(root, "score");
        var parsed = HomeworkScoreParser.TryParse(scoreText, out var score);
        return new GeminiGradeDraft
        {
            Score = parsed ? score : null,
            ScoreMissing = !parsed,
            ShortComment = ReadJsonText(root, "shortComment"),
            ReportText = ReadReport(root, parsed ? score.ToString("0.##", CultureInfo.InvariantCulture) : "")
        };
    }

    private static string ReadReport(JsonElement root, string scoreText)
    {
        if (!root.TryGetProperty("report", out var node) || node.ValueKind == JsonValueKind.Null)
        {
            return "";
        }

        if (node.ValueKind == JsonValueKind.Object)
        {
            return FormatCriteria(node, scoreText);
        }

        var text = node.ValueKind == JsonValueKind.String ? node.GetString() ?? "" : node.ToString();
        return NormalizeReport(text, scoreText);
    }

    public static string NormalizeReport(string? report, string? scoreText)
    {
        var text = (report ?? "").Trim();
        return TryFormatCriteria(text, scoreText ?? "") ?? text;
    }

    private static string? TryFormatCriteria(string text, string scoreText)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith('{') || !trimmed.EndsWith('}'))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? FormatCriteria(document.RootElement, scoreText)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string FormatCriteria(JsonElement criteria, string scoreText)
    {
        var text = new StringBuilder("PHIẾU CHẤM BÀI NÓI");
        if (!string.IsNullOrWhiteSpace(scoreText))
        {
            text.Append("\n\nĐiểm: ").Append(scoreText.Trim());
        }

        foreach (var item in criteria.EnumerateObject())
        {
            var value = item.Value.ValueKind == JsonValueKind.String
                ? item.Value.GetString() ?? ""
                : item.Value.ToString();
            value = value.Trim();
            if (value.Length == 0)
            {
                continue;
            }

            text.Append("\n\n").Append(Capitalize(item.Name.Trim())).Append(": ").Append(value);
        }

        return text.ToString();
    }

    private static string Capitalize(string name)
    {
        if (name.Length == 0)
        {
            return name;
        }

        return char.ToUpper(name[0], CultureInfo.GetCultureInfo("vi-VN")) + name[1..];
    }

    private static string ReadJsonText(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var node) || node.ValueKind == JsonValueKind.Null)
        {
            return "";
        }

        return node.ValueKind == JsonValueKind.String ? node.GetString() ?? "" : node.ToString();
    }
}

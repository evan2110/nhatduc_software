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
            return shared + " Đây là bài nói. report gồm điểm và nhận xét phát âm, độ trôi chảy, ngữ pháp, từ vựng, nội dung.";
        }

        return shared + " Đây là bài viết. Đối chiếu với đáp án: " + (answerKeyText ?? "") + ". report bắt đầu bằng dòng PHIẾU CHẤM & SỬA BÀI, rồi tên học viên, lớp, tên bài, điểm, từng chỗ đúng hoặc sai kèm bài làm, đáp án đúng và câu sửa, cuối là nhận xét chung.";
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
            ReportText = ReadJsonText(root, "report")
        };
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

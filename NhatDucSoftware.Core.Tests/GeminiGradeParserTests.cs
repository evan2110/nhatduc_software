using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class GeminiGradeParserTests
{
    [Fact]
    public void Build_RequiresWrittenSlipHeaderAndSpeakingCriteria()
    {
        var written = GeminiGradePrompt.Build("Written", "Grade 2A", "Unit 3", "An", "Đề", "Đáp án A");
        Assert.Contains("PHIẾU CHẤM & SỬA BÀI", written);
        Assert.Contains("Đáp án A", written);

        var speaking = GeminiGradePrompt.Build("Speaking", "Grade 2A", "Unit 3", "An", "Đề", null);
        Assert.Contains("PHIẾU CHẤM BÀI NÓI", speaking);
        Assert.Contains("phát âm", speaking);
        Assert.Contains("độ trôi chảy", speaking);
        Assert.Contains("ngữ pháp", speaking);
        Assert.Contains("từ vựng", speaking);
        Assert.Contains("nội dung", speaking);
        Assert.Contains("không phải object", speaking);
    }

    [Fact]
    public void Parse_ReadsScoreAndLeavesInvalidScoreForTheTeacher()
    {
        var ok = GeminiGradeParser.Parse("{\"score\":\"8,67\",\"shortComment\":\"Tốt\",\"report\":\"PHIẾU CHẤM & SỬA BÀI\"}");
        Assert.Equal(8.67m, ok.Score);
        Assert.Equal("Tốt", ok.ShortComment);
        Assert.False(ok.ScoreMissing);

        var bad = GeminiGradeParser.Parse("```json\n{\"score\":\"12\",\"shortComment\":\"Xem lại\",\"report\":\"Phiếu\"}\n```");
        Assert.Null(bad.Score);
        Assert.True(bad.ScoreMissing);
        Assert.Equal("Xem lại", bad.ShortComment);
        Assert.Equal("Phiếu", bad.ReportText);
    }

    [Fact]
    public void Parse_TurnsASpeakingCriteriaObjectIntoReadableText()
    {
        var draft = GeminiGradeParser.Parse("""
            {"score":"8","shortComment":"Khá","report":{"phát âm":"Rõ từ \"show\".","độ trôi chảy":"Chậm."}}
            """);

        Assert.Equal(8m, draft.Score);
        Assert.Equal("PHIẾU CHẤM BÀI NÓI\n\nĐiểm: 8\n\nPhát âm: Rõ từ \"show\".\n\nĐộ trôi chảy: Chậm.", draft.ReportText);
    }
}

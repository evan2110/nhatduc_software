using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkScoreParserTests
{
    [Theory]
    [InlineData("8,67", 8.67)]
    [InlineData("8.67", 8.67)]
    [InlineData("7,5", 7.5)]
    [InlineData("0", 0)]
    [InlineData("10", 10)]
    public void TryParse_AcceptsScaleOfTen(string text, decimal expected)
    {
        Assert.True(HomeworkScoreParser.TryParse(text, out var score));
        Assert.Equal(expected, score);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tám")]
    [InlineData("-1")]
    [InlineData("10.01")]
    public void TryParse_RejectsMissingOrOutOfRange(string? text)
    {
        Assert.False(HomeworkScoreParser.TryParse(text, out _));
    }
}

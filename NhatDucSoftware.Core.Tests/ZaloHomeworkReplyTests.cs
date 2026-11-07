using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Core.Tests;

public class ZaloHomeworkReplyTests
{
    [Fact]
    public void Parse_ReadsStudentTitleAndAnswerBelowTheMention()
    {
        var reply = ZaloHomeworkReplyParser.Parse("""
            @Bot Nhật Đức
            NGUYỄN THỊ NHƯ Ý + Làm toán
            2
            """);

        Assert.NotNull(reply);
        Assert.Equal("NGUYỄN THỊ NHƯ Ý", reply.StudentName);
        Assert.Equal("Làm toán", reply.AssignmentTitle);
        Assert.Equal("2", reply.Answer);
    }

    [Fact]
    public void Parse_KeepsAMultilineAnswerAndIgnoresMessagesWithoutAPlus()
    {
        var reply = ZaloHomeworkReplyParser.Parse("@Bot Nhat Duc LÊ GIA HUY + Luyện nói\nDòng một\nDòng hai");
        Assert.Equal("Dòng một\nDòng hai", reply?.Answer);

        Assert.Null(ZaloHomeworkReplyParser.Parse("@Bot Nhật Đức Cancel"));
        Assert.Null(ZaloHomeworkReplyParser.Parse("@Bot Nhật Đức TOÁN THCS LỚP 6"));
    }

    [Fact]
    public void Parse_ReturnsAnEmptyAnswerWhenOnlyTheHeaderIsPresent()
    {
        var reply = ZaloHomeworkReplyParser.Parse("@Bot Nhật Đức\nAn + Làm toán");

        Assert.NotNull(reply);
        Assert.Equal("", reply.Answer);
    }

    [Fact]
    public void Find_UsesTheLatestAssignmentForTheNamedStudent()
    {
        var older = new DateTime(2026, 11, 1);
        var newer = new DateTime(2026, 11, 2);
        var classes = new[]
        {
            new ZaloHomeworkClass(4, "LỚP 7", [
                new ZaloHomeworkStudent(9, "NGUYỄN THỊ NHƯ Ý")
            ], [
                new ZaloHomeworkAssignmentRef(1, 3, "Làm toán", older, "Written"),
                new ZaloHomeworkAssignmentRef(2, 3, "Làm toán", newer, "Written")
            ])
        };

        var (target, message) = ZaloHomeworkMatch.Find(classes, "nguyễn thị như ý", "làm toán");

        Assert.Null(message);
        Assert.Equal(2, target?.AssignmentId);
        Assert.Equal(9, target?.StudentId);
        Assert.Equal("NGUYỄN THỊ NHƯ Ý", target?.StudentName);
    }

    [Fact]
    public void Find_ExplainsAMissingStudentOrAssignment()
    {
        var classes = new[]
        {
            new ZaloHomeworkClass(4, "LỚP 7", [
                new ZaloHomeworkStudent(9, "An")
            ], [
                new ZaloHomeworkAssignmentRef(2, 3, "Làm toán", new DateTime(2026, 11, 2), "Written")
            ])
        };

        Assert.Equal(ZaloHomeworkMatch.NotLinked, ZaloHomeworkMatch.Find([], "An", "Làm toán").Message);
        Assert.Contains("Không thấy học viên", ZaloHomeworkMatch.Find(classes, "Bình", "Làm toán").Message);
        Assert.Contains("Không thấy bài", ZaloHomeworkMatch.Find(classes, "An", "Luyện nói").Message);
    }
}

using System.Globalization;

namespace NhatDucSoftware.Core.Helpers;

public sealed record ZaloHomeworkReply(string StudentName, string AssignmentTitle, string Answer);

public static class ZaloHomeworkReplyParser
{
    private static readonly string[] Mentions = ["@Bot Nhật Đức", "@Bot Nhat Duc", "Bot Nhật Đức", "Bot Nhat Duc"];

    public static ZaloHomeworkReply? Parse(string? messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return null;
        }

        var text = messageText.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        var culture = CultureInfo.GetCultureInfo("vi-VN");
        foreach (var mention in Mentions.OrderByDescending(item => item.Length))
        {
            if (text.StartsWith(mention, ignoreCase: true, culture))
            {
                text = text[mention.Length..].Trim();
                break;
            }
        }

        var mentionLines = text.Split('\n');
        var firstLine = mentionLines[0].Trim();
        if (firstLine.IndexOf('+') < 0
            && (firstLine.StartsWith("@Bot", ignoreCase: true, culture) || firstLine.StartsWith("Bot ", ignoreCase: true, culture)))
        {
            text = string.Join('\n', mentionLines.Skip(1)).Trim();
        }

        var lines = text.Split('\n');
        var headerIndex = -1;
        for (var i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                headerIndex = i;
                break;
            }
        }

        if (headerIndex < 0)
        {
            return null;
        }

        var header = lines[headerIndex].Trim();
        var plus = header.IndexOf('+');
        if (plus <= 0 || plus == header.Length - 1)
        {
            return null;
        }

        var studentName = header[..plus].Trim();
        var title = header[(plus + 1)..].Trim();
        if (studentName.Length == 0 || title.Length == 0)
        {
            return null;
        }

        var answer = string.Join('\n', lines.Skip(headerIndex + 1)).Trim();
        return new ZaloHomeworkReply(studentName, title, answer);
    }
}

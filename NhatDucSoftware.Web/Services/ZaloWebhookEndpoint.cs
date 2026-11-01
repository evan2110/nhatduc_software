using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Web.Services;

public static class ZaloWebhookEndpoint
{
    public const string GroupAck = "Bot đã nhận tin trong nhóm.";

    public static string LinkedAck(string className) =>
        $"Đã gắn nhóm này với lớp {className}. Bài tập của lớp sẽ được gửi vào đây.";

    public const string NotLinkedAck = "Nhóm này chưa gắn với lớp nào.";

    public static string UnlinkedAck(IReadOnlyList<string> classNames)
    {
        if (classNames.Count == 0)
        {
            return NotLinkedAck;
        }

        if (classNames.Count == 1)
        {
            return $"Đã hủy gắn nhóm này với lớp {classNames[0]}.";
        }

        return "Đã hủy gắn nhóm này với các lớp " + string.Join(", ", classNames) + ".";
    }

    public static async Task<IResult> Handle(
        HttpContext context,
        ZaloBotClient zalo,
        ZaloChatService chats,
        ClassService classes,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("ZaloWebhook");
        var secret = Environment.GetEnvironmentVariable("ZALO_WEBHOOK_SECRET") ?? "";
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var provided = context.Request.Headers["X-Bot-Api-Secret-Token"].ToString();
        if (!ZaloWebhookSecret.Matches(secret, provided))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        string body;
        using (var reader = new StreamReader(context.Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }

        var update = ZaloWebhookEvent.Parse(body);
        if (string.Equals(update.ChatType, "GROUP", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(update.ChatId))
        {
            var reply = GroupAck;
            try
            {
                chats.Remember(update.ChatId, "GROUP");
                if (ZaloClassLink.IsCancel(update.Text))
                {
                    reply = UnlinkedAck(chats.UnlinkByChat(update.ChatId));
                }
                else
                {
                    var names = classes.GetAll().Select(item => (item.Id, item.ClassName));
                    var match = ZaloClassLink.Match(names, update.Text);
                    if (match is ZaloClassMatch linked)
                    {
                        chats.LinkClass(linked.Id, update.ChatId);
                        reply = LinkedAck(linked.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không lưu được mã nhóm Zalo.");
            }

            try
            {
                await zalo.SendTextToAsync(update.ChatId, reply);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không gửi được tin trả lời vào nhóm Zalo.");
            }
        }

        return Results.Ok(new { message = "Success" });
    }
}

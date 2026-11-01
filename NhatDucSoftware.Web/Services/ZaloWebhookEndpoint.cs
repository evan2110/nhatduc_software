using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Web.Services;

public static class ZaloWebhookEndpoint
{
    public const string GroupAck = "Bot đã nhận tin trong nhóm.";

    public static async Task<IResult> Handle(
        HttpContext context,
        ZaloBotClient zalo,
        ZaloChatService chats,
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
            try
            {
                chats.Remember(update.ChatId, "GROUP");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không lưu được mã nhóm Zalo.");
            }

            try
            {
                await zalo.SendTextToAsync(update.ChatId, GroupAck);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không gửi được tin trả lời vào nhóm Zalo.");
            }
        }

        return Results.Ok(new { message = "Success" });
    }
}

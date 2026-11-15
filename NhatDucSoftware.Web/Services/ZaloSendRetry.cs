using NhatDucSoftware.Core.Helpers;

namespace NhatDucSoftware.Web.Services;

public static class ZaloSendRetry
{
    public static Task SendAsync(ZaloBotDirectory bots, string? botKey, string chatId, string text, ILogger? logger = null)
    {
        return HomeworkAutoRetry.UntilAsync(
            async () =>
            {
                await bots.SendTextToAsync(botKey, chatId, text);
                return true;
            },
            static sent => sent,
            HomeworkAutoRetry.IsTransientSend,
            onRetry: (exception, attempt) => logger?.LogWarning(exception, "Gửi Zalo lần {Attempt} chưa được.", attempt));
    }
}

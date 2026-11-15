using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkAutoRetry
{
    public const int MaxAttempts = 3;
    public const string IncompleteGradeMessage = "Gemini chưa trả đủ điểm và nhận xét. Hãy thử lại.";

    public static bool IsComplete(GeminiGradeDraft draft)
    {
        return draft.Score is not null
            && !draft.ScoreMissing
            && !string.IsNullOrWhiteSpace(draft.ShortComment)
            && !string.IsNullOrWhiteSpace(draft.ReportText);
    }

    public static bool IsTransientGrade(Exception exception)
    {
        if (exception is OperationCanceledException or HttpRequestException)
        {
            return true;
        }

        return exception is InvalidOperationException
            && exception.Message.Contains("Hãy thử lại", StringComparison.Ordinal);
    }

    public static bool IsTransientSend(Exception exception)
    {
        if (exception is OperationCanceledException or HttpRequestException)
        {
            return true;
        }

        return exception is InvalidOperationException
            && exception.Message == ZaloBotClient.SendFailedMessage;
    }

    public static TimeSpan Delay(int failedAttempt)
    {
        return failedAttempt <= 1 ? TimeSpan.FromSeconds(2) : TimeSpan.FromSeconds(5);
    }

    public static async Task<T> UntilAsync<T>(
        Func<Task<T>> action,
        Func<T, bool> succeeded,
        Func<Exception, bool> transient,
        Func<TimeSpan, Task>? wait = null,
        Action<Exception, int>? onRetry = null,
        int maxAttempts = MaxAttempts)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var result = await action();
                if (succeeded(result))
                {
                    return result;
                }

                last = new InvalidOperationException(IncompleteGradeMessage);
            }
            catch (Exception ex) when (transient(ex))
            {
                last = ex;
            }

            if (attempt >= maxAttempts)
            {
                break;
            }

            onRetry?.Invoke(last!, attempt);
            var pause = Delay(attempt);
            if (wait is null)
            {
                await Task.Delay(pause);
            }
            else
            {
                await wait(pause);
            }
        }

        throw last ?? new InvalidOperationException(IncompleteGradeMessage);
    }
}

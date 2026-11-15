using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkAutoRetryTests
{
    [Fact]
    public async Task UntilAsync_RetriesATransientFailureThenReturnsTheCompletedGrade()
    {
        var calls = 0;
        var waits = 0;
        var draft = await HomeworkAutoRetry.UntilAsync(
            () =>
            {
                calls++;
                if (calls == 1)
                {
                    throw new InvalidOperationException("Gemini không chấm được bài. Hãy thử lại.");
                }

                return Task.FromResult(new GeminiGradeDraft
                {
                    Score = 8m,
                    ShortComment = "Tốt",
                    ReportText = "Phiếu"
                });
            },
            HomeworkAutoRetry.IsComplete,
            HomeworkAutoRetry.IsTransientGrade,
            _ =>
            {
                waits++;
                return Task.CompletedTask;
            });

        Assert.Equal(8m, draft.Score);
        Assert.Equal(2, calls);
        Assert.Equal(1, waits);
    }

    [Fact]
    public async Task UntilAsync_DoesNotRetryAMissingAnswerKey()
    {
        var calls = 0;
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            HomeworkAutoRetry.UntilAsync(
                () =>
                {
                    calls++;
                    return Task.FromException<GeminiGradeDraft>(new InvalidOperationException("Bài viết cần đáp án trước khi chấm."));
                },
                HomeworkAutoRetry.IsComplete,
                HomeworkAutoRetry.IsTransientGrade,
                _ => Task.CompletedTask));

        Assert.Equal("Bài viết cần đáp án trước khi chấm.", exception.Message);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void SendFailure_IsRetriedButAMissingBotIsNot()
    {
        Assert.True(HomeworkAutoRetry.IsTransientSend(new InvalidOperationException(ZaloBotClient.SendFailedMessage)));
        Assert.False(HomeworkAutoRetry.IsTransientSend(new InvalidOperationException(ZaloBotClient.NotConfiguredMessage)));
    }
}

using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Tests;

public class GeminiHomeworkRequestTests
{
    [Fact]
    public void Endpoint_UsesTheConfiguredModel()
    {
        Assert.Equal(
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent",
            GeminiHomeworkRequest.Endpoint("gemini-2.5-flash"));
    }
}

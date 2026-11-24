using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace NhatDucSoftware.Web.Services;

// Adds ?v=<content hash> to files under wwwroot so browsers fetch the new copy after a deploy.
// App.razor is a component, where asp-append-version does nothing.
public sealed class StaticAssetVersion
{
    private readonly IWebHostEnvironment _env;
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);

    public StaticAssetVersion(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string For(string path) => _cache.GetOrAdd(path, Compute);

    private string Compute(string path)
    {
        var file = _env.WebRootFileProvider.GetFileInfo(path);
        if (!file.Exists)
        {
            return path;
        }

        using var stream = file.CreateReadStream();
        var hash = SHA256.HashData(stream);
        return $"{path}?v={Convert.ToHexString(hash, 0, 6).ToLowerInvariant()}";
    }
}

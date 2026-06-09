using System.Reflection;using System.Text.Json;
using System.Text.Json.Serialization;

namespace NhatDucSoftware.Services;

public sealed class UpdateInfo
{
    public required Version CurrentVersion { get; init; }
    public required Version LatestVersion { get; init; }
    public required string ReleaseTitle { get; init; }
    public required string ReleaseNotes { get; init; }
    public required string DownloadUrl { get; init; }
    public required string DownloadFileName { get; init; }
    public required string ReleasePageUrl { get; init; }
}

public sealed class UpdateCheckService
{
    private const string GitHubOwner = "evan2110";
    private const string GitHubRepo = "nhatduc_software";

    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "NhatDucSoftware-UpdateChecker" },
            { "Accept", "application/vnd.github+json" }
        }
    };

    public Version GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = GetCurrentVersion();
        var release = await FetchLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
        if (release is null)
        {
            return null;
        }

        var latestVersion = ParseReleaseVersion(release.TagName);
        if (latestVersion is null || latestVersion <= currentVersion)
        {
            return null;
        }

        var asset = SelectDownloadAsset(release.Assets);
        if (asset is null)
        {
            return null;
        }

        return new UpdateInfo
        {
            CurrentVersion = currentVersion,
            LatestVersion = latestVersion,
            ReleaseTitle = string.IsNullOrWhiteSpace(release.Name) ? release.TagName : release.Name,
            ReleaseNotes = release.Body?.Trim() ?? string.Empty,
            DownloadUrl = asset.BrowserDownloadUrl,
            DownloadFileName = asset.Name,
            ReleasePageUrl = release.HtmlUrl
        };
    }

    public async Task<string> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var targetDirectory = Path.Combine(Path.GetTempPath(), "NhatDucSoftware", "Updates");
        Directory.CreateDirectory(targetDirectory);

        var targetPath = Path.Combine(targetDirectory, updateInfo.DownloadFileName);
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        using var response = await Http.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long downloadedBytes = 0;
        int readBytes;

        while ((readBytes = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, readBytes), cancellationToken).ConfigureAwait(false);
            downloadedBytes += readBytes;

            if (totalBytes is > 0)
            {
                var percent = (int)(downloadedBytes * 100 / totalBytes.Value);
                progress?.Report(percent);
            }
        }

        progress?.Report(100);
        return targetPath;
    }

    private static async Task<GitHubRelease?> FetchLatestReleaseAsync(CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
        using var response = await Http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task DownloadAndApplyUpdateAsync(
        UpdateInfo updateInfo,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var downloadedPath = await DownloadUpdateAsync(updateInfo, progress, cancellationToken).ConfigureAwait(false);
        new UpdateApplyService().ApplyUpdateAndRestart(downloadedPath);
    }

    private static Version? ParseReleaseVersion(string tagName)
    {
        var normalized = tagName.Trim();

        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[1..];
        }

        normalized = normalized.TrimStart('.', '-', '_', ' ');

        if (Version.TryParse(normalized, out var version))
        {
            return version;
        }

        var match = System.Text.RegularExpressions.Regex.Match(tagName, @"(\d+(?:\.\d+)+)");
        if (match.Success && Version.TryParse(match.Groups[1].Value, out version))
        {
            return version;
        }

        return null;
    }

    private static GitHubAsset? SelectDownloadAsset(GitHubAsset[] assets)
    {
        if (assets.Length == 0)
        {
            return null;
        }

        var preferredExtensions = new[] { ".rar", ".zip", ".exe", ".msi" };
        foreach (var extension in preferredExtensions)
        {
            var match = assets.FirstOrDefault(asset =>
                asset.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return assets[0];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public GitHubAsset[] Assets { get; set; } = Array.Empty<GitHubAsset>();
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}

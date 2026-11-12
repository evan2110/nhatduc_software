using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Web.Services;

public sealed class ZaloBotDirectory
{
    private readonly IReadOnlyList<(string Key, ZaloBotClient Client)> _bots;

    public ZaloBotDirectory(IReadOnlyList<(string Key, ZaloBotClient Client)> bots)
    {
        _bots = bots;
    }

    public IReadOnlyList<(string Key, ZaloBotClient Client)> All => _bots;

    public ZaloBotClient? Find(string? key)
    {
        var wanted = string.IsNullOrWhiteSpace(key) ? "1" : key.Trim();
        foreach (var item in _bots)
        {
            if (item.Key == wanted)
            {
                return item.Client;
            }
        }

        return _bots.Count == 0 ? null : _bots[0].Client;
    }

    public async Task SendTextToAsync(string? botKey, string chatId, string text, CancellationToken cancellationToken = default)
    {
        var client = Find(botKey) ?? throw new InvalidOperationException(ZaloBotClient.NotConfiguredMessage);
        await client.SendTextToAsync(chatId, text, cancellationToken);
    }
}

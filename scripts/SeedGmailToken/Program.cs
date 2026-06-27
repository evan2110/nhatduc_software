using System.Text.Json;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Services;

var root = Directory.Exists(@"d:\Tool\NhatDucSoftware")
    ? @"d:\Tool\NhatDucSoftware"
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var secretsPath = Path.Combine(root, "NhatDucSoftware.Web", "appsettings.Secrets.json");
var tokenPath = Path.Combine(root, "scripts", "google-gmail-token.json");

string? token = null;
if (File.Exists(secretsPath))
{
    using var stream = File.OpenRead(secretsPath);
    var secrets = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
    if (secrets.TryGetProperty("GoogleDrive", out var drive)
        && drive.TryGetProperty("GmailRefreshToken", out var gmailToken))
    {
        token = gmailToken.GetString()?.Trim();
    }
}

if (string.IsNullOrWhiteSpace(token) && File.Exists(tokenPath))
{
    using var stream = File.OpenRead(tokenPath);
    var payload = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
    if (payload.TryGetProperty("refresh_token", out var refreshToken))
    {
        token = refreshToken.GetString()?.Trim();
    }
}

if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Khong tim thay Gmail refresh token.");
    return 1;
}

var dbPassword = Environment.GetEnvironmentVariable("SUPABASE_DB_PASSWORD") ?? "@Donhatduc2001";
DbContext.Configure(password: dbPassword);
DatabaseInitializer.Initialize();
AppSettingsService.Upsert("NHATDUC_GMAIL_REFRESH_TOKEN", token);

Console.WriteLine("Da luu NHATDUC_GMAIL_REFRESH_TOKEN vao Supabase AppSettings.");
return 0;

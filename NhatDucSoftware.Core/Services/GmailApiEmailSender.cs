using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MimeKit;

namespace NhatDucSoftware.Core.Services;

public static class GmailApiEmailSender
{
    private const string GmailSendScope = "https://www.googleapis.com/auth/gmail.send";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string SendEndpoint = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

    public static bool ShouldPreferGmailApi()
    {
        var explicitSetting = Read("NHATDUC_EMAIL_VIA_GMAIL_API");
        if (explicitSetting.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (explicitSetting.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return TryReadConfig(out _);
        }

        // Render free tier blocks outbound SMTP (ports 25/465/587).
        return IsRenderEnvironment() && TryReadConfig(out _);
    }

    public static bool TryReadConfig(out GmailApiConfig config)
    {
        config = new GmailApiConfig
        {
            ClientId = FirstNonEmpty(
                Read("NHATDUC_GMAIL_CLIENT_ID"),
                Read("GOOGLE_DRIVE_CLIENT_ID")),
            ClientSecret = FirstNonEmpty(
                Read("NHATDUC_GMAIL_CLIENT_SECRET"),
                Read("GOOGLE_DRIVE_CLIENT_SECRET")),
            RefreshToken = FirstNonEmpty(
                Read("NHATDUC_GMAIL_REFRESH_TOKEN"),
                Read("GOOGLE_DRIVE_REFRESH_TOKEN"))
        };

        return !string.IsNullOrWhiteSpace(config.ClientId)
               && !string.IsNullOrWhiteSpace(config.ClientSecret)
               && !string.IsNullOrWhiteSpace(config.RefreshToken);
    }

    public static async Task SendAsync(
        MimeMessage message,
        GmailApiConfig config,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(config, cancellationToken);
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        using var request = new HttpRequestMessage(HttpMethod.Post, SendEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var payload = JsonSerializer.Serialize(new
        {
            raw = ToGmailRaw(message)
        });
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new InvalidOperationException(TranslateGmailApiError(response.StatusCode, body));
    }

    public static bool IsSmtpBlockedError(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException or System.Net.Sockets.SocketException or IOException)
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<string> GetAccessTokenAsync(
        GmailApiConfig config,
        CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["refresh_token"] = config.RefreshToken,
            ["grant_type"] = "refresh_token"
        });

        using var response = await http.PostAsync(TokenEndpoint, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(TranslateTokenError(body));
        }

        using var json = JsonDocument.Parse(body);
        if (json.RootElement.TryGetProperty("access_token", out var tokenElement))
        {
            var token = tokenElement.GetString();
            if (!string.IsNullOrWhiteSpace(token))
            {
                if (json.RootElement.TryGetProperty("scope", out var scopeElement))
                {
                    var scope = scopeElement.GetString() ?? string.Empty;
                    if (!scope.Contains("gmail.send", StringComparison.OrdinalIgnoreCase)
                        && !scope.Contains("mail.google.com", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "Refresh token chua co quyen gmail.send. "
                            + "Thu hoi quyen app tai https://myaccount.google.com/permissions, "
                            + "chay scripts/generate-gmail-token.py, "
                            + "roi dat NHATDUC_GMAIL_REFRESH_TOKEN tren Render.");
                    }
                }

                return token;
            }
        }

        throw new InvalidOperationException("Google OAuth không trả về access_token.");
    }

    private static string ToGmailRaw(MimeMessage message)
    {
        using var stream = new MemoryStream();
        message.WriteTo(stream);
        var encoded = Convert.ToBase64String(stream.ToArray());
        return encoded.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string TranslateTokenError(string body)
    {
        if (body.Contains("invalid_scope", StringComparison.OrdinalIgnoreCase)
            || body.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
        {
            return "Refresh token chưa có quyền gửi Gmail. "
                   + "Chạy scripts/generate-google-drive-token.py để tạo token mới (scope drive + gmail.send), "
                   + "rồi cập nhật GOOGLE_DRIVE_REFRESH_TOKEN trên Render.";
        }

        if (body.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
        {
            return "Refresh token Google không hợp lệ hoặc đã hết hạn. "
                   + "Hãy tạo lại token bằng scripts/generate-google-drive-token.py.";
        }

        return $"Không thể lấy access token Google: {body}";
    }

    private static string TranslateGmailApiError(System.Net.HttpStatusCode statusCode, string body)
    {
        if (body.Contains("insufficientPermissions", StringComparison.OrdinalIgnoreCase)
            || body.Contains("insufficient", StringComparison.OrdinalIgnoreCase)
            || body.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
        {
            return "Tài khoản Google chưa cấp quyền gửi mail (gmail.send). "
                   + "1) Bật Gmail API trên Google Cloud. "
                   + "2) Thu hồi quyền app tại https://myaccount.google.com/permissions . "
                   + "3) Chạy scripts/generate-gmail-token.py và đặt NHATDUC_GMAIL_REFRESH_TOKEN trên Render.";
        }

        return $"Gmail API trả lỗi {(int)statusCode}: {body}";
    }

    public static bool IsRenderEnvironment() =>
        Read("RENDER").Equals("true", StringComparison.OrdinalIgnoreCase);

    private static string Read(string key) =>
        Environment.GetEnvironmentVariable(key)?.Trim() ?? string.Empty;

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }
}

public sealed class GmailApiConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

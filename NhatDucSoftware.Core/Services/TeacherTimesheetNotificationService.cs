using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class TeacherTimesheetNotificationService
{
    private const string CompanyEmail = "ctytnhhgiaoducnhatduc@gmail.com";
    private const string RecipientEmail = CompanyEmail;
    private const int SmtpTimeoutMs = 60_000;

    private const string CompanyName = "CÔNG TY TNHH PHÁT TRIỂN GIÁO DỤC NHẬT ĐỨC";
    private const string CompanyAddress = "Phú Hòa Nam, Trường Phú, Quảng Trị, Việt Nam";
    private const string CompanyTaxId = "3101139405";
    private const string CompanyPhone = "08887058685";
    private const string LeaderName = "Nguyễn Thị Duyến";
    private const string PreparedByName = "Đỗ Nhật Đức";

    public bool TrySendTimesheetEmail(
        Teacher teacher,
        string username,
        IReadOnlyCollection<TeacherTimesheetEmailEntry> entries,
        out string errorMessage)
    {
        errorMessage = string.Empty;

        if (entries.Count == 0)
        {
            return true;
        }

        if (!TryReadSmtpConfig(out var config, out errorMessage))
        {
            return false;
        }

        try
        {
            var message = BuildTimesheetMessage(teacher, username, entries, config);
            return TrySendMimeMessage(message, config, out errorMessage);
        }
        catch (Exception ex)
        {
            errorMessage = $"Gửi email chấm công thất bại: {ex.Message}";
            return false;
        }
    }

    public bool TrySendPayrollEmail(
        Teacher teacher,
        TeacherPayrollEmailData payroll,
        out string errorMessage)
    {
        var result = TrySendPayrollEmailAsync(teacher, payroll).GetAwaiter().GetResult();
        errorMessage = result.ErrorMessage;
        return result.Success;
    }

    public async Task<(bool Success, string ErrorMessage)> TrySendPayrollEmailAsync(
        Teacher teacher,
        TeacherPayrollEmailData payroll,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacher.Email))
        {
            return (false, $"Giáo viên {teacher.FullName} chưa có email.");
        }

        if (!TryReadSmtpConfig(out var config, out var errorMessage))
        {
            return (false, errorMessage);
        }

        try
        {
            var message = BuildPayrollMessage(teacher, payroll, config);
            var sent = await TrySendMimeMessageAsync(message, config, cancellationToken);
            if (sent)
            {
                return (true, string.Empty);
            }

            return (false, "Gửi phiếu lương thất bại: không thể kết nối SMTP.");
        }
        catch (Exception ex)
        {
            return (false, $"Gửi phiếu lương thất bại: {ex.Message}");
        }
    }

    private static MimeMessage BuildTimesheetMessage(
        Teacher teacher,
        string username,
        IReadOnlyCollection<TeacherTimesheetEmailEntry> entries,
        SmtpConfig config)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromEmail, config.FromEmail));
        message.To.Add(MailboxAddress.Parse(RecipientEmail));
        message.Subject = $"[Cham cong giao vien] {teacher.FullName} - {DateTime.Now:dd/MM/yyyy HH:mm}";
        message.Body = new TextPart("plain")
        {
            Text = BuildEmailBody(teacher, username, entries),
            ContentTransferEncoding = ContentEncoding.EightBit
        };
        return message;
    }

    private static MimeMessage BuildPayrollMessage(
        Teacher teacher,
        TeacherPayrollEmailData payroll,
        SmtpConfig config)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(CompanyName, config.FromEmail));
        message.To.Add(MailboxAddress.Parse(teacher.Email!.Trim()));
        message.Subject = $"PHIẾU LƯƠNG Tháng {payroll.Month:D2}/{payroll.Year} - {teacher.FullName}";
        message.Body = new BodyBuilder
        {
            HtmlBody = BuildPayrollEmailHtml(teacher, payroll)
        }.ToMessageBody();
        return message;
    }

    private static bool TrySendMimeMessage(MimeMessage message, SmtpConfig config, out string errorMessage)
    {
        try
        {
            var sent = TrySendMimeMessageAsync(message, config).GetAwaiter().GetResult();
            errorMessage = sent ? string.Empty : "Gửi email thất bại: không thể kết nối SMTP.";
            return sent;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static async Task<bool> TrySendMimeMessageAsync(
        MimeMessage message,
        SmtpConfig config,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        foreach (var profile in GetSmtpConnectionProfiles(config))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var client = new SmtpClient { Timeout = SmtpTimeoutMs };
                await client.ConnectAsync(profile.Host, profile.Port, profile.SecureSocketOptions, cancellationToken);
                await client.AuthenticateAsync(config.Username, config.NormalizedPassword, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
                return true;
            }
            catch (Exception ex) when (IsRetryableSmtpError(ex))
            {
                lastException = ex;
            }
        }

        if (lastException is not null)
        {
            throw lastException;
        }

        return false;
    }

    private static IEnumerable<SmtpConnectionProfile> GetSmtpConnectionProfiles(SmtpConfig config)
    {
        var profiles = new List<SmtpConnectionProfile>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string host, int port, SecureSocketOptions secureSocketOptions)
        {
            var key = $"{host}:{port}:{secureSocketOptions}";
            if (seen.Add(key))
            {
                profiles.Add(new SmtpConnectionProfile(host, port, secureSocketOptions));
            }
        }

        Add(config.Host, config.Port, GetSecureSocketOptions(config.Port, config.EnableSsl));

        if (config.Port != 587)
        {
            Add(config.Host, 587, SecureSocketOptions.StartTls);
        }

        if (config.Port != 465)
        {
            Add(config.Host, 465, SecureSocketOptions.SslOnConnect);
        }

        return profiles;
    }

    private static SecureSocketOptions GetSecureSocketOptions(int port, bool enableSsl)
    {
        if (!enableSsl)
        {
            return SecureSocketOptions.None;
        }

        return port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.Auto
        };
    }

    private static bool IsRetryableSmtpError(Exception ex) =>
        ex is TimeoutException
        || ex is SmtpCommandException
        || ex is SmtpProtocolException
        || ex is IOException
        || ex is SocketException
        || (ex.InnerException is not null && IsRetryableSmtpError(ex.InnerException));

    private static string BuildEmailBody(Teacher teacher, string username, IReadOnlyCollection<TeacherTimesheetEmailEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Thong tin cham cong giao vien");
        sb.AppendLine($"Giao vien: {teacher.FullName} (ID: {teacher.Id})");
        sb.AppendLine($"Tai khoan: {username}");
        sb.AppendLine($"Thoi gian gui: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("Chi tiet cham cong:");

        foreach (var entry in entries.OrderBy(e => e.WorkDate).ThenBy(e => e.ShiftNumber))
        {
            var status = entry.IsPresent ? "Co mat" : "Vang";
            var shiftText = TeacherTimesheet.GetShiftDescription(entry.ShiftNumber);
            var note = string.IsNullOrWhiteSpace(entry.Note) ? string.Empty : $" | Ghi chu: {entry.Note}";
            sb.AppendLine($"- {entry.WorkDate:dd/MM/yyyy} | {shiftText} | {status}{note}");
        }

        return sb.ToString();
    }

    private static string BuildPayrollEmailHtml(Teacher teacher, TeacherPayrollEmailData payroll)
    {
        var actualDays = payroll.ActualWorkingDays.ToString("0.0", CultureInfo.InvariantCulture);
        var standardDays = payroll.StandardWorkingDays.ToString(CultureInfo.InvariantCulture);
        var totalPay = payroll.TotalPay.ToString("N0", CultureInfo.InvariantCulture);
        var monthYear = $"Tháng {payroll.Month:D2} năm {payroll.Year}";

        return $"""
<!DOCTYPE html>
<html lang="vi">
<head>
<meta charset="utf-8" />
<title>PHIẾU LƯƠNG</title>
</head>
<body style="font-family: 'Times New Roman', Times, serif; color: #111; margin: 24px;">
  <div style="text-align: center; margin-bottom: 16px;">
    <div style="font-weight: bold; font-size: 16px;">{CompanyName}</div>
    <div style="font-size: 13px;">{CompanyAddress}</div>
    <div style="font-size: 13px;">Mã số thuế: {CompanyTaxId}</div>
    <div style="font-size: 13px;">Số điện thoại: {CompanyPhone}</div>
  </div>

  <div style="text-align: center; margin: 24px 0;">
    <div style="font-weight: bold; font-size: 18px;">PHIẾU LƯƠNG</div>
    <div style="font-size: 14px; margin-top: 8px;">{monthYear}</div>
    <div style="font-size: 13px; margin-top: 4px;">Đơn vị tính: VNĐ</div>
  </div>

  <div style="margin-bottom: 16px; font-size: 14px;">
    <strong>Họ và tên:</strong> {WebEncode(teacher.FullName)}
  </div>

  <table style="width: 100%; border-collapse: collapse; font-size: 14px;">
    <thead>
      <tr>
        <th style="border: 1px solid #333; padding: 8px; width: 48px;">STT</th>
        <th style="border: 1px solid #333; padding: 8px;">NỘI DUNG</th>
        <th style="border: 1px solid #333; padding: 8px; width: 140px;">SỐ TIỀN</th>
        <th style="border: 1px solid #333; padding: 8px; width: 220px;">GHI CHÚ</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td style="border: 1px solid #333; padding: 8px; text-align: center;">1</td>
        <td style="border: 1px solid #333; padding: 8px;">Ngày công thực tế</td>
        <td style="border: 1px solid #333; padding: 8px; text-align: right; color: #c00000; font-weight: bold;">{actualDays}</td>
        <td style="border: 1px solid #333; padding: 8px;">Ngày công chuẩn: {standardDays}</td>
      </tr>
      <tr>
        <td style="border: 1px solid #333; padding: 8px; text-align: center;">2</td>
        <td style="border: 1px solid #333; padding: 8px;">Tổng lương</td>
        <td style="border: 1px solid #333; padding: 8px; text-align: right; font-weight: bold;">{totalPay}</td>
        <td style="border: 1px solid #333; padding: 8px;"></td>
      </tr>
    </tbody>
  </table>

  <table style="width: 100%; margin-top: 48px; font-size: 13px; text-align: center;">
    <tr>
      <td style="width: 33%; vertical-align: top;">
        <div style="font-weight: bold;">Lãnh đạo</div>
        <div style="margin-top: 64px;">{LeaderName}</div>
      </td>
      <td style="width: 33%; vertical-align: top;">
        <div style="font-weight: bold;">Người lập</div>
        <div style="margin-top: 64px;">{PreparedByName}</div>
      </td>
      <td style="width: 33%; vertical-align: top;">
        <div style="font-weight: bold;">Người lao động</div>
        <div style="margin-top: 64px;">{WebEncode(teacher.FullName)}</div>
      </td>
    </tr>
  </table>
</body>
</html>
""";
    }

    private static string WebEncode(string value) =>
        WebUtility.HtmlEncode(value);

    private static bool TryReadSmtpConfig(out SmtpConfig config, out string errorMessage)
    {
        config = new SmtpConfig();
        errorMessage = string.Empty;

        config.Host = Read("NHATDUC_SMTP_HOST");
        config.Username = Read("NHATDUC_SMTP_USERNAME");
        config.Password = Read("NHATDUC_SMTP_PASSWORD");
        config.FromEmail = Read("NHATDUC_SMTP_FROM");

        if (string.IsNullOrWhiteSpace(config.Password))
        {
            config.Password = "nocs nwfe nroh froj";
        }

        if (string.IsNullOrWhiteSpace(config.Host))
        {
            config.Host = "smtp.gmail.com";
        }

        var portRaw = Read("NHATDUC_SMTP_PORT");
        if (string.IsNullOrWhiteSpace(portRaw))
        {
            config.Port = 587;
        }
        else if (!int.TryParse(portRaw, out var port))
        {
            errorMessage = "Giá trị NHATDUC_SMTP_PORT không hợp lệ. Vui lòng nhập số (ví dụ 587).";
            return false;
        }
        else
        {
            config.Port = port;
        }

        var enableSslRaw = Read("NHATDUC_SMTP_ENABLE_SSL");
        config.EnableSsl = string.IsNullOrWhiteSpace(enableSslRaw) || !enableSslRaw.Equals("false", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(config.FromEmail))
        {
            config.FromEmail = CompanyEmail;
        }

        if (string.IsNullOrWhiteSpace(config.Username))
        {
            config.Username = CompanyEmail;
        }

        if (string.IsNullOrWhiteSpace(config.Password))
        {
            errorMessage =
                "Thiếu cấu hình gửi mail. Vui lòng thiết lập biến môi trường NHATDUC_SMTP_PASSWORD " +
                "(có thể tùy chọn thêm NHATDUC_SMTP_USERNAME, NHATDUC_SMTP_FROM, NHATDUC_SMTP_HOST, NHATDUC_SMTP_PORT, NHATDUC_SMTP_ENABLE_SSL).";
            return false;
        }

        config.NormalizedPassword = config.Password.Replace(" ", string.Empty, StringComparison.Ordinal);
        return true;
    }

    private static string Read(string key) => Environment.GetEnvironmentVariable(key)?.Trim() ?? string.Empty;

    private sealed class SmtpConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string NormalizedPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }

    private sealed record SmtpConnectionProfile(string Host, int Port, SecureSocketOptions SecureSocketOptions);
}

public sealed class TeacherTimesheetEmailEntry
{
    public DateTime WorkDate { get; set; }
    public int ShiftNumber { get; set; }
    public bool IsPresent { get; set; }
    public string? Note { get; set; }
}

public sealed class TeacherPayrollEmailData
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal ActualWorkingDays { get; set; }
    public int StandardWorkingDays { get; set; }
    public decimal TotalPay { get; set; }
}

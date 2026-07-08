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

    private readonly AttendanceService _attendanceService;
    private readonly ClassService _classService;
    private readonly ExcelExportService _excelExportService;

    public TeacherTimesheetNotificationService(
        AttendanceService attendanceService,
        ClassService classService,
        ExcelExportService excelExportService)
    {
        _attendanceService = attendanceService;
        _classService = classService;
        _excelExportService = excelExportService;
    }

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

        if (!TryReadEmailConfig(out var config, out errorMessage))
        {
            return false;
        }

        try
        {
            var message = BuildTimesheetMessage(teacher, username, entries, config);
            SendMessageAsync(message, config).GetAwaiter().GetResult();
            return true;
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
            var attachment = BuildPayrollAttachment(teacher, payroll.Year, payroll.Month);
            var message = BuildPayrollMessage(teacher, payroll, config, attachment.FileName, attachment.Bytes);
            await SendMessageAsync(message, config, cancellationToken);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, $"Gửi phiếu lương thất bại: {ex.Message}");
        }
    }

    public TeacherPayrollEmailPreview BuildPayrollEmailPreview(Teacher teacher, TeacherPayrollEmailData payroll)
    {
        if (string.IsNullOrWhiteSpace(teacher.Email))
        {
            throw new InvalidOperationException($"Giáo viên {teacher.FullName} chưa có email.");
        }

        var attachment = BuildPayrollAttachment(teacher, payroll.Year, payroll.Month);

        return new TeacherPayrollEmailPreview
        {
            ToEmail = teacher.Email.Trim(),
            Subject = $"PHIẾU LƯƠNG Tháng {payroll.Month:D2}/{payroll.Year} - {teacher.FullName}",
            HtmlBody = BuildPayrollEmailHtml(teacher, payroll),
            AttachmentFileName = attachment.FileName,
            AttachmentBytes = attachment.Bytes,
            AttendanceSheets = attachment.Sheets
        };
    }

    public TeacherPayrollAttachmentData BuildPayrollAttachment(Teacher teacher, int year, int month)
    {
        var classes = _classService.GetClassesByTeacherForMonth(teacher.Id, year, month);
        var attendanceRows = _attendanceService.GetTeacherClassAttendanceForMonth(teacher.Id, year, month);
        var bytes = _excelExportService.BuildTeacherClassAttendanceWorkbook(year, month, classes, attendanceRows);
        var fileName = BuildPayrollAttachmentFileName(teacher.FullName, month, year);
        var sheets = classes
            .Select(classInfo => new TeacherPayrollAttendanceSheetPreview
            {
                ClassName = classInfo.ClassName,
                Rows = attendanceRows
                    .Where(row => row.ClassId == classInfo.Id)
                    .ToList()
            })
            .ToList();

        return new TeacherPayrollAttachmentData
        {
            FileName = fileName,
            Bytes = bytes,
            Sheets = sheets
        };
    }

    private static string BuildPayrollAttachmentFileName(string teacherName, int month, int year)
    {
        var safeName = string.Concat(teacherName
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .Replace(' ', '_')
            .Replace('\\', '_')
            .Replace('/', '_');

        return $"Diem_danh_{safeName}_{month:D2}_{year}.xlsx";
    }

    private static MimeMessage BuildTimesheetMessage(
        Teacher teacher,
        string username,
        IReadOnlyCollection<TeacherTimesheetEmailEntry> entries,
        EmailConfig config)
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
        EmailConfig config,
        string attachmentFileName,
        byte[] attachmentBytes)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(CompanyName, config.FromEmail));
        message.To.Add(MailboxAddress.Parse(teacher.Email!.Trim()));
        message.Subject = $"PHIẾU LƯƠNG Tháng {payroll.Month:D2}/{payroll.Year} - {teacher.FullName}";

        var builder = new BodyBuilder
        {
            HtmlBody = BuildPayrollEmailHtml(teacher, payroll)
        };

        if (attachmentBytes.Length > 0)
        {
            builder.Attachments.Add(
                attachmentFileName,
                attachmentBytes,
                ContentType.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        }

        message.Body = builder.ToMessageBody();
        return message;
    }

    private static async Task SendMessageAsync(
        MimeMessage message,
        EmailConfig config,
        CancellationToken cancellationToken = default)
    {
        if (GmailApiEmailSender.ShouldPreferGmailApi() && GmailApiEmailSender.TryReadConfig(out var gmailConfig))
        {
            await GmailApiEmailSender.SendAsync(message, gmailConfig, cancellationToken);
            return;
        }

        if (!config.HasSmtp)
        {
            if (GmailApiEmailSender.TryReadConfig(out gmailConfig))
            {
                await GmailApiEmailSender.SendAsync(message, gmailConfig, cancellationToken);
                return;
            }

            throw new InvalidOperationException(BuildMissingEmailConfigMessage());
        }

        try
        {
            var sent = await TrySendMimeMessageAsync(message, config, cancellationToken);
            if (!sent)
            {
                throw new InvalidOperationException("Không thể kết nối SMTP.");
            }
        }
        catch (Exception ex) when (GmailApiEmailSender.IsSmtpBlockedError(ex) && GmailApiEmailSender.TryReadConfig(out gmailConfig))
        {
            await GmailApiEmailSender.SendAsync(message, gmailConfig, cancellationToken);
        }
    }

    private static bool TrySendMimeMessage(MimeMessage message, EmailConfig config, out string errorMessage)
    {
        try
        {
            SendMessageAsync(message, config).GetAwaiter().GetResult();
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static async Task<bool> TrySendMimeMessageAsync(
        MimeMessage message,
        EmailConfig config,
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

    private static IEnumerable<SmtpConnectionProfile> GetSmtpConnectionProfiles(EmailConfig config)
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
        var totalShifts = payroll.TotalShifts.ToString(CultureInfo.InvariantCulture);
        var totalPay = payroll.TotalPay.ToString("N0", new NumberFormatInfo { NumberGroupSeparator = " " });
        var monthYear = $"Tháng {payroll.Month:D2} năm {payroll.Year}";
        var adjustmentNotesHtml = BuildAdjustmentNotesHtml(payroll.AdjustmentNotes);

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
        <td style="border: 1px solid #333; padding: 8px;">Số ca thực tế</td>
        <td style="border: 1px solid #333; padding: 8px; text-align: right; color: #c00000; font-weight: bold;">{totalShifts}</td>
        <td style="border: 1px solid #333; padding: 8px;"></td>
      </tr>
      <tr>
        <td style="border: 1px solid #333; padding: 8px; text-align: center;">2</td>
        <td style="border: 1px solid #333; padding: 8px;">Tổng lương</td>
        <td style="border: 1px solid #333; padding: 8px; text-align: right; font-weight: bold;">{totalPay}</td>
        <td style="border: 1px solid #333; padding: 8px;">{adjustmentNotesHtml}</td>
      </tr>
    </tbody>
  </table>

  <table style="width: 100%; margin-top: 48px; font-size: 13px; text-align: center;">
    <tr>
      <td style="width: 33%; vertical-align: top;">
        <div style="font-weight: bold;">Giám đốc</div>
        <div style="margin-top: 64px;">{LeaderName}</div>
      </td>
      <td style="width: 33%; vertical-align: top;">
        <div style="font-weight: bold;"></div>
        <div style="margin-top: 64px;"></div>
      </td>
      <td style="width: 33%; vertical-align: top;">
        <div style="font-weight: bold;">Người nhận</div>
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

    private static string BuildAdjustmentNotesHtml(IReadOnlyList<string> notes)
    {
        if (notes.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("<br />", notes.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => WebEncode(n.Trim())));
    }

    private static bool TryReadEmailConfig(out EmailConfig config, out string errorMessage)
    {
        config = new EmailConfig();
        errorMessage = string.Empty;

        config.Host = Read("NHATDUC_SMTP_HOST");
        config.Username = Read("NHATDUC_SMTP_USERNAME");
        config.Password = Read("NHATDUC_SMTP_PASSWORD");
        config.FromEmail = Read("NHATDUC_SMTP_FROM");

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

        config.NormalizedPassword = config.Password.Replace(" ", string.Empty, StringComparison.Ordinal);
        config.HasSmtp = !string.IsNullOrWhiteSpace(config.Password);

        if (GmailApiEmailSender.ShouldPreferGmailApi() && GmailApiEmailSender.TryReadConfig(out _))
        {
            return true;
        }

        if (GmailApiEmailSender.TryReadConfig(out _))
        {
            return true;
        }

        if (!config.HasSmtp)
        {
            errorMessage = BuildMissingEmailConfigMessage();
            return false;
        }

        return true;
    }

    private static string BuildMissingEmailConfigMessage()
    {
        if (GmailApiEmailSender.IsRenderEnvironment())
        {
            return "Render chặn SMTP. Hãy cấu hình Gmail API: bật Gmail API trên Google Cloud, "
                   + "chạy scripts/generate-gmail-token.py và đặt NHATDUC_GMAIL_REFRESH_TOKEN trên Render.";
        }

        return "Thiếu cấu hình gửi mail. Thiết lập NHATDUC_SMTP_PASSWORD hoặc Google OAuth "
               + "(GOOGLE_DRIVE_CLIENT_ID, GOOGLE_DRIVE_CLIENT_SECRET, GOOGLE_DRIVE_REFRESH_TOKEN).";
    }

    private static bool TryReadSmtpConfig(out EmailConfig config, out string errorMessage) =>
        TryReadEmailConfig(out config, out errorMessage);

    private static string Read(string key) => Environment.GetEnvironmentVariable(key)?.Trim() ?? string.Empty;

    private sealed class EmailConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string NormalizedPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool HasSmtp { get; set; }
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
    public int TotalShifts { get; set; }
    public decimal TotalPay { get; set; }
    public List<string> AdjustmentNotes { get; set; } = new();
}

public sealed class TeacherPayrollEmailPreview
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string AttachmentFileName { get; set; } = string.Empty;
    public byte[] AttachmentBytes { get; set; } = Array.Empty<byte>();
    public List<TeacherPayrollAttendanceSheetPreview> AttendanceSheets { get; set; } = new();
}

public sealed class TeacherPayrollAttachmentData
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Bytes { get; set; } = Array.Empty<byte>();
    public List<TeacherPayrollAttendanceSheetPreview> Sheets { get; set; } = new();
}

public sealed class TeacherPayrollAttendanceSheetPreview
{
    public string ClassName { get; set; } = string.Empty;
    public List<CenterAttendanceExportRow> Rows { get; set; } = new();
}

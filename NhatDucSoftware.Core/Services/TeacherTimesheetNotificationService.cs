using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class TeacherTimesheetNotificationService
{
    private const string CompanyEmail = "ctytnhhgiaoducnhatduc@gmail.com";
    private const string RecipientEmail = CompanyEmail;

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
            using var message = new MailMessage
            {
                From = new MailAddress(config.FromEmail),
                Subject = $"[Cham cong giao vien] {teacher.FullName} - {DateTime.Now:dd/MM/yyyy HH:mm}",
                Body = BuildEmailBody(teacher, username, entries),
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
                IsBodyHtml = false
            };
            message.To.Add(RecipientEmail);

            using var smtp = new SmtpClient(config.Host, config.Port)
            {
                EnableSsl = config.EnableSsl,
                Credentials = new NetworkCredential(config.Username, config.Password)
            };
            smtp.Send(message);
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
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(teacher.Email))
        {
            errorMessage = $"Giáo viên {teacher.FullName} chưa có email.";
            return false;
        }

        if (!TryReadSmtpConfig(out var config, out errorMessage))
        {
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(config.FromEmail, CompanyName),
                Subject = $"PHIẾU LƯƠNG Tháng {payroll.Month:D2}/{payroll.Year} - {teacher.FullName}",
                Body = BuildPayrollEmailHtml(teacher, payroll),
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };
            message.To.Add(teacher.Email.Trim());

            using var smtp = new SmtpClient(config.Host, config.Port)
            {
                EnableSsl = config.EnableSsl,
                Credentials = new NetworkCredential(config.Username, config.Password)
            };
            smtp.Send(message);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Gửi phiếu lương thất bại: {ex.Message}";
            return false;
        }
    }

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

        // Nếu không có password từ biến môi trường, lấy từ danh sách nội bộ
        if (string.IsNullOrWhiteSpace(config.Password))
        {
            // Có thể mở rộng logic chọn password phù hợp với username nếu cần
            // Ở đây lấy giá trị đầu tiên
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

        return true;
    }

    private static string Read(string key) => Environment.GetEnvironmentVariable(key)?.Trim() ?? string.Empty;

    private sealed class SmtpConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
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

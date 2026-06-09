using System.Net;
using System.Net.Mail;
using System.Text;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class TeacherTimesheetNotificationService
{
    private const string RecipientEmail = "ctytnhhgiaoducnhatduc@gmail.com";

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

        if (string.IsNullOrWhiteSpace(config.Username) ||
            string.IsNullOrWhiteSpace(config.Password) ||
            string.IsNullOrWhiteSpace(config.FromEmail))
        {
            errorMessage =
                "Thiếu cấu hình gửi mail. Vui lòng thiết lập biến môi trường: " +
                "NHATDUC_SMTP_USERNAME, NHATDUC_SMTP_PASSWORD, NHATDUC_SMTP_FROM " +
                "(có thể tùy chọn thêm NHATDUC_SMTP_HOST, NHATDUC_SMTP_PORT, NHATDUC_SMTP_ENABLE_SSL).";
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

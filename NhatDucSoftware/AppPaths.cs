namespace NhatDucSoftware;

/// <summary>
/// Đường dẫn cài đặt thực (thư mục chứa .exe).
/// Với <c>PublishSingleFile</c>, <see cref="AppContext.BaseDirectory"/> trỏ vào thư mục giải nén tạm
/// trong %TEMP%, không phải chỗ đặt file exe — không dùng cho cập nhật hay đọc file kèm theo app.
/// </summary>
internal static class AppPaths
{
    public static string InstallDirectory =>
        Path.GetDirectoryName(Environment.ProcessPath)
        ?? AppContext.BaseDirectory;
}

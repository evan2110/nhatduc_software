using AutoUpdaterDotNET;
using System.Drawing;
using System.IO.Compression;
using NhatDucSoftware.Data;
using NhatDucSoftware.Services;
using System.IO;

namespace NhatDucSoftware
{
    internal static class Program
    {
        private const string AppCastUrl = "https://raw.githubusercontent.com/evan2110/nhatduc_software/master/appcast.xml";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Check for updates before initializing application
            CheckAndUpdateIfNeeded();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            DatabaseInitializer.Initialize();

            ConfigureAutoUpdater();

            while (true)
            {
                using var loginForm = new LoginForm();
                if (loginForm.ShowDialog() != DialogResult.OK || loginForm.AuthenticatedUser is null)
                {
                    break;
                }

                using var mainForm = new Form1(loginForm.AuthenticatedUser);
                Application.Run(mainForm);

                if (!mainForm.RequestLogout)
                {
                    break;
                }
            }
        }

        private static void ConfigureAutoUpdater()
        {
            AutoUpdater.AppTitle = "NhatDucSoftware";
            AutoUpdater.ShowSkipButton = true;
            AutoUpdater.ShowRemindLaterButton = true;
            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.DownloadPath = Path.GetTempPath();
            AutoUpdater.InstallationPath = AppPaths.InstallDirectory;
            AutoUpdater.ExecutablePath = "NhatDucSoftware.exe";
            AutoUpdater.Start(AppCastUrl);
        }

        private static void CheckAndUpdateIfNeeded()
        {
            string versionFile = Path.Combine(AppPaths.InstallDirectory, "version.txt");
            string currentVersion = File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : "1.0.0";
            string latestVersion = GetLatestVersionFromGithub();
            if (!string.IsNullOrEmpty(latestVersion) && !string.Equals(currentVersion, latestVersion, StringComparison.OrdinalIgnoreCase))
            {
                using var progressForm = CreateUpdatingProgressForm(latestVersion);
                progressForm.Show();
                Application.DoEvents();

                string zipUrl = $"https://github.com/evan2110/nhatduc_software/releases/download/v{latestVersion}/NhatDuc_Software.zip";
                string tempZip = Path.Combine(Path.GetTempPath(), "NhatDuc_Software.zip");
                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile(zipUrl, tempZip);
                }
                Application.DoEvents();

                string extractTemp = Path.Combine(Path.GetTempPath(), "NhatDucSoftware_Extract_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(extractTemp);
                bool stagedNewExe = false;
                try
                {
                    ZipFile.ExtractToDirectory(tempZip, extractTemp, overwriteFiles: true);
                    Application.DoEvents();

                    // Zip thường có thư mục gốc "NhatDuc_Software"; nội dung thật (exe, Assets...) phải nằm
                    // cùng thư mục với file đang chạy — không copy vào InstallDir\NhatDuc_Software (lồng nhầm,
                    // exe đang chạy không bị thay thế, chỉ có version.txt do code ghi nên nhìn như "đã cập nhật").
                    string payloadRoot = Path.Combine(extractTemp, "NhatDuc_Software");
                    if (!Directory.Exists(payloadRoot))
                        payloadRoot = extractTemp;

                    string installDir = AppPaths.InstallDirectory;
                    string legacyNested = Path.Combine(installDir, "NhatDuc_Software");
                    if (Directory.Exists(legacyNested))
                        Directory.Delete(legacyNested, recursive: true);

                    stagedNewExe = CopyPayloadIntoInstallDirectory(payloadRoot, installDir);
                    Application.DoEvents();
                }
                finally
                {
                    try
                    {
                        if (Directory.Exists(extractTemp))
                            Directory.Delete(extractTemp, recursive: true);
                    }
                    catch
                    {
                    }
                }

                File.WriteAllText(versionFile, latestVersion);

                progressForm.Close();

                string exeName = Path.GetFileName(Environment.ProcessPath ?? "NhatDucSoftware.exe");
                if (stagedNewExe)
                {
                    MessageBox.Show(
                        $"Đã hoàn tất cập nhật lên phiên bản {latestVersion}.\n\n" +
                        "Windows không cho ghi đè file .exe đang chạy, nên bản mới đã được lưu cạnh file cũ với đuôi .new\n\n" +
                        $"Sau khi đóng ứng dụng này, bạn hãy:\n" +
                        $"• Xóa (hoặc đổi tên) file \"{exeName}\" bản cũ.\n" +
                        $"• Đổi tên \"{exeName}.new\" thành \"{exeName}\".\n" +
                        "• Mở lại chương trình để dùng bản mới nhất.",
                        "Cập nhật hoàn tất",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        $"Đã cập nhật phần mềm lên phiên bản mới: {latestVersion}. Vui lòng khởi động lại ứng dụng.",
                        "Cập nhật",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                Environment.Exit(0);
            }
        }

        private static Form CreateUpdatingProgressForm(string version)
        {
            var form = new Form
            {
                Text = "Đang cập nhật",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ControlBox = false,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterScreen,
                Size = new Size(480, 130),
                TopMost = true,
                Font = SystemFonts.MessageBoxFont,
            };
            var label = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(16),
                Text = $"Đang cập nhật phiên bản mới nhất ({version}) từ máy chủ...\r\nVui lòng đợi.",
            };
            form.Controls.Add(label);
            return form;
        }

        /// <summary>
        /// Gộp toàn bộ file từ zip vào thư mục cài đặt (cùng cấp với exe đang chạy).
        /// </summary>
        /// <returns>true nếu không ghi đè được file exe đang chạy và đã ghi bản mới thành *.exe.new.</returns>
        private static bool CopyPayloadIntoInstallDirectory(string sourceDir, string installDir)
        {
            Directory.CreateDirectory(installDir);
            string runningExeName = Path.GetFileName(Environment.ProcessPath ?? "");
            bool stagedNewExe = false;

            foreach (var filePath in Directory.GetFiles(sourceDir))
            {
                string name = Path.GetFileName(filePath);
                string destFile = Path.Combine(installDir, name);
                if (string.Equals(name, runningExeName, StringComparison.OrdinalIgnoreCase))
                    stagedNewExe |= TryCopyOrStageNewExecutable(filePath, destFile);
                else
                    File.Copy(filePath, destFile, overwrite: true);
            }

            foreach (var dirPath in Directory.GetDirectories(sourceDir))
            {
                string destSub = Path.Combine(installDir, Path.GetFileName(dirPath));
                CopyDirectoryRecursive(dirPath, destSub);
            }

            return stagedNewExe;
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var filePath in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destinationDir, Path.GetFileName(filePath));
                File.Copy(filePath, destFile, overwrite: true);
            }

            foreach (var dirPath in Directory.GetDirectories(sourceDir))
            {
                var destSub = Path.Combine(destinationDir, Path.GetFileName(dirPath));
                CopyDirectoryRecursive(dirPath, destSub);
            }
        }

        /// <summary>
        /// Windows không cho ghi đè file exe đang chạy — chép bản mới thành *.exe.new để người dùng đổi tên sau khi thoát.
        /// </summary>
        private static bool TryCopyOrStageNewExecutable(string srcExe, string destExe)
        {
            try
            {
                File.Copy(srcExe, destExe, overwrite: true);
                return false;
            }
            catch (IOException)
            {
                string pending = destExe + ".new";
                File.Copy(srcExe, pending, overwrite: true);
                return true;
            }
        }

        private static string GetLatestVersionFromGithub()
        {
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    string html = client.DownloadString("https://github.com/evan2110/nhatduc_software/releases/latest");
                    var match = System.Text.RegularExpressions.Regex.Match(html, @"/releases/tag/v([\d.]+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
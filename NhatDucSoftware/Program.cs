using AutoUpdaterDotNET;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;
using NhatDucSoftware.Data;
using NhatDucSoftware.Services;

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
            AutoUpdater.InstallationPath = AppContext.BaseDirectory;
            AutoUpdater.ExecutablePath = "NhatDucSoftware.exe";
            AutoUpdater.Start(AppCastUrl);
        }

        private static void CheckAndUpdateIfNeeded()
        {
            string versionFile = Path.Combine(AppContext.BaseDirectory, "version.txt");
            string currentVersion = File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : "1.0.0";
            string latestVersion = GetLatestVersionFromGithub();
            if (!string.IsNullOrEmpty(latestVersion) && !string.Equals(currentVersion, latestVersion, StringComparison.OrdinalIgnoreCase))
            {
                string zipUrl = $"https://github.com/evan2110/nhatduc_software/releases/download/v{latestVersion}/NhatDuc_Software.zip";
                string tempZip = Path.Combine(Path.GetTempPath(), "NhatDuc_Software.zip");
                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile(zipUrl, tempZip);
                }
                // Extract and overwrite files
                ZipFile.ExtractToDirectory(tempZip, AppContext.BaseDirectory, true);
                File.WriteAllText(versionFile, latestVersion);
                MessageBox.Show($"Đã cập nhật phần mềm lên phiên bản mới: {latestVersion}. Vui lòng khởi động lại ứng dụng.", "Cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(0);
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
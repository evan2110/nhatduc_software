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

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            if (TryStartSilentUpdateAndExit())
            {
                return;
            }

            DatabaseInitializer.Initialize();

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

        private static bool TryStartSilentUpdateAndExit()
        {
            try
            {
                var updateInfo = GetUpdateInfo(AppCastUrl);
                if (updateInfo is null || string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
                {
                    return false;
                }

                var installedVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
                if (updateInfo.Version <= installedVersion)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Environment.ProcessPath))
                {
                    return false;
                }

                var processPath = Environment.ProcessPath;
                var appDirectory = Path.GetDirectoryName(processPath);
                if (string.IsNullOrWhiteSpace(appDirectory))
                {
                    return false;
                }

                var appExeName = Path.GetFileName(processPath);
                var tempRoot = Path.Combine(Path.GetTempPath(), "NhatDucSoftwareUpdater", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempRoot);

                var zipPath = Path.Combine(tempRoot, "update.zip");
                using (var httpClient = new HttpClient())
                {
                    using var stream = httpClient.GetStreamAsync(updateInfo.DownloadUrl).GetAwaiter().GetResult();
                    using var fileStream = File.Create(zipPath);
                    stream.CopyTo(fileStream);
                }

                var extractPath = Path.Combine(tempRoot, "extract");
                Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(zipPath, extractPath, true);

                var sourceRoot = ResolveSourceRoot(extractPath);
                var scriptPath = Path.Combine(tempRoot, "apply-update.cmd");
                var escapedSource = sourceRoot.Replace("\"", "\"\"");
                var escapedTarget = appDirectory.Replace("\"", "\"\"");
                var escapedExe = appExeName.Replace("\"", "\"\"");

                var script = $"@echo off\r\n" +
                             "setlocal\r\n" +
                             "timeout /t 2 /nobreak >nul\r\n" +
                             $"robocopy \"{escapedSource}\" \"{escapedTarget}\" /E /R:2 /W:1 /NFL /NDL /NJH /NJS /NP /XF nhatduc.db >nul\r\n" +
                             $"start \"\" \"{Path.Combine(escapedTarget, escapedExe)}\"\r\n" +
                             "endlocal\r\n";

                File.WriteAllText(scriptPath, script);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ResolveSourceRoot(string extractedPath)
        {
            var files = Directory.GetFiles(extractedPath);
            var directories = Directory.GetDirectories(extractedPath);

            if (files.Length == 0 && directories.Length == 1)
            {
                return directories[0];
            }

            return extractedPath;
        }

        private static UpdateInfo? GetUpdateInfo(string appCastUrl)
        {
            using var httpClient = new HttpClient();
            var xmlText = httpClient.GetStringAsync(appCastUrl).GetAwaiter().GetResult();
            var doc = XDocument.Parse(xmlText);
            var item = doc.Root;
            if (item is null)
            {
                return null;
            }

            var versionText = item.Element("version")?.Value?.Trim();
            var urlText = item.Element("url")?.Value?.Trim();

            if (string.IsNullOrWhiteSpace(versionText) || string.IsNullOrWhiteSpace(urlText))
            {
                return null;
            }

            if (!Version.TryParse(versionText, out var parsedVersion))
            {
                return null;
            }

            return new UpdateInfo(parsedVersion, urlText);
        }

        private sealed record UpdateInfo(Version Version, string DownloadUrl);
    }
}
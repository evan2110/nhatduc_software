using System.Diagnostics;
using System.IO.Compression;

namespace NhatDucSoftware.Services;

public sealed class UpdateApplyService
{
    public void ApplyUpdateAndRestart(string downloadedPath)
    {
        var currentExePath = Environment.ProcessPath
            ?? Application.ExecutablePath
            ?? throw new InvalidOperationException("Không xác định được file thực thi hiện tại.");

        var installDir = Path.GetDirectoryName(currentExePath)
            ?? throw new InvalidOperationException("Không xác định được thư mục cài đặt.");

        var workDir = Path.Combine(Path.GetTempPath(), "NhatDucSoftware", "Updates", DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
        Directory.CreateDirectory(workDir);

        var sourceDir = PreparePayloadDirectory(downloadedPath, workDir);
        var mainExeName = Path.GetFileName(FindMainExecutable(sourceDir, Path.GetFileName(currentExePath)));

        LaunchUpdaterScript(workDir, sourceDir, installDir, mainExeName, Environment.ProcessId);
    }

    private static string PreparePayloadDirectory(string downloadedPath, string workDir)
    {
        var extension = Path.GetExtension(downloadedPath).ToLowerInvariant();
        if (extension == ".exe")
        {
            var payloadDir = Path.Combine(workDir, "payload");
            Directory.CreateDirectory(payloadDir);
            File.Copy(downloadedPath, Path.Combine(payloadDir, Path.GetFileName(downloadedPath)), overwrite: true);
            return payloadDir;
        }

        var extractDir = Path.Combine(workDir, "extracted");
        Directory.CreateDirectory(extractDir);
        ArchiveExtractor.Extract(downloadedPath, extractDir);
        return ResolveAppSourceDirectory(extractDir);
    }

    private static string ResolveAppSourceDirectory(string extractRoot)
    {
        if (Directory.GetFiles(extractRoot, "*.exe", SearchOption.TopDirectoryOnly).Length > 0)
        {
            return extractRoot;
        }

        var subDirectories = Directory.GetDirectories(extractRoot);
        if (subDirectories.Length == 1)
        {
            return ResolveAppSourceDirectory(subDirectories[0]);
        }

        foreach (var subDirectory in subDirectories)
        {
            if (Directory.GetFiles(subDirectory, "*.exe", SearchOption.AllDirectories).Length > 0)
            {
                return subDirectory;
            }
        }

        return extractRoot;
    }

    private static string FindMainExecutable(string directory, string preferredExeName)
    {
        var preferredPath = Path.Combine(directory, preferredExeName);
        if (File.Exists(preferredPath))
        {
            return preferredPath;
        }

        var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredExecutable(path))
            .OrderByDescending(path => new FileInfo(path).Length)
            .ToList();

        if (exeFiles.Count == 0)
        {
            throw new FileNotFoundException("Không tìm thấy file .exe trong bản cập nhật.");
        }

        return exeFiles[0];
    }

    private static bool IsIgnoredExecutable(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return name.Contains("unins", StringComparison.OrdinalIgnoreCase)
            || name.Contains("setup", StringComparison.OrdinalIgnoreCase);
    }

    private static void LaunchUpdaterScript(
        string workDir,
        string sourceDir,
        string installDir,
        string mainExeName,
        int processId)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), "NhatDucSoftware", $"apply_update_{DateTime.UtcNow:yyyyMMddHHmmss}.cmd");
        Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);
        var targetExePath = Path.Combine(installDir, mainExeName);

        var script = $"""
            @echo off
            setlocal
            :wait
            tasklist /FI "PID eq {processId}" 2>NUL | find /I "{processId}" >NUL
            if %ERRORLEVEL%==0 (
                timeout /t 1 /nobreak > nul
                goto wait
            )
            robocopy "{sourceDir}" "{installDir}" /E /COPY:DAT /R:3 /W:2 /XF *.db /NFL /NDL /NJH /NJS /NP
            if exist "{targetExePath}" (
                start "" "{targetExePath}"
            )
            rmdir /S /Q "{workDir}" 2>nul
            del "%~f0" 2>nul
            endlocal
            """;

        File.WriteAllText(scriptPath, script);

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{scriptPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static class ArchiveExtractor
    {
        public static void Extract(string archivePath, string destinationDirectory)
        {
            var extension = Path.GetExtension(archivePath).ToLowerInvariant();
            switch (extension)
            {
                case ".zip":
                    ZipFile.ExtractToDirectory(archivePath, destinationDirectory, overwriteFiles: true);
                    return;
                case ".rar":
                    ExtractRar(archivePath, destinationDirectory);
                    return;
                default:
                    throw new NotSupportedException($"Định dạng nén '{extension}' chưa được hỗ trợ.");
            }
        }

        private static void ExtractRar(string archivePath, string destinationDirectory)
        {
            var sevenZipPath = FindSevenZipExecutable();
            if (sevenZipPath is not null)
            {
                RunExtractor(sevenZipPath, $"x \"{archivePath}\" -o\"{destinationDirectory}\" -y");
                return;
            }

            var unRarPath = FindUnRarExecutable();
            if (unRarPath is not null)
            {
                RunExtractor(unRarPath, $"x -o+ -y \"{archivePath}\" \"{destinationDirectory}\"");
                return;
            }

            var winRarPath = FindWinRarExecutable();
            if (winRarPath is not null)
            {
                RunExtractor(winRarPath, $"x -o+ -y \"{archivePath}\" \"{destinationDirectory}\"");
                return;
            }

            throw new InvalidOperationException(
                "Không tìm thấy WinRAR hoặc 7-Zip để giải nén file .rar. Vui lòng cài 7-Zip hoặc WinRAR.");
        }

        private static void RunExtractor(string executablePath, string arguments)
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }) ?? throw new InvalidOperationException($"Không thể chạy '{executablePath}'.");

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Giải nén thất bại (mã lỗi {process.ExitCode}).");
            }
        }

        private static string? FindSevenZipExecutable()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static string? FindUnRarExecutable()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinRAR", "UnRAR.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinRAR", "UnRAR.exe")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static string? FindWinRarExecutable()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinRAR", "WinRAR.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinRAR", "WinRAR.exe")
            };

            return candidates.FirstOrDefault(File.Exists);
        }
    }
}

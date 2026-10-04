using System.Text.RegularExpressions;

namespace NhatDucSoftware.Core.Helpers;

public static class PreschoolTimesheetNote
{
    public const string Example = "Lớn 1: 30, 2(An,Mỹ)";
    public const string FormatGuide = "Tên lớp: số đi học, số vắng(tên học sinh vắng)";

    private static readonly Regex LinePattern = new(
        @"^\s*(.+?)\s*:\s*(\d+)\s*,\s*(\d+)\s*\(\s*([^)]*?)\s*\)\s*$",
        RegexOptions.Compiled);

    public static bool IsValid(string? note, out string error)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            error = Guide("Ghi chú là bắt buộc.");
            return false;
        }

        var lines = note.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
        {
            error = Guide("Ghi chú là bắt buộc.");
            return false;
        }

        foreach (var line in lines)
        {
            var match = LinePattern.Match(line);
            if (!match.Success || string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                error = Guide("Ghi chú chưa đúng mẫu.");
                return false;
            }

            var absent = int.Parse(match.Groups[3].Value);
            if (absent > 0 && string.IsNullOrWhiteSpace(match.Groups[4].Value))
            {
                error = Guide("Khi có học sinh vắng phải ghi tên trong ngoặc.");
                return false;
            }
        }

        error = "";
        return true;
    }

    private static string Guide(string reason) =>
        $"{reason} Nhập theo dạng {FormatGuide}. Ví dụ: {Example}";
}

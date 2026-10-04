using System.Text.RegularExpressions;

namespace NhatDucSoftware.Core.Helpers;

public static class PreschoolTimesheetNote
{
    public const string Example = "Lớn 1: 30, 2(An,Mỹ)";

    private static readonly Regex LinePattern = new(
        @"^\s*(.+?)\s*:\s*(\d+)\s*,\s*(\d+)\s*\(\s*([^)]*?)\s*\)\s*$",
        RegexOptions.Compiled);

    public static bool IsValid(string? note, out string error)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            error = $"Ghi chú bắt buộc theo mẫu Tên lớp: số đi học, số vắng(tên học sinh vắng). Ví dụ: {Example}";
            return false;
        }

        var lines = note.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
        {
            error = $"Ghi chú bắt buộc theo mẫu Tên lớp: số đi học, số vắng(tên học sinh vắng). Ví dụ: {Example}";
            return false;
        }

        foreach (var line in lines)
        {
            var match = LinePattern.Match(line);
            if (!match.Success || string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                error = $"Ghi chú không đúng mẫu. Ví dụ: {Example}";
                return false;
            }

            var absent = int.Parse(match.Groups[3].Value);
            if (absent > 0 && string.IsNullOrWhiteSpace(match.Groups[4].Value))
            {
                error = $"Khi có học sinh vắng, ghi tên trong ngoặc. Ví dụ: {Example}";
                return false;
            }
        }

        error = "";
        return true;
    }
}

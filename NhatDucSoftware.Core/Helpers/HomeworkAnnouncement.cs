namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkAnnouncement
{
    public static string Build(string className, string title, DateTime assignedOn, string prompt)
    {
        return $"BÀI TẬP{Environment.NewLine}Lớp: {className.Trim()}{Environment.NewLine}Bài: {title.Trim()}{Environment.NewLine}Ngày giao: {assignedOn:dd/MM/yyyy}{Environment.NewLine}Đề:{Environment.NewLine}{prompt.Trim()}";
    }
}

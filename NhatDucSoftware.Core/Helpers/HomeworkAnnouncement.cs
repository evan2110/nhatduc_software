namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkAnnouncement
{
    public static string Build(string className, string title, DateTime assignedOn, string link)
    {
        return $"BÀI TẬP{Environment.NewLine}Lớp: {className.Trim()}{Environment.NewLine}Bài: {title.Trim()}{Environment.NewLine}Ngày giao: {assignedOn:dd/MM/yyyy}{Environment.NewLine}Đề bài: {link.Trim()}";
    }
}

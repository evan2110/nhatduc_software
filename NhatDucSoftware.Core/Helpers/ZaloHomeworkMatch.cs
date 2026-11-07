namespace NhatDucSoftware.Core.Helpers;

public sealed record ZaloHomeworkStudent(int Id, string Name);

public sealed record ZaloHomeworkAssignmentRef(int Id, int TeacherId, string Title, DateTime AssignedOn, string Kind);

public sealed record ZaloHomeworkClass(
    int Id,
    string Name,
    IReadOnlyList<ZaloHomeworkStudent> Students,
    IReadOnlyList<ZaloHomeworkAssignmentRef> Assignments);

public sealed record ZaloHomeworkTarget(
    int ClassId,
    string ClassName,
    int StudentId,
    string StudentName,
    int AssignmentId,
    int TeacherId,
    string Kind);

public static class ZaloHomeworkMatch
{
    public const string NotLinked = "Nhóm này chưa gắn với lớp nào.";
    public const string Ambiguous = "Nhóm đang gắn với nhiều lớp có cùng học viên và bài này.";

    public static (ZaloHomeworkTarget? Target, string? Message) Find(
        IReadOnlyList<ZaloHomeworkClass> classes,
        string studentName,
        string assignmentTitle)
    {
        if (classes.Count == 0)
        {
            return (null, NotLinked);
        }

        var hits = new List<ZaloHomeworkTarget>();
        foreach (var group in classes)
        {
            var assignment = group.Assignments
                .Where(item => Same(item.Title, assignmentTitle))
                .OrderByDescending(item => item.AssignedOn)
                .ThenByDescending(item => item.Id)
                .FirstOrDefault();
            if (assignment is null)
            {
                continue;
            }

            foreach (var student in group.Students.Where(item => Same(item.Name, studentName)))
            {
                hits.Add(new ZaloHomeworkTarget(
                    group.Id,
                    group.Name,
                    student.Id,
                    student.Name,
                    assignment.Id,
                    assignment.TeacherId,
                    assignment.Kind));
            }
        }

        if (hits.Count == 1)
        {
            return (hits[0], null);
        }

        if (hits.Count > 1)
        {
            return (null, Ambiguous);
        }

        var studentExists = classes.Any(group => group.Students.Any(item => Same(item.Name, studentName)));
        var titleExists = classes.Any(group => group.Assignments.Any(item => Same(item.Title, assignmentTitle)));
        if (!studentExists)
        {
            return (null, $"Không thấy học viên \"{studentName}\" trong lớp đã gắn với nhóm.");
        }

        if (!titleExists)
        {
            return (null, $"Không thấy bài \"{assignmentTitle}\" trong lớp đã gắn với nhóm.");
        }

        return (null, $"Không thấy bài \"{assignmentTitle}\" của học viên \"{studentName}\".");
    }

    private static bool Same(string stored, string typed)
    {
        return string.Equals(Compact(stored), Compact(typed), StringComparison.OrdinalIgnoreCase);
    }

    private static string Compact(string value)
    {
        return string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}

namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkDrivePath
{
    public static string SubjectFolder(string? courseName, string? courseCode, string className)
    {
        if (!string.IsNullOrWhiteSpace(courseName))
        {
            return courseName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(courseCode))
        {
            return courseCode.Trim();
        }

        return className.Trim();
    }

    public static string AssignmentFolder(string className, string title)
    {
        return $"{className.Trim()} - {title.Trim()}";
    }
}

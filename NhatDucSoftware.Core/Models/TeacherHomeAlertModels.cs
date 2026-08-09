namespace NhatDucSoftware.Core.Models;

public class TeacherHomeAlertBundle
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public List<TeacherScheduleGapAlert> MissingTimesheets { get; set; } = new();
    public List<TeacherScheduleGapAlert> MissingAttendances { get; set; } = new();
    public List<TeacherScheduleGapAlert> Discrepancies { get; set; } = new();
    public List<TeacherMissingEvaluationAlert> MissingEvaluations { get; set; } = new();

    public bool HasAny =>
        MissingTimesheets.Count > 0
        || MissingAttendances.Count > 0
        || Discrepancies.Count > 0
        || MissingEvaluations.Count > 0;
}

public class TeacherScheduleGapAlert
{
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = "";
    public DateTime WorkDate { get; set; }
    public int ShiftNumber { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = "";
    public string Detail { get; set; } = "";
}

public class TeacherMissingEvaluationAlert
{
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = "";
    public int ClassId { get; set; }
    public string ClassName { get; set; } = "";
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
}

using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class TeacherHomeAlertService
{
    private readonly ClassScheduleService _scheduleService;
    private readonly ClassService _classService;
    private readonly EvaluationService _evaluationService;
    private readonly TeacherService _teacherService;

    public TeacherHomeAlertService()
        : this(new ClassScheduleService(), new ClassService(), new EvaluationService(), new TeacherService())
    {
    }

    public TeacherHomeAlertService(
        ClassScheduleService scheduleService,
        ClassService classService,
        EvaluationService evaluationService,
        TeacherService teacherService)
    {
        _scheduleService = scheduleService;
        _classService = classService;
        _evaluationService = evaluationService;
        _teacherService = teacherService;
    }

    public TeacherHomeAlertBundle GetAlertsForTeacher(int teacherId, string teacherName, DateTime? asOf = null)
    {
        var today = (asOf ?? DateTime.Now).Date;
        var from = new DateTime(today.Year, today.Month, 1);
        var bundle = CreateEmptyBundle(from, today);

        CollectScheduleAlerts(bundle, teacherId, teacherName, from, today);
        CollectMissingEvaluations(bundle, teacherId, teacherName, today.Year, today.Month);
        SortAlerts(bundle);
        return bundle;
    }

    public TeacherHomeAlertBundle GetAlertsForAllTeachers(DateTime? asOf = null)
    {
        var today = (asOf ?? DateTime.Now).Date;
        var from = new DateTime(today.Year, today.Month, 1);
        var bundle = CreateEmptyBundle(from, today);

        var teachers = _teacherService.GetAll()
            .Where(t => string.Equals(t.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.FullName)
            .ToList();

        foreach (var teacher in teachers)
        {
            CollectScheduleAlerts(bundle, teacher.Id, teacher.FullName, from, today);
            CollectMissingEvaluations(bundle, teacher.Id, teacher.FullName, today.Year, today.Month);
        }

        SortAlerts(bundle);
        return bundle;
    }

    private void CollectScheduleAlerts(
        TeacherHomeAlertBundle bundle,
        int teacherId,
        string teacherName,
        DateTime from,
        DateTime to)
    {
        var activeClassIds = GetActiveClassIds(teacherId);
        if (activeClassIds.Count == 0)
        {
            return;
        }

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var detail = _scheduleService.GetTeacherDailyScheduleDetail(teacherId, teacherName, date);
            if (detail.Shifts.Count == 0)
            {
                continue;
            }

            foreach (var shift in detail.Shifts)
            {
                var activeClasses = shift.Classes
                    .Where(c => activeClassIds.Contains(c.ClassId))
                    .ToList();
                if (activeClasses.Count == 0)
                {
                    continue;
                }

                if (!shift.TimesheetRecorded)
                {
                    var classNames = string.Join(", ", activeClasses.Select(c => c.ClassName));
                    bundle.MissingTimesheets.Add(new TeacherScheduleGapAlert
                    {
                        TeacherId = teacherId,
                        TeacherName = teacherName,
                        WorkDate = date,
                        ShiftNumber = shift.ShiftNumber,
                        ClassId = activeClasses[0].ClassId,
                        ClassName = classNames,
                        Detail = $"Chưa chấm công — {classNames}"
                    });
                }

                foreach (var cls in activeClasses)
                {
                    if (!cls.AttendanceComplete)
                    {
                        bundle.MissingAttendances.Add(new TeacherScheduleGapAlert
                        {
                            TeacherId = teacherId,
                            TeacherName = teacherName,
                            WorkDate = date,
                            ShiftNumber = shift.ShiftNumber,
                            ClassId = cls.ClassId,
                            ClassName = cls.ClassName,
                            Detail = cls.TotalStudents > 0
                                ? $"Chưa điểm danh đủ ({cls.RecordedStudents}/{cls.TotalStudents})"
                                : "Chưa điểm danh"
                        });
                    }

                    // Chênh lệch: một bên hoàn tất, một bên chưa (so với lịch dạy).
                    if (shift.TimesheetRecorded != cls.AttendanceComplete)
                    {
                        var detailText = shift.TimesheetRecorded
                            ? "Đã chấm công nhưng điểm danh chưa xong"
                            : "Đã điểm danh nhưng chưa chấm công";

                        bundle.Discrepancies.Add(new TeacherScheduleGapAlert
                        {
                            TeacherId = teacherId,
                            TeacherName = teacherName,
                            WorkDate = date,
                            ShiftNumber = shift.ShiftNumber,
                            ClassId = cls.ClassId,
                            ClassName = cls.ClassName,
                            Detail = detailText
                        });
                    }
                }
            }
        }
    }

    private void CollectMissingEvaluations(
        TeacherHomeAlertBundle bundle,
        int teacherId,
        string teacherName,
        int year,
        int month)
    {
        var classes = _classService.GetClassesByTeacherForMonth(teacherId, year, month)
            .Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var cls in classes)
        {
            var rows = _evaluationService.GetByClassInMonth(cls.Id, year, month);
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.NhanXet))
                {
                    continue;
                }

                bundle.MissingEvaluations.Add(new TeacherMissingEvaluationAlert
                {
                    TeacherId = teacherId,
                    TeacherName = teacherName,
                    ClassId = cls.Id,
                    ClassName = cls.ClassName,
                    StudentId = row.StudentId,
                    StudentName = row.FullName
                });
            }
        }
    }

    private HashSet<int> GetActiveClassIds(int teacherId) =>
        _classService.GetClassesByTeacher(teacherId)
            .Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Id)
            .ToHashSet();

    private static TeacherHomeAlertBundle CreateEmptyBundle(DateTime from, DateTime to) => new()
    {
        FromDate = from,
        ToDate = to,
        Year = to.Year,
        Month = to.Month
    };

    private static void SortAlerts(TeacherHomeAlertBundle bundle)
    {
        bundle.MissingTimesheets = bundle.MissingTimesheets
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.TeacherName)
            .ThenBy(x => x.ShiftNumber)
            .ThenBy(x => x.ClassName)
            .ToList();

        bundle.MissingAttendances = bundle.MissingAttendances
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.TeacherName)
            .ThenBy(x => x.ShiftNumber)
            .ThenBy(x => x.ClassName)
            .ToList();

        bundle.Discrepancies = bundle.Discrepancies
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.TeacherName)
            .ThenBy(x => x.ShiftNumber)
            .ThenBy(x => x.ClassName)
            .ToList();

        bundle.MissingEvaluations = bundle.MissingEvaluations
            .OrderBy(x => x.TeacherName)
            .ThenBy(x => x.ClassName)
            .ThenBy(x => x.StudentName)
            .ToList();
    }
}

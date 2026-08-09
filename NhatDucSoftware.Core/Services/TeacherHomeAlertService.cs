using System.Data.Common;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

/// <summary>
/// Tổng hợp thông báo trang chủ bằng batch query theo tháng (tránh N+1 từng ngày/GV).
/// </summary>
public class TeacherHomeAlertService
{
    public TeacherHomeAlertBundle GetAlertsForTeacher(int teacherId, string teacherName, DateTime? asOf = null) =>
        BuildAlerts(asOf, teacherId, teacherName);

    public TeacherHomeAlertBundle GetAlertsForAllTeachers(DateTime? asOf = null) =>
        BuildAlerts(asOf, teacherId: null, teacherName: null);

    private TeacherHomeAlertBundle BuildAlerts(DateTime? asOf, int? teacherId, string? teacherName)
    {
        var today = (asOf ?? DateTime.Now).Date;
        var from = new DateTime(today.Year, today.Month, 1);
        var bundle = CreateEmptyBundle(from, today);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        var teachers = LoadTeachers(connection, teacherId);
        if (teachers.Count == 0)
        {
            return bundle;
        }

        // Single-teacher override display name if provided.
        if (teacherId is int tid && !string.IsNullOrWhiteSpace(teacherName) && teachers.ContainsKey(tid))
        {
            teachers[tid] = teacherName.Trim();
        }

        var classes = LoadActiveClasses(connection, teacherId);
        if (classes.Count == 0)
        {
            CollectMissingEvaluationsBatch(bundle, connection, teachers, classes, today.Year, today.Month);
            SortAlerts(bundle);
            return bundle;
        }

        var classIds = classes.Keys.ToList();
        var teacherIds = teachers.Keys.ToList();
        var scheduledSlots = ExpandScheduledSlots(connection, classes, from, today);
        var timesheets = LoadTimesheets(connection, teacherIds, from, today);
        var studentJoins = LoadStudentJoins(connection, classIds);
        var attendanceStats = LoadAttendanceStats(connection, classIds, from, today);

        BuildScheduleAlerts(bundle, teachers, classes, scheduledSlots, timesheets, studentJoins, attendanceStats);
        CollectMissingEvaluationsBatch(bundle, connection, teachers, classes, today.Year, today.Month);
        SortAlerts(bundle);
        return bundle;
    }

    private static Dictionary<int, string> LoadTeachers(DbConnection connection, int? teacherId)
    {
        using var command = connection.CreateCommand();
        if (teacherId is int tid)
        {
            command.CommandText = @"
SELECT Id, FullName
FROM Teachers
WHERE Id = @id AND LOWER(Status) = 'active';";
            AddParam(command, "@id", tid);
        }
        else
        {
            command.CommandText = @"
SELECT Id, FullName
FROM Teachers
WHERE LOWER(Status) = 'active'
ORDER BY FullName;";
        }

        var result = new Dictionary<int, string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[Convert.ToInt32(reader.GetValue(0))] = reader.GetString(1);
        }

        return result;
    }

    private static Dictionary<int, ActiveClassRow> LoadActiveClasses(DbConnection connection, int? teacherId)
    {
        using var command = connection.CreateCommand();
        if (teacherId is int tid)
        {
            command.CommandText = @"
SELECT Id, ClassName, TeacherId
FROM Classes
WHERE LOWER(Status) = 'active'
  AND TeacherId = @teacherId;";
            AddParam(command, "@teacherId", tid);
        }
        else
        {
            command.CommandText = @"
SELECT Id, ClassName, TeacherId
FROM Classes
WHERE LOWER(Status) = 'active'
  AND TeacherId IS NOT NULL;";
        }

        var result = new Dictionary<int, ActiveClassRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var classId = Convert.ToInt32(reader.GetValue(0));
            result[classId] = new ActiveClassRow
            {
                ClassId = classId,
                ClassName = reader.GetString(1),
                TeacherId = Convert.ToInt32(reader.GetValue(2))
            };
        }

        return result;
    }

    private static List<ScheduledSlot> ExpandScheduledSlots(
        DbConnection connection,
        Dictionary<int, ActiveClassRow> classes,
        DateTime from,
        DateTime to)
    {
        var classIds = classes.Keys.ToList();
        var schedulesByClass = LoadAllSchedules(connection, classIds);
        var slots = new List<ScheduledSlot>();

        var mondays = new List<DateTime>();
        for (var monday = ClassWeeklySchedule.GetMondayOfWeek(from);
             monday <= ClassWeeklySchedule.GetMondayOfWeek(to);
             monday = monday.AddDays(7))
        {
            mondays.Add(monday);
        }

        var resolvedByClassWeek = new Dictionary<(int ClassId, DateTime Monday), List<(int DayOfWeek, int ShiftNumber)>>();
        foreach (var classId in classIds)
        {
            schedulesByClass.TryGetValue(classId, out var classSchedules);
            classSchedules ??= new List<ScheduleRow>();

            foreach (var monday in mondays)
            {
                var weekStr = monday.ToString("yyyy-MM-dd");
                var exact = classSchedules.Where(s => s.WeekStartDate == weekStr).ToList();
                List<(int DayOfWeek, int ShiftNumber)> entries;
                if (exact.Count > 0)
                {
                    entries = exact.Select(s => (s.DayOfWeek, s.ShiftNumber)).ToList();
                }
                else
                {
                    var prev = classSchedules
                        .Where(s => string.CompareOrdinal(s.WeekStartDate, weekStr) < 0)
                        .GroupBy(s => s.WeekStartDate)
                        .OrderByDescending(g => g.Key)
                        .FirstOrDefault();
                    entries = prev?.Select(s => (s.DayOfWeek, s.ShiftNumber)).ToList()
                              ?? new List<(int, int)>();
                }

                resolvedByClassWeek[(classId, monday)] = entries;
            }
        }

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var monday = ClassWeeklySchedule.GetMondayOfWeek(date);
            var dayOfWeek = ToScheduleDayOfWeek(date);
            foreach (var classId in classIds)
            {
                if (!resolvedByClassWeek.TryGetValue((classId, monday), out var entries))
                {
                    continue;
                }

                var cls = classes[classId];
                foreach (var (dow, shift) in entries.Where(e => e.DayOfWeek == dayOfWeek))
                {
                    slots.Add(new ScheduledSlot
                    {
                        TeacherId = cls.TeacherId,
                        ClassId = classId,
                        ClassName = cls.ClassName,
                        WorkDate = date,
                        ShiftNumber = shift
                    });
                }
            }
        }

        return slots;
    }

    private static Dictionary<int, List<ScheduleRow>> LoadAllSchedules(DbConnection connection, List<int> classIds)
    {
        var result = new Dictionary<int, List<ScheduleRow>>();
        if (classIds.Count == 0)
        {
            return result;
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ClassId, WeekStartDate, DayOfWeek, ShiftNumber
FROM ClassWeeklySchedules
WHERE ClassId = ANY(@classIds)
ORDER BY ClassId, WeekStartDate, DayOfWeek, ShiftNumber;";
        AddParam(command, "@classIds", classIds.ToArray());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var classId = Convert.ToInt32(reader.GetValue(0));
            if (!result.TryGetValue(classId, out var list))
            {
                list = new List<ScheduleRow>();
                result[classId] = list;
            }

            list.Add(new ScheduleRow
            {
                WeekStartDate = reader.GetValue(1)?.ToString() ?? "",
                DayOfWeek = Convert.ToInt32(reader.GetValue(2)),
                ShiftNumber = Convert.ToInt32(reader.GetValue(3))
            });
        }

        return result;
    }

    private static Dictionary<(int TeacherId, string WorkDate, int Shift), bool> LoadTimesheets(
        DbConnection connection,
        List<int> teacherIds,
        DateTime from,
        DateTime to)
    {
        var result = new Dictionary<(int, string, int), bool>();
        if (teacherIds.Count == 0)
        {
            return result;
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TeacherId, WorkDate, ShiftNumber, IsPresent
FROM TeacherTimesheets
WHERE TeacherId = ANY(@teacherIds)
  AND WorkDate >= @fromDate
  AND WorkDate <= @toDate;";
        AddParam(command, "@teacherIds", teacherIds.ToArray());
        AddParam(command, "@fromDate", from.ToString("yyyy-MM-dd"));
        AddParam(command, "@toDate", to.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var teacherId = Convert.ToInt32(reader.GetValue(0));
            var workDate = NormalizeDateKey(reader.GetValue(1));
            var shift = Convert.ToInt32(reader.GetValue(2));
            var present = Convert.ToInt32(reader.GetValue(3)) == 1;
            result[(teacherId, workDate, shift)] = present;
        }

        return result;
    }

    private static Dictionary<int, List<(int StudentId, DateTime JoinedDate)>> LoadStudentJoins(
        DbConnection connection,
        List<int> classIds)
    {
        var result = new Dictionary<int, List<(int, DateTime)>>();
        if (classIds.Count == 0)
        {
            return result;
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ClassId, StudentId, JoinedDate
FROM ClassStudents
WHERE ClassId = ANY(@classIds);";
        AddParam(command, "@classIds", classIds.ToArray());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var classId = Convert.ToInt32(reader.GetValue(0));
            var studentId = Convert.ToInt32(reader.GetValue(1));
            var joined = ParseDate(reader.GetValue(2)) ?? DateTime.MinValue;
            if (!result.TryGetValue(classId, out var list))
            {
                list = new List<(int, DateTime)>();
                result[classId] = list;
            }

            list.Add((studentId, joined.Date));
        }

        return result;
    }

    private static Dictionary<(int ClassId, string SessionDate, int Shift), (int Recorded, HashSet<int> RecordedStudentIds)> LoadAttendanceStats(
        DbConnection connection,
        List<int> classIds,
        DateTime from,
        DateTime to)
    {
        var result = new Dictionary<(int, string, int), (int, HashSet<int>)>();
        if (classIds.Count == 0)
        {
            return result;
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ats.ClassId, ats.SessionDate, ats.ShiftNumber, ar.StudentId, ar.Status
FROM AttendanceSessions ats
LEFT JOIN AttendanceRecords ar ON ar.SessionId = ats.Id
WHERE ats.ClassId = ANY(@classIds)
  AND ats.SessionDate >= @fromDate
  AND ats.SessionDate <= @toDate;";
        AddParam(command, "@classIds", classIds.ToArray());
        AddParam(command, "@fromDate", from.ToString("yyyy-MM-dd"));
        AddParam(command, "@toDate", to.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var classId = Convert.ToInt32(reader.GetValue(0));
            var sessionDate = NormalizeDateKey(reader.GetValue(1));
            var shift = Convert.ToInt32(reader.GetValue(2));
            var key = (classId, sessionDate, shift);
            if (!result.TryGetValue(key, out var stats))
            {
                stats = (0, new HashSet<int>());
                result[key] = stats;
            }

            if (reader.IsDBNull(3))
            {
                continue;
            }

            var studentId = Convert.ToInt32(reader.GetValue(3));
            var status = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim().ToUpperInvariant();
            if (status is "C" or "V")
            {
                stats.Item2.Add(studentId);
                result[key] = (stats.Item2.Count, stats.Item2);
            }
        }

        return result;
    }

    private static void BuildScheduleAlerts(
        TeacherHomeAlertBundle bundle,
        Dictionary<int, string> teachers,
        Dictionary<int, ActiveClassRow> classes,
        List<ScheduledSlot> slots,
        Dictionary<(int TeacherId, string WorkDate, int Shift), bool> timesheets,
        Dictionary<int, List<(int StudentId, DateTime JoinedDate)>> studentJoins,
        Dictionary<(int ClassId, string SessionDate, int Shift), (int Recorded, HashSet<int> RecordedStudentIds)> attendanceStats)
    {
        var byTeacherDateShift = slots
            .GroupBy(s => (s.TeacherId, s.WorkDate, s.ShiftNumber))
            .OrderBy(g => g.Key.WorkDate)
            .ThenBy(g => g.Key.TeacherId)
            .ThenBy(g => g.Key.ShiftNumber);

        foreach (var group in byTeacherDateShift)
        {
            var (teacherId, workDate, shiftNumber) = group.Key;
            if (!teachers.TryGetValue(teacherId, out var teacherName))
            {
                continue;
            }

            var dateKey = workDate.ToString("yyyy-MM-dd");
            var classSlots = group.GroupBy(x => x.ClassId).Select(g => g.First()).ToList();
            var hasTimesheet = timesheets.TryGetValue((teacherId, dateKey, shiftNumber), out var isPresent);
            bool? timesheetPresent = hasTimesheet ? isPresent : null;

            if (!hasTimesheet)
            {
                var classNames = string.Join(", ", classSlots.Select(c => c.ClassName).OrderBy(n => n));
                bundle.MissingTimesheets.Add(new TeacherScheduleGapAlert
                {
                    TeacherId = teacherId,
                    TeacherName = teacherName,
                    WorkDate = workDate,
                    ShiftNumber = shiftNumber,
                    ClassId = classSlots[0].ClassId,
                    ClassName = classNames,
                    Detail = $"Chưa chấm công — {classNames}"
                });
            }

            // GV vắng: không báo thiếu điểm danh / chênh lệch.
            if (timesheetPresent == false)
            {
                continue;
            }

            foreach (var slot in classSlots)
            {
                var totalStudents = 0;
                if (studentJoins.TryGetValue(slot.ClassId, out var joins))
                {
                    totalStudents = joins.Count(j => j.JoinedDate <= workDate);
                }

                var recorded = 0;
                if (attendanceStats.TryGetValue((slot.ClassId, dateKey, shiftNumber), out var stats))
                {
                    if (studentJoins.TryGetValue(slot.ClassId, out var joinRows))
                    {
                        var eligible = joinRows
                            .Where(j => j.JoinedDate <= workDate)
                            .Select(j => j.StudentId)
                            .ToHashSet();
                        recorded = stats.RecordedStudentIds.Count(id => eligible.Contains(id));
                    }
                    else
                    {
                        recorded = stats.Recorded;
                    }
                }

                var attendanceComplete = totalStudents == 0 || recorded == totalStudents;

                if (!attendanceComplete)
                {
                    bundle.MissingAttendances.Add(new TeacherScheduleGapAlert
                    {
                        TeacherId = teacherId,
                        TeacherName = teacherName,
                        WorkDate = workDate,
                        ShiftNumber = shiftNumber,
                        ClassId = slot.ClassId,
                        ClassName = slot.ClassName,
                        Detail = totalStudents > 0
                            ? $"Chưa điểm danh đủ ({recorded}/{totalStudents})"
                            : "Chưa điểm danh"
                    });
                }

                if (hasTimesheet != attendanceComplete)
                {
                    var detailText = hasTimesheet
                        ? "Đã chấm công (có mặt) nhưng điểm danh chưa xong"
                        : "Đã điểm danh nhưng chưa chấm công";

                    bundle.Discrepancies.Add(new TeacherScheduleGapAlert
                    {
                        TeacherId = teacherId,
                        TeacherName = teacherName,
                        WorkDate = workDate,
                        ShiftNumber = shiftNumber,
                        ClassId = slot.ClassId,
                        ClassName = slot.ClassName,
                        Detail = detailText
                    });
                }
            }
        }
    }

    private static void CollectMissingEvaluationsBatch(
        TeacherHomeAlertBundle bundle,
        DbConnection connection,
        Dictionary<int, string> teachers,
        Dictionary<int, ActiveClassRow> classes,
        int year,
        int month)
    {
        if (classes.Count == 0)
        {
            return;
        }

        var classIds = classes.Keys.ToList();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT cs.ClassId,
       s.Id,
       s.FullName,
       e.Comment
FROM ClassStudents cs
INNER JOIN Students s ON s.Id = cs.StudentId
LEFT JOIN LATERAL (
    SELECT se.Comment
    FROM StudentEvaluations se
    WHERE se.StudentId = cs.StudentId
      AND se.ClassId = cs.ClassId
      AND LEFT(se.CreatedAt::text, 7) = @yearMonth
    ORDER BY se.CreatedAt DESC, se.Id DESC
    LIMIT 1
) e ON true
WHERE cs.ClassId = ANY(@classIds)
ORDER BY cs.ClassId, s.FullName;";
        AddParam(command, "@classIds", classIds.ToArray());
        AddParam(command, "@yearMonth", $"{year}-{month:D2}");

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var classId = Convert.ToInt32(reader.GetValue(0));
            if (!classes.TryGetValue(classId, out var cls))
            {
                continue;
            }

            if (!teachers.TryGetValue(cls.TeacherId, out var teacherName))
            {
                continue;
            }

            var comment = reader.IsDBNull(3) ? "" : reader.GetString(3);
            if (!string.IsNullOrWhiteSpace(comment))
            {
                continue;
            }

            bundle.MissingEvaluations.Add(new TeacherMissingEvaluationAlert
            {
                TeacherId = cls.TeacherId,
                TeacherName = teacherName,
                ClassId = classId,
                ClassName = cls.ClassName,
                StudentId = Convert.ToInt32(reader.GetValue(1)),
                StudentName = reader.GetString(2)
            });
        }
    }

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

    private static int ToScheduleDayOfWeek(DateTime date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => 0,
        DayOfWeek.Tuesday => 1,
        DayOfWeek.Wednesday => 2,
        DayOfWeek.Thursday => 3,
        DayOfWeek.Friday => 4,
        DayOfWeek.Saturday => 5,
        DayOfWeek.Sunday => 6,
        _ => 0
    };

    private static string NormalizeDateKey(object? value)
    {
        if (value is DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd");
        }

        var text = Convert.ToString(value) ?? "";
        return DateTime.TryParse(text, out var parsed)
            ? parsed.ToString("yyyy-MM-dd")
            : text.Length >= 10 ? text[..10] : text;
    }

    private static DateTime? ParseDate(object? value)
    {
        if (value is DateTime dt)
        {
            return dt.Date;
        }

        var text = Convert.ToString(value);
        return DateTime.TryParse(text, out var parsed) ? parsed.Date : null;
    }

    private static void AddParam(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed class ActiveClassRow
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public int TeacherId { get; set; }
    }

    private sealed class ScheduleRow
    {
        public string WeekStartDate { get; set; } = "";
        public int DayOfWeek { get; set; }
        public int ShiftNumber { get; set; }
    }

    private sealed class ScheduledSlot
    {
        public int TeacherId { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public DateTime WorkDate { get; set; }
        public int ShiftNumber { get; set; }
    }
}

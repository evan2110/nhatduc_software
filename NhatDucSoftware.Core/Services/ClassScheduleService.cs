using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class ClassScheduleService
{
    /// <summary>
    /// Lấy lịch học của lớp cho tuần chỉ định.
    /// Nếu tuần đó chưa có lịch, tìm tuần gần nhất trước đó có lịch.
    /// </summary>
    public List<ClassWeeklySchedule> GetScheduleForWeek(int classId, DateTime weekMonday)
    {
        var weekStr = weekMonday.ToString("yyyy-MM-dd");

        // Try exact week first
        var result = LoadSchedule(classId, weekStr);
        if (result.Count > 0) return result;

        // Find the most recent week that has schedule
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT DISTINCT WeekStartDate FROM ClassWeeklySchedules
WHERE ClassId = @classId AND WeekStartDate < @week
ORDER BY WeekStartDate DESC LIMIT 1;";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@week", weekStr);

        var prevWeek = command.ExecuteScalar() as string;
        if (prevWeek != null)
        {
            return LoadSchedule(classId, prevWeek);
        }

        return new List<ClassWeeklySchedule>();
    }

    private List<ClassWeeklySchedule> LoadSchedule(int classId, string weekStartDate)
    {
        var result = new List<ClassWeeklySchedule>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, ClassId, WeekStartDate, DayOfWeek, ShiftNumber
FROM ClassWeeklySchedules
WHERE ClassId = @classId AND WeekStartDate = @week
ORDER BY DayOfWeek, ShiftNumber;";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@week", weekStartDate);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ClassWeeklySchedule
            {
                Id = reader.GetInt32(0),
                ClassId = reader.GetInt32(1),
                WeekStartDate = reader.GetString(2),
                DayOfWeek = reader.GetInt32(3),
                ShiftNumber = reader.GetInt32(4)
            });
        }

        return result;
    }

    /// <summary>
    /// Lưu lịch học cho lớp ở tuần chỉ định.
    /// entries: list of (DayOfWeek, ShiftNumber)
    /// </summary>
    public void SaveScheduleForWeek(int classId, DateTime weekMonday, List<(int DayOfWeek, int ShiftNumber)> entries)
    {
        var weekStr = weekMonday.ToString("yyyy-MM-dd");

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        // Delete existing for this week
        using var delCmd = connection.CreateCommand();
        delCmd.Transaction = transaction;
        delCmd.CommandText = "DELETE FROM ClassWeeklySchedules WHERE ClassId = @classId AND WeekStartDate = @week;";
        delCmd.Parameters.AddWithValue("@classId", classId);
        delCmd.Parameters.AddWithValue("@week", weekStr);
        delCmd.ExecuteNonQuery();

        // Insert new
        foreach (var (day, shift) in entries)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"INSERT INTO ClassWeeklySchedules(ClassId, WeekStartDate, DayOfWeek, ShiftNumber)
VALUES(@classId, @week, @day, @shift);";
            cmd.Parameters.AddWithValue("@classId", classId);
            cmd.Parameters.AddWithValue("@week", weekStr);
            cmd.Parameters.AddWithValue("@day", day);
            cmd.Parameters.AddWithValue("@shift", shift);
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>
    /// Lấy tất cả lịch dạy của giáo viên cho tuần chỉ định.
    /// </summary>
    public List<(string ClassName, int DayOfWeek, int ShiftNumber)> GetTeacherScheduleForWeek(int teacherId, DateTime weekMonday)
    {
        return GetTeacherScheduleEntriesForWeek(teacherId, weekMonday)
            .Select(e => (e.ClassName, e.DayOfWeek, e.ShiftNumber))
            .ToList();
    }

    /// <summary>
    /// Lấy lịch dạy của giáo viên kèm mã lớp.
    /// </summary>
    public List<TeacherScheduleEntry> GetTeacherScheduleEntriesForWeek(int teacherId, DateTime weekMonday)
    {
        var result = new List<TeacherScheduleEntry>();
        var weekStart = ClassWeeklySchedule.GetMondayOfWeek(weekMonday);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var classCmd = connection.CreateCommand();
        classCmd.CommandText = @"
SELECT Id, ClassName, Status, InactiveFromWeekStart
FROM Classes
WHERE TeacherId = @tid;";
        classCmd.Parameters.AddWithValue("@tid", teacherId);

        var classes = new List<(int Id, string Name, string Status, string? InactiveFromWeekStart)>();
        using (var reader = classCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                classes.Add((
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3)));
            }
        }

        foreach (var (classId, className, status, inactiveFromWeekStart) in classes)
        {
            if (!ClassVisibility.IsVisibleForWeek(status, inactiveFromWeekStart, weekStart))
            {
                continue;
            }

            var schedule = GetScheduleForWeek(classId, weekStart);
            foreach (var s in schedule)
            {
                result.Add(new TeacherScheduleEntry
                {
                    ClassId = classId,
                    ClassName = className,
                    DayOfWeek = s.DayOfWeek,
                    ShiftNumber = s.ShiftNumber
                });
            }
        }

        return result.OrderBy(x => x.DayOfWeek).ThenBy(x => x.ShiftNumber).ThenBy(x => x.ClassName).ToList();
    }

    /// <summary>
    /// Tra cứu tên lớp theo ngày và ca của giáo viên trong tháng (theo lịch tuần).
    /// </summary>
    public IReadOnlyDictionary<(DateTime Date, int ShiftNumber), IReadOnlyList<string>> GetTeacherClassNamesByDateAndShiftForMonth(
        int teacherId, int year, int month)
    {
        var result = new Dictionary<(DateTime Date, int ShiftNumber), IReadOnlyList<string>>();
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var weekSchedules = new Dictionary<DateTime, List<TeacherScheduleEntry>>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            var monday = ClassWeeklySchedule.GetMondayOfWeek(date);
            if (!weekSchedules.TryGetValue(monday, out var entries))
            {
                entries = GetTeacherScheduleEntriesForWeek(teacherId, monday);
                weekSchedules[monday] = entries;
            }

            var scheduleDay = ToScheduleDayOfWeek(date);
            foreach (var group in entries.Where(e => e.DayOfWeek == scheduleDay).GroupBy(e => e.ShiftNumber))
            {
                result[(date.Date, group.Key)] = group
                    .Select(e => e.ClassName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList()
                    .AsReadOnly();
            }
        }

        return result;
    }

    /// <summary>
    /// Lấy các ca học của lớp trong ngày chỉ định (theo lịch tuần).
    /// </summary>
    public List<int> GetShiftsForClassOnDate(int classId, DateTime date)
    {
        if (!IsClassVisibleForDate(classId, date))
        {
            return new List<int>();
        }

        var monday = ClassWeeklySchedule.GetMondayOfWeek(date);
        var scheduleDay = ToScheduleDayOfWeek(date);
        return GetScheduleForWeek(classId, monday)
            .Where(s => s.DayOfWeek == scheduleDay)
            .Select(s => s.ShiftNumber)
            .Distinct()
            .OrderBy(s => s)
            .ToList();
    }

    /// <summary>
    /// Lấy các ca giáo viên được điểm danh: lớp thuộc GV và có lịch học ca đó trong ngày.
    /// </summary>
    public List<int> GetTeacherShiftsForClassOnDate(int classId, int teacherId, DateTime date)
    {
        if (!IsTeacherAssignedToClass(classId, teacherId))
        {
            return new List<int>();
        }

        return GetShiftsForClassOnDate(classId, date);
    }

    /// <summary>
    /// Lấy lịch dạy theo ngày của toàn bộ giáo viên (dùng cho admin xem TKB).
    /// </summary>
    public List<TeacherDailyScheduleRow> GetAllTeachersScheduleForDate(DateTime date)
    {
        var monday = ClassWeeklySchedule.GetMondayOfWeek(date);
        var scheduleDay = ToScheduleDayOfWeek(date);
        var teachers = new List<(int Id, string FullName)>();

        using (var connection = DbContext.CreateConnection())
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, FullName FROM Teachers ORDER BY FullName;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                teachers.Add((reader.GetInt32(0), reader.GetString(1)));
            }
        }

        var result = new List<TeacherDailyScheduleRow>();
        foreach (var (teacherId, teacherName) in teachers)
        {
            var weekSchedule = GetTeacherScheduleEntriesForWeek(teacherId, monday)
                .Where(x => x.DayOfWeek == scheduleDay);

            var shiftClasses = weekSchedule
                .GroupBy(x => x.ShiftNumber)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new TeacherDailyClassInfo { ClassId = x.ClassId, ClassName = x.ClassName })
                        .DistinctBy(x => x.ClassId)
                        .OrderBy(x => x.ClassName)
                        .ToList());

            result.Add(new TeacherDailyScheduleRow
            {
                TeacherId = teacherId,
                TeacherName = teacherName,
                ShiftClasses = shiftClasses
            });
        }

        return result;
    }

    /// <summary>
    /// Chi tiết lịch dạy theo ngày của một giáo viên, kèm trạng thái chấm công và điểm danh.
    /// </summary>
    public TeacherDailyScheduleDetail GetTeacherDailyScheduleDetail(int teacherId, string teacherName, DateTime date)
    {
        var monday = ClassWeeklySchedule.GetMondayOfWeek(date);
        var scheduleDay = ToScheduleDayOfWeek(date);
        var entries = GetTeacherScheduleEntriesForWeek(teacherId, monday)
            .Where(x => x.DayOfWeek == scheduleDay)
            .ToList();

        var timesheetService = new TeacherTimesheetService();
        var attendanceService = new AttendanceService();

        var shiftDetails = entries
            .GroupBy(e => e.ShiftNumber)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var shiftNumber = g.Key;
                var timesheetStatus = timesheetService.GetTimesheetStatusForShift(teacherId, date, shiftNumber);
                var classes = g.GroupBy(e => e.ClassId)
                    .Select(cg =>
                    {
                        var classEntry = cg.First();
                        var completion = attendanceService.GetAttendanceCompletionStatus(classEntry.ClassId, date, shiftNumber);
                        return new TeacherDailyClassDetail
                        {
                            ClassId = classEntry.ClassId,
                            ClassName = classEntry.ClassName,
                            TotalStudents = completion.TotalStudents,
                            RecordedStudents = completion.RecordedStudents,
                            AttendanceComplete = completion.IsComplete
                        };
                    })
                    .OrderBy(c => c.ClassName)
                    .ToList();

                return new TeacherDailyShiftDetail
                {
                    ShiftNumber = shiftNumber,
                    TimesheetRecorded = timesheetStatus.HasValue,
                    TimesheetPresent = timesheetStatus,
                    Classes = classes
                };
            })
            .ToList();

        return new TeacherDailyScheduleDetail
        {
            TeacherId = teacherId,
            TeacherName = teacherName,
            WorkDate = date.Date,
            Shifts = shiftDetails,
            AllTimesheetsComplete = shiftDetails.Count > 0 && shiftDetails.All(s => s.TimesheetRecorded),
            AllAttendanceComplete = shiftDetails.Count > 0 && shiftDetails.All(s =>
                s.TimesheetPresent == false || s.Classes.All(c => c.AttendanceComplete))
        };
    }

    private static bool IsClassVisibleForDate(int classId, DateTime date)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Status, InactiveFromWeekStart
FROM Classes
WHERE Id = @classId
LIMIT 1;";
        command.Parameters.AddWithValue("@classId", classId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return false;
        }

        var status = reader.GetString(0);
        var inactiveFromWeekStart = reader.IsDBNull(1) ? null : reader.GetString(1);
        return ClassVisibility.IsVisibleForWeek(status, inactiveFromWeekStart, ClassWeeklySchedule.GetMondayOfWeek(date));
    }

    private static bool IsTeacherAssignedToClass(int classId, int teacherId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM Classes WHERE Id = @classId AND TeacherId = @teacherId;";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@teacherId", teacherId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
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
}

public class TeacherScheduleEntry
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = "";
    public int DayOfWeek { get; set; }
    public int ShiftNumber { get; set; }
}

public class TeacherDailyScheduleRow
{
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = "";
    public Dictionary<int, List<TeacherDailyClassInfo>> ShiftClasses { get; set; } = new();

    public bool HasShift(int shiftNumber) => ShiftClasses.ContainsKey(shiftNumber);

    public string? GetShiftTooltip(int shiftNumber) =>
        ShiftClasses.TryGetValue(shiftNumber, out var classes) && classes.Count > 0
            ? string.Join(", ", classes.Select(c => c.ClassName))
            : null;
}

public class TeacherDailyClassInfo
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = "";
}

public class TeacherDailyScheduleDetail
{
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = "";
    public DateTime WorkDate { get; set; }
    public List<TeacherDailyShiftDetail> Shifts { get; set; } = new();
    public bool AllTimesheetsComplete { get; set; }
    public bool AllAttendanceComplete { get; set; }

    public int RequiredShiftCount => Shifts.Count;
    public int RecordedShiftCount => Shifts.Count(s => s.TimesheetRecorded);
    public int RequiredClassShiftCount => Shifts.Sum(s => s.Classes.Count);
    public int CompletedClassShiftCount => Shifts.Sum(s =>
        s.TimesheetPresent == false
            ? s.Classes.Count
            : s.Classes.Count(c => c.AttendanceComplete));
}

public class TeacherDailyShiftDetail
{
    public int ShiftNumber { get; set; }
    public bool TimesheetRecorded { get; set; }
    public bool? TimesheetPresent { get; set; }
    public List<TeacherDailyClassDetail> Classes { get; set; } = new();
}

public class TeacherDailyClassDetail
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = "";
    public int TotalStudents { get; set; }
    public int RecordedStudents { get; set; }
    public bool AttendanceComplete { get; set; }
}

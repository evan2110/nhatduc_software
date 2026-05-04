using NhatDucSoftware.Data;
using NhatDucSoftware.Models;

namespace NhatDucSoftware.Services;

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
        var weekStr = weekMonday.ToString("yyyy-MM-dd");
        var result = new List<(string, int, int)>();

        using var connection = DbContext.CreateConnection();
        connection.Open();

        // Get all classes for this teacher
        using var classCmd = connection.CreateCommand();
        classCmd.CommandText = "SELECT Id, ClassName FROM Classes WHERE TeacherId = @tid;";
        classCmd.Parameters.AddWithValue("@tid", teacherId);

        var classes = new List<(int Id, string Name)>();
        using (var reader = classCmd.ExecuteReader())
        {
            while (reader.Read())
                classes.Add((reader.GetInt32(0), reader.GetString(1)));
        }

        foreach (var (classId, className) in classes)
        {
            var schedule = GetScheduleForWeek(classId, weekMonday);
            foreach (var s in schedule)
            {
                result.Add((className, s.DayOfWeek, s.ShiftNumber));
            }
        }

        return result.OrderBy(x => x.Item2).ThenBy(x => x.Item3).ToList();
    }
}

using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class TeacherTimesheetService
{
    /// <summary>
    /// Lưu chấm công cho giáo viên theo ngày và ca.
    /// </summary>
    public void SaveTimesheet(int teacherId, DateTime workDate, int shiftNumber, bool isPresent, string? note = null)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO TeacherTimesheets (TeacherId, WorkDate, ShiftNumber, IsPresent, Note)
VALUES (@teacherId, @workDate, @shift, @present, @note)
ON CONFLICT(TeacherId, WorkDate, ShiftNumber)
DO UPDATE SET IsPresent = @present, Note = @note;";
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@workDate", workDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@shift", shiftNumber);
        command.Parameters.AddWithValue("@present", isPresent ? 1 : 0);
        command.Parameters.AddWithValue("@note", (object?)note ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Lưu chấm công cho tất cả 5 ca trong 1 ngày.
    /// </summary>
    public void SaveDayTimesheet(int teacherId, DateTime workDate, bool[] shifts, string? note = null)
    {
        for (int i = 0; i < 5; i++)
        {
            SaveTimesheet(teacherId, workDate, i + 1, shifts[i], note);
        }
    }

    /// <summary>
    /// Lấy bảng chấm công của giáo viên theo tháng.
    /// </summary>
    public List<TeacherTimesheet> GetTimesheetByMonth(int teacherId, int year, int month)
    {
        var result = new List<TeacherTimesheet>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT tt.Id, tt.TeacherId, tt.WorkDate, tt.ShiftNumber, tt.IsPresent, tt.Note, t.FullName
FROM TeacherTimesheets tt
INNER JOIN Teachers t ON t.Id = tt.TeacherId
WHERE tt.TeacherId = @teacherId
  AND tt.WorkDate >= @startDate
  AND tt.WorkDate <= @endDate
ORDER BY tt.WorkDate, tt.ShiftNumber;";
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@startDate", $"{year:D4}-{month:D2}-01");
        var lastDay = DateTime.DaysInMonth(year, month);
        command.Parameters.AddWithValue("@endDate", $"{year:D4}-{month:D2}-{lastDay:D2}");

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new TeacherTimesheet
            {
                Id = reader.GetInt32(0),
                TeacherId = reader.GetInt32(1),
                WorkDate = DateTime.Parse(reader.GetString(2)),
                ShiftNumber = reader.GetInt32(3),
                IsPresent = reader.GetInt32(4) == 1,
                Note = reader.IsDBNull(5) ? null : reader.GetString(5),
                TeacherName = reader.GetString(6)
            });
        }

        return result;
    }

    public List<CenterTimesheetExportRow> GetTimesheetsByDateRange(DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date.ToString("yyyy-MM-dd");
        var to = toDate.Date.ToString("yyyy-MM-dd");
        var result = new List<CenterTimesheetExportRow>();

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT tt.TeacherId,
       tt.WorkDate,
       t.FullName,
       tt.ShiftNumber,
       tt.IsPresent,
       tt.Note
FROM TeacherTimesheets tt
INNER JOIN Teachers t ON t.Id = tt.TeacherId
WHERE tt.WorkDate >= @fromDate
  AND tt.WorkDate <= @toDate
ORDER BY tt.WorkDate, t.FullName, tt.ShiftNumber;";
        command.Parameters.AddWithValue("@fromDate", from);
        command.Parameters.AddWithValue("@toDate", to);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var teacherId = reader.GetInt32(0);
            var workDate = DateTime.Parse(reader.GetString(1));
            var isPresent = reader.GetInt32(4) == 1;
            result.Add(new CenterTimesheetExportRow
            {
                WorkDate = workDate,
                TeacherName = reader.GetString(2),
                ShiftNumber = reader.GetInt32(3),
                IsPresent = isPresent,
                Note = reader.IsDBNull(5) ? null : reader.GetString(5),
                ShiftPay = isPresent ? GetShiftPay(teacherId, workDate, reader.GetInt32(3)) : 0
            });
        }

        return result;
    }

    /// <summary>
    /// null = chưa chấm công; true/false = có mặt / vắng.
    /// </summary>
    public bool? GetTimesheetStatusForShift(int teacherId, DateTime workDate, int shiftNumber)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT IsPresent
FROM TeacherTimesheets
WHERE TeacherId = @teacherId
  AND WorkDate = @workDate
  AND ShiftNumber = @shift
LIMIT 1;";
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@workDate", workDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@shift", shiftNumber);

        var value = command.ExecuteScalar();
        if (value is null || value == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt32(value) == 1;
    }

    /// <summary>
    /// Tính tổng số ca đã dạy trong tháng.
    /// </summary>
    public int GetTotalShiftsInMonth(int teacherId, int year, int month)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(*)
FROM TeacherTimesheets
WHERE TeacherId = @teacherId
  AND IsPresent = 1
  AND WorkDate >= @startDate
  AND WorkDate <= @endDate;";
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@startDate", $"{year:D4}-{month:D2}-01");
        var lastDay = DateTime.DaysInMonth(year, month);
        command.Parameters.AddWithValue("@endDate", $"{year:D4}-{month:D2}-{lastDay:D2}");

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<TeacherClassPaySetting> GetClassPaySettings(int teacherId)
    {
        var result = new List<TeacherClassPaySetting>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT c.Id,
       c.ClassName,
       tcp.PayPerShift
FROM Classes c
LEFT JOIN TeacherClassPayRates tcp ON tcp.ClassId = c.Id AND tcp.TeacherId = @teacherId
WHERE c.TeacherId = @teacherId
ORDER BY c.ClassName;";
        command.Parameters.AddWithValue("@teacherId", teacherId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var isConfigured = !reader.IsDBNull(2);
            result.Add(new TeacherClassPaySetting
            {
                ClassId = reader.GetInt32(0),
                ClassName = reader.GetString(1),
                PayPerShift = isConfigured
                    ? Convert.ToDecimal(reader.GetValue(2))
                    : TeacherTimesheet.DefaultPayPerShift,
                IsConfigured = isConfigured
            });
        }

        return result;
    }

    public void SaveClassPayRate(int teacherId, int classId, decimal payPerShift)
    {
        if (payPerShift <= 0)
        {
            throw new InvalidOperationException("Lương mỗi ca phải lớn hơn 0.");
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT COUNT(1) FROM Classes WHERE Id = @classId AND TeacherId = @teacherId;";
        verify.Parameters.AddWithValue("@classId", classId);
        verify.Parameters.AddWithValue("@teacherId", teacherId);
        if (Convert.ToInt32(verify.ExecuteScalar()) == 0)
        {
            throw new InvalidOperationException("Giáo viên không phụ trách lớp này.");
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO TeacherClassPayRates(TeacherId, ClassId, PayPerShift)
VALUES(@teacherId, @classId, @payPerShift)
ON CONFLICT(TeacherId, ClassId)
DO UPDATE SET PayPerShift = EXCLUDED.PayPerShift;";
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@payPerShift", payPerShift);
        command.ExecuteNonQuery();
    }

    public decimal GetShiftPay(int teacherId, DateTime workDate, int shiftNumber) =>
        GetTeacherPayPerShift(teacherId);

    /// <summary>
    /// Lương/ca áp dụng cho giáo viên theo cài đặt lương/ca (lấy mức cao nhất trong các lớp phụ trách).
    /// </summary>
    public decimal GetTeacherPayPerShift(int teacherId)
    {
        var settings = GetClassPaySettings(teacherId);
        return settings.Count == 0
            ? TeacherTimesheet.DefaultPayPerShift
            : settings.Max(x => x.PayPerShift);
    }

    /// <summary>
    /// Tính lương giáo viên trong tháng: số ca có mặt × lương/ca đã cài đặt.
    /// </summary>
    public decimal CalculateMonthlyPay(int teacherId, int year, int month) =>
        GetTotalShiftsInMonth(teacherId, year, month) * GetTeacherPayPerShift(teacherId);

    /// <summary>
    /// Số ngày có mặt thực tế trong tháng (đếm theo ngày, không theo ca).
    /// </summary>
    public int GetActualWorkingDaysInMonth(int teacherId, int year, int month) =>
        GetTimesheetByMonth(teacherId, year, month)
            .Where(t => t.IsPresent)
            .Select(t => t.WorkDate.Date)
            .Distinct()
            .Count();

    /// <summary>
    /// Ngày công chuẩn trong tháng (Thứ 2–Thứ 7, không tính Chủ nhật).
    /// </summary>
    public static int GetStandardWorkingDaysInMonth(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var count = 0;
        for (var day = 1; day <= daysInMonth; day++)
        {
            if (new DateTime(year, month, day).DayOfWeek != DayOfWeek.Sunday)
            {
                count++;
            }
        }

        return count;
    }
}

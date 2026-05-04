using NhatDucSoftware.Data;
using NhatDucSoftware.Models;

namespace NhatDucSoftware.Services;

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

    /// <summary>
    /// Tính lương giáo viên trong tháng = số ca * 100,000 VND.
    /// </summary>
    public decimal CalculateMonthlyPay(int teacherId, int year, int month)
    {
        var totalShifts = GetTotalShiftsInMonth(teacherId, year, month);
        return totalShifts * TeacherTimesheet.PayPerShift;
    }
}

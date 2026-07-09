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

    public decimal GetShiftPay(int teacherId, DateTime workDate, int shiftNumber)
    {
        var year = workDate.Year;
        var month = workDate.Month;
        var defaultRate = GetTeacherPayPerShift(teacherId);
        var presentShifts = GetPresentShiftsOrdered(teacherId, year, month);
        var index = presentShifts.FindIndex(s =>
            s.WorkDate.Date == workDate.Date && s.ShiftNumber == shiftNumber);
        if (index < 0)
        {
            return defaultRate;
        }

        var adjustments = GetPayAdjustmentsOrdered(teacherId, year, month);
        return GetPayRateForShiftIndex(index, presentShifts.Count, adjustments, defaultRate);
    }

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
    /// Tính lương giáo viên trong tháng: cộng dồn các lần điều chỉnh theo thứ tự thời gian.
    /// </summary>
    public decimal CalculateMonthlyPay(int teacherId, int year, int month)
    {
        var presentShifts = GetPresentShiftsOrdered(teacherId, year, month);
        if (presentShifts.Count == 0)
        {
            return 0;
        }

        var defaultRate = GetTeacherPayPerShift(teacherId);
        var adjustments = GetPayAdjustmentsOrdered(teacherId, year, month);
        return CalculatePayFromAdjustments(presentShifts.Count, adjustments, defaultRate);
    }

    public decimal[] CalculateExpenseByMonthForYear(int year, IReadOnlyList<int> teacherIds)
    {
        var totals = new decimal[12];
        if (teacherIds.Count == 0)
        {
            return totals;
        }

        var shiftCounts = LoadPresentShiftCountsByTeacherMonth(year, teacherIds);
        var defaultRates = LoadDefaultPayRates(teacherIds);
        var adjustments = LoadPayAdjustmentsByTeacherMonth(year, teacherIds);

        foreach (var teacherId in teacherIds)
        {
            var defaultRate = defaultRates.GetValueOrDefault(teacherId, TeacherTimesheet.DefaultPayPerShift);
            for (var month = 1; month <= 12; month++)
            {
                var shiftCount = shiftCounts.GetValueOrDefault((teacherId, month));
                if (shiftCount <= 0)
                {
                    continue;
                }

                var monthAdjustments = adjustments.GetValueOrDefault((teacherId, month)) ?? new List<TeacherPayAdjustment>();
                totals[month - 1] += CalculatePayFromAdjustments(shiftCount, monthAdjustments, defaultRate);
            }
        }

        return totals;
    }

    public decimal CalculateTeacherExpense(int teacherId, int year, int? month = null)
    {
        if (month is >= 1 and <= 12)
        {
            return CalculateMonthlyPay(teacherId, year, month.Value);
        }

        decimal total = 0;
        for (var m = 1; m <= 12; m++)
        {
            total += CalculateMonthlyPay(teacherId, year, m);
        }

        return total;
    }

    public Dictionary<int, decimal> CalculateTeacherExpenseByTeacherForYear(int year, IReadOnlyList<int> teacherIds, int? month = null)
    {
        var result = new Dictionary<int, decimal>();
        if (teacherIds.Count == 0)
        {
            return result;
        }

        if (month is >= 1 and <= 12)
        {
            var shiftCounts = LoadPresentShiftCountsByTeacherMonth(year, teacherIds, month.Value);
            var defaultRates = LoadDefaultPayRates(teacherIds);
            var adjustments = LoadPayAdjustmentsByTeacherMonth(year, teacherIds, month.Value);

            foreach (var teacherId in teacherIds)
            {
                var shiftCount = shiftCounts.GetValueOrDefault((teacherId, month.Value));
                if (shiftCount <= 0)
                {
                    continue;
                }

                var defaultRate = defaultRates.GetValueOrDefault(teacherId, TeacherTimesheet.DefaultPayPerShift);
                var monthAdjustments = adjustments.GetValueOrDefault((teacherId, month.Value)) ?? new List<TeacherPayAdjustment>();
                var pay = CalculatePayFromAdjustments(shiftCount, monthAdjustments, defaultRate);
                if (pay > 0)
                {
                    result[teacherId] = pay;
                }
            }

            return result;
        }

        var yearlyShiftCounts = LoadPresentShiftCountsByTeacherMonth(year, teacherIds);
        var yearlyDefaultRates = LoadDefaultPayRates(teacherIds);
        var yearlyAdjustments = LoadPayAdjustmentsByTeacherMonth(year, teacherIds);

        foreach (var teacherId in teacherIds)
        {
            var defaultRate = yearlyDefaultRates.GetValueOrDefault(teacherId, TeacherTimesheet.DefaultPayPerShift);
            decimal total = 0;
            for (var m = 1; m <= 12; m++)
            {
                var shiftCount = yearlyShiftCounts.GetValueOrDefault((teacherId, m));
                if (shiftCount <= 0)
                {
                    continue;
                }

                var monthAdjustments = yearlyAdjustments.GetValueOrDefault((teacherId, m)) ?? new List<TeacherPayAdjustment>();
                total += CalculatePayFromAdjustments(shiftCount, monthAdjustments, defaultRate);
            }

            if (total > 0)
            {
                result[teacherId] = total;
            }
        }

        return result;
    }

    public Dictionary<int, decimal> CalculateMonthlyPayForTeachers(int year, int month, IReadOnlyList<int> teacherIds)
    {
        var result = new Dictionary<int, decimal>();
        if (teacherIds.Count == 0)
        {
            return result;
        }

        var shiftCounts = LoadPresentShiftCountsByTeacherMonth(year, teacherIds, month);
        var defaultRates = LoadDefaultPayRates(teacherIds);
        var adjustments = LoadPayAdjustmentsByTeacherMonth(year, teacherIds, month);

        foreach (var teacherId in teacherIds)
        {
            var shiftCount = shiftCounts.GetValueOrDefault((teacherId, month));
            if (shiftCount <= 0)
            {
                result[teacherId] = 0;
                continue;
            }

            var defaultRate = defaultRates.GetValueOrDefault(teacherId, TeacherTimesheet.DefaultPayPerShift);
            var monthAdjustments = adjustments.GetValueOrDefault((teacherId, month)) ?? new List<TeacherPayAdjustment>();
            result[teacherId] = CalculatePayFromAdjustments(shiftCount, monthAdjustments, defaultRate);
        }

        return result;
    }

    public Dictionary<int, int> GetTotalShiftsInMonthForTeachers(int year, int month, IReadOnlyList<int> teacherIds)
    {
        var shiftCounts = LoadPresentShiftCountsByTeacherMonth(year, teacherIds, month);
        return teacherIds.ToDictionary(
            teacherId => teacherId,
            teacherId => shiftCounts.GetValueOrDefault((teacherId, month)));
    }

    private static Dictionary<int, decimal> LoadDefaultPayRates(IReadOnlyList<int> teacherIds)
    {
        var result = new Dictionary<int, decimal>();
        if (teacherIds.Count == 0)
        {
            return result;
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TeacherId, COALESCE(MAX(PayPerShift), @defaultRate)
FROM TeacherClassPayRates
WHERE TeacherId = ANY(@teacherIds)
GROUP BY TeacherId;";
        command.Parameters.AddWithValue("@defaultRate", TeacherTimesheet.DefaultPayPerShift);
        command.Parameters.AddWithValue("@teacherIds", teacherIds.ToArray());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[reader.GetInt32(0)] = Convert.ToDecimal(reader.GetValue(1));
        }

        return result;
    }

    private static Dictionary<(int TeacherId, int Month), int> LoadPresentShiftCountsByTeacherMonth(
        int year,
        IReadOnlyList<int> teacherIds,
        int? month = null)
    {
        var result = new Dictionary<(int TeacherId, int Month), int>();
        if (teacherIds.Count == 0)
        {
            return result;
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = month is >= 1 and <= 12
            ? @"
SELECT TeacherId, COUNT(*) AS ShiftCount
FROM TeacherTimesheets
WHERE TeacherId = ANY(@teacherIds)
  AND IsPresent = 1
  AND EXTRACT(YEAR FROM WorkDate::date) = @year
  AND EXTRACT(MONTH FROM WorkDate::date) = @month
GROUP BY TeacherId;"
            : @"
SELECT TeacherId,
       CAST(EXTRACT(MONTH FROM WorkDate::date) AS INTEGER) AS Month,
       COUNT(*) AS ShiftCount
FROM TeacherTimesheets
WHERE TeacherId = ANY(@teacherIds)
  AND IsPresent = 1
  AND EXTRACT(YEAR FROM WorkDate::date) = @year
GROUP BY TeacherId, EXTRACT(MONTH FROM WorkDate::date);";
        command.Parameters.AddWithValue("@teacherIds", teacherIds.ToArray());
        command.Parameters.AddWithValue("@year", year);
        if (month is >= 1 and <= 12)
        {
            command.Parameters.AddWithValue("@month", month.Value);
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (month is >= 1 and <= 12)
            {
                result[(reader.GetInt32(0), month.Value)] = reader.GetInt32(1);
            }
            else
            {
                result[(reader.GetInt32(0), reader.GetInt32(1))] = reader.GetInt32(2);
            }
        }

        return result;
    }

    private static Dictionary<(int TeacherId, int Month), List<TeacherPayAdjustment>> LoadPayAdjustmentsByTeacherMonth(
        int year,
        IReadOnlyList<int> teacherIds,
        int? month = null)
    {
        var result = new Dictionary<(int TeacherId, int Month), List<TeacherPayAdjustment>>();
        if (teacherIds.Count == 0)
        {
            return result;
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = month is >= 1 and <= 12
            ? @"
SELECT Id, TeacherId, Year, Month, ShiftCount, PayPerShift, Note, CreatedByUserId, CreatedByUsername, CreatedAt
FROM TeacherPayAdjustments
WHERE TeacherId = ANY(@teacherIds)
  AND Year = @year
  AND Month = @month
ORDER BY TeacherId, CreatedAt, Id;"
            : @"
SELECT Id, TeacherId, Year, Month, ShiftCount, PayPerShift, Note, CreatedByUserId, CreatedByUsername, CreatedAt
FROM TeacherPayAdjustments
WHERE TeacherId = ANY(@teacherIds)
  AND Year = @year
ORDER BY TeacherId, Month, CreatedAt, Id;";
        command.Parameters.AddWithValue("@teacherIds", teacherIds.ToArray());
        command.Parameters.AddWithValue("@year", year);
        if (month is >= 1 and <= 12)
        {
            command.Parameters.AddWithValue("@month", month.Value);
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var adjustment = ReadPayAdjustment(reader);
            var key = (adjustment.TeacherId, adjustment.Month);
            if (!result.TryGetValue(key, out var list))
            {
                list = new List<TeacherPayAdjustment>();
                result[key] = list;
            }

            list.Add(adjustment);
        }

        return result;
    }

    /// <summary>
    /// Ước tính lương tháng nếu thêm một lần điều chỉnh mới (chưa lưu).
    /// </summary>
    public decimal CalculateEstimatedMonthlyPay(
        int teacherId,
        int year,
        int month,
        int additionalShiftCount,
        decimal additionalPayPerShift)
    {
        var presentShifts = GetPresentShiftsOrdered(teacherId, year, month);
        if (presentShifts.Count == 0)
        {
            return 0;
        }

        var defaultRate = GetTeacherPayPerShift(teacherId);
        var adjustments = GetPayAdjustmentsOrdered(teacherId, year, month);
        return CalculatePayFromAdjustments(
            presentShifts.Count,
            adjustments,
            defaultRate,
            additionalShiftCount,
            additionalPayPerShift);
    }

    public int GetTotalAdjustedShiftCount(int teacherId, int year, int month) =>
        GetPayAdjustmentsOrdered(teacherId, year, month).Sum(a => a.ShiftCount);

    public int GetRemainingAdjustableShifts(int teacherId, int year, int month)
    {
        var totalShifts = GetTotalShiftsInMonth(teacherId, year, month);
        return Math.Max(0, totalShifts - GetTotalAdjustedShiftCount(teacherId, year, month));
    }

    public TeacherPayAdjustment? GetLatestPayAdjustment(int teacherId, int year, int month)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, TeacherId, Year, Month, ShiftCount, PayPerShift, Note, CreatedByUserId, CreatedByUsername, CreatedAt
FROM TeacherPayAdjustments
WHERE TeacherId = @teacherId AND Year = @year AND Month = @month
ORDER BY CreatedAt DESC, Id DESC
LIMIT 1;";
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@month", month);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadPayAdjustment(reader) : null;
    }

    public List<TeacherPayAdjustment> GetPayAdjustmentHistory(int teacherId, int year, int month)
    {
        var result = new List<TeacherPayAdjustment>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, TeacherId, Year, Month, ShiftCount, PayPerShift, Note, CreatedByUserId, CreatedByUsername, CreatedAt
FROM TeacherPayAdjustments
WHERE TeacherId = @teacherId AND Year = @year AND Month = @month
ORDER BY CreatedAt DESC, Id DESC;";
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@month", month);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadPayAdjustment(reader));
        }

        return result;
    }

    public List<TeacherPayAdjustment> GetPayAdjustmentsOrdered(int teacherId, int year, int month)
    {
        var result = new List<TeacherPayAdjustment>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, TeacherId, Year, Month, ShiftCount, PayPerShift, Note, CreatedByUserId, CreatedByUsername, CreatedAt
FROM TeacherPayAdjustments
WHERE TeacherId = @teacherId AND Year = @year AND Month = @month
ORDER BY CreatedAt ASC, Id ASC;";
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@month", month);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadPayAdjustment(reader));
        }

        return result;
    }

    public void SavePayAdjustment(
        int teacherId,
        int year,
        int month,
        int shiftCount,
        decimal payPerShift,
        string? note,
        int createdByUserId,
        string createdByUsername)
    {
        if (payPerShift <= 0)
        {
            throw new InvalidOperationException("Lương mỗi ca phải lớn hơn 0.");
        }

        var totalShifts = GetTotalShiftsInMonth(teacherId, year, month);
        if (totalShifts == 0)
        {
            throw new InvalidOperationException("Giáo viên chưa có ca có mặt trong tháng này.");
        }

        var remainingShifts = GetRemainingAdjustableShifts(teacherId, year, month);
        if (remainingShifts == 0)
        {
            throw new InvalidOperationException("Đã điều chỉnh hết số ca có mặt trong tháng này.");
        }

        if (shiftCount <= 0 || shiftCount > remainingShifts)
        {
            throw new InvalidOperationException($"Số ca điều chỉnh phải từ 1 đến {remainingShifts}.");
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO TeacherPayAdjustments(TeacherId, Year, Month, ShiftCount, PayPerShift, Note, CreatedByUserId, CreatedByUsername, CreatedAt)
VALUES(@teacherId, @year, @month, @shiftCount, @payPerShift, @note, @createdByUserId, @createdByUsername, @createdAt);";
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@shiftCount", shiftCount);
        command.Parameters.AddWithValue("@payPerShift", payPerShift);
        command.Parameters.AddWithValue("@note", (object?)note ?? DBNull.Value);
        command.Parameters.AddWithValue("@createdByUserId", createdByUserId);
        command.Parameters.AddWithValue("@createdByUsername", createdByUsername);
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }

    public void DeletePayAdjustment(long adjustmentId, int teacherId, int year, int month)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
DELETE FROM TeacherPayAdjustments
WHERE Id = @id AND TeacherId = @teacherId AND Year = @year AND Month = @month;";
        command.Parameters.AddWithValue("@id", adjustmentId);
        command.Parameters.AddWithValue("@teacherId", teacherId);
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@month", month);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Không tìm thấy bản ghi điều chỉnh.");
        }
    }

    private static decimal CalculatePayFromAdjustments(
        int totalShifts,
        IReadOnlyList<TeacherPayAdjustment> adjustments,
        decimal defaultRate,
        int additionalShiftCount = 0,
        decimal additionalPayPerShift = 0)
    {
        if (totalShifts == 0)
        {
            return 0;
        }

        decimal total = 0;
        var shiftIndex = 0;

        void ApplyBucket(int count, decimal rate)
        {
            if (count <= 0 || shiftIndex >= totalShifts)
            {
                return;
            }

            var applied = Math.Min(count, totalShifts - shiftIndex);
            total += applied * rate;
            shiftIndex += applied;
        }

        foreach (var adjustment in adjustments)
        {
            ApplyBucket(adjustment.ShiftCount, adjustment.PayPerShift);
        }

        if (additionalShiftCount > 0 && additionalPayPerShift > 0)
        {
            ApplyBucket(additionalShiftCount, additionalPayPerShift);
        }

        total += (totalShifts - shiftIndex) * defaultRate;
        return total;
    }

    private static decimal GetPayRateForShiftIndex(
        int shiftIndex,
        int totalShifts,
        IReadOnlyList<TeacherPayAdjustment> adjustments,
        decimal defaultRate)
    {
        var cursor = 0;
        foreach (var adjustment in adjustments)
        {
            var count = Math.Min(adjustment.ShiftCount, totalShifts - cursor);
            if (shiftIndex < cursor + count)
            {
                return adjustment.PayPerShift;
            }

            cursor += count;
        }

        return defaultRate;
    }

    private List<TeacherTimesheet> GetPresentShiftsOrdered(int teacherId, int year, int month) =>
        GetTimesheetByMonth(teacherId, year, month)
            .Where(t => t.IsPresent)
            .OrderBy(t => t.WorkDate)
            .ThenBy(t => t.ShiftNumber)
            .ToList();

    private static TeacherPayAdjustment ReadPayAdjustment(System.Data.Common.DbDataReader reader) =>
        new()
        {
            Id = reader.GetInt64(0),
            TeacherId = reader.GetInt32(1),
            Year = reader.GetInt32(2),
            Month = reader.GetInt32(3),
            ShiftCount = reader.GetInt32(4),
            PayPerShift = Convert.ToDecimal(reader.GetValue(5)),
            Note = reader.IsDBNull(6) ? null : reader.GetString(6),
            CreatedByUserId = reader.GetInt32(7),
            CreatedByUsername = reader.GetString(8),
            CreatedAt = DateTime.Parse(reader.GetString(9))
        };

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

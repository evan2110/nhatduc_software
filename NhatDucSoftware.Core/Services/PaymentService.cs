using System.Data.Common;
using NhatDucSoftware.Core.Data;

namespace NhatDucSoftware.Core.Services;

public class PaymentService
{
    public const string BalancePaymentNote = "Thanh toán từ số dư";

    public decimal GetStudentBalance(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(Balance, 0) FROM Students WHERE Id = @studentId;";
        command.Parameters.AddWithValue("@studentId", studentId);
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public decimal GetTotalTuitionByStudent(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        // Tính: số buổi có mặt (C) của từng lớp × học phí khóa học tương ứng
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COALESCE(SUM(attended * co.TuitionFee), 0)
FROM (
    SELECT cs.ClassId,
           (SELECT COUNT(*) FROM AttendanceRecords ar
            INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
            WHERE ar.StudentId = @studentId AND ats.ClassId = cs.ClassId AND ar.Status = 'C') AS attended
    FROM ClassStudents cs
    WHERE cs.StudentId = @studentId
) sub
INNER JOIN Classes c ON c.Id = sub.ClassId
INNER JOIN Courses co ON co.Id = c.CourseId;";
        command.Parameters.AddWithValue("@studentId", studentId);

        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public decimal GetPaidAmount(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(SUM(Amount), 0) FROM Payments WHERE StudentId = @studentId;";
        command.Parameters.AddWithValue("@studentId", studentId);
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public decimal GetPaidAmountByStudentMonthYear(int studentId, int month, int year, int classId = 0)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"SELECT COALESCE(SUM(Amount), 0)
FROM Payments
WHERE StudentId = @studentId
  AND (@classId = 0 OR ClassId = @classId)
  AND EXTRACT(MONTH FROM CAST(PaymentDate AS timestamp)) = @month::numeric
  AND EXTRACT(YEAR FROM CAST(PaymentDate AS timestamp)) = @year::numeric;";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public decimal GetTotalTuitionByStudentInClassMonthYear(int studentId, int classId, int month, int year)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COALESCE(SUM(co.TuitionFee), 0)
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
INNER JOIN Classes c ON c.Id = ats.ClassId
INNER JOIN Courses co ON co.Id = c.CourseId
WHERE ar.StudentId = @studentId
  AND ar.Status = 'C'
  AND (@classId = 0 OR ats.ClassId = @classId)
  AND EXTRACT(MONTH FROM CAST(ats.SessionDate AS date)) = @month::numeric
  AND EXTRACT(YEAR FROM CAST(ats.SessionDate AS date)) = @year::numeric;";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var tuition = Convert.ToDecimal(command.ExecuteScalar());

        // Add carry-over from previous month
        using var carryCmd = connection.CreateCommand();
        carryCmd.CommandText = @"SELECT COALESCE(SUM(Amount), 0) FROM PaymentCarryOvers
WHERE StudentId = @studentId AND (@classId = 0 OR ClassId = @classId)
  AND ToMonth = @month AND ToYear = @year;";
        carryCmd.Parameters.AddWithValue("@studentId", studentId);
        carryCmd.Parameters.AddWithValue("@classId", classId);
        carryCmd.Parameters.AddWithValue("@month", month);
        carryCmd.Parameters.AddWithValue("@year", year);

        tuition += Convert.ToDecimal(carryCmd.ExecuteScalar());

        return tuition;
    }

    public decimal GetCarryOverAmount(int studentId, int classId, int month, int year)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT COALESCE(SUM(Amount), 0) FROM PaymentCarryOvers
WHERE StudentId = @studentId AND (@classId = 0 OR ClassId = @classId)
  AND ToMonth = @month AND ToYear = @year;";
        cmd.Parameters.AddWithValue("@studentId", studentId);
        cmd.Parameters.AddWithValue("@classId", classId);
        cmd.Parameters.AddWithValue("@month", month);
        cmd.Parameters.AddWithValue("@year", year);
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

    public decimal GetRemainingAmount(int studentId)
    {
        var remaining = GetTotalTuitionByStudent(studentId) - GetPaidAmount(studentId);
        return remaining > 0 ? remaining : 0;
    }

    public void Collect(int studentId, decimal amount, int createdBy, string? note, int? classId = null)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Số tiền thu bắt buộc phải lớn hơn 0.");
        }

        var today = DateTime.Today;
        if (classId is int collectClassId && IsFinalized(collectClassId, today.Month, today.Year))
        {
            throw new InvalidOperationException("Tháng này đã được chốt số liệu, không thể thu học phí.");
        }

        var remaining = GetRemainingAmount(studentId);
        if (remaining <= 0)
        {
            throw new InvalidOperationException("Học viên không còn học phí cần thu.");
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        long paymentId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO Payments(StudentId, ClassId, Amount, PaymentDate, Note, CreatedBy)
VALUES(@studentId, @classId, @amount, @date, @note, @createdBy)
RETURNING Id;";
            command.Parameters.AddWithValue("@studentId", studentId);
            command.Parameters.AddWithValue("@classId", classId.HasValue ? (object)classId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@note", (object?)note ?? DBNull.Value);
            command.Parameters.AddWithValue("@createdBy", createdBy);
            paymentId = Convert.ToInt64(command.ExecuteScalar());
        }

        using (var ledgerCmd = connection.CreateCommand())
        {
            ledgerCmd.Transaction = transaction;
            ledgerCmd.CommandText = @"INSERT INTO RevenueLedger(SourcePaymentId, Amount, PaymentDate)
SELECT Id, Amount, PaymentDate FROM Payments WHERE Id = @paymentId;";
            ledgerCmd.Parameters.AddWithValue("@paymentId", paymentId);
            ledgerCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public decimal PayFromBalance(int studentId, int classId, int month, int year, int createdBy)
    {
        if (IsFinalized(classId, month, year))
        {
            throw new InvalidOperationException("Tháng này đã được chốt số liệu, không thể thanh toán.");
        }

        var balance = GetStudentBalance(studentId);
        if (balance <= 0)
        {
            throw new InvalidOperationException("Học viên không có số dư để thanh toán.");
        }

        var total = GetTotalTuitionByStudentInClassMonthYear(studentId, classId, month, year);
        var paid = GetPaidAmountByStudentMonthYear(studentId, month, year, classId);
        var remaining = total - paid;
        if (remaining <= 0)
        {
            throw new InvalidOperationException("Học viên không còn học phí cần đóng trong tháng này.");
        }

        var amount = Math.Min(balance, remaining);

        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using (var balanceCmd = connection.CreateCommand())
        {
            balanceCmd.Transaction = transaction;
            balanceCmd.CommandText = @"UPDATE Students
SET Balance = Balance - @amount
WHERE Id = @studentId AND Balance >= @amount;";
            balanceCmd.Parameters.AddWithValue("@amount", amount);
            balanceCmd.Parameters.AddWithValue("@studentId", studentId);
            if (balanceCmd.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("Không thể trừ số dư. Vui lòng kiểm tra lại.");
            }
        }

        long paymentId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO Payments(StudentId, ClassId, Amount, PaymentDate, Note, CreatedBy)
VALUES(@studentId, @classId, @amount, @date, @note, @createdBy)
RETURNING Id;";
            command.Parameters.AddWithValue("@studentId", studentId);
            command.Parameters.AddWithValue("@classId", classId);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@note", BalancePaymentNote);
            command.Parameters.AddWithValue("@createdBy", createdBy);
            paymentId = Convert.ToInt64(command.ExecuteScalar());
        }

        using (var ledgerCmd = connection.CreateCommand())
        {
            ledgerCmd.Transaction = transaction;
            ledgerCmd.CommandText = @"INSERT INTO RevenueLedger(SourcePaymentId, Amount, PaymentDate)
SELECT Id, Amount, PaymentDate FROM Payments WHERE Id = @paymentId;";
            ledgerCmd.Parameters.AddWithValue("@paymentId", paymentId);
            ledgerCmd.ExecuteNonQuery();
        }

        transaction.Commit();
        return amount;
    }

    /// <summary>
    /// Lấy thông tin điểm danh của học viên: tổng buổi, có mặt, vắng.
    /// </summary>
    public (int TotalSessions, int Attended, int Absent) GetAttendanceSummary(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT 
    COUNT(*) AS Total,
    COALESCE(SUM(CASE WHEN Status = 'C' THEN 1 ELSE 0 END), 0) AS Attended,
    COALESCE(SUM(CASE WHEN Status = 'V' THEN 1 ELSE 0 END), 0) AS Absent
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
WHERE ar.StudentId = @studentId;";
        command.Parameters.AddWithValue("@studentId", studentId);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
        }
        return (0, 0, 0);
    }

    /// <summary>
    /// Lấy chi tiết các buổi học của học viên: ngày, lớp, trạng thái.
    /// </summary>
    public List<AttendanceDetailRow> GetAttendanceDetails(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ats.SessionDate, c.ClassName, ats.ShiftNumber, ar.Status
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
INNER JOIN Classes c ON c.Id = ats.ClassId
WHERE ar.StudentId = @studentId
ORDER BY ats.SessionDate DESC, ats.ShiftNumber;";
        command.Parameters.AddWithValue("@studentId", studentId);

        var results = new List<AttendanceDetailRow>();
        using var reader2 = command.ExecuteReader();
        while (reader2.Read())
        {
            results.Add(new AttendanceDetailRow
            {
                Ngay = reader2.GetString(0),
                Lop = reader2.GetString(1),
                Ca = reader2.GetInt32(2),
                TrangThai = reader2.GetString(3) == "C" ? "Có mặt" : "Vắng"
            });
        }
        return results;
    }

    public List<PaymentHistoryRow> GetPaymentHistory(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT p.Id,
       p.PaymentDate,
       p.Amount,
       COALESCE(u.Username, ''),
       COALESCE(p.Note, '')
FROM Payments p
LEFT JOIN Users u ON u.Id = p.CreatedBy
WHERE p.StudentId = @studentId
ORDER BY p.PaymentDate DESC, p.Id DESC;";
        command.Parameters.AddWithValue("@studentId", studentId);

        var results = new List<PaymentHistoryRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapPaymentHistoryRow(reader));
        }

        return results;
    }

    public List<PaymentHistoryRow> GetPaymentHistoryByClassMonthYear(int studentId, int classId, int month, int year)
    {
        if (month is < 1 or > 12)
        {
            return new List<PaymentHistoryRow>();
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT p.Id,
       p.PaymentDate,
       p.Amount,
       COALESCE(u.Username, ''),
       COALESCE(p.Note, ''),
       COALESCE(c.ClassName, '')
FROM Payments p
LEFT JOIN Users u ON u.Id = p.CreatedBy
LEFT JOIN Classes c ON c.Id = p.ClassId
WHERE p.StudentId = @studentId
  AND LEFT(p.PaymentDate::text, 7) = @yearMonth
  AND (@classId = 0 OR p.ClassId = @classId)
ORDER BY p.PaymentDate DESC, p.Id DESC;";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@yearMonth", $"{year}-{month:D2}");

        var results = new List<PaymentHistoryRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapPaymentHistoryRow(reader, includeClassName: true));
        }

        return results;
    }

    public List<StudentClassTuitionRow> GetStudentTuitionBreakdownByClassMonthYear(int studentId, int month, int year)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
WITH Enrollments AS (
    SELECT cs.ClassId, c.ClassName
    FROM ClassStudents cs
    INNER JOIN Classes c ON c.Id = cs.ClassId
    WHERE cs.StudentId = @studentId
),
StudentTuition AS (
    SELECT ats.ClassId,
           COALESCE(SUM(co.TuitionFee), 0) AS AttendanceTuition
    FROM AttendanceRecords ar
    INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
    INNER JOIN Classes c ON c.Id = ats.ClassId
    INNER JOIN Courses co ON co.Id = c.CourseId
    WHERE ar.StudentId = @studentId
      AND ar.Status = 'C'
      AND EXTRACT(MONTH FROM CAST(ats.SessionDate AS date)) = @month::numeric
      AND EXTRACT(YEAR FROM CAST(ats.SessionDate AS date)) = @year::numeric
    GROUP BY ats.ClassId
),
StudentCarryOver AS (
    SELECT ClassId,
           COALESCE(SUM(Amount), 0) AS CarryOver
    FROM PaymentCarryOvers
    WHERE StudentId = @studentId
      AND ToMonth = @month
      AND ToYear = @year
    GROUP BY ClassId
),
StudentPaid AS (
    SELECT p.ClassId,
           COALESCE(SUM(p.Amount), 0) AS Paid
    FROM Payments p
    WHERE p.StudentId = @studentId
      AND EXTRACT(MONTH FROM CAST(p.PaymentDate AS timestamp)) = @month::numeric
      AND EXTRACT(YEAR FROM CAST(p.PaymentDate AS timestamp)) = @year::numeric
    GROUP BY p.ClassId
)
SELECT e.ClassId,
       e.ClassName,
       COALESCE(st.AttendanceTuition, 0) + COALESCE(sc.CarryOver, 0),
       COALESCE(sp.Paid, 0),
       COALESCE(sc.CarryOver, 0)
FROM Enrollments e
LEFT JOIN StudentTuition st ON st.ClassId = e.ClassId
LEFT JOIN StudentCarryOver sc ON sc.ClassId = e.ClassId
LEFT JOIN StudentPaid sp ON sp.ClassId = e.ClassId
ORDER BY e.ClassName;";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var results = new List<StudentClassTuitionRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var due = ReadDecimal(reader, 2);
            var paid = ReadDecimal(reader, 3);
            results.Add(new StudentClassTuitionRow
            {
                ClassId = reader.GetInt32(0),
                ClassName = reader.GetString(1),
                TotalDue = due,
                Paid = paid,
                Remaining = Math.Max(0, due - paid),
                CarryOver = ReadDecimal(reader, 4)
            });
        }

        return results;
    }

    private static PaymentHistoryRow MapPaymentHistoryRow(DbDataReader reader, bool includeClassName = false)
    {
        return new PaymentHistoryRow
        {
            PaymentId = Convert.ToInt32(reader.GetValue(0)),
            NgayThu = FormatPaymentDate(reader, 1),
            SoTien = ReadDecimal(reader, 2),
            NguoiThu = ReadString(reader, 3),
            GhiChu = ReadString(reader, 4),
            Lop = includeClassName ? ReadString(reader, 5) : ""
        };
    }

    private static string ReadString(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return "";
        }

        return Convert.ToString(reader.GetValue(ordinal)) ?? "";
    }

    private static decimal ReadDecimal(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static string FormatPaymentDate(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return "";
        }

        var value = reader.GetValue(ordinal);
        if (value is DateTime dt)
        {
            return dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        }

        var text = Convert.ToString(value) ?? "";
        return DateTime.TryParse(text, out var parsed)
            ? parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            : text;
    }

    public List<PaymentClassListRow> GetPaymentListByClassMonthYear(int classId, int month, int year)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
WITH FilteredStudents AS (
    SELECT s.Id,
           s.FullName
    FROM Students s
    WHERE @classId = 0
       OR EXISTS (
           SELECT 1
           FROM ClassStudents cs
           WHERE cs.StudentId = s.Id
             AND cs.ClassId = @classId
       )
),
FilteredPayments AS (
    SELECT p.Id,
           p.StudentId,
           p.Amount,
           p.PaymentDate,
           p.CreatedBy
    FROM Payments p
    WHERE (@classId = 0 OR p.ClassId = @classId)
      AND EXTRACT(MONTH FROM CAST(p.PaymentDate AS timestamp)) = @month::numeric
      AND EXTRACT(YEAR FROM CAST(p.PaymentDate AS timestamp)) = @year::numeric
),
LatestPaymentByStudent AS (
    SELECT fp.StudentId,
           fp.Id,
           fp.PaymentDate,
           fp.CreatedBy
    FROM FilteredPayments fp
    WHERE fp.Id = (
        SELECT fp2.Id
        FROM FilteredPayments fp2
        WHERE fp2.StudentId = fp.StudentId
        ORDER BY fp2.PaymentDate DESC, fp2.Id DESC
        LIMIT 1
    )
),
TotalAmountByStudent AS (
    SELECT fp.StudentId,
           SUM(fp.Amount) AS TotalAmount
    FROM FilteredPayments fp
    GROUP BY fp.StudentId
)
SELECT COALESCE(lp.Id, 0)::int,
       fs.Id,
       fs.FullName,
       COALESCE((
           SELECT STRING_AGG(c.ClassName, ', ')
           FROM ClassStudents cs
           INNER JOIN Classes c ON c.Id = cs.ClassId
           WHERE cs.StudentId = fs.Id
             AND (@classId = 0 OR cs.ClassId = @classId)
       ), ''),
       lp.PaymentDate,
       COALESCE(t.TotalAmount, 0),
       COALESCE(u.Username, '')
FROM FilteredStudents fs
LEFT JOIN LatestPaymentByStudent lp ON lp.StudentId = fs.Id
LEFT JOIN TotalAmountByStudent t ON t.StudentId = fs.Id
LEFT JOIN Users u ON u.Id = lp.CreatedBy
ORDER BY (lp.PaymentDate IS NULL), lp.PaymentDate DESC, fs.FullName ASC;";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var results = new List<PaymentClassListRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var displayDate = string.Empty;
            if (!reader.IsDBNull(4))
            {
                displayDate = FormatPaymentDate(reader, 4);
            }

            results.Add(new PaymentClassListRow
            {
                PaymentId = reader.GetInt32(0),
                StudentId = reader.GetInt32(1),
                HoVaTen = reader.GetString(2),
                Lop = reader.GetString(3),
                NgayThu = displayDate,
                SoTien = Convert.ToDecimal(reader.GetValue(5)),
                NguoiThu = reader.GetString(6)
            });
        }

        for (int i = 0; i < results.Count; i++)
        {
            results[i].ThuTu = i + 1;
        }

        return results;
    }

    public ClassPaymentSummary GetClassPaymentSummary(int classId, int month, int year)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
WITH Enrollments AS (
    SELECT cs.StudentId, cs.ClassId
    FROM ClassStudents cs
    WHERE @classId = 0 OR cs.ClassId = @classId
),
StudentTuition AS (
    SELECT ar.StudentId,
           ats.ClassId,
           COALESCE(SUM(co.TuitionFee), 0) AS AttendanceTuition
    FROM AttendanceRecords ar
    INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
    INNER JOIN Classes c ON c.Id = ats.ClassId
    INNER JOIN Courses co ON co.Id = c.CourseId
    WHERE ar.Status = 'C'
      AND (@classId = 0 OR ats.ClassId = @classId)
      AND EXTRACT(MONTH FROM CAST(ats.SessionDate AS date)) = @month::numeric
      AND EXTRACT(YEAR FROM CAST(ats.SessionDate AS date)) = @year::numeric
    GROUP BY ar.StudentId, ats.ClassId
),
StudentCarryOver AS (
    SELECT StudentId,
           ClassId,
           COALESCE(SUM(Amount), 0) AS CarryOver
    FROM PaymentCarryOvers
    WHERE (@classId = 0 OR ClassId = @classId)
      AND ToMonth = @month
      AND ToYear = @year
    GROUP BY StudentId, ClassId
),
StudentPaid AS (
    SELECT p.StudentId,
           p.ClassId,
           COALESCE(SUM(p.Amount), 0) AS Paid
    FROM Payments p
    WHERE (@classId = 0 OR p.ClassId = @classId)
      AND EXTRACT(MONTH FROM CAST(p.PaymentDate AS timestamp)) = @month::numeric
      AND EXTRACT(YEAR FROM CAST(p.PaymentDate AS timestamp)) = @year::numeric
    GROUP BY p.StudentId, p.ClassId
),
PerClass AS (
    SELECT e.StudentId,
           e.ClassId,
           COALESCE(st.AttendanceTuition, 0) AS AttendanceDue,
           COALESCE(sc.CarryOver, 0) AS CarryOverDue,
           COALESCE(st.AttendanceTuition, 0) + COALESCE(sc.CarryOver, 0) AS Due,
           COALESCE(sp.Paid, 0) AS Paid
    FROM Enrollments e
    LEFT JOIN StudentTuition st ON st.StudentId = e.StudentId AND st.ClassId = e.ClassId
    LEFT JOIN StudentCarryOver sc ON sc.StudentId = e.StudentId AND sc.ClassId = e.ClassId
    LEFT JOIN StudentPaid sp ON sp.StudentId = e.StudentId AND sp.ClassId = e.ClassId
)
SELECT COALESCE(SUM(Due), 0),
       COALESCE(SUM(Paid), 0),
       COALESCE(SUM(GREATEST(0, Due - Paid)), 0),
       COALESCE(SUM(CarryOverDue), 0),
       COALESCE(SUM(AttendanceDue), 0)
FROM PerClass;";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new ClassPaymentSummary
            {
                TotalDue = ReadDecimal(reader, 0),
                TotalPaid = ReadDecimal(reader, 1),
                TotalRemaining = ReadDecimal(reader, 2),
                TotalCarryOver = ReadDecimal(reader, 3),
                TotalAttendanceDue = ReadDecimal(reader, 4)
            };
        }

        return new ClassPaymentSummary();
    }

    public bool IsFinalized(int classId, int month, int year)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM PaymentFinalizations WHERE ClassId = @classId AND Month = @month AND Year = @year;";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public void FinalizePayment(int classId, int month, int year, int finalizedBy)
    {
        var today = DateTime.Today;
        if (month != today.Month || year != today.Year)
        {
            throw new InvalidOperationException("Chỉ được chốt số liệu cho tháng hiện tại.");
        }

        if (today.Day != DateTime.DaysInMonth(today.Year, today.Month))
        {
            throw new InvalidOperationException(
                $"Chỉ có thể chốt số liệu vào ngày cuối cùng của tháng (ngày {DateTime.DaysInMonth(today.Year, today.Month)}).");
        }

        if (IsFinalized(classId, month, year))
        {
            throw new InvalidOperationException("Tháng này đã được chốt số liệu rồi.");
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        // Get all students in the class with remaining amounts
        var studentsWithRemaining = new List<(int StudentId, decimal Remaining)>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = @"
SELECT cs.StudentId
FROM ClassStudents cs
WHERE cs.ClassId = @classId;";
            cmd.Parameters.AddWithValue("@classId", classId);
            using var reader = cmd.ExecuteReader();
            var studentIds = new List<int>();
            while (reader.Read())
            {
                studentIds.Add(reader.GetInt32(0));
            }
            reader.Close();

            foreach (var studentId in studentIds)
            {
                var total = GetTotalTuitionByStudentInClassMonthYear(studentId, classId, month, year);
                var paid = GetPaidAmountByStudentMonthYear(studentId, month, year, classId);
                var remaining = total - paid;
                if (remaining > 0)
                {
                    studentsWithRemaining.Add((studentId, remaining));
                }
            }
        }

        // Carry over remaining amounts to next month
        int nextMonth = month == 12 ? 1 : month + 1;
        int nextYear = month == 12 ? year + 1 : year;

        foreach (var (studentId, remaining) in studentsWithRemaining)
        {
            using var carryCmd = connection.CreateCommand();
            carryCmd.Transaction = transaction;
            carryCmd.CommandText = @"INSERT INTO PaymentCarryOvers(StudentId, ClassId, FromMonth, FromYear, ToMonth, ToYear, Amount)
VALUES(@studentId, @classId, @fromMonth, @fromYear, @toMonth, @toYear, @amount)
ON CONFLICT(StudentId, ClassId, FromMonth, FromYear)
DO UPDATE SET Amount = EXCLUDED.Amount, ToMonth = EXCLUDED.ToMonth, ToYear = EXCLUDED.ToYear;";
            carryCmd.Parameters.AddWithValue("@studentId", studentId);
            carryCmd.Parameters.AddWithValue("@classId", classId);
            carryCmd.Parameters.AddWithValue("@fromMonth", month);
            carryCmd.Parameters.AddWithValue("@fromYear", year);
            carryCmd.Parameters.AddWithValue("@toMonth", nextMonth);
            carryCmd.Parameters.AddWithValue("@toYear", nextYear);
            carryCmd.Parameters.AddWithValue("@amount", remaining);
            carryCmd.ExecuteNonQuery();
        }

        // Insert finalization record
        using (var finalizeCmd = connection.CreateCommand())
        {
            finalizeCmd.Transaction = transaction;
            finalizeCmd.CommandText = @"INSERT INTO PaymentFinalizations(ClassId, Month, Year, FinalizedAt, FinalizedBy)
VALUES(@classId, @month, @year, @finalizedAt, @finalizedBy);";
            finalizeCmd.Parameters.AddWithValue("@classId", classId);
            finalizeCmd.Parameters.AddWithValue("@month", month);
            finalizeCmd.Parameters.AddWithValue("@year", year);
            finalizeCmd.Parameters.AddWithValue("@finalizedAt", DateTime.UtcNow.ToString("o"));
            finalizeCmd.Parameters.AddWithValue("@finalizedBy", finalizedBy);
            finalizeCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void UpdatePaymentHistory(int paymentId, int studentId, decimal amount, string? note)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Số tiền thu bắt buộc phải lớn hơn 0.");
        }

        EnsurePaymentNotFinalized(paymentId, studentId);

        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using (var updatePaymentCmd = connection.CreateCommand())
        {
            updatePaymentCmd.Transaction = transaction;
            updatePaymentCmd.CommandText = @"UPDATE Payments
SET Amount = @amount, Note = @note
WHERE Id = @paymentId AND StudentId = @studentId;";
            updatePaymentCmd.Parameters.AddWithValue("@amount", amount);
            updatePaymentCmd.Parameters.AddWithValue("@note", (object?)note ?? DBNull.Value);
            updatePaymentCmd.Parameters.AddWithValue("@paymentId", paymentId);
            updatePaymentCmd.Parameters.AddWithValue("@studentId", studentId);

            if (updatePaymentCmd.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("Không tìm thấy lịch sử thu để cập nhật.");
            }
        }

        using (var updateLedgerCmd = connection.CreateCommand())
        {
            updateLedgerCmd.Transaction = transaction;
            updateLedgerCmd.CommandText = @"UPDATE RevenueLedger
SET Amount = @amount
WHERE SourcePaymentId = @paymentId;";
            updateLedgerCmd.Parameters.AddWithValue("@amount", amount);
            updateLedgerCmd.Parameters.AddWithValue("@paymentId", paymentId);
            updateLedgerCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void DeletePaymentHistory(int paymentId, int studentId)
    {
        EnsurePaymentNotFinalized(paymentId, studentId);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        decimal paymentAmount = 0;
        string paymentNote = "";
        using (var readCmd = connection.CreateCommand())
        {
            readCmd.CommandText = @"SELECT Amount, COALESCE(Note, '') FROM Payments WHERE Id = @paymentId AND StudentId = @studentId;";
            readCmd.Parameters.AddWithValue("@paymentId", paymentId);
            readCmd.Parameters.AddWithValue("@studentId", studentId);
            using var reader = readCmd.ExecuteReader();
            if (!reader.Read())
            {
                throw new InvalidOperationException("Không tìm thấy lịch sử thu để xóa.");
            }
            paymentAmount = Convert.ToDecimal(reader.GetValue(0));
            paymentNote = reader.GetString(1);
        }

        using var transaction = connection.BeginTransaction();

        using (var deleteLedgerCmd = connection.CreateCommand())
        {
            deleteLedgerCmd.Transaction = transaction;
            deleteLedgerCmd.CommandText = "DELETE FROM RevenueLedger WHERE SourcePaymentId = @paymentId;";
            deleteLedgerCmd.Parameters.AddWithValue("@paymentId", paymentId);
            deleteLedgerCmd.ExecuteNonQuery();
        }

        using (var deletePaymentCmd = connection.CreateCommand())
        {
            deletePaymentCmd.Transaction = transaction;
            deletePaymentCmd.CommandText = "DELETE FROM Payments WHERE Id = @paymentId AND StudentId = @studentId;";
            deletePaymentCmd.Parameters.AddWithValue("@paymentId", paymentId);
            deletePaymentCmd.Parameters.AddWithValue("@studentId", studentId);

            if (deletePaymentCmd.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("Không tìm thấy lịch sử thu để xóa.");
            }
        }

        if (paymentNote == BalancePaymentNote)
        {
            using var restoreCmd = connection.CreateCommand();
            restoreCmd.Transaction = transaction;
            restoreCmd.CommandText = @"UPDATE Students SET Balance = Balance + @amount WHERE Id = @studentId;";
            restoreCmd.Parameters.AddWithValue("@amount", paymentAmount);
            restoreCmd.Parameters.AddWithValue("@studentId", studentId);
            restoreCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private void EnsurePaymentNotFinalized(int paymentId, int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ClassId, PaymentDate
FROM Payments
WHERE Id = @paymentId AND StudentId = @studentId
LIMIT 1;";
        command.Parameters.AddWithValue("@paymentId", paymentId);
        command.Parameters.AddWithValue("@studentId", studentId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("Không tìm thấy lịch sử thu để thao tác.");
        }

        if (reader.IsDBNull(0))
        {
            return;
        }

        var classId = reader.GetInt32(0);
        var (month, year) = ExtractMonthYear(reader.GetValue(1));
        if (IsFinalized(classId, month, year))
        {
            throw new InvalidOperationException("Tháng này đã được chốt số liệu, không thể sửa hoặc xóa lịch sử thu.");
        }
    }

    private static (int Month, int Year) ExtractMonthYear(object value)
    {
        if (value is DateTime dt)
        {
            var local = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
            return (local.Month, local.Year);
        }

        var text = Convert.ToString(value) ?? string.Empty;
        if (DateTime.TryParse(text, out var parsed))
        {
            var local = parsed.Kind == DateTimeKind.Utc ? parsed.ToLocalTime() : parsed;
            return (local.Month, local.Year);
        }

        throw new InvalidOperationException("Không thể xác định tháng/năm của lịch sử thu.");
    }
}

public class AttendanceDetailRow
{
    public string Ngay { get; set; } = "";
    public string Lop { get; set; } = "";
    public int Ca { get; set; }
    public string TrangThai { get; set; } = "";
}

public class PaymentHistoryRow
{
    public int PaymentId { get; set; }
    public string NgayThu { get; set; } = "";
    public decimal SoTien { get; set; }
    public string NguoiThu { get; set; } = "";
    public string GhiChu { get; set; } = "";
    public string Lop { get; set; } = "";
}

public class StudentClassTuitionRow
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = "";
    public decimal TotalDue { get; set; }
    public decimal Paid { get; set; }
    public decimal Remaining { get; set; }
    public decimal CarryOver { get; set; }
}

public class ClassPaymentSummary
{
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal TotalCarryOver { get; set; }
    public decimal TotalAttendanceDue { get; set; }
}

public class PaymentClassListRow
{
    public int ThuTu { get; set; }
    public int PaymentId { get; set; }
    public int StudentId { get; set; }
    public string HoVaTen { get; set; } = "";
    public string Lop { get; set; } = "";
    public string NgayThu { get; set; } = "";
    public decimal SoTien { get; set; }
    public string NguoiThu { get; set; } = "";
}

using System.Data.Common;
using Npgsql;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Models;

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
        var allocations = BuildStudentTuitionAllocations(studentId, month, year);
        if (classId == 0)
        {
            return allocations.Sum(a => a.TotalDue);
        }

        return allocations.FirstOrDefault(a => a.ClassId == classId)?.TotalDue ?? 0;
    }

    public decimal GetStudentTuitionDiscountPercent(int studentId, int month, int year) =>
        GetStudentTuitionDiscount(studentId, month, year).DiscountPercent;

    public StudentTuitionDiscountInfo GetStudentTuitionDiscount(int studentId, int month, int year)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT DiscountPercent, COALESCE(Note, '')
FROM StudentTuitionDiscounts
WHERE StudentId = @studentId
  AND Month = @month
  AND Year = @year
LIMIT 1;";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return new StudentTuitionDiscountInfo();
        }

        return new StudentTuitionDiscountInfo
        {
            DiscountPercent = ReadDecimal(reader, 0),
            Note = reader.GetString(1)
        };
    }

    public bool IsStudentTuitionDiscountLocked(int studentId, int month, int year)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT COUNT(*)
FROM ClassStudents cs
INNER JOIN PaymentFinalizations pf
        ON pf.ClassId = cs.ClassId
       AND pf.Month = @month
       AND pf.Year = @year
WHERE cs.StudentId = @studentId;";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public StudentTuitionDiscountPreview GetStudentTuitionDiscountPreview(
        int studentId,
        int month,
        int year,
        decimal? discountPercent = null,
        string? note = null)
    {
        var grossRows = GetStudentTuitionGrossRows(studentId, month, year);
        var storedDiscount = GetStudentTuitionDiscount(studentId, month, year);
        var percent = discountPercent ?? storedDiscount.DiscountPercent;
        var allocations = TuitionDiscountAllocator.Allocate(grossRows, percent);
        var totalGross = grossRows.Sum(r => r.GrossAttendance);
        var totalCarryOver = grossRows.Sum(r => r.CarryOver);
        var totalDiscountable = grossRows.Sum(r => r.DiscountableBase);
        var totalDiscount = allocations.Sum(a => a.DiscountAllocated);
        var totalDue = allocations.Sum(a => a.TotalDue);
        var totalPaid = GetPaidAmountByStudentMonthYear(studentId, month, year, 0);

        return new StudentTuitionDiscountPreview
        {
            StudentId = studentId,
            Month = month,
            Year = year,
            DiscountPercent = percent,
            Note = note ?? storedDiscount.Note,
            TotalGrossAttendance = totalGross,
            TotalCarryOver = totalCarryOver,
            TotalDiscountableBase = totalDiscountable,
            TotalDiscountAmount = totalDiscount,
            TotalDueAfterDiscount = totalDue,
            TotalPaid = totalPaid,
            TotalRemainingAfterDiscount = Math.Max(0, totalDue - totalPaid),
            ClassAllocations = allocations,
            IsLocked = IsStudentTuitionDiscountLocked(studentId, month, year)
        };
    }

    public void SetStudentTuitionDiscount(
        int studentId,
        int month,
        int year,
        decimal discountPercent,
        int createdBy,
        string? note = null)
    {
        if (discountPercent < 0 || discountPercent > 100)
        {
            throw new InvalidOperationException("Phần trăm giảm phải từ 0 đến 100.");
        }

        if (discountPercent > 0 && string.IsNullOrWhiteSpace(note))
        {
            throw new InvalidOperationException("Vui lòng nhập ghi chú lý do giảm phí.");
        }

        EnsureCanManageTuitionDiscount(createdBy);

        if (IsStudentTuitionDiscountLocked(studentId, month, year))
        {
            throw new InvalidOperationException("Tháng này đã được chốt số liệu ở ít nhất một lớp, không thể sửa giảm phí.");
        }

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        using var connection = DbContext.CreateConnection();
        connection.Open();

        if (discountPercent == 0)
        {
            using var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = @"
DELETE FROM StudentTuitionDiscounts
WHERE StudentId = @studentId
  AND Month = @month
  AND Year = @year;";
            deleteCmd.Parameters.AddWithValue("@studentId", studentId);
            deleteCmd.Parameters.AddWithValue("@month", month);
            deleteCmd.Parameters.AddWithValue("@year", year);
            deleteCmd.ExecuteNonQuery();
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO StudentTuitionDiscounts(StudentId, Month, Year, DiscountPercent, Note, CreatedBy, CreatedAt)
VALUES(@studentId, @month, @year, @discountPercent, @note, @createdBy, @createdAt)
ON CONFLICT(StudentId, Month, Year)
DO UPDATE SET DiscountPercent = EXCLUDED.DiscountPercent,
              Note = EXCLUDED.Note,
              CreatedBy = EXCLUDED.CreatedBy,
              CreatedAt = EXCLUDED.CreatedAt;";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@discountPercent", discountPercent);
        command.Parameters.AddWithValue("@note", (object?)normalizedNote ?? DBNull.Value);
        command.Parameters.AddWithValue("@createdBy", createdBy);
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }

    private static void EnsureCanManageTuitionDiscount(int userId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Username, Role FROM Users WHERE Id = @userId LIMIT 1;";
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("Không tìm thấy tài khoản thực hiện thao tác.");
        }

        var user = new AuthenticatedUser
        {
            Id = userId,
            Username = reader.GetString(0),
            Role = reader.GetString(1)
        };

        if (!AdminPermissions.CanManageTuitionDiscount(user))
        {
            throw new InvalidOperationException("Bạn không có quyền giảm phí học viên.");
        }
    }

    private List<TuitionClassGrossRow> GetStudentTuitionGrossRows(int studentId, int month, int year)
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
)
SELECT e.ClassId,
       e.ClassName,
       COALESCE(st.AttendanceTuition, 0),
       COALESCE(sc.CarryOver, 0)
FROM Enrollments e
LEFT JOIN StudentTuition st ON st.ClassId = e.ClassId
LEFT JOIN StudentCarryOver sc ON sc.ClassId = e.ClassId
ORDER BY e.ClassName;";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var rows = new List<TuitionClassGrossRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new TuitionClassGrossRow
            {
                ClassId = reader.GetInt32(0),
                ClassName = reader.GetString(1),
                GrossAttendance = ReadDecimal(reader, 2),
                CarryOver = ReadDecimal(reader, 3)
            });
        }

        return rows;
    }

    private List<TuitionClassAllocation> BuildStudentTuitionAllocations(int studentId, int month, int year)
    {
        var grossRows = GetStudentTuitionGrossRows(studentId, month, year);
        var discountPercent = GetStudentTuitionDiscountPercent(studentId, month, year);
        return TuitionDiscountAllocator.Allocate(grossRows, discountPercent);
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

    public decimal GetStudentCollectibleRemaining(int studentId, int month, int year) =>
        GetStudentClassCollectibleRows(studentId, month, year)
            .Where(r => !r.IsFinalized && r.Remaining > 0)
            .Sum(r => r.Remaining);

    public List<ClassCollectibleRow> GetStudentClassCollectibleRows(int studentId, int month, int year) =>
        GetStudentTuitionBreakdownByClassMonthYear(studentId, month, year)
            .Select(row => new ClassCollectibleRow
            {
                ClassId = row.ClassId,
                ClassName = row.ClassName,
                Remaining = row.Remaining,
                IsFinalized = IsFinalized(row.ClassId, month, year)
            })
            .ToList();

    public void CollectForStudentMonth(
        int studentId,
        int month,
        int year,
        decimal amount,
        int createdBy,
        string? note)
    {
        EnsureCurrentMonth(month, year);

        if (amount <= 0)
        {
            throw new InvalidOperationException("Số tiền thu bắt buộc phải lớn hơn 0.");
        }

        var collectible = GetStudentCollectibleRemaining(studentId, month, year);
        if (collectible <= 0)
        {
            throw new InvalidOperationException("Học viên không còn học phí cần thu trong tháng này.");
        }

        if (amount > collectible)
        {
            throw new InvalidOperationException(
                $"Số tiền thu không được lớn hơn số còn lại ({collectible:N0}đ).");
        }

        var slices = BuildPaymentSlices(studentId, month, year, amount);
        InsertAllocatedPayments(studentId, slices, createdBy, note);
    }

    public decimal PayFromBalanceForStudentMonth(int studentId, int month, int year, int createdBy)
    {
        EnsureCurrentMonth(month, year);

        var balance = GetStudentBalance(studentId);
        if (balance <= 0)
        {
            throw new InvalidOperationException("Học viên không có số dư để thanh toán.");
        }

        var collectible = GetStudentCollectibleRemaining(studentId, month, year);
        if (collectible <= 0)
        {
            throw new InvalidOperationException("Học viên không còn học phí cần đóng trong tháng này.");
        }

        var amount = Math.Min(balance, collectible);
        var slices = BuildPaymentSlices(studentId, month, year, amount);

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

        InsertAllocatedPayments(studentId, slices, createdBy, BalancePaymentNote, connection, transaction);
        transaction.Commit();
        return amount;
    }

    private static void EnsureCurrentMonth(int month, int year)
    {
        var today = DateTime.Today;
        if (month != today.Month || year != today.Year)
        {
            throw new InvalidOperationException("Chỉ được phép thu học phí của tháng hiện tại.");
        }
    }

    private List<PaymentClassSlice> BuildPaymentSlices(int studentId, int month, int year, decimal amount)
    {
        var rows = GetStudentClassCollectibleRows(studentId, month, year);
        var slices = PaymentAllocator.Allocate(amount, rows);
        if (slices.Count == 0)
        {
            throw new InvalidOperationException("Không thể phân bổ khoản thu. Vui lòng kiểm tra lại trạng thái chốt số liệu.");
        }

        return slices;
    }

    private void InsertAllocatedPayments(
        int studentId,
        IReadOnlyList<PaymentClassSlice> slices,
        int createdBy,
        string? note)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        InsertAllocatedPayments(studentId, slices, createdBy, note, connection, transaction);
        transaction.Commit();
    }

    private static void InsertAllocatedPayments(
        int studentId,
        IReadOnlyList<PaymentClassSlice> slices,
        int createdBy,
        string? note,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        var paymentDate = DateTime.UtcNow.ToString("o");

        foreach (var slice in slices)
        {
            if (slice.Amount <= 0)
            {
                continue;
            }

            long paymentId;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO Payments(StudentId, ClassId, Amount, PaymentDate, Note, CreatedBy)
VALUES(@studentId, @classId, @amount, @date, @note, @createdBy)
RETURNING Id;";
                command.Parameters.AddWithValue("@studentId", studentId);
                command.Parameters.AddWithValue("@classId", slice.ClassId);
                command.Parameters.AddWithValue("@amount", slice.Amount);
                command.Parameters.AddWithValue("@date", paymentDate);
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
        }
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
        var allocations = BuildStudentTuitionAllocations(studentId, month, year);
        var paidByClass = GetStudentPaidByClass(studentId, month, year);

        var rows = allocations
            .Select(allocation =>
            {
                var paid = paidByClass.GetValueOrDefault(allocation.ClassId);
                var totalDue = allocation.TotalDue;
                return new StudentClassTuitionRow
                {
                    ClassId = allocation.ClassId,
                    ClassName = allocation.ClassName,
                    GrossAttendance = allocation.GrossAttendance,
                    DiscountAmount = allocation.DiscountAllocated,
                    TotalDue = totalDue,
                    Paid = paid,
                    Remaining = Math.Max(0, totalDue - paid),
                    CarryOver = allocation.CarryOver
                };
            })
            .ToList();

        var totalPaid = GetPaidAmountByStudentMonthYear(studentId, month, year, 0);
        var allocatedPaid = paidByClass.Values.Sum();
        var unallocatedPaid = totalPaid - allocatedPaid;
        if (unallocatedPaid > 0)
        {
            PaymentServiceInternals.ApplyUnallocatedPayments(rows, unallocatedPaid);
        }

        PaymentServiceInternals.ApplyOverpaymentCredit(rows);
        return rows;
    }

    private static void ApplyOverpaymentCredit(List<StudentClassTuitionRow> rows) =>
        PaymentServiceInternals.ApplyOverpaymentCredit(rows);

    private static void ApplyUnallocatedPayments(List<StudentClassTuitionRow> rows, decimal unallocatedPaid) =>
        PaymentServiceInternals.ApplyUnallocatedPayments(rows, unallocatedPaid);

    private Dictionary<int, decimal> GetStudentPaidByClass(int studentId, int month, int year)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT p.ClassId,
       COALESCE(SUM(p.Amount), 0) AS Paid
FROM Payments p
WHERE p.StudentId = @studentId
  AND EXTRACT(MONTH FROM CAST(p.PaymentDate AS timestamp)) = @month::numeric
  AND EXTRACT(YEAR FROM CAST(p.PaymentDate AS timestamp)) = @year::numeric
GROUP BY p.ClassId;";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var paidByClass = new Dictionary<int, decimal>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            paidByClass[reader.GetInt32(0)] = ReadDecimal(reader, 1);
        }

        return paidByClass;
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

    public ClassPaymentSummary GetClassPaymentSummary(int classId, int month, int year) =>
        PaymentMonthBatch.Load(classId, month, year).Summary;

    public PaymentMonthBatch LoadMonthBatch(int classId, int month, int year) =>
        PaymentMonthBatch.Load(classId, month, year);

    public TuitionYearReportData LoadTuitionYearReport(int year) =>
        TuitionYearReportData.Load(year);

    public List<ClassTuitionDetailStat> GetClassTuitionAfterDiscountDetail(int year, int? month = null) =>
        TuitionYearReportData.Load(year).GetClassDetail(month);

    private List<int> GetStudentIdsForPaymentSummary(int classId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = classId == 0
            ? @"SELECT DISTINCT cs.StudentId FROM ClassStudents cs ORDER BY cs.StudentId;"
            : @"SELECT DISTINCT cs.StudentId FROM ClassStudents cs WHERE cs.ClassId = @classId ORDER BY cs.StudentId;";
        if (classId > 0)
        {
            command.Parameters.AddWithValue("@classId", classId);
        }

        var studentIds = new List<int>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            studentIds.Add(reader.GetInt32(0));
        }

        return studentIds;
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

    public List<int> GetPendingFinalizeClassIds(int month, int year)
    {
        var classIds = new ClassService().GetAll().Select(c => c.Id).ToList();
        if (classIds.Count == 0)
        {
            return new List<int>();
        }

        using var connection = DbContext.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ClassId
FROM PaymentFinalizations
WHERE Month = @month AND Year = @year;";
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var finalized = new HashSet<int>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                finalized.Add(reader.GetInt32(0));
            }
        }

        return classIds.Where(id => !finalized.Contains(id)).ToList();
    }

    public FinalizeAllResult FinalizeAllPayments(int month, int year, int finalizedBy)
    {
        var allClassIds = new ClassService().GetAll().Select(c => c.Id).ToList();
        var pending = GetPendingFinalizeClassIds(month, year);
        var result = new FinalizeAllResult
        {
            SkippedAlreadyFinalized = allClassIds.Count - pending.Count
        };

        foreach (var classId in pending)
        {
            FinalizePayment(classId, month, year, finalizedBy);
            result.FinalizedCount++;
        }

        return result;
    }

    public void FinalizePayment(int classId, int month, int year, int finalizedBy)
    {
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
                var breakdown = GetStudentTuitionBreakdownByClassMonthYear(studentId, month, year);
                var remaining = PaymentServiceInternals.GetCarryAmountForClass(breakdown, classId);
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
    public decimal GrossAttendance { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAttendance => Math.Max(0, GrossAttendance - Math.Min(DiscountAmount, GrossAttendance));
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

public sealed class FinalizeAllResult
{
    public int FinalizedCount { get; set; }
    public int SkippedAlreadyFinalized { get; set; }
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

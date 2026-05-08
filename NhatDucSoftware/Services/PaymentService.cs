using NhatDucSoftware.Data;

namespace NhatDucSoftware.Services;

public class PaymentService
{
    public decimal GetTotalTuitionByStudent(int studentId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        // Tính: số buổi có mặt (C) của từng lớp × học phí khóa học tương ứng
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT IFNULL(SUM(attended * co.TuitionFee), 0)
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
        command.CommandText = "SELECT IFNULL(SUM(Amount), 0) FROM Payments WHERE StudentId = @studentId;";
        command.Parameters.AddWithValue("@studentId", studentId);
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public decimal GetPaidAmountByStudentMonthYear(int studentId, int month, int year)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"SELECT IFNULL(SUM(Amount), 0)
FROM Payments
WHERE StudentId = @studentId
  AND CAST(strftime('%m', PaymentDate) AS INTEGER) = @month
  AND CAST(strftime('%Y', PaymentDate) AS INTEGER) = @year;";
        command.Parameters.AddWithValue("@studentId", studentId);
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
SELECT IFNULL(SUM(co.TuitionFee), 0)
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
INNER JOIN Classes c ON c.Id = ats.ClassId
INNER JOIN Courses co ON co.Id = c.CourseId
WHERE ar.StudentId = @studentId
  AND ar.Status = 'C'
  AND (@classId = 0 OR ats.ClassId = @classId)
  AND CAST(strftime('%m', ats.SessionDate) AS INTEGER) = @month
  AND CAST(strftime('%Y', ats.SessionDate) AS INTEGER) = @year
  AND ats.Id = (
      SELECT s2.Id
      FROM AttendanceSessions s2
      WHERE s2.ClassId = ats.ClassId AND s2.SessionDate = ats.SessionDate
      ORDER BY s2.Id DESC
      LIMIT 1
  );";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public decimal GetRemainingAmount(int studentId)
    {
        var remaining = GetTotalTuitionByStudent(studentId) - GetPaidAmount(studentId);
        return remaining > 0 ? remaining : 0;
    }

    public void Collect(int studentId, decimal amount, int createdBy, string? note)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Số tiền thu bắt buộc phải lớn hơn 0.");
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
VALUES(@studentId, NULL, @amount, @date, @note, @createdBy);";
            command.Parameters.AddWithValue("@studentId", studentId);
            command.Parameters.AddWithValue("@amount", amount);
            command.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@note", (object?)note ?? DBNull.Value);
            command.Parameters.AddWithValue("@createdBy", createdBy);
            command.ExecuteNonQuery();
        }

        using (var idCmd = connection.CreateCommand())
        {
            idCmd.Transaction = transaction;
            idCmd.CommandText = "SELECT last_insert_rowid();";
            paymentId = Convert.ToInt64(idCmd.ExecuteScalar());
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
WHERE ar.StudentId = @studentId
  AND ats.Id = (
      SELECT s2.Id FROM AttendanceSessions s2
      WHERE s2.ClassId = ats.ClassId AND s2.SessionDate = ats.SessionDate
      Order BY s2.Id DESC LIMIT 1
  );";
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
SELECT ats.SessionDate, c.ClassName, ar.Status
FROM AttendanceRecords ar
INNER JOIN AttendanceSessions ats ON ats.Id = ar.SessionId
INNER JOIN Classes c ON c.Id = ats.ClassId
WHERE ar.StudentId = @studentId
  AND ats.Id = (
      SELECT s2.Id FROM AttendanceSessions s2
      WHERE s2.ClassId = ats.ClassId AND s2.SessionDate = ats.SessionDate
      ORDER BY s2.Id DESC LIMIT 1
  )
ORDER BY ats.SessionDate DESC;";
        command.Parameters.AddWithValue("@studentId", studentId);

        var results = new List<AttendanceDetailRow>();
        using var reader2 = command.ExecuteReader();
        while (reader2.Read())
        {
            results.Add(new AttendanceDetailRow
            {
                Ngay = reader2.GetString(0),
                Lop = reader2.GetString(1),
                TrangThai = reader2.GetString(2) == "C" ? "Có mặt" : "Vắng"
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
       IFNULL(u.Username, ''),
       IFNULL(p.Note, '')
FROM Payments p
LEFT JOIN Users u ON u.Id = p.CreatedBy
WHERE p.StudentId = @studentId
ORDER BY p.PaymentDate DESC, p.Id DESC;";
        command.Parameters.AddWithValue("@studentId", studentId);

        var results = new List<PaymentHistoryRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var rawDate = reader.GetString(1);
            var displayDate = DateTime.TryParse(rawDate, out var parsed)
                ? parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : rawDate;

            results.Add(new PaymentHistoryRow
            {
                PaymentId = reader.GetInt32(0),
                NgayThu = displayDate,
                SoTien = Convert.ToDecimal(reader.GetDouble(2)),
                NguoiThu = reader.GetString(3),
                GhiChu = reader.GetString(4)
            });
        }

        return results;
    }

    public List<PaymentHistoryRow> GetPaymentHistoryByClassMonthYear(int studentId, int classId, int month, int year)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT p.Id,
       p.PaymentDate,
       p.Amount,
       IFNULL(u.Username, ''),
       IFNULL(p.Note, '')
FROM Payments p
LEFT JOIN Users u ON u.Id = p.CreatedBy
WHERE p.StudentId = @studentId
  AND CAST(strftime('%m', p.PaymentDate) AS INTEGER) = @month
  AND CAST(strftime('%Y', p.PaymentDate) AS INTEGER) = @year
  AND (@classId = 0 OR EXISTS (
      SELECT 1
      FROM ClassStudents cs
      WHERE cs.StudentId = p.StudentId
        AND cs.ClassId = @classId
  ))
ORDER BY p.PaymentDate DESC, p.Id DESC;";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var results = new List<PaymentHistoryRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var rawDate = reader.GetString(1);
            var displayDate = DateTime.TryParse(rawDate, out var parsed)
                ? parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : rawDate;

            results.Add(new PaymentHistoryRow
            {
                PaymentId = reader.GetInt32(0),
                NgayThu = displayDate,
                SoTien = Convert.ToDecimal(reader.GetDouble(2)),
                NguoiThu = reader.GetString(3),
                GhiChu = reader.GetString(4)
            });
        }

        return results;
    }

    public void UpdatePaymentHistory(int paymentId, int studentId, decimal amount, string? note)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Số tiền thu bắt buộc phải lớn hơn 0.");
        }

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
        using var connection = DbContext.CreateConnection();
        connection.Open();
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

        transaction.Commit();
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
    WHERE CAST(strftime('%m', p.PaymentDate) AS INTEGER) = @month
      AND CAST(strftime('%Y', p.PaymentDate) AS INTEGER) = @year
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
SELECT IFNULL(lp.Id, 0),
       fs.Id,
       fs.FullName,
       IFNULL((
           SELECT GROUP_CONCAT(c.ClassName, ', ')
           FROM ClassStudents cs
           INNER JOIN Classes c ON c.Id = cs.ClassId
           WHERE cs.StudentId = fs.Id
             AND (@classId = 0 OR cs.ClassId = @classId)
       ), ''),
       lp.PaymentDate,
       IFNULL(t.TotalAmount, 0),
       IFNULL(u.Username, '')
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
                var rawDate = reader.GetString(4);
                displayDate = DateTime.TryParse(rawDate, out var parsed)
                    ? parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                    : rawDate;
            }

            results.Add(new PaymentClassListRow
            {
                PaymentId = reader.GetInt32(0),
                StudentId = reader.GetInt32(1),
                HoVaTen = reader.GetString(2),
                Lop = reader.GetString(3),
                NgayThu = displayDate,
                SoTien = Convert.ToDecimal(reader.GetDouble(5)),
                NguoiThu = reader.GetString(6)
            });
        }

        for (int i = 0; i < results.Count; i++)
        {
            results[i].ThuTu = i + 1;
        }

        return results;
    }
}

public class AttendanceDetailRow
{
    public string Ngay { get; set; } = "";
    public string Lop { get; set; } = "";
    public string TrangThai { get; set; } = "";
}

public class PaymentHistoryRow
{
    public int PaymentId { get; set; }
    public string NgayThu { get; set; } = "";
    public decimal SoTien { get; set; }
    public string NguoiThu { get; set; } = "";
    public string GhiChu { get; set; } = "";
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

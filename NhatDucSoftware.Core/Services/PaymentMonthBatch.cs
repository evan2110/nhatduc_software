using System.Data.Common;
using Npgsql;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public sealed class PaymentMonthBatch
{
    private readonly int _classId;
    private readonly int _month;
    private readonly int _year;
    private readonly Dictionary<int, List<TuitionClassGrossRow>> _grossByStudent;
    private readonly Dictionary<int, StudentTuitionDiscountInfo> _discountByStudent;
    private readonly Dictionary<int, Dictionary<int, decimal>> _paidByClassByStudent;
    private readonly Dictionary<int, decimal> _totalPaidByStudent;
    private readonly HashSet<int> _finalizedClassIds;
    private readonly Dictionary<int, List<StudentClassTuitionRow>> _breakdownCache = new();

    private PaymentMonthBatch(
        int classId,
        int month,
        int year,
        Dictionary<int, List<TuitionClassGrossRow>> grossByStudent,
        Dictionary<int, StudentTuitionDiscountInfo> discountByStudent,
        Dictionary<int, Dictionary<int, decimal>> paidByClassByStudent,
        Dictionary<int, decimal> totalPaidByStudent,
        HashSet<int> finalizedClassIds)
    {
        _classId = classId;
        _month = month;
        _year = year;
        _grossByStudent = grossByStudent;
        _discountByStudent = discountByStudent;
        _paidByClassByStudent = paidByClassByStudent;
        _totalPaidByStudent = totalPaidByStudent;
        _finalizedClassIds = finalizedClassIds;
    }

    public ClassPaymentSummary Summary => BuildSummary();

    public static PaymentMonthBatch Load(int classId, int month, int year)
    {
        using var connection = (NpgsqlConnection)DbContext.CreateConnection();
        connection.Open();

        var grossByStudent = LoadGrossRows(connection, classId, month, year);
        var discountByStudent = LoadDiscounts(connection, month, year);
        var paidByClassByStudent = LoadPaidByClass(connection, month, year);
        var totalPaidByStudent = LoadTotalPaid(connection, month, year);
        var finalizedClassIds = LoadFinalizedClassIds(connection, month, year);

        return new PaymentMonthBatch(
            classId,
            month,
            year,
            grossByStudent,
            discountByStudent,
            paidByClassByStudent,
            totalPaidByStudent,
            finalizedClassIds);
    }

    public StudentTuitionDiscountInfo GetDiscount(int studentId) =>
        _discountByStudent.GetValueOrDefault(studentId) ?? new StudentTuitionDiscountInfo();

    public decimal GetTotalDue(int studentId, int classId)
    {
        var breakdown = GetStudentBreakdown(studentId);
        if (classId == 0)
        {
            return breakdown.Sum(r => r.TotalDue);
        }

        return breakdown.FirstOrDefault(r => r.ClassId == classId)?.TotalDue ?? 0;
    }

    public decimal GetPaidAmount(int studentId, int classId = 0)
    {
        if (classId > 0)
        {
            return _paidByClassByStudent.GetValueOrDefault(studentId)?.GetValueOrDefault(classId) ?? 0;
        }

        return _totalPaidByStudent.GetValueOrDefault(studentId);
    }

    public decimal GetCollectibleRemaining(int studentId) =>
        GetClassCollectibleRows(studentId)
            .Where(r => !r.IsFinalized && r.Remaining > 0)
            .Sum(r => r.Remaining);

    public decimal GetCarryOverAmount(int studentId, int classId)
    {
        var rows = GetGrossRows(studentId);
        if (classId == 0)
        {
            return rows.Sum(r => r.CarryOver);
        }

        return rows.FirstOrDefault(r => r.ClassId == classId)?.CarryOver ?? 0;
    }

    public List<StudentClassTuitionRow> GetStudentBreakdown(int studentId)
    {
        if (_breakdownCache.TryGetValue(studentId, out var cached))
        {
            return cached;
        }

        var grossRows = GetGrossRows(studentId);
        var discountPercent = GetDiscount(studentId).DiscountPercent;
        var allocations = TuitionDiscountAllocator.Allocate(grossRows, discountPercent);
        var paidByClass = _paidByClassByStudent.GetValueOrDefault(studentId) ?? new Dictionary<int, decimal>();

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

        var totalPaid = _totalPaidByStudent.GetValueOrDefault(studentId);
        var allocatedPaid = paidByClass.Values.Sum();
        var unallocatedPaid = totalPaid - allocatedPaid;
        if (unallocatedPaid > 0)
        {
            PaymentServiceInternals.ApplyUnallocatedPayments(rows, unallocatedPaid);
        }

        PaymentServiceInternals.ApplyOverpaymentCredit(rows);
        _breakdownCache[studentId] = rows;
        return rows;
    }

    public List<ClassCollectibleRow> GetClassCollectibleRows(int studentId) =>
        GetStudentBreakdown(studentId)
            .Select(row => new ClassCollectibleRow
            {
                ClassId = row.ClassId,
                ClassName = row.ClassName,
                Remaining = row.Remaining,
                IsFinalized = _finalizedClassIds.Contains(row.ClassId)
            })
            .ToList();

    public IEnumerable<(int ClassId, string ClassName, decimal NetAttendance)> GetClassNetAttendanceTotals()
    {
        var totals = new Dictionary<int, (string Name, decimal Amount)>();
        foreach (var studentId in _grossByStudent.Keys)
        {
            foreach (var row in GetStudentBreakdown(studentId))
            {
                if (row.NetAttendance <= 0)
                {
                    continue;
                }

                if (!totals.TryGetValue(row.ClassId, out var current))
                {
                    totals[row.ClassId] = (row.ClassName, row.NetAttendance);
                }
                else
                {
                    totals[row.ClassId] = (current.Name, current.Amount + row.NetAttendance);
                }
            }
        }

        foreach (var kv in totals)
        {
            yield return (kv.Key, kv.Value.Name, kv.Value.Amount);
        }
    }

    private List<TuitionClassGrossRow> GetGrossRows(int studentId)
    {
        if (!_grossByStudent.TryGetValue(studentId, out var rows))
        {
            return new List<TuitionClassGrossRow>();
        }

        if (_classId == 0)
        {
            return rows;
        }

        return rows.Where(r => r.ClassId == _classId).ToList();
    }

    private ClassPaymentSummary BuildSummary()
    {
        var summary = new ClassPaymentSummary();
        foreach (var studentId in _grossByStudent.Keys)
        {
            foreach (var row in GetStudentBreakdown(studentId))
            {
                if (_classId > 0 && row.ClassId != _classId)
                {
                    continue;
                }

                summary.TotalDue += row.TotalDue;
                summary.TotalPaid += row.Paid;
                summary.TotalRemaining += row.Remaining;
                summary.TotalCarryOver += row.CarryOver;
                summary.TotalAttendanceDue += row.NetAttendance;
            }
        }

        return summary;
    }

    private static Dictionary<int, List<TuitionClassGrossRow>> LoadGrossRows(
        NpgsqlConnection connection,
        int classId,
        int month,
        int year)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
WITH Enrollments AS (
    SELECT cs.StudentId, cs.ClassId, c.ClassName
    FROM ClassStudents cs
    INNER JOIN Classes c ON c.Id = cs.ClassId
    WHERE (@classId = 0 OR cs.ClassId = @classId)
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
      AND EXTRACT(MONTH FROM CAST(ats.SessionDate AS date)) = @month::numeric
      AND EXTRACT(YEAR FROM CAST(ats.SessionDate AS date)) = @year::numeric
    GROUP BY ar.StudentId, ats.ClassId
),
StudentCarryOver AS (
    SELECT StudentId,
           ClassId,
           COALESCE(SUM(Amount), 0) AS CarryOver
    FROM PaymentCarryOvers
    WHERE ToMonth = @month
      AND ToYear = @year
    GROUP BY StudentId, ClassId
)
SELECT e.StudentId,
       e.ClassId,
       e.ClassName,
       COALESCE(st.AttendanceTuition, 0),
       COALESCE(sc.CarryOver, 0)
FROM Enrollments e
LEFT JOIN StudentTuition st ON st.StudentId = e.StudentId AND st.ClassId = e.ClassId
LEFT JOIN StudentCarryOver sc ON sc.StudentId = e.StudentId AND sc.ClassId = e.ClassId
ORDER BY e.StudentId, e.ClassName;";
        command.Parameters.AddWithValue("@classId", classId);
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var result = new Dictionary<int, List<TuitionClassGrossRow>>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var studentId = reader.GetInt32(0);
            if (!result.TryGetValue(studentId, out var rows))
            {
                rows = new List<TuitionClassGrossRow>();
                result[studentId] = rows;
            }

            rows.Add(new TuitionClassGrossRow
            {
                ClassId = reader.GetInt32(1),
                ClassName = reader.GetString(2),
                GrossAttendance = ReadDecimal(reader, 3),
                CarryOver = ReadDecimal(reader, 4)
            });
        }

        return result;
    }

    private static Dictionary<int, StudentTuitionDiscountInfo> LoadDiscounts(
        NpgsqlConnection connection,
        int month,
        int year)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT StudentId, DiscountPercent, COALESCE(Note, '')
FROM StudentTuitionDiscounts
WHERE Month = @month
  AND Year = @year;";
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var result = new Dictionary<int, StudentTuitionDiscountInfo>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[reader.GetInt32(0)] = new StudentTuitionDiscountInfo
            {
                DiscountPercent = ReadDecimal(reader, 1),
                Note = reader.GetString(2)
            };
        }

        return result;
    }

    private static Dictionary<int, Dictionary<int, decimal>> LoadPaidByClass(
        NpgsqlConnection connection,
        int month,
        int year)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT StudentId, ClassId, COALESCE(SUM(Amount), 0)
FROM Payments
WHERE ClassId IS NOT NULL
  AND EXTRACT(MONTH FROM CAST(PaymentDate AS timestamp)) = @month::numeric
  AND EXTRACT(YEAR FROM CAST(PaymentDate AS timestamp)) = @year::numeric
GROUP BY StudentId, ClassId;";
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var result = new Dictionary<int, Dictionary<int, decimal>>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var studentId = reader.GetInt32(0);
            if (!result.TryGetValue(studentId, out var paidByClass))
            {
                paidByClass = new Dictionary<int, decimal>();
                result[studentId] = paidByClass;
            }

            paidByClass[reader.GetInt32(1)] = ReadDecimal(reader, 2);
        }

        return result;
    }

    private static Dictionary<int, decimal> LoadTotalPaid(
        NpgsqlConnection connection,
        int month,
        int year)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT StudentId, COALESCE(SUM(Amount), 0)
FROM Payments
WHERE EXTRACT(MONTH FROM CAST(PaymentDate AS timestamp)) = @month::numeric
  AND EXTRACT(YEAR FROM CAST(PaymentDate AS timestamp)) = @year::numeric
GROUP BY StudentId;";
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var result = new Dictionary<int, decimal>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[reader.GetInt32(0)] = ReadDecimal(reader, 1);
        }

        return result;
    }

    private static HashSet<int> LoadFinalizedClassIds(NpgsqlConnection connection, int month, int year)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT ClassId
FROM PaymentFinalizations
WHERE Month = @month
  AND Year = @year;";
        command.Parameters.AddWithValue("@month", month);
        command.Parameters.AddWithValue("@year", year);

        var result = new HashSet<int>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(reader.GetInt32(0));
        }

        return result;
    }

    private static decimal ReadDecimal(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToDecimal(reader.GetValue(ordinal));
    }
}

internal static class PaymentServiceInternals
{
    internal static void ApplyUnallocatedPayments(List<StudentClassTuitionRow> rows, decimal unallocatedPaid)
    {
        var collectibleRows = rows
            .Where(r => r.Remaining > 0)
            .Select(r => new ClassCollectibleRow
            {
                ClassId = r.ClassId,
                ClassName = r.ClassName,
                Remaining = r.Remaining,
                IsFinalized = false
            })
            .ToList();

        var slices = PaymentAllocator.Allocate(unallocatedPaid, collectibleRows);
        foreach (var slice in slices)
        {
            var row = rows.First(r => r.ClassId == slice.ClassId);
            row.Paid += slice.Amount;
            row.Remaining = Math.Max(0, row.TotalDue - row.Paid);
        }
    }

    internal static void ApplyOverpaymentCredit(List<StudentClassTuitionRow> rows)
    {
        var credit = rows.Sum(r => Math.Max(0, r.Paid - r.TotalDue));
        if (credit <= 0)
        {
            foreach (var row in rows)
            {
                row.Remaining = Math.Max(0, row.TotalDue - row.Paid);
            }

            return;
        }

        foreach (var row in rows.OrderBy(r => r.ClassName, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.ClassId))
        {
            var baseRemaining = Math.Max(0, row.TotalDue - row.Paid);
            if (credit <= 0)
            {
                row.Remaining = baseRemaining;
                continue;
            }

            var reduction = Math.Min(credit, baseRemaining);
            row.Remaining = baseRemaining - reduction;
            credit -= reduction;
        }

        foreach (var row in rows.Where(r => r.Paid > r.TotalDue))
        {
            row.Remaining = 0;
        }
    }
}

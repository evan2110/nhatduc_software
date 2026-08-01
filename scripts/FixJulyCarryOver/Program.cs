using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Services;
using Npgsql;

// Recalculate July 2026 → August 2026 carry-overs using the same Remaining
// as the payments UI (after discount, unallocated payments, overpayment credit).
//
// Usage:
//   dotnet run --project scripts/FixJulyCarryOver          # dry-run
//   dotnet run --project scripts/FixJulyCarryOver -- --apply

const int FromMonth = 7;
const int FromYear = 2026;
const int ToMonth = 8;
const int ToYear = 2026;
const decimal ExpectedJulyRemaining = 79_057_500m;

var apply = args.Any(a => string.Equals(a, "--apply", StringComparison.OrdinalIgnoreCase));

DbContext.Configure();

Console.WriteLine(apply
    ? "=== APPLY mode: will update PaymentCarryOvers ==="
    : "=== DRY-RUN mode: no writes (pass --apply to commit) ===");
Console.WriteLine($"From {FromMonth:D2}/{FromYear} → To {ToMonth:D2}/{ToYear}");
Console.WriteLine();

var finalizedClassIds = LoadFinalizedClassIds(FromMonth, FromYear);
if (finalizedClassIds.Count == 0)
{
    Console.WriteLine("Không tìm thấy lớp nào đã chốt tháng 7/2026.");
    return 1;
}

Console.WriteLine($"Số lớp đã chốt tháng 7/2026: {finalizedClassIds.Count}");

var batch = PaymentMonthBatch.Load(0, FromMonth, FromYear);
var julySummary = batch.Summary;
Console.WriteLine($"Tổng còn lại tháng 7 (UI): {julySummary.TotalRemaining:N0}đ");
Console.WriteLine($"  (mục tiêu khớp: {ExpectedJulyRemaining:N0}đ)");
Console.WriteLine();

var correctByKey = new Dictionary<(int StudentId, int ClassId), decimal>();
foreach (var classId in finalizedClassIds)
{
    foreach (var studentId in LoadStudentIdsInClass(classId))
    {
        var remaining = batch.GetStudentBreakdown(studentId)
            .FirstOrDefault(r => r.ClassId == classId)?.Remaining ?? 0m;
        if (remaining > 0)
        {
            correctByKey[(studentId, classId)] = remaining;
        }
    }
}

var correctTotal = correctByKey.Values.Sum();
var existing = LoadExistingCarryOvers(FromMonth, FromYear);
var existingTotal = existing.Values.Sum();

Console.WriteLine($"Nợ chuyển hiện tại (DB From 7/2026): {existingTotal:N0}đ");
Console.WriteLine($"Nợ chuyển đúng (tính lại):           {correctTotal:N0}đ");
Console.WriteLine($"Chênh lệch:                          {(existingTotal - correctTotal):N0}đ");
Console.WriteLine();

var toUpdate = new List<(int StudentId, int ClassId, decimal OldAmount, decimal NewAmount)>();
var toInsert = new List<(int StudentId, int ClassId, decimal Amount)>();
var toDelete = new List<(int StudentId, int ClassId, decimal OldAmount)>();

foreach (var (key, newAmount) in correctByKey)
{
    if (existing.TryGetValue(key, out var oldAmount))
    {
        if (oldAmount != newAmount)
        {
            toUpdate.Add((key.StudentId, key.ClassId, oldAmount, newAmount));
        }
    }
    else
    {
        toInsert.Add((key.StudentId, key.ClassId, newAmount));
    }
}

foreach (var (key, oldAmount) in existing)
{
    if (!correctByKey.ContainsKey(key))
    {
        toDelete.Add((key.StudentId, key.ClassId, oldAmount));
    }
}

Console.WriteLine($"Cần UPDATE: {toUpdate.Count} | INSERT: {toInsert.Count} | DELETE: {toDelete.Count}");
foreach (var row in toUpdate.OrderByDescending(r => Math.Abs(r.OldAmount - r.NewAmount)).Take(20))
{
    Console.WriteLine($"  UPDATE student={row.StudentId} class={row.ClassId}: {row.OldAmount:N0} → {row.NewAmount:N0} (Δ {(row.NewAmount - row.OldAmount):N0})");
}
foreach (var row in toDelete.OrderByDescending(r => r.OldAmount).Take(20))
{
    Console.WriteLine($"  DELETE student={row.StudentId} class={row.ClassId}: {row.OldAmount:N0}");
}
foreach (var row in toInsert.OrderByDescending(r => r.Amount).Take(20))
{
    Console.WriteLine($"  INSERT student={row.StudentId} class={row.ClassId}: {row.Amount:N0}");
}

if (correctTotal != ExpectedJulyRemaining)
{
    Console.WriteLine();
    Console.WriteLine($"CẢNH BÁO: tổng tính lại ({correctTotal:N0}) ≠ mục tiêu UI ({ExpectedJulyRemaining:N0}).");
    Console.WriteLine("Có thể còn lớp chưa chốt, hoặc dữ liệu tháng 7 đã đổi sau khi xem UI.");
}

if (!apply)
{
    Console.WriteLine();
    Console.WriteLine("Dry-run xong. Chạy lại với --apply để ghi DB.");
    return 0;
}

ApplyChanges(correctByKey, existing.Keys.ToHashSet(), FromMonth, FromYear, ToMonth, ToYear);

var after = LoadExistingCarryOvers(FromMonth, FromYear);
var afterTotal = after.Values.Sum();
var augBatch = PaymentMonthBatch.Load(0, ToMonth, ToYear);
Console.WriteLine();
Console.WriteLine($"Đã cập nhật. Tổng PaymentCarryOvers From 7/2026: {afterTotal:N0}đ");
Console.WriteLine($"Tổng nợ chuyển tháng 8 (UI): {augBatch.Summary.TotalCarryOver:N0}đ");
Console.WriteLine($"Tổng cần đóng tháng 8 (UI): {augBatch.Summary.TotalDue:N0}đ");

return afterTotal == ExpectedJulyRemaining ? 0 : 2;

static List<int> LoadFinalizedClassIds(int month, int year)
{
    using var connection = DbContext.CreateConnection();
    connection.Open();
    using var cmd = connection.CreateCommand();
    cmd.CommandText = @"SELECT ClassId FROM PaymentFinalizations WHERE Month = @month AND Year = @year ORDER BY ClassId;";
    cmd.Parameters.AddWithValue("@month", month);
    cmd.Parameters.AddWithValue("@year", year);
    var ids = new List<int>();
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        ids.Add(reader.GetInt32(0));
    }

    return ids;
}

static List<int> LoadStudentIdsInClass(int classId)
{
    using var connection = DbContext.CreateConnection();
    connection.Open();
    using var cmd = connection.CreateCommand();
    cmd.CommandText = @"SELECT StudentId FROM ClassStudents WHERE ClassId = @classId;";
    cmd.Parameters.AddWithValue("@classId", classId);
    var ids = new List<int>();
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        ids.Add(Convert.ToInt32(reader.GetValue(0)));
    }

    return ids;
}

static Dictionary<(int StudentId, int ClassId), decimal> LoadExistingCarryOvers(int fromMonth, int fromYear)
{
    using var connection = DbContext.CreateConnection();
    connection.Open();
    using var cmd = connection.CreateCommand();
    cmd.CommandText = @"
SELECT StudentId, ClassId, Amount
FROM PaymentCarryOvers
WHERE FromMonth = @fromMonth AND FromYear = @fromYear;";
    cmd.Parameters.AddWithValue("@fromMonth", fromMonth);
    cmd.Parameters.AddWithValue("@fromYear", fromYear);

    var result = new Dictionary<(int, int), decimal>();
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        var studentId = Convert.ToInt32(reader.GetValue(0));
        var classId = Convert.ToInt32(reader.GetValue(1));
        var amount = Convert.ToDecimal(reader.GetValue(2));
        result[(studentId, classId)] = amount;
    }

    return result;
}

static void ApplyChanges(
    Dictionary<(int StudentId, int ClassId), decimal> correctByKey,
    HashSet<(int StudentId, int ClassId)> existingKeys,
    int fromMonth,
    int fromYear,
    int toMonth,
    int toYear)
{
    using var connection = (NpgsqlConnection)DbContext.CreateConnection();
    connection.Open();
    using var tx = connection.BeginTransaction();

    foreach (var key in existingKeys)
    {
        if (correctByKey.ContainsKey(key))
        {
            continue;
        }

        using var del = connection.CreateCommand();
        del.Transaction = tx;
        del.CommandText = @"
DELETE FROM PaymentCarryOvers
WHERE StudentId = @studentId AND ClassId = @classId
  AND FromMonth = @fromMonth AND FromYear = @fromYear;";
        del.Parameters.AddWithValue("@studentId", key.StudentId);
        del.Parameters.AddWithValue("@classId", key.ClassId);
        del.Parameters.AddWithValue("@fromMonth", fromMonth);
        del.Parameters.AddWithValue("@fromYear", fromYear);
        del.ExecuteNonQuery();
    }

    foreach (var (key, amount) in correctByKey)
    {
        using var upsert = connection.CreateCommand();
        upsert.Transaction = tx;
        upsert.CommandText = @"
INSERT INTO PaymentCarryOvers(StudentId, ClassId, FromMonth, FromYear, ToMonth, ToYear, Amount)
VALUES(@studentId, @classId, @fromMonth, @fromYear, @toMonth, @toYear, @amount)
ON CONFLICT(StudentId, ClassId, FromMonth, FromYear)
DO UPDATE SET Amount = EXCLUDED.Amount, ToMonth = EXCLUDED.ToMonth, ToYear = EXCLUDED.ToYear;";
        upsert.Parameters.AddWithValue("@studentId", key.StudentId);
        upsert.Parameters.AddWithValue("@classId", key.ClassId);
        upsert.Parameters.AddWithValue("@fromMonth", fromMonth);
        upsert.Parameters.AddWithValue("@fromYear", fromYear);
        upsert.Parameters.AddWithValue("@toMonth", toMonth);
        upsert.Parameters.AddWithValue("@toYear", toYear);
        upsert.Parameters.AddWithValue("@amount", amount);
        upsert.ExecuteNonQuery();
    }

    tx.Commit();
}

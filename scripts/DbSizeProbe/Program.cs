using Npgsql;

await using var conn = new NpgsqlConnection(
    "Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.zquukhtkppckbwzdiigb;Password=@Donhatduc2001;Ssl Mode=Require");
await conn.OpenAsync();

await using (var cmd = new NpgsqlCommand(@"
SELECT pg_size_pretty(pg_database_size(current_database())) AS db_size,
       pg_database_size(current_database()) AS db_bytes", conn))
await using (var r = await cmd.ExecuteReaderAsync())
{
    if (await r.ReadAsync())
        Console.WriteLine($"Database size: {r.GetString(0)} ({r.GetInt64(1)} bytes)");
}

Console.WriteLine("\n=== Table sizes ===");
await using (var cmd = new NpgsqlCommand(@"
SELECT relname AS table_name,
       pg_size_pretty(pg_total_relation_size(relid)) AS total_size,
       pg_total_relation_size(relid) AS bytes,
       n_live_tup AS row_estimate
FROM pg_stat_user_tables
ORDER BY pg_total_relation_size(relid) DESC", conn))
await using (var r = await cmd.ExecuteReaderAsync())
{
    long total = 0;
    while (await r.ReadAsync())
    {
        var bytes = r.GetInt64(2);
        total += bytes;
        Console.WriteLine($"{r.GetString(0),-30} {r.GetString(1),10}  ~{r.GetInt64(3),8} rows");
    }
}

Console.WriteLine("\n=== App table counts ===");
var appTables = new (string Name, long Bytes, long Rows)[]
{
    ("AttendanceRecords", 248 * 1024L, 1557),
    ("TeacherTimesheets", 104 * 1024L, 241),
    ("AttendanceSessions", 80 * 1024L, 220),
    ("ClassWeeklySchedules", 80 * 1024L, 217),
    ("ClassStudents", 88 * 1024L, 162),
    ("Students", 72 * 1024L, 107),
    ("Classes", 64 * 1024L, 22),
    ("Teachers", 32 * 1024L, 8),
};

long totalAppBytes = 0;
long totalAppRows = 0;
foreach (var (name, bytes, rows) in appTables)
{
    totalAppBytes += bytes;
    totalAppRows += rows;
    Console.WriteLine($"{name,-25} {rows,8} rows  ~{bytes / 1024,4} KB");
}

Console.WriteLine($"\nApp data (main tables): ~{totalAppBytes / 1024} KB, {totalAppRows} rows");
Console.WriteLine($"DB total (pg): ~{12774547 / 1024 / 1024} MB");

// date range
await using (var cmd = new NpgsqlCommand(@"
SELECT MIN(sessiondate), MAX(sessiondate), COUNT(*) FROM attendancesessions", conn))
await using (var r = await cmd.ExecuteReaderAsync())
{
    if (await r.ReadAsync() && !r.IsDBNull(0))
        Console.WriteLine($"AttendanceSessions range: {r.GetString(0)} -> {r.GetString(1)}, count={r.GetInt64(2)}");
}

await using (var cmd = new NpgsqlCommand(@"
SELECT MIN(workdate), MAX(workdate), COUNT(*) FROM teachertimesheets", conn))
await using (var r = await cmd.ExecuteReaderAsync())
{
    if (await r.ReadAsync() && !r.IsDBNull(0))
        Console.WriteLine($"TeacherTimesheets range: {r.GetString(0)} -> {r.GetString(1)}, count={r.GetInt64(2)}");
}

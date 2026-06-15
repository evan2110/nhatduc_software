namespace NhatDucSoftware.Core.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using (var ping = connection.CreateCommand())
        {
            ping.CommandText = "SELECT 1;";
            ping.ExecuteScalar();
        }

        using (var createEvaluations = connection.CreateCommand())
        {
            createEvaluations.CommandText = @"
CREATE TABLE IF NOT EXISTS StudentEvaluations (
    Id BIGSERIAL PRIMARY KEY,
    StudentId BIGINT NOT NULL,
    ClassId BIGINT NOT NULL,
    TeacherId BIGINT NOT NULL,
    Score NUMERIC(5,2) NULL,
    Comment TEXT NULL,
    CreatedAt TEXT NOT NULL
);";
            createEvaluations.ExecuteNonQuery();
        }

        using (var createPayRates = connection.CreateCommand())
        {
            createPayRates.CommandText = @"
CREATE TABLE IF NOT EXISTS TeacherClassPayRates (
    Id BIGSERIAL PRIMARY KEY,
    TeacherId BIGINT NOT NULL,
    ClassId BIGINT NOT NULL,
    PayPerShift NUMERIC(18,2) NOT NULL DEFAULT 100000,
    CONSTRAINT UQ_TeacherClassPayRates_TeacherId_ClassId UNIQUE (TeacherId, ClassId)
);";
            createPayRates.ExecuteNonQuery();
        }

        MigrateAttendanceSessionsShiftNumber(connection);
    }

    private static void MigrateAttendanceSessionsShiftNumber(System.Data.Common.DbConnection connection)
    {
        using (var addColumn = connection.CreateCommand())
        {
            addColumn.CommandText = @"
ALTER TABLE AttendanceSessions
ADD COLUMN IF NOT EXISTS ShiftNumber INTEGER NOT NULL DEFAULT 1;";
            addColumn.ExecuteNonQuery();
        }

        using (var dedupeRecords = connection.CreateCommand())
        {
            dedupeRecords.CommandText = @"
DELETE FROM AttendanceRecords
WHERE SessionId IN (
    SELECT s.Id
    FROM AttendanceSessions s
    WHERE s.Id NOT IN (
        SELECT DISTINCT ON (ClassId, SessionDate) Id
        FROM AttendanceSessions
        ORDER BY ClassId, SessionDate, Id DESC
    )
);";
            dedupeRecords.ExecuteNonQuery();
        }

        using (var dedupeSessions = connection.CreateCommand())
        {
            dedupeSessions.CommandText = @"
DELETE FROM AttendanceSessions
WHERE Id NOT IN (
    SELECT DISTINCT ON (ClassId, SessionDate) Id
    FROM AttendanceSessions
    ORDER BY ClassId, SessionDate, Id DESC
);";
            dedupeSessions.ExecuteNonQuery();
        }

        using (var createIndex = connection.CreateCommand())
        {
            createIndex.CommandText = @"
CREATE UNIQUE INDEX IF NOT EXISTS UQ_AttendanceSessions_ClassId_SessionDate_ShiftNumber
ON AttendanceSessions(ClassId, SessionDate, ShiftNumber);";
            createIndex.ExecuteNonQuery();
        }
    }
}

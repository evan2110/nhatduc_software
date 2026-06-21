namespace NhatDucSoftware.Core.Data;

public static class DatabaseInitializer
{
    private const string ShiftNumberMigrationId = "attendance_sessions_shift_v2";

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

        EnsureSchemaMigrationsTable(connection);
        MigrateAttendanceSessionsShiftNumber(connection);
        MigrateStudentBalanceColumn(connection);
        MigrateStudentBalanceHistoryTable(connection);
        MigrateTeacherProfileColumns(connection);
    }

    private static void MigrateTeacherProfileColumns(System.Data.Common.DbConnection connection)
    {
        using (var addDob = connection.CreateCommand())
        {
            addDob.CommandText = @"
ALTER TABLE Teachers
ADD COLUMN IF NOT EXISTS DateOfBirth TEXT;";
            addDob.ExecuteNonQuery();
        }

        using (var addAddress = connection.CreateCommand())
        {
            addAddress.CommandText = @"
ALTER TABLE Teachers
ADD COLUMN IF NOT EXISTS Address TEXT;";
            addAddress.ExecuteNonQuery();
        }

        using (var addQualification = connection.CreateCommand())
        {
            addQualification.CommandText = @"
ALTER TABLE Teachers
ADD COLUMN IF NOT EXISTS Qualification TEXT;";
            addQualification.ExecuteNonQuery();
        }

        using (var addSubjects = connection.CreateCommand())
        {
            addSubjects.CommandText = @"
ALTER TABLE Teachers
ADD COLUMN IF NOT EXISTS TeachingSubjects TEXT;";
            addSubjects.ExecuteNonQuery();
        }

        using (var createMaterials = connection.CreateCommand())
        {
            createMaterials.CommandText = @"
CREATE TABLE IF NOT EXISTS TeacherMaterials (
    Id BIGSERIAL PRIMARY KEY,
    TeacherId BIGINT NOT NULL,
    SubjectName TEXT NOT NULL,
    FileName TEXT NOT NULL,
    DriveFileId TEXT NOT NULL,
    DriveWebViewLink TEXT,
    UploadedAt TEXT NOT NULL
);";
            createMaterials.ExecuteNonQuery();
        }
    }

    private static void MigrateStudentBalanceColumn(System.Data.Common.DbConnection connection)
    {
        using var addColumn = connection.CreateCommand();
        addColumn.CommandText = @"
ALTER TABLE Students
ADD COLUMN IF NOT EXISTS Balance NUMERIC(18,2) NOT NULL DEFAULT 0;";
        addColumn.ExecuteNonQuery();
    }

    private static void MigrateStudentBalanceHistoryTable(System.Data.Common.DbConnection connection)
    {
        using var createTable = connection.CreateCommand();
        createTable.CommandText = @"
CREATE TABLE IF NOT EXISTS StudentBalanceHistory (
    Id BIGSERIAL PRIMARY KEY,
    StudentId BIGINT NOT NULL,
    OldBalance NUMERIC(18,2) NOT NULL,
    NewBalance NUMERIC(18,2) NOT NULL,
    UpdatedAt TEXT NOT NULL,
    UpdatedBy BIGINT NOT NULL
);";
        createTable.ExecuteNonQuery();
    }

    private static void EnsureSchemaMigrationsTable(System.Data.Common.DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS SchemaMigrations (
    Id TEXT PRIMARY KEY,
    AppliedAt TEXT NOT NULL
);";
        command.ExecuteNonQuery();
    }

    private static bool IsMigrationApplied(System.Data.Common.DbConnection connection, string migrationId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Id = @id;";
        AddParameter(command, "@id", migrationId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void MarkMigrationApplied(System.Data.Common.DbConnection connection, string migrationId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO SchemaMigrations(Id, AppliedAt)
VALUES(@id, @appliedAt)
ON CONFLICT(Id) DO NOTHING;";
        AddParameter(command, "@id", migrationId);
        AddParameter(command, "@appliedAt", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
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

        using (var createIndex = connection.CreateCommand())
        {
            createIndex.CommandText = @"
CREATE UNIQUE INDEX IF NOT EXISTS UQ_AttendanceSessions_ClassId_SessionDate_ShiftNumber
ON AttendanceSessions(ClassId, SessionDate, ShiftNumber);";
            createIndex.ExecuteNonQuery();
        }

        if (IsMigrationApplied(connection, ShiftNumberMigrationId))
        {
            return;
        }

        // One-time cleanup: remove only exact duplicates (same class, date AND shift).
        // Never collapse multiple shifts on the same day — that was the previous bug.
        using (var dedupeRecords = connection.CreateCommand())
        {
            dedupeRecords.CommandText = @"
DELETE FROM AttendanceRecords
WHERE SessionId IN (
    SELECT s.Id
    FROM AttendanceSessions s
    WHERE s.Id NOT IN (
        SELECT DISTINCT ON (ClassId, SessionDate, ShiftNumber) Id
        FROM AttendanceSessions
        ORDER BY ClassId, SessionDate, ShiftNumber, Id DESC
    )
);";
            dedupeRecords.ExecuteNonQuery();
        }

        using (var dedupeSessions = connection.CreateCommand())
        {
            dedupeSessions.CommandText = @"
DELETE FROM AttendanceSessions
WHERE Id NOT IN (
    SELECT DISTINCT ON (ClassId, SessionDate, ShiftNumber) Id
    FROM AttendanceSessions
    ORDER BY ClassId, SessionDate, ShiftNumber, Id DESC
);";
            dedupeSessions.ExecuteNonQuery();
        }

        MarkMigrationApplied(connection, ShiftNumberMigrationId);
    }
}

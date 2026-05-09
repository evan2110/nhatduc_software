using Microsoft.Data.Sqlite;

namespace NhatDucSoftware.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS Teachers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    Phone TEXT,
    Email TEXT,
    Status TEXT NOT NULL DEFAULT 'Active'
);

CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Role TEXT NOT NULL,
    TeacherId INTEGER NULL,
    FOREIGN KEY (TeacherId) REFERENCES Teachers(Id)
);

CREATE TABLE IF NOT EXISTS Students (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    Phone TEXT,
    Email TEXT,
    BirthYear INTEGER NULL,
    Address TEXT NULL,
    Language TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Active',
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Courses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    Language TEXT NOT NULL,
    TuitionFee REAL NOT NULL,
    DurationHours INTEGER NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Active'
);

CREATE TABLE IF NOT EXISTS Classes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ClassName TEXT NOT NULL,
    CourseId INTEGER NOT NULL,
    TeacherId INTEGER NULL,
    MaxSize INTEGER NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Active',
    FOREIGN KEY (CourseId) REFERENCES Courses(Id),
    FOREIGN KEY (TeacherId) REFERENCES Teachers(Id)
);

CREATE TABLE IF NOT EXISTS ClassSchedules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ClassId INTEGER NOT NULL,
    DayOfWeek TEXT NOT NULL,
    StartTime TEXT NOT NULL,
    EndTime TEXT NOT NULL,
    Room TEXT,
    FOREIGN KEY (ClassId) REFERENCES Classes(Id)
);

CREATE TABLE IF NOT EXISTS ClassWeeklySchedules (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ClassId INTEGER NOT NULL,
    WeekStartDate TEXT NOT NULL,
    DayOfWeek INTEGER NOT NULL CHECK(DayOfWeek BETWEEN 0 AND 6),
    ShiftNumber INTEGER NOT NULL CHECK(ShiftNumber BETWEEN 1 AND 5),
    FOREIGN KEY (ClassId) REFERENCES Classes(Id),
    UNIQUE (ClassId, WeekStartDate, DayOfWeek, ShiftNumber)
);

CREATE TABLE IF NOT EXISTS StudentCourses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    CourseId INTEGER NOT NULL,
    EnrollDate TEXT NOT NULL,
    Status TEXT NOT NULL,
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    FOREIGN KEY (CourseId) REFERENCES Courses(Id)
);

CREATE TABLE IF NOT EXISTS ClassStudents (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ClassId INTEGER NOT NULL,
    StudentId INTEGER NOT NULL,
    JoinedDate TEXT NOT NULL,
    FOREIGN KEY (ClassId) REFERENCES Classes(Id),
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    UNIQUE (ClassId, StudentId)
);

CREATE TABLE IF NOT EXISTS Payments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    ClassId INTEGER NULL,
    Amount REAL NOT NULL,
    PaymentDate TEXT NOT NULL,
    Note TEXT,
    CreatedBy INTEGER NOT NULL,
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    FOREIGN KEY (ClassId) REFERENCES Classes(Id),
    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS RevenueLedger (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SourcePaymentId INTEGER NULL UNIQUE,
    Amount REAL NOT NULL,
    PaymentDate TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS AttendanceSessions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ClassId INTEGER NOT NULL,
    SessionDate TEXT NOT NULL,
    CreatedByTeacherId INTEGER NOT NULL,
    FOREIGN KEY (ClassId) REFERENCES Classes(Id),
    FOREIGN KEY (CreatedByTeacherId) REFERENCES Teachers(Id)
);

CREATE TABLE IF NOT EXISTS AttendanceRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SessionId INTEGER NOT NULL,
    StudentId INTEGER NOT NULL,
    Status TEXT NOT NULL,
    FOREIGN KEY (SessionId) REFERENCES AttendanceSessions(Id),
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    UNIQUE (SessionId, StudentId)
);

CREATE TABLE IF NOT EXISTS StudentEvaluations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    ClassId INTEGER NOT NULL,
    TeacherId INTEGER NOT NULL,
    Score REAL NULL,
    Comment TEXT,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    FOREIGN KEY (ClassId) REFERENCES Classes(Id),
    FOREIGN KEY (TeacherId) REFERENCES Teachers(Id)
);

CREATE TABLE IF NOT EXISTS TeacherTimesheets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TeacherId INTEGER NOT NULL,
    WorkDate TEXT NOT NULL,
    ShiftNumber INTEGER NOT NULL CHECK(ShiftNumber BETWEEN 1 AND 5),
    IsPresent INTEGER NOT NULL DEFAULT 0,
    Note TEXT,
    FOREIGN KEY (TeacherId) REFERENCES Teachers(Id),
    UNIQUE (TeacherId, WorkDate, ShiftNumber)
);

CREATE TABLE IF NOT EXISTS PaymentFinalizations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ClassId INTEGER NOT NULL,
    Month INTEGER NOT NULL,
    Year INTEGER NOT NULL,
    FinalizedAt TEXT NOT NULL,
    FinalizedBy INTEGER NOT NULL,
    FOREIGN KEY (ClassId) REFERENCES Classes(Id),
    FOREIGN KEY (FinalizedBy) REFERENCES Users(Id),
    UNIQUE (ClassId, Month, Year)
);

CREATE TABLE IF NOT EXISTS PaymentCarryOvers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    ClassId INTEGER NOT NULL,
    FromMonth INTEGER NOT NULL,
    FromYear INTEGER NOT NULL,
    ToMonth INTEGER NOT NULL,
    ToYear INTEGER NOT NULL,
    Amount REAL NOT NULL,
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    FOREIGN KEY (ClassId) REFERENCES Classes(Id),
    UNIQUE (StudentId, ClassId, FromMonth, FromYear)
);
";
        command.ExecuteNonQuery();

        MigrateStudentsPhoneColumn(connection);
        MigrateStudentsBirthYearAndAddress(connection);
        SyncRevenueLedger(connection);
        Seed(connection);
    }

    private static void SyncRevenueLedger(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO RevenueLedger(SourcePaymentId, Amount, PaymentDate)
SELECT p.Id, p.Amount, p.PaymentDate
FROM Payments p
WHERE NOT EXISTS (
    SELECT 1 FROM RevenueLedger r WHERE r.SourcePaymentId = p.Id
);";
        command.ExecuteNonQuery();
    }

    private static void MigrateStudentsPhoneColumn(SqliteConnection connection)
    {
        // Check if Phone column still has UNIQUE constraint by inspecting table SQL
        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='Students';";
        var tableSql = checkCmd.ExecuteScalar()?.ToString() ?? string.Empty;

        // If Phone is still UNIQUE, rebuild the table without that constraint
        if (!tableSql.Contains("Phone TEXT NOT NULL UNIQUE") && !tableSql.Contains("Phone TEXT UNIQUE"))
            return;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
PRAGMA foreign_keys = OFF;

CREATE TABLE IF NOT EXISTS Students_new (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    Phone TEXT,
    Email TEXT,
    Language TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Active',
    CreatedAt TEXT NOT NULL
);

INSERT INTO Students_new (Id, FullName, Phone, Email, Language, Status, CreatedAt)
SELECT Id, FullName, Phone, Email, Language, Status, CreatedAt FROM Students;

DROP TABLE Students;

ALTER TABLE Students_new RENAME TO Students;

PRAGMA foreign_keys = ON;
";
        cmd.ExecuteNonQuery();
    }

    private static void MigrateStudentsBirthYearAndAddress(SqliteConnection connection)
    {
        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "PRAGMA table_info(Students);";

        var hasBirthYear = false;
        var hasAddress = false;

        using (var reader = checkCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var columnName = reader.GetString(1);
                if (columnName.Equals("BirthYear", StringComparison.OrdinalIgnoreCase))
                {
                    hasBirthYear = true;
                }
                else if (columnName.Equals("Address", StringComparison.OrdinalIgnoreCase))
                {
                    hasAddress = true;
                }
            }
        }

        if (hasBirthYear && hasAddress)
        {
            return;
        }

        if (!hasBirthYear)
        {
            using var addBirthYearCmd = connection.CreateCommand();
            addBirthYearCmd.CommandText = "ALTER TABLE Students ADD COLUMN BirthYear INTEGER NULL;";
            addBirthYearCmd.ExecuteNonQuery();
        }

        if (!hasAddress)
        {
            using var addAddressCmd = connection.CreateCommand();
            addAddressCmd.CommandText = "ALTER TABLE Students ADD COLUMN Address TEXT NULL;";
            addAddressCmd.ExecuteNonQuery();
        }
    }

    private static void Seed(SqliteConnection connection)
    {
        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(1) FROM Users;";
        var count = Convert.ToInt32(checkCmd.ExecuteScalar());
        if (count > 0)
        {
            return;
        }

        using var teacherCmd = connection.CreateCommand();
        teacherCmd.CommandText = "INSERT INTO Teachers(FullName, Phone, Email, Status) VALUES (@name, @phone, @mail, 'Active');";
        teacherCmd.Parameters.AddWithValue("@name", "Teacher Demo");
        teacherCmd.Parameters.AddWithValue("@phone", "0900000002");
        teacherCmd.Parameters.AddWithValue("@mail", "teacher@demo.local");
        teacherCmd.ExecuteNonQuery();

        long teacherId;
        using (var idCmd = connection.CreateCommand())
        {
            idCmd.CommandText = "SELECT last_insert_rowid();";
            teacherId = Convert.ToInt64(idCmd.ExecuteScalar());
        }

        using var adminCmd = connection.CreateCommand();
        adminCmd.CommandText = "INSERT INTO Users(Username, PasswordHash, Role, TeacherId) VALUES ('admin', '123456', 'Admin', NULL);";
        adminCmd.ExecuteNonQuery();

        using var userCmd = connection.CreateCommand();
        userCmd.CommandText = "INSERT INTO Users(Username, PasswordHash, Role, TeacherId) VALUES ('teacher', '123456', 'Teacher', @teacherId);";
        userCmd.Parameters.AddWithValue("@teacherId", teacherId);
        userCmd.ExecuteNonQuery();
    }
}

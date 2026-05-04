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
    Phone TEXT NOT NULL UNIQUE,
    Email TEXT,
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
";
        command.ExecuteNonQuery();

        Seed(connection);
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

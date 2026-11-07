namespace NhatDucSoftware.Core.Data;

public static class HomeworkSchema
{
    public const string CreateTables = @"
CREATE TABLE IF NOT EXISTS HomeworkAssignments (
    Id BIGSERIAL PRIMARY KEY,
    ClassId BIGINT NOT NULL,
    TeacherId BIGINT NOT NULL,
    Title TEXT NOT NULL,
    AssignedOn TEXT NOT NULL,
    Kind TEXT NOT NULL,
    Prompt TEXT NOT NULL,
    AnswerKeyText TEXT NULL,
    AnswerKeyFileName TEXT NULL,
    AnswerKeyDriveFileId TEXT NULL,
    AnswerKeyDriveWebViewLink TEXT NULL,
    DriveFolderId TEXT NULL,
    PublicToken TEXT NULL,
    CreatedAt TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS HomeworkSubmissions (
    Id BIGSERIAL PRIMARY KEY,
    AssignmentId BIGINT NOT NULL,
    StudentId BIGINT NOT NULL,
    Score NUMERIC(5,2) NULL,
    ShortComment TEXT NULL,
    ReportText TEXT NULL,
    FileName TEXT NULL,
    DriveFileId TEXT NULL,
    DriveWebViewLink TEXT NULL,
    PendingFileName TEXT NULL,
    PendingDriveFileId TEXT NULL,
    PendingDriveWebViewLink TEXT NULL,
    GradedAt TEXT NULL,
    CONSTRAINT UQ_HomeworkSubmissions_Assignment_Student UNIQUE (AssignmentId, StudentId)
);";
}

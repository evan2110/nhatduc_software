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
    }
}

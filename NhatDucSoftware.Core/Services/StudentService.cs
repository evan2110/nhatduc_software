using Npgsql;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class StudentService
{
    public List<Student> GetAll()
    {
        var result = new List<Student>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT s.Id,
       s.FullName,
       COALESCE((SELECT STRING_AGG(c.ClassName, ', ')
                 FROM ClassStudents cs
                 INNER JOIN Classes c ON c.Id = cs.ClassId
                 WHERE cs.StudentId = s.Id), ''),
       s.Phone,
       s.Email,
       s.BirthYear,
       s.Address,
       s.Status,
       COALESCE(s.Balance, 0)
FROM Students s
ORDER BY s.Id ASC;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Student
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                ClassName = reader.GetString(2),
                Phone = reader.GetString(3),
                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                BirthYear = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                Status = reader.GetString(7),
                Balance = Convert.ToDecimal(reader.GetValue(8))
            });
        }

        return result;
    }

    private int GetNextAvailableId(NpgsqlConnection connection)
    {
        using var cmd = connection.CreateCommand();
        // Find the smallest positive integer not currently used as an Id
        cmd.CommandText = @"
            WITH RECURSIVE seq(n) AS (
                SELECT 1
                UNION ALL
                SELECT n + 1 FROM seq WHERE n < (SELECT COALESCE(MAX(Id), 0) + 1 FROM Students)
            )
            SELECT MIN(n) FROM seq WHERE n NOT IN (SELECT Id FROM Students);";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void Add(Student student, int? updatedByUserId = null)
    {
        ValidateStudent(student);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var nextId = GetNextAvailableId(connection);

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO Students(Id, FullName, Phone, Email, BirthYear, Address, Language, Status, CreatedAt, Balance)
VALUES(@id, @name, @phone, @mail, @birthYear, @address, '', @status, @createdAt, @balance);";
            command.Parameters.AddWithValue("@id", nextId);
            command.Parameters.AddWithValue("@name", student.FullName);
            command.Parameters.AddWithValue("@phone", student.Phone);
            command.Parameters.AddWithValue("@mail", (object?)student.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@birthYear", (object?)student.BirthYear ?? DBNull.Value);
            command.Parameters.AddWithValue("@address", (object?)student.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@status", student.Status);
            command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
            command.Parameters.AddWithValue("@balance", student.Balance);
            command.ExecuteNonQuery();
        }

        if (updatedByUserId is int userId && student.Balance != 0)
        {
            LogBalanceChange(connection, transaction, nextId, 0, student.Balance, userId);
        }

        transaction.Commit();
    }

    public void Update(Student student, int updatedByUserId)
    {
        ValidateStudent(student);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var oldBalance = GetStudentBalance(connection, transaction, student.Id);

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"UPDATE Students
SET FullName = @name, Phone = @phone, Email = @mail, BirthYear = @birthYear, Address = @address, Status = @status, Balance = @balance
WHERE Id = @id;";
            command.Parameters.AddWithValue("@id", student.Id);
            command.Parameters.AddWithValue("@name", student.FullName);
            command.Parameters.AddWithValue("@phone", student.Phone);
            command.Parameters.AddWithValue("@mail", (object?)student.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@birthYear", (object?)student.BirthYear ?? DBNull.Value);
            command.Parameters.AddWithValue("@address", (object?)student.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@status", student.Status);
            command.Parameters.AddWithValue("@balance", student.Balance);
            command.ExecuteNonQuery();
        }

        if (oldBalance != student.Balance)
        {
            LogBalanceChange(connection, transaction, student.Id, oldBalance, student.Balance, updatedByUserId);
        }

        transaction.Commit();
    }

    public List<StudentBalanceHistory> GetBalanceHistory(int studentId)
    {
        var result = new List<StudentBalanceHistory>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT h.Id,
       h.OldBalance,
       h.NewBalance,
       h.UpdatedAt,
       h.UpdatedBy,
       COALESCE(u.Username, '')
FROM StudentBalanceHistory h
LEFT JOIN Users u ON u.Id = h.UpdatedBy
WHERE h.StudentId = @studentId
ORDER BY h.UpdatedAt DESC, h.Id DESC;";
        command.Parameters.AddWithValue("@studentId", studentId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new StudentBalanceHistory
            {
                Id = reader.GetInt64(0),
                StudentId = studentId,
                OldBalance = Convert.ToDecimal(reader.GetValue(1)),
                NewBalance = Convert.ToDecimal(reader.GetValue(2)),
                UpdatedAt = DateTime.Parse(reader.GetString(3)),
                UpdatedBy = reader.GetInt32(4),
                UpdatedByName = reader.GetString(5)
            });
        }

        return result;
    }

    private static decimal GetStudentBalance(NpgsqlConnection connection, NpgsqlTransaction transaction, int studentId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COALESCE(Balance, 0) FROM Students WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", studentId);
        return Convert.ToDecimal(command.ExecuteScalar() ?? 0m);
    }

    private static void LogBalanceChange(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int studentId,
        decimal oldBalance,
        decimal newBalance,
        int updatedByUserId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
INSERT INTO StudentBalanceHistory(StudentId, OldBalance, NewBalance, UpdatedAt, UpdatedBy)
VALUES(@studentId, @oldBalance, @newBalance, @updatedAt, @updatedBy);";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@oldBalance", oldBalance);
        command.Parameters.AddWithValue("@newBalance", newBalance);
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("@updatedBy", updatedByUserId);
        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        void Exec(string sql)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        Exec("DELETE FROM StudentBalanceHistory WHERE StudentId = @id;");
        Exec("DELETE FROM StudentEvaluations WHERE StudentId = @id;");
        Exec("DELETE FROM AttendanceRecords WHERE StudentId = @id;");
        Exec("DELETE FROM Payments WHERE StudentId = @id;");
        Exec("DELETE FROM ClassStudents WHERE StudentId = @id;");
        Exec("DELETE FROM PaymentCarryOvers WHERE StudentId = @id;");
        Exec("DELETE FROM StudentCourses WHERE StudentId = @id;");
        Exec("DELETE FROM Students WHERE Id = @id;");

        transaction.Commit();
    }

    public void AssignCourse(int studentId, int courseId)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(1) FROM StudentCourses WHERE StudentId = @studentId AND CourseId = @courseId AND Status = 'Active';";
        checkCmd.Parameters.AddWithValue("@studentId", studentId);
        checkCmd.Parameters.AddWithValue("@courseId", courseId);

        if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO StudentCourses(StudentId, CourseId, EnrollDate, Status)
VALUES(@studentId, @courseId, @date, 'Active');";
        command.Parameters.AddWithValue("@studentId", studentId);
        command.Parameters.AddWithValue("@courseId", courseId);
        command.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }

    private static void ValidateStudent(Student student)
    {
        if (string.IsNullOrWhiteSpace(student.FullName))
        {
            throw new InvalidOperationException("Họ tên không được để trống.");
        }
    }
}

using System.Data.Common;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class IncomeService
{
    public const string TypeOther = "Khác";
    public const string TypePreschool = "Thu trường mầm non";
    public const decimal DefaultSalaryPerLesson = 80_000m;

    public static readonly string[] IncomeTypes = [TypeOther, TypePreschool];

    public List<Income> GetByYearMonth(int year, int? month = null)
    {
        var result = new List<Income>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        if (month is >= 1 and <= 12)
        {
            command.CommandText = @"
SELECT Id, IncomeDate, IncomeType, Title, Amount, Note, CollectedBy,
       StudentCount, PricePerLesson, SalaryPerLesson, CreatedAt, CreatedBy
FROM Incomes
WHERE LEFT(IncomeDate, 4) = @yearText
  AND SUBSTRING(IncomeDate FROM 6 FOR 2) = @monthText
ORDER BY IncomeDate DESC, Id DESC;";
            command.Parameters.AddWithValue("@yearText", year.ToString());
            command.Parameters.AddWithValue("@monthText", month.Value.ToString("D2"));
        }
        else
        {
            command.CommandText = @"
SELECT Id, IncomeDate, IncomeType, Title, Amount, Note, CollectedBy,
       StudentCount, PricePerLesson, SalaryPerLesson, CreatedAt, CreatedBy
FROM Incomes
WHERE LEFT(IncomeDate, 4) = @yearText
ORDER BY IncomeDate DESC, Id DESC;";
            command.Parameters.AddWithValue("@yearText", year.ToString());
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadIncome(reader));
        }

        return result;
    }

    public int Add(Income income)
    {
        Validate(income);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO Incomes(
    IncomeDate, IncomeType, Title, Amount, Note, CollectedBy,
    StudentCount, PricePerLesson, SalaryPerLesson, CreatedAt, CreatedBy)
VALUES(
    @incomeDate, @incomeType, @title, @amount, @note, @collectedBy,
    @studentCount, @pricePerLesson, @salaryPerLesson, @createdAt, @createdBy)
RETURNING Id;";
        AddWriteParameters(command, income);
        command.Parameters.AddWithValue("@createdAt",
            string.IsNullOrWhiteSpace(income.CreatedAt)
                ? DateTime.UtcNow.ToString("O")
                : income.CreatedAt);
        command.Parameters.AddWithValue("@createdBy", (object?)income.CreatedBy ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Update(Income income)
    {
        if (income.Id <= 0)
        {
            throw new InvalidOperationException("Khoản thu không hợp lệ.");
        }

        Validate(income);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE Incomes SET
    IncomeDate = @incomeDate,
    IncomeType = @incomeType,
    Title = @title,
    Amount = @amount,
    Note = @note,
    CollectedBy = @collectedBy,
    StudentCount = @studentCount,
    PricePerLesson = @pricePerLesson,
    SalaryPerLesson = @salaryPerLesson
WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", income.Id);
        AddWriteParameters(command, income);
        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Incomes WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    private static void AddWriteParameters(DbCommand command, Income income)
    {
        AddParameter(command, "@incomeDate", income.IncomeDate.Trim());
        AddParameter(command, "@incomeType", income.IncomeType.Trim());
        AddParameter(command, "@title", income.Title.Trim());
        AddParameter(command, "@amount", income.Amount);
        AddParameter(command, "@note", (object?)income.Note?.Trim() ?? DBNull.Value);
        AddParameter(command, "@collectedBy", (object?)income.CollectedBy?.Trim() ?? DBNull.Value);
        AddParameter(command, "@studentCount", (object?)income.StudentCount ?? DBNull.Value);
        AddParameter(command, "@pricePerLesson", (object?)income.PricePerLesson ?? DBNull.Value);
        AddParameter(command, "@salaryPerLesson", (object?)income.SalaryPerLesson ?? DBNull.Value);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static void Validate(Income income)
    {
        if (string.IsNullOrWhiteSpace(income.IncomeDate)
            || !DateTime.TryParse(income.IncomeDate, out var parsedDate))
        {
            throw new InvalidOperationException("Ngày thu không hợp lệ.");
        }

        income.IncomeDate = parsedDate.ToString("yyyy-MM-dd");

        var incomeType = income.IncomeType?.Trim() ?? string.Empty;
        if (!IncomeTypes.Contains(incomeType, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Loại khoản thu không hợp lệ.");
        }

        income.IncomeType = incomeType;

        if (string.IsNullOrWhiteSpace(income.Title))
        {
            throw new InvalidOperationException(
                incomeType == TypePreschool ? "Tên trường không được để trống." : "Tên khoản thu không được để trống.");
        }

        if (income.Amount <= 0)
        {
            throw new InvalidOperationException("Số tiền thu phải lớn hơn 0.");
        }

        if (string.IsNullOrWhiteSpace(income.CollectedBy))
        {
            throw new InvalidOperationException("Người thu không được để trống.");
        }

        if (incomeType == TypePreschool)
        {
            if (income.StudentCount is null or <= 0)
            {
                throw new InvalidOperationException("Tổng số học sinh phải lớn hơn 0.");
            }

            if (income.PricePerLesson is null or <= 0)
            {
                throw new InvalidOperationException("Giá tiền 1 tiết phải lớn hơn 0.");
            }

            if (income.SalaryPerLesson is null or < 0)
            {
                throw new InvalidOperationException("Lương 1 tiết không hợp lệ.");
            }
        }
        else
        {
            income.StudentCount = null;
            income.PricePerLesson = null;
            income.SalaryPerLesson = null;
        }
    }

    private static Income ReadIncome(DbDataReader reader) => new()
    {
        Id = Convert.ToInt32(reader.GetValue(0)),
        IncomeDate = reader.IsDBNull(1) ? string.Empty : reader.GetValue(1)?.ToString() ?? string.Empty,
        IncomeType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
        Title = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
        Amount = Convert.ToDecimal(reader.GetValue(4)),
        Note = reader.IsDBNull(5) ? null : reader.GetString(5),
        CollectedBy = reader.IsDBNull(6) ? null : reader.GetString(6),
        StudentCount = reader.IsDBNull(7) ? null : Convert.ToInt32(reader.GetValue(7)),
        PricePerLesson = reader.IsDBNull(8) ? null : Convert.ToDecimal(reader.GetValue(8)),
        SalaryPerLesson = reader.IsDBNull(9) ? null : Convert.ToDecimal(reader.GetValue(9)),
        CreatedAt = reader.IsDBNull(10) ? string.Empty : reader.GetValue(10)?.ToString() ?? string.Empty,
        CreatedBy = reader.IsDBNull(11) ? null : reader.GetString(11)
    };
}

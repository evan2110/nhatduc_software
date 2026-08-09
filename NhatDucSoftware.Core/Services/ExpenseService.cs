using System.Data.Common;
using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Services;

public class ExpenseService
{
    public static readonly string[] Categories =
    [
        "Mặt bằng",
        "Điện nước",
        "Văn phòng",
        "Mua sắm",
        "Khác"
    ];

    public List<Expense> GetByYearMonth(int year, int? month = null)
    {
        var result = new List<Expense>();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        if (month is >= 1 and <= 12)
        {
            command.CommandText = @"
SELECT Id, ExpenseDate, Amount, Category, Note, PaidBy, InvoiceNumber,
       AttachmentFileId, AttachmentFileName, AttachmentUrl, CreatedAt, CreatedBy
FROM Expenses
WHERE LEFT(ExpenseDate, 4) = @yearText
  AND SUBSTRING(ExpenseDate FROM 6 FOR 2) = @monthText
ORDER BY ExpenseDate DESC, Id DESC;";
            command.Parameters.AddWithValue("@yearText", year.ToString());
            command.Parameters.AddWithValue("@monthText", month.Value.ToString("D2"));
        }
        else
        {
            command.CommandText = @"
SELECT Id, ExpenseDate, Amount, Category, Note, PaidBy, InvoiceNumber,
       AttachmentFileId, AttachmentFileName, AttachmentUrl, CreatedAt, CreatedBy
FROM Expenses
WHERE LEFT(ExpenseDate, 4) = @yearText
ORDER BY ExpenseDate DESC, Id DESC;";
            command.Parameters.AddWithValue("@yearText", year.ToString());
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadExpense(reader));
        }

        return result;
    }

    public Expense? GetById(int id)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, ExpenseDate, Amount, Category, Note, PaidBy, InvoiceNumber,
       AttachmentFileId, AttachmentFileName, AttachmentUrl, CreatedAt, CreatedBy
FROM Expenses
WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadExpense(reader) : null;
    }

    public List<MonthlyAmountStat> GetAmountByMonth(int year)
    {
        var result = CreateEmptyMonthlyAmounts();
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT CAST(SUBSTRING(ExpenseDate FROM 6 FOR 2) AS INTEGER) AS Month,
       COALESCE(SUM(Amount), 0) AS TotalAmount
FROM Expenses
WHERE LEFT(ExpenseDate, 4) = @yearText
  AND LENGTH(ExpenseDate) >= 7
  AND SUBSTRING(ExpenseDate FROM 6 FOR 2) ~ '^\d{2}$'
GROUP BY SUBSTRING(ExpenseDate FROM 6 FOR 2)
ORDER BY Month;";
        command.Parameters.AddWithValue("@yearText", year.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var month = Convert.ToInt32(reader.GetValue(0));
            if (month is >= 1 and <= 12)
            {
                result[month - 1].Amount = Convert.ToDecimal(reader.GetValue(1));
            }
        }

        return result;
    }

    public decimal GetTotalForYear(int year) =>
        GetAmountByMonth(year).Sum(x => x.Amount);

    public int Add(Expense expense)
    {
        Validate(expense);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO Expenses(
    ExpenseDate, Amount, Category, Note, PaidBy, InvoiceNumber,
    AttachmentFileId, AttachmentFileName, AttachmentUrl, CreatedAt, CreatedBy)
VALUES(
    @expenseDate, @amount, @category, @note, @paidBy, @invoiceNumber,
    @attachmentFileId, @attachmentFileName, @attachmentUrl, @createdAt, @createdBy)
RETURNING Id;";
        AddWriteParameters(command, expense);
        command.Parameters.AddWithValue("@createdAt",
            string.IsNullOrWhiteSpace(expense.CreatedAt)
                ? DateTime.UtcNow.ToString("O")
                : expense.CreatedAt);
        command.Parameters.AddWithValue("@createdBy", (object?)expense.CreatedBy ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Update(Expense expense)
    {
        if (expense.Id <= 0)
        {
            throw new InvalidOperationException("Khoản chi không hợp lệ.");
        }

        Validate(expense);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE Expenses SET
    ExpenseDate = @expenseDate,
    Amount = @amount,
    Category = @category,
    Note = @note,
    PaidBy = @paidBy,
    InvoiceNumber = @invoiceNumber,
    AttachmentFileId = @attachmentFileId,
    AttachmentFileName = @attachmentFileName,
    AttachmentUrl = @attachmentUrl
WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", expense.Id);
        AddWriteParameters(command, expense);
        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Expenses WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    private static void AddWriteParameters(DbCommand command, Expense expense)
    {
        AddParameter(command, "@expenseDate", expense.ExpenseDate.Trim());
        AddParameter(command, "@amount", expense.Amount);
        AddParameter(command, "@category", expense.Category.Trim());
        AddParameter(command, "@note", (object?)expense.Note?.Trim() ?? DBNull.Value);
        AddParameter(command, "@paidBy", (object?)expense.PaidBy?.Trim() ?? DBNull.Value);
        AddParameter(command, "@invoiceNumber", (object?)expense.InvoiceNumber?.Trim() ?? DBNull.Value);
        AddParameter(command, "@attachmentFileId", (object?)expense.AttachmentFileId ?? DBNull.Value);
        AddParameter(command, "@attachmentFileName", (object?)expense.AttachmentFileName ?? DBNull.Value);
        AddParameter(command, "@attachmentUrl", (object?)expense.AttachmentUrl ?? DBNull.Value);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static void Validate(Expense expense)
    {
        if (string.IsNullOrWhiteSpace(expense.ExpenseDate)
            || !DateTime.TryParse(expense.ExpenseDate, out var parsedDate))
        {
            throw new InvalidOperationException("Ngày chi không hợp lệ.");
        }

        expense.ExpenseDate = parsedDate.ToString("yyyy-MM-dd");

        if (expense.Amount <= 0)
        {
            throw new InvalidOperationException("Số tiền phải lớn hơn 0.");
        }

        if (string.IsNullOrWhiteSpace(expense.Category)
            || !Categories.Contains(expense.Category.Trim(), StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Danh mục chi tiêu không hợp lệ.");
        }
    }

    private static Expense ReadExpense(DbDataReader reader) => new()
    {
        Id = Convert.ToInt32(reader.GetValue(0)),
        ExpenseDate = reader.IsDBNull(1) ? string.Empty : reader.GetValue(1)?.ToString() ?? string.Empty,
        Amount = Convert.ToDecimal(reader.GetValue(2)),
        Category = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
        Note = reader.IsDBNull(4) ? null : reader.GetString(4),
        PaidBy = reader.IsDBNull(5) ? null : reader.GetString(5),
        InvoiceNumber = reader.IsDBNull(6) ? null : reader.GetString(6),
        AttachmentFileId = reader.IsDBNull(7) ? null : reader.GetString(7),
        AttachmentFileName = reader.IsDBNull(8) ? null : reader.GetString(8),
        AttachmentUrl = reader.IsDBNull(9) ? null : reader.GetString(9),
        CreatedAt = reader.IsDBNull(10) ? string.Empty : reader.GetValue(10)?.ToString() ?? string.Empty,
        CreatedBy = reader.IsDBNull(11) ? null : reader.GetString(11)
    };

    private static List<MonthlyAmountStat> CreateEmptyMonthlyAmounts()
    {
        var result = new List<MonthlyAmountStat>(12);
        for (var month = 1; month <= 12; month++)
        {
            result.Add(new MonthlyAmountStat
            {
                Month = month,
                MonthName = $"Tháng {month}",
                Amount = 0
            });
        }

        return result;
    }
}

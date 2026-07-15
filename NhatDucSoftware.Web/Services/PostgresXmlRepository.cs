using System.Collections.ObjectModel;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using NhatDucSoftware.Core.Data;

namespace NhatDucSoftware.Web.Services;

/// <summary>
/// Persists ASP.NET Data Protection keys in Postgres so antiforgery/auth cookies
/// survive Render restarts and redeploys (ephemeral container disk cannot).
/// </summary>
public sealed class PostgresXmlRepository : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var elements = new List<XElement>();

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Xml FROM DataProtectionKeys;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var xml = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(xml))
            {
                elements.Add(XElement.Parse(xml));
            }
        }

        return new ReadOnlyCollection<XElement>(elements);
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);

        using var connection = DbContext.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO DataProtectionKeys(FriendlyName, Xml)
VALUES (@friendlyName, @xml);";
        command.Parameters.AddWithValue("friendlyName", (object?)friendlyName ?? DBNull.Value);
        command.Parameters.AddWithValue("xml", element.ToString(SaveOptions.DisableFormatting));
        command.ExecuteNonQuery();
    }
}

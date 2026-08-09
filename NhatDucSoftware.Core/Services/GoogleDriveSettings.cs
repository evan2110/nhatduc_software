namespace NhatDucSoftware.Core.Services;

public class GoogleDriveSettings
{
    public string RootFolderId { get; set; } = "1g1sl-pKk1d3sixMkpXbSWmiFDXb55u-n";
    public string TeacherProfileRootFolderId { get; set; } = "1yq8ByWsZv5-AQiteWVbETVKpcplQBcbq";
    public string ExpenseRootFolderId { get; set; } = "1Hh5whYpr638YxI9JMO8Y8R_Zyiy7wS6T";
    public string? ServiceAccountJson { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RefreshToken { get; set; }
}

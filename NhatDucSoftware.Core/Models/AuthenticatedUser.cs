namespace NhatDucSoftware.Core.Models;

public class AuthenticatedUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? TeacherId { get; set; }
}

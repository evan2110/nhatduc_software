using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Helpers;

public static class AdminPermissions
{
    private static readonly HashSet<string> RestrictedAdmins = new(StringComparer.OrdinalIgnoreCase)
    {
        "ntduyen"
    };

    private static bool IsAdmin(AuthenticatedUser? user) =>
        user is not null && string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);

    private static bool IsRestrictedAdmin(AuthenticatedUser? user) =>
        IsAdmin(user) && RestrictedAdmins.Contains(user!.Username);

    public static bool CanDeleteStudent(AuthenticatedUser? user) =>
        IsAdmin(user) && !IsRestrictedAdmin(user);

    public static bool CanDeleteClass(AuthenticatedUser? user) =>
        IsAdmin(user) && !IsRestrictedAdmin(user);

    public static bool CanRemoveStudentFromClass(AuthenticatedUser? user) =>
        IsAdmin(user) && !IsRestrictedAdmin(user);

    public static bool CanTransferClass(AuthenticatedUser? user) =>
        IsAdmin(user) && !IsRestrictedAdmin(user);

    public static bool CanDeleteTeacher(AuthenticatedUser? user) =>
        IsAdmin(user) && !IsRestrictedAdmin(user);

    public static bool CanManagePaySettings(AuthenticatedUser? user) =>
        IsAdmin(user) && !IsRestrictedAdmin(user);
}

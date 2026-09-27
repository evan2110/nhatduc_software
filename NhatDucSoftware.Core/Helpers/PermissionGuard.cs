using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Core.Helpers;

public static class PermissionGuard
{
    public static void Ensure(AuthenticatedUser? actor, Func<AuthenticatedUser?, bool> allowed, string deniedMessage)
    {
        if (!allowed(actor))
        {
            throw new InvalidOperationException(deniedMessage);
        }
    }
}

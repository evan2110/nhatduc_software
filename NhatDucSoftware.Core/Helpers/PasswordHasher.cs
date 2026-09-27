using Microsoft.AspNetCore.Identity;

namespace NhatDucSoftware.Core.Helpers;

public static class PasswordHasher
{
    private static readonly object Marker = new();
    private static readonly PasswordHasher<object> Hasher = new();

    public static bool IsHashed(string? stored) =>
        !string.IsNullOrEmpty(stored) && stored.StartsWith("AQAAAA", StringComparison.Ordinal);

    public static string Hash(string password) => Hasher.HashPassword(Marker, password);

    public static bool Verify(string stored, string password, out bool needsUpgrade)
    {
        needsUpgrade = false;
        if (string.IsNullOrEmpty(stored) || password is null)
        {
            return false;
        }

        if (IsHashed(stored))
        {
            var result = Hasher.VerifyHashedPassword(Marker, stored, password);
            needsUpgrade = result == PasswordVerificationResult.SuccessRehashNeeded;
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }

        if (!string.Equals(stored, password, StringComparison.Ordinal))
        {
            return false;
        }

        needsUpgrade = true;
        return true;
    }
}

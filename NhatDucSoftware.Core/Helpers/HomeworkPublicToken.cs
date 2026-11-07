using System.Security.Cryptography;

namespace NhatDucSoftware.Core.Helpers;

public static class HomeworkPublicToken
{
    public static string Create()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
    }
}

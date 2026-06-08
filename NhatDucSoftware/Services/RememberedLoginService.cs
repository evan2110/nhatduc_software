using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NhatDucSoftware.Services;

public class RememberedLoginService
{
    private sealed class RememberedLoginData
    {
        public bool RememberMe { get; set; }
        public string Username { get; set; } = string.Empty;
        public byte[]? ProtectedPassword { get; set; }
    }

    private static string FilePath
    {
        get
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NhatDucSoftware");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "remembered-login.json");
        }
    }

    public (bool RememberMe, string Username, string Password) Load()
    {
        if (!File.Exists(FilePath))
        {
            return (false, string.Empty, string.Empty);
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var data = JsonSerializer.Deserialize<RememberedLoginData>(json);
            if (data is null || !data.RememberMe || string.IsNullOrWhiteSpace(data.Username) || data.ProtectedPassword is null)
            {
                return (false, string.Empty, string.Empty);
            }

            var passwordBytes = ProtectedData.Unprotect(data.ProtectedPassword, null, DataProtectionScope.CurrentUser);
            return (true, data.Username, Encoding.UTF8.GetString(passwordBytes));
        }
        catch
        {
            Clear();
            return (false, string.Empty, string.Empty);
        }
    }

    public void Save(string username, string password)
    {
        var protectedPassword = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(password),
            null,
            DataProtectionScope.CurrentUser);

        var data = new RememberedLoginData
        {
            RememberMe = true,
            Username = username,
            ProtectedPassword = protectedPassword
        };

        File.WriteAllText(FilePath, JsonSerializer.Serialize(data));
    }

    public void Clear()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }
}

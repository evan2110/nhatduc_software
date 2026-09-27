using System.Text.Json;

namespace NhatDucSoftware.Services;

public class RememberedLoginService
{
    private sealed class RememberedLoginData
    {
        public bool RememberMe { get; set; }
        public string Username { get; set; } = string.Empty;
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

    public (bool RememberMe, string Username) Load()
    {
        if (!File.Exists(FilePath))
        {
            return (false, string.Empty);
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var data = JsonSerializer.Deserialize<RememberedLoginData>(json);
            if (data is null || !data.RememberMe || string.IsNullOrWhiteSpace(data.Username))
            {
                return (false, string.Empty);
            }

            return (true, data.Username);
        }
        catch
        {
            Clear();
            return (false, string.Empty);
        }
    }

    public void Save(string username)
    {
        var data = new RememberedLoginData
        {
            RememberMe = true,
            Username = username
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

using System.IO;
using System.Text.Json;
using OutlineVsix.Models;

namespace OutlineVsix.Services;

public class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OutlineVsix");
    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    public AppConfig Load()
    {
        if (!File.Exists(ConfigFile)) return new AppConfig();
        try
        {
            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch { return new AppConfig(); }
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFile, json);
    }
}

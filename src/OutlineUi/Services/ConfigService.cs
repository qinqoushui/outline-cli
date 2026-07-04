using System;
using System.IO;
using System.Text;
using System.Text.Json;
using OutlineUi.Models;

namespace OutlineUi.Services;

public class ConfigService
{
    private static readonly string ConfigFile = Path.Combine(
        AppContext.BaseDirectory,
        "config.json"
    );

    public AppConfig Load()
    {
        if (!File.Exists(ConfigFile))
        {
            var defaultConfig = new AppConfig
            {
                ApiUrl = "https://your-team.getoutline.com",
                ApiToken = "" 
            };
            Save(defaultConfig);
        }

        var config = new AppConfig
        {
            ApiUrl = Environment.GetEnvironmentVariable("OUTLINE_API_URL") ?? "",
            ApiToken = Environment.GetEnvironmentVariable("OUTLINE_API_TOKEN") ?? "" 
        };

        if (!config.IsValid())
        {
            try
            {
                var json = File.ReadAllText(ConfigFile,Encoding.UTF8);
                var fileConfig = JsonSerializer.Deserialize<AppConfig>(json);
                if (fileConfig != null)
                {
                    if (string.IsNullOrWhiteSpace(config.ApiUrl))
                        config.ApiUrl = fileConfig.ApiUrl;
                    if (string.IsNullOrWhiteSpace(config.ApiToken))
                        config.ApiToken = fileConfig.ApiToken;
                }
            }
            catch { }
        }

        return config;
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(ConfigFile, json,Encoding.UTF8);
    }

    public string GetConfigPath() => ConfigFile;

    public string GetTheme()
    {
        try
        {
            var json = File.ReadAllText(ConfigFile, Encoding.UTF8);
            var config = JsonSerializer.Deserialize<AppConfig>(json);
            return config?.Theme ?? "Light";
        }
        catch
        {
            return "Light";
        }
    }

    public void SetTheme(string theme)
    {
        try
        {
            var json = File.ReadAllText(ConfigFile, Encoding.UTF8);
            var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            config.Theme = theme;
            Save(config);
        }
        catch { }
    }
}

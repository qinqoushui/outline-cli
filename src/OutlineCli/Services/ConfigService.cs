using System.Text.Json;
using OutlineCli.Models;

namespace OutlineCli.Services;

public class ConfigService
{
    private static readonly string ConfigFile = Path.Combine(
        AppContext.BaseDirectory,
        "config.json"
    );

    public AppConfig Load()
    {
        // 如果配置文件不存在，创建默认配置文件
        if (!File.Exists(ConfigFile))
        {
            var defaultConfig = new AppConfig
            {
                ApiUrl = "https://your-team.getoutline.com",
                ApiToken = "",
                DefaultCollectionId = ""
            };
            Save(defaultConfig);
        }

        // 优先从环境变量读取
        var config = new AppConfig
        {
            ApiUrl = Environment.GetEnvironmentVariable("OUTLINE_API_URL") ?? "",
            ApiToken = Environment.GetEnvironmentVariable("OUTLINE_API_TOKEN") ?? "",
            DefaultCollectionId = Environment.GetEnvironmentVariable("OUTLINE_COLLECTION_ID")
        };

        // 如果环境变量不完整，从配置文件读取
        if (!config.IsValid())
        {
            try
            {
                var json = File.ReadAllText(ConfigFile);
                var fileConfig = JsonSerializer.Deserialize<AppConfig>(json);
                if (fileConfig != null)
                {
                    if (string.IsNullOrWhiteSpace(config.ApiUrl))
                        config.ApiUrl = fileConfig.ApiUrl;
                    if (string.IsNullOrWhiteSpace(config.ApiToken))
                        config.ApiToken = fileConfig.ApiToken;
                    if (string.IsNullOrWhiteSpace(config.DefaultCollectionId))
                        config.DefaultCollectionId = fileConfig.DefaultCollectionId;
                }
            }
            catch { /* 忽略解析错误 */ }
        }

        return config;
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(ConfigFile, json);
    }

    public string GetConfigPath() => ConfigFile;
}

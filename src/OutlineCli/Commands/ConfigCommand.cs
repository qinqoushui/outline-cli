using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;
using OutlineCli.Services;

namespace OutlineCli.Commands;

[Command("config", Description = "配置 API 连接")]
public class ConfigCommand
{
    [Option("--url", "API URL", CommandOptionType.SingleValue)]
    public string? Url { get; set; }

    [Option("--token", "API Token", CommandOptionType.SingleValue)]
    public string? Token { get; set; }

    [Option("--collection-id", "默认集合ID", CommandOptionType.SingleValue)]
    public string? CollectionId { get; set; }

    private readonly ConfigService _configService = new();

    public async Task<int> OnExecuteAsync()
    {
        AnsiConsole.MarkupLine("[bold blue]Outline CLI 配置[/][/]");

        if (string.IsNullOrWhiteSpace(Url))
            Url = AnsiConsole.Ask<string>("请输入 API URL (例如 [dim]https://your-team.getoutline.com[/]): ");

        if (string.IsNullOrWhiteSpace(Token))
            Token = AnsiConsole.Prompt(
                new TextPrompt<string>("请输入 API Token: ")
                    .Secret());

        if (string.IsNullOrWhiteSpace(CollectionId))
            CollectionId = AnsiConsole.Ask<string?>("默认集合ID (可选，按回车跳过): ", null);

        var config = new Models.AppConfig
        {
            ApiUrl = Url.TrimEnd('/'),
            ApiToken = Token 
        };

        _configService.Save(config);
        AnsiConsole.MarkupLine("[green]✓ 配置已保存[/]");

        // 测试连接
        try
        {
            var api = new OutlineApiService(config.ApiUrl, config.ApiToken);
            var collections = await api.ListCollectionsAsync();
            AnsiConsole.MarkupLine($"[green]✓ 连接成功，找到 {collections.Count} 个集合[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ 连接测试失败: {ex.Message}[/]");
        }

        return 0;
    }
}

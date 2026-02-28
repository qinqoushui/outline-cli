using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;
using OutlineCli.Services;
using OutlineCli.Utils;

namespace OutlineCli.Commands;

[Command("push", Description = "上传本地文件到 Outline")]
public class PushCommand
{
    [Argument(0, Description = "本地 Markdown 文件路径")]
    public required string File { get; set; }

    [Option("--id", "文档ID", CommandOptionType.SingleValue)]
    public string? DocumentId { get; set; }

    [Option("--collection-id", "集合ID", CommandOptionType.SingleValue)]
    public string? CollectionId { get; set; }

    [Option("--title", "文档标题", CommandOptionType.SingleValue)]
    public string? Title { get; set; }

    [Option("--create", "创建新文档", CommandOptionType.NoValue)]
    public bool Create { get; set; }

    [Option("--publish", "发布文档", CommandOptionType.NoValue)]
    public bool Publish { get; set; } = true;

    public async Task<int> OnExecuteAsync()
    {
        if (!System.IO.File.Exists(File))
        {
            AnsiConsole.MarkupLine($"[red]错误: 文件不存在: {File}[/]");
            return 1;
        }

        var configService = new ConfigService();
        var config = configService.Load();

        if (!config.IsValid())
        {
            AnsiConsole.MarkupLine("[red]错误: 未配置 API，请先运行 'outline config' 进行配置[/]");
            return 1;
        }

        var api = new OutlineApiService(config.ApiUrl, config.ApiToken);

        var fileContent = await System.IO.File.ReadAllTextAsync(File);
        var (metadata, content) = DocumentHelper.ParseFileContent(fileContent);

        var docId = DocumentId ?? metadata.GetValueOrDefault("id");
        var title = Title ?? metadata.GetValueOrDefault("title") ?? Path.GetFileNameWithoutExtension(File);
        var collectionId = CollectionId ?? metadata.GetValueOrDefault("collection_id") ?? config.DefaultCollectionId;

        return await AnsiConsole.Status()
            .StartAsync("正在处理...", async ctx =>
            {
                try
                {
                    Models.Document doc;
                    string action;

                    if (Create || string.IsNullOrWhiteSpace(docId))
                    {
                        if (string.IsNullOrWhiteSpace(collectionId))
                        {
                            AnsiConsole.MarkupLine("[red]错误: 创建文档需要指定集合ID，使用 --collection-id 或配置默认集合[/]");
                            return 1;
                        }

                        ctx.Status("正在创建文档...");
                        doc = await api.CreateDocumentAsync(title, content, collectionId, publish: Publish);
                        action = "创建";
                    }
                    else
                    {
                        ctx.Status("正在更新文档...");
                        doc = await api.UpdateDocumentAsync(docId, title, content, Publish);
                        action = "更新";
                    }

                    AnsiConsole.MarkupLine($"[green]✓ 文档已{action}[/]");
                    AnsiConsole.MarkupLine($"  标题: {doc.Title}");
                    AnsiConsole.MarkupLine($"  ID: {doc.Id}");
                    AnsiConsole.MarkupLine($"  URL: {doc.Url}");

                    return 0;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
                    return 1;
                }
            });
    }
}

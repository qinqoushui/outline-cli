using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;
using OutlineCli.Services;
using OutlineCli.Utils;

namespace OutlineCli.Commands;

[Command("pull", Description = "下载 Outline 文档到本地")]
public class PullCommand
{
    [Argument(0, Description = "文档ID、分享ID或文档URL（可选，不指定则拉取所有文档）")]
    public string? Source { get; set; }

    [Option(" -o|--output", "输出文件或目录路径", CommandOptionType.SingleValue)]
    public string? Output { get; set; }

    [Option("--no-frontmatter", "不添加 frontmatter", CommandOptionType.NoValue)]
    public bool NoFrontMatter { get; set; }

    public async Task<int> OnExecuteAsync()
    {
        var configService = new ConfigService();
        var config = configService.Load();

        if (!config.IsValid())
        {
            AnsiConsole.MarkupLine("[red]错误: 未配置 API，请先运行 'outline config' 进行配置[/]");
            return 1;
        }

        // 如果不指定 output，使用默认目录
        var outputDir = Output;
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            if (string.IsNullOrWhiteSpace(config.DefaultCollectionId))
            {
                AnsiConsole.MarkupLine("[red]错误: 未配置 default_collection_id，请先运行 'outline config' 进行配置，或使用 --output 指定输出目录[/]");
                return 1;
            }
            outputDir = Path.Combine(AppContext.BaseDirectory,"doc", config.DefaultCollectionId);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
        }

        var api = new OutlineApiService(config.ApiUrl, config.ApiToken);

        // 如果不指定 source，拉取所有文档
        if (string.IsNullOrWhiteSpace(Source))
        {
            return await PullAllDocumentsAsync(api, config.DefaultCollectionId, outputDir);
        }

        // 拉取单个文档
        return await PullSingleDocumentAsync(api, Source, outputDir);
    }

    private async Task<int> PullSingleDocumentAsync(OutlineApiService api, string source, string outputDir)
    {
        return await AnsiConsole.Status()
            .StartAsync("正在获取文档...", async ctx =>
            {
                try
                {
                    var (docId, shareId) = DocumentHelper.ParseDocumentUrl(source);

                    var doc = shareId != null
                        ? await api.GetDocumentByShareIdAsync(shareId)
                        : docId != null
                            ? await api.GetDocumentAsync(docId)
                            : await api.GetDocumentAsync(source);

                    ctx.Status("正在保存文件...");
                    var filePath = DocumentHelper.SaveDocumentToFile(doc, outputDir, !NoFrontMatter);

                    AnsiConsole.MarkupLine($"[green]✓ 文档已保存到: {filePath}[/]");
                    AnsiConsole.MarkupLine($"  标题: {doc.Title}");
                    AnsiConsole.MarkupLine($"  ID: {doc.Id}");

                    return 0;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
                    return 1;
                }
            });
    }

    private async Task<int> PullAllDocumentsAsync(OutlineApiService api, string? collectionName, string outputDir)
    {
        try
        {
            // 获取所有集合
            var collections = await api.ListCollectionsAsync();

            if (collections.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]没有找到集合[/]");
                return 0;
            }

            // 根据名称匹配集合ID
            string? collectionId = null;
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                // 如果没有指定集合名称，使用第一个集合
                collectionId = collections[0].Id;
                AnsiConsole.MarkupLine($"[dim]使用集合: {collections[0].Name}[/]");
            }
            else
            {
                // 根据名称匹配
                var matched = collections.FirstOrDefault(c =>
                    c.Name.Equals(collectionName, StringComparison.OrdinalIgnoreCase) ||
                    c.Id.Equals(collectionName, StringComparison.OrdinalIgnoreCase));

                if (matched == null)
                {
                    AnsiConsole.MarkupLine($"[red]错误: 未找到名称为 '{collectionName}' 的集合[/]");
                    AnsiConsole.MarkupLine("[dim]可用的集合:[/]");
                    foreach (var col in collections)
                    {
                        AnsiConsole.MarkupLine($"  - {col.Name} (ID: {col.Id})");
                    }
                    return 1;
                }

                collectionId = matched.Id;
            }

            var docs = await api.ListDocumentsAsync(collectionId);

            if (docs.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]没有找到文档[/]");
                return 0;
            }

            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask($"下载 {docs.Count} 个文档", maxValue: docs.Count);
                    int successCount = 0;
                    int failCount = 0;

                    foreach (var doc in docs)
                    {
                        try
                        {
                            DocumentHelper.SaveDocumentToFile(doc, outputDir, !NoFrontMatter);
                            successCount++;
                        }
                        catch
                        {
                            failCount++;
                        }
                        task.Increment(1);
                    }

                    AnsiConsole.MarkupLine($"[green]✓ 下载完成: {successCount} 成功, {failCount} 失败[/]");
                });

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
            return 1;
        }
    }
}

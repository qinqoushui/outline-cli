using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;
using OutlineCli.Services;
using OutlineCli.Utils;
using OutlineCli.Models;

namespace OutlineCli.Commands;

[Command("pull", Description = "下载 Outline 文档到本地")]
public class PullCommand
{
    [Argument(0, Description = "文档ID、分享ID或文档URL（可选，不指定则拉取所有文档）")]
    public string? Source { get; set; }

    [Option(" -o|--output", "输出文件或目录路径（默认: ./doc）", CommandOptionType.SingleValue)]
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
            outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "doc");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
        }

        var api = new OutlineApiService(config.ApiUrl, config.ApiToken);

        // 如果不指定 source，拉取所有集合的文档
        if (string.IsNullOrWhiteSpace(Source))
        {
            return await PullAllDocumentsAsync(api, outputDir);
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

    private async Task<int> PullAllDocumentsAsync(OutlineApiService api, string outputDir)
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

            AnsiConsole.MarkupLine($"[cyan]找到 {collections.Count} 个集合[/]");

            var totalSuccessCount = 0;
            var totalFailCount = 0;

            // 为每个集合创建目录并下载文档
            foreach (var collection in collections)
            {
                try
                {
                    // 为每个集合创建对应的子目录
                    var collectionDir = System.IO.Path.Combine(outputDir, collection.Name);
                    if (!Directory.Exists(collectionDir))
                    {
                        Directory.CreateDirectory(collectionDir);
                    }

                    // 获取该集合下的所有文档
                    var docs = await api.ListDocumentsAsync(collection.Id);

                    if (docs.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[dim]  - {collection.Name}: 无文档[/]");
                        continue;
                    }

                    AnsiConsole.MarkupLine($"[cyan]  - {collection.Name}: {docs.Count} 个文档[/]");

                    int successCount = 0;
                    int failCount = 0;

                    foreach (var doc in docs)
                    {
                        try
                        {
                            DocumentHelper.SaveDocumentToFile(doc, collectionDir, !NoFrontMatter);
                            successCount++;
                        }
                        catch
                        {
                            failCount++;
                        }
                    }

                    totalSuccessCount += successCount;
                    totalFailCount += failCount;

                    AnsiConsole.MarkupLine($"    [green]成功: {successCount}, 失败: {failCount}[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]  - {collection.Name}: 处理失败 - {ex.Message}[/]");
                }
            }

            AnsiConsole.MarkupLine($"\n[cyan]总计: {totalSuccessCount} 成功, {totalFailCount} 失败[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
            return 1;
        }
    }
}

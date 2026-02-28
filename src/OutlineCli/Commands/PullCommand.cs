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

                    ctx.Status("正在检查文件...");
                    var fileName = OutlineCli.Utils.DocumentHelper.SanitizeFileName(doc.Title) + ".md";
                    var filePath = System.IO.Path.Combine(outputDir, fileName);

                    // 检查文件是否已存在且修改时间相同
                    bool skip = false;
                    if (File.Exists(filePath))
                    {
                        var existingFileInfo = new FileInfo(filePath);
                        if (doc.UpdatedAt.HasValue && existingFileInfo.LastWriteTimeUtc == doc.UpdatedAt.Value)
                        {
                            skip = true;
                        }
                    }

                    if (skip)
                    {
                        ctx.Status("已完成");
                        AnsiConsole.MarkupLine($"[yellow]- 文档已是最新，跳过: {doc.Title}[/]");
                        AnsiConsole.MarkupLine($"  ID: {doc.Id}");
                        AnsiConsole.MarkupLine($"  本地路径: {filePath}");
                    }
                    else
                    {
                        ctx.Status("正在保存文件...");
                        var savedPath = OutlineCli.Utils.DocumentHelper.SaveDocumentToFile(doc, outputDir, !NoFrontMatter);

                        AnsiConsole.MarkupLine($"[green]✓ 文档已保存[/]");
                        AnsiConsole.MarkupLine($"  标题: {doc.Title}");
                        AnsiConsole.MarkupLine($"  ID: {doc.Id}");
                        AnsiConsole.MarkupLine($"  本地路径: {savedPath}");
                        if (doc.UpdatedAt.HasValue)
                        {
                            AnsiConsole.MarkupLine($"  更新时间: {doc.UpdatedAt.Value:yyyy-MM-dd HH:mm:ss} UTC");
                        }
                    }

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
            var totalSkipCount = 0;
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
                    int skipCount = 0;
                    int failCount = 0;

                    foreach (var doc in docs)
                    {
                        try
                        {
                            var fileName = OutlineCli.Utils.DocumentHelper.SanitizeFileName(doc.Title) + ".md";
                            var filePath = System.IO.Path.Combine(collectionDir, fileName);

                            // 检查文件是否已存在且修改时间相同
                            bool skip = false;
                            if (File.Exists(filePath))
                            {
                                var existingFileInfo = new FileInfo(filePath);
                                if (doc.UpdatedAt.HasValue && existingFileInfo.LastWriteTimeUtc == doc.UpdatedAt.Value)
                                {
                                    skip = true;
                                }
                            }

                            if (skip)
                            {
                                skipCount++;
                                AnsiConsole.MarkupLine($"    [dim] - {doc.Title} (已是最新)[/]");
                            }
                            else
                            {
                                OutlineCli.Utils.DocumentHelper.SaveDocumentToFile(doc, collectionDir, !NoFrontMatter);
                                successCount++;
                                var status = File.Exists(filePath) ? "更新" : "新增";
                                var updateTime = doc.UpdatedAt.HasValue ? $" - {doc.UpdatedAt.Value:yyyy-MM-dd HH:mm:ss} UTC" : "";
                                AnsiConsole.MarkupLine($"    [green]✓ {doc.Title} ({status}){updateTime}[/]");
                            }
                        }
                        catch (Exception ex)
                        {
                            failCount++;
                            AnsiConsole.MarkupLine($"    [red]✗ {doc.Title}: {ex.Message}[/]");
                        }
                    }

                    totalSuccessCount += successCount;
                    totalSkipCount += skipCount;
                    totalFailCount += failCount;

                    if (skipCount > 0)
                    {
                        AnsiConsole.MarkupLine($"    [dim]统计: 新增/更新 {successCount}, 跳过 {skipCount}, 失败 {failCount}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"    [green]统计: 新增/更新 {successCount}, 失败 {failCount}[/]");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]  - {collection.Name}: 处理失败 - {ex.Message}[/]");
                }
            }

            AnsiConsole.MarkupLine($"\n[cyan]总计: 新增/更新 {totalSuccessCount}, 跳过 {totalSkipCount}, 失败 {totalFailCount}[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
            return 1;
        }
    }
}

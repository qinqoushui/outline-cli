using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;
using OutlineCli.Services;
using OutlineCli.Utils;
using OutlineCli.Models;
using IOPath = System.IO.Path;

namespace OutlineCli.Commands;

[Command("push", Description = "上传本地文件或目录到 Outline")]
public class PushCommand
{
    [Argument(0, Description = "本地 Markdown 文件路径或目录路径（默认: ./doc）")]
    public string? Path { get; set; }

    [Option("--id", "文档ID（仅处理指定ID的文档）", CommandOptionType.SingleValue)]
    public string? DocumentId { get; set; }

    [Option("--collection-id", "集合ID", CommandOptionType.SingleValue)]
    public string? CollectionId { get; set; }

    [Option("--title", "文档标题", CommandOptionType.SingleValue)]
    public string? Title { get; set; }

    [Option("--create", "创建新文档", CommandOptionType.NoValue)]
    public bool Create { get; set; }

    [Option("--publish", "发布文档", CommandOptionType.NoValue)]
    public bool Publish { get; set; } = true;

    [Option("--recursive", "递归处理子目录", CommandOptionType.NoValue)]
    public bool Recursive { get; set; }

    private class LocalDocumentInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CollectionId { get; set; } = string.Empty;
        public DateTime LastModifiedTime { get; set; }
        public string? DocumentId { get; set; }
    }

    public async Task<int> OnExecuteAsync()
    {
        var configService = new ConfigService();
        var config = configService.Load();

        if (!config.IsValid())
        {
            AnsiConsole.MarkupLine("[red]错误: 未配置 API，请先运行 'outline config' 进行配置[/]");
            return 1;
        }

        var api = new OutlineApiService(config.ApiUrl, config.ApiToken);

        try
        {
            // 获取所有集合用于目录名匹配
            var collections = await api.ListCollectionsAsync();
            var collectionMap = collections.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

            // 收集需要处理的本地文档
            var localDocs = new List<LocalDocumentInfo>();


            // 未提供路径，处理 doc 目录下的一级子目录（每个子目录对应一个 collection）
            var docPath = IOPath.Combine(AppContext.BaseDirectory, "doc");

            if (!Directory.Exists(docPath))
            {
                AnsiConsole.MarkupLine($"[red]错误: 默认目录不存在: {docPath}[/]");
                return 1;
            }

            // 获取一级子目录
            var subDirectories = Directory.GetDirectories(docPath, "*", SearchOption.TopDirectoryOnly);

            if (subDirectories.Length == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]doc 目录下没有找到子目录[/]");
                return 0;
            }

            // 处理每个子目录
            foreach (var subDir in subDirectories)
            {
                var dirName = IOPath.GetFileName(subDir);
                if (!string.IsNullOrWhiteSpace(Path) && !dirName.Equals(Path))
                {
                    continue;
                }
                // 检查是否有对应的集合
                if (!collectionMap.TryGetValue(dirName, out var collectionId))
                {
                    AnsiConsole.MarkupLine($"[yellow]警告: 目录 '{dirName}' 没有匹配的集合，将跳过[/]");
                    continue;
                }

                // 处理该子目录下的所有文件
                var docs = await AnalyzeDirectory(subDir, collectionMap, collectionId, Recursive);
                localDocs.AddRange(docs);
            }


            if (localDocs.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]未找到需要处理的 Markdown 文件[/]");
                return 0;
            }

            // 如果指定了文档ID，只处理该文档
            if (!string.IsNullOrWhiteSpace(DocumentId))
            {
                localDocs = localDocs.Where(d => d.DocumentId == DocumentId).ToList();
                if (localDocs.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]未找到文档ID为 {DocumentId} 的文件[/]");
                    return 0;
                }
            }

            AnsiConsole.MarkupLine($"[cyan]找到 {localDocs.Count} 个文件需要处理[/]");

            // 处理所有文档
            var successCount = 0;
            var skipCount = 0;
            var errorCount = 0;

            var table = new Table();
            table.AddColumn("状态");
            table.AddColumn("文档");
            table.AddColumn("操作");
            table.AddColumn("集合");

            foreach (var localDoc in localDocs)
            {
                var result = await ProcessDocument(api, localDoc, Create, Publish);

                var statusEmoji = result.Success ? "[green]✓[/]" : "[red]✗[/]";
                var operation = result.Operation ?? "跳过";
                var status = result.Success ? "成功" : "失败";

                table.AddRow(statusEmoji, localDoc.Title, operation, localDoc.CollectionId[..Math.Min(8, localDoc.CollectionId.Length)] + "...");

                if (result.Success)
                {
                    if (result.Operation == "创建" || result.Operation == "更新")
                        successCount++;
                    else
                        skipCount++;
                }
                else
                {
                    errorCount++;
                }
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[cyan]统计: 成功 {successCount}, 跳过 {skipCount}, 失败 {errorCount}[/]");

            return errorCount > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task<LocalDocumentInfo?> AnalyzeSingleFile(string filePath, Dictionary<string, string> collectionMap, string? defaultCollectionId)
    {
        var fileContent = await System.IO.File.ReadAllTextAsync(filePath);
        var (metadata, content) = DocumentHelper.ParseFileContent(fileContent);

        var docId = metadata.GetValueOrDefault("id");
        var title = Title ?? metadata.GetValueOrDefault("title") ?? IOPath.GetFileNameWithoutExtension(filePath);
        var collectionId = CollectionId ?? metadata.GetValueOrDefault("collection_id");
        var fileInfo = new FileInfo(filePath);

        // 如果没有指定集合ID，尝试从配置获取
        if (string.IsNullOrWhiteSpace(collectionId))
        {
            collectionId = defaultCollectionId;
        }

        if (string.IsNullOrWhiteSpace(collectionId))
        {
            AnsiConsole.MarkupLine($"[yellow]警告: {IOPath.GetFileName(filePath)} 未指定集合ID，将跳过[/]");
            return null;
        }

        return new LocalDocumentInfo
        {
            FilePath = filePath,
            Title = title,
            CollectionId = collectionId,
            LastModifiedTime = fileInfo.LastWriteTimeUtc,
            DocumentId = docId
        };
    }

    private async Task<List<LocalDocumentInfo>> AnalyzeDirectory(string directoryPath, Dictionary<string, string> collectionMap, string? defaultCollectionId, bool recursive, string? forcedCollectionId = null)
    {
        var docs = new List<LocalDocumentInfo>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var filePath in Directory.GetFiles(directoryPath, "*.md", searchOption))
        {
            var fileContent = await System.IO.File.ReadAllTextAsync(filePath);
            var (metadata, content) = DocumentHelper.ParseFileContent(fileContent);

            var docId = metadata.GetValueOrDefault("id");
            var title = metadata.GetValueOrDefault("title") ?? IOPath.GetFileNameWithoutExtension(filePath);
            var collectionId = CollectionId ?? metadata.GetValueOrDefault("collection_id");
            var fileInfo = new FileInfo(filePath);

            // 如果强制指定了集合ID，使用它
            if (!string.IsNullOrWhiteSpace(forcedCollectionId))
            {
                collectionId = forcedCollectionId;
            }
            // 如果没有指定集合ID，尝试从配置获取
            else if (string.IsNullOrWhiteSpace(collectionId))
            {
                collectionId = defaultCollectionId;
            }

            if (string.IsNullOrWhiteSpace(collectionId))
            {
                AnsiConsole.MarkupLine($"[yellow]警告: {IOPath.GetFileName(filePath)} 未指定集合ID，将跳过[/]");
                continue;
            }

            docs.Add(new LocalDocumentInfo
            {
                FilePath = filePath,
                Title = title,
                CollectionId = collectionId,
                LastModifiedTime = fileInfo.LastWriteTimeUtc,
                DocumentId = docId
            });
        }

        return docs;
    }

    private async Task<(bool Success, string? Operation)> ProcessDocument(OutlineApiService api, LocalDocumentInfo localDoc, bool forceCreate, bool publish)
    {
        try
        {
            var fileContent = await System.IO.File.ReadAllTextAsync(localDoc.FilePath);
            var (_, content) = DocumentHelper.ParseFileContent(fileContent);

            // 如果指定了强制创建或没有文档ID，则创建新文档
            if (forceCreate || string.IsNullOrWhiteSpace(localDoc.DocumentId))
            {
                var doc = await api.CreateDocumentAsync(localDoc.Title, content, localDoc.CollectionId, publish: publish);
                AnsiConsole.MarkupLine($"[green]✓ 创建成功: {localDoc.Title} (ID: {doc.Id})[/]");
                return (true, "创建");
            }

            // 获取服务器文档
            var serverDoc = await api.GetDocumentAsync(localDoc.DocumentId);

            // 比对修改时间
            // 如果本地文件修改时间晚于服务器文档更新时间，则更新
            if (serverDoc.UpdatedAt.HasValue && localDoc.LastModifiedTime <= serverDoc.UpdatedAt.Value)
            {
                AnsiConsole.MarkupLine($"[yellow]⊘ 跳过 {localDoc.Title} (本地文件未修改)[/]");
                return (true, "跳过");
            }

            // 更新文档
            var updatedDoc = await api.UpdateDocumentAsync(localDoc.DocumentId, localDoc.Title, content, publish);
            AnsiConsole.MarkupLine($"[green]✓ 更新成功: {localDoc.Title}[/]");
            return (true, "更新");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ 处理失败 {localDoc.Title}: {ex.Message}[/]");
            return (false, null);
        }
    }
}

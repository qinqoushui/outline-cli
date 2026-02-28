using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;
using OutlineCli.Services;

namespace OutlineCli.Commands;

[Command("search", Description = "搜索文档")]
public class SearchCommand
{
    [Argument(0, Description = "搜索关键词")]
    public required string Query { get; set; }

    [Option("--limit", "返回数量限制", CommandOptionType.SingleValue)]
    public int Limit { get; set; } = 10;

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
            var docs = await api.SearchDocumentsAsync(Query, Limit);

            if (docs.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]没有找到匹配的文档[/]");
                return 0;
            }

            var table = new Table().Title($"搜索结果: {Query}");
            table.AddColumn("ID");
            table.AddColumn("标题");
            table.AddColumn("URL");

            foreach (var doc in docs)
            {
                table.AddRow(
                    doc.Id.Length > 8 ? doc.Id[..8] + "..." : doc.Id,
                    doc.Title.Length > 50 ? doc.Title[..50] + "..." : doc.Title,
                    doc.Url
                );
            }

            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
            return 1;
        }
    }
}

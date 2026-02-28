using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;
using OutlineCli.Services;

namespace OutlineCli.Commands;

[Command("list", Description = "列出文档")]
public class ListCommand
{
    [Option("--collection-id", "集合ID", CommandOptionType.SingleValue)]
    public string? CollectionId { get; set; }

    [Option("--limit", "返回数量限制", CommandOptionType.SingleValue)]
    public int Limit { get; set; } = 50;

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
            var docs = await api.ListDocumentsAsync(CollectionId);

            if (docs.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]没有找到文档[/]");
                return 0;
            }

            var table = new Table().Title($"文档列表 ({docs.Count} 个)");
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

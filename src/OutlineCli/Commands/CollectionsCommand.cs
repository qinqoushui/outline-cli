using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;
using OutlineCli.Services;

namespace OutlineCli.Commands;

[Command("collections", Description = "列出所有集合")]
public class CollectionsCommand
{
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
            var collections = await api.ListCollectionsAsync();

            if (collections.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]没有找到集合[/]");
                return 0;
            }

            var table = new Table().Title($"集合列表 ({collections.Count} 个)");
            table.AddColumn("ID");
            table.AddColumn("名称");
            table.AddColumn("描述");

            foreach (var col in collections)
            {
                table.AddRow(
                    col.Id.Length > 8 ? col.Id[..8] + "..." : col.Id,
                    col.Name,
                    (col.Description ?? "").Length > 40 ? col.Description[..40] + "..." : col.Description ?? ""
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

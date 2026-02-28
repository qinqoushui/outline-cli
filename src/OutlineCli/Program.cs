using McMaster.Extensions.CommandLineUtils;
using OutlineCli.Commands;

var app = new CommandLineApplication
{
    Name = "outline",
    Description = "Outline CLI - 命令行工具，用于下载和上传 Outline 文档"
};

app.HelpOption();
app.VersionOption("--version", "1.0.0");

app.Command<ConfigCommand>("config", config => config.Conventions.UseDefaultConventions());
app.Command<PullCommand>("pull", config => config.Conventions.UseDefaultConventions());
app.Command<PushCommand>("push", config => config.Conventions.UseDefaultConventions());
app.Command<ListCommand>("list", config => config.Conventions.UseDefaultConventions());
app.Command<SearchCommand>("search", config => config.Conventions.UseDefaultConventions());
app.Command<CollectionsCommand>("collections", config => config.Conventions.UseDefaultConventions());

app.OnExecute(() =>
{
    app.ShowHelp();
    return 0;
});

return await app.ExecuteAsync(args);

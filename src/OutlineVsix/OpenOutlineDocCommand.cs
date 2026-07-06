using System;
using System.ComponentModel.Design;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace OutlineVsix;

internal sealed class OpenOutlineDocCommand
{
    public const int CommandId = 0x0100;
    public static readonly Guid CommandSet = new("b2c3d4e5-f6a7-8901-bcde-f12345678901");

    private readonly AsyncPackage _package;

    private OpenOutlineDocCommand(AsyncPackage package)
    {
        _package = package;
    }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        var commandService = (IMenuCommandService)await package.GetServiceAsync(typeof(IMenuCommandService));
        var cmdId = new CommandID(CommandSet, CommandId);
        var menuCmd = new MenuCommand(async (s, e) => await ExecuteAsync(), cmdId);
        commandService.AddCommand(menuCmd);
    }

    private static async System.Threading.Tasks.Task ExecuteAsync()
    {
        try
        {
            var configService = new Services.ConfigService();
            var config = configService.Load();

            if (!config.IsValid())
            {
                var configWin = new Views.ConfigWindow(configService);
                configWin.ShowDialog();
                config = configService.Load();
                if (!config.IsValid()) return;
            }

            var api = new Services.OutlineApiService(config);
            var editor = OutlineVsixPackage.EditorService;

            var picker = new Views.DocumentPickerWindow(api);
            var result = picker.ShowDialog();
            if (result != true || picker.SelectedDocument == null) return;

            await editor.OpenDocumentInEditorAsync(picker.SelectedDocument);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"错误: {ex.Message}", "Outline Wiki",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}

using System;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using OutlineVsix.Services;
using Task = System.Threading.Tasks.Task;

namespace OutlineVsix;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
public sealed class OutlineVsixPackage : AsyncPackage
{
    public static OutlineEditorService EditorService { get; private set; }
    private SaveEventListener? _saveListener;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var configService = new ConfigService();
        var config = configService.Load();
        var api = new OutlineApiService(config);
        EditorService = new OutlineEditorService(api);

        var dte = (DTE2)await GetServiceAsync(typeof(DTE));
        if (dte != null)
        {
            _saveListener = new SaveEventListener(dte, EditorService);
        }

        await OpenOutlineDocCommand.InitializeAsync(this);
    }

    protected override void Dispose(bool disposing)
    {
        _saveListener?.Dispose();
        base.Dispose(disposing);
    }
}

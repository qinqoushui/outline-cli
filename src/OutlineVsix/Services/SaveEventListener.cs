using System;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using OutlineVsix.Services;

namespace OutlineVsix.Services;

public class SaveEventListener : IDisposable
{
    private readonly DTE2 _dte;
    private readonly OutlineEditorService _editor;
    private Events? _events;
    private DocumentEvents? _docEvents;

    public SaveEventListener(DTE2 dte, OutlineEditorService editor)
    {
        _dte = dte;
        _editor = editor;
        StartListening();
    }

    private void StartListening()
    {
        _events = _dte.Events;
        _docEvents = _events.DocumentEvents;
        _docEvents.DocumentSaved += OnDocumentSaved;
    }

    private async void OnDocumentSaved(Document doc)
    {
        try
        {
            var path = doc.FullName;
            if (!_editor.IsTracked(path)) return;

            var uploaded = await _editor.UploadIfTrackedAsync(path);
            if (uploaded)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var statusBar = (Microsoft.VisualStudio.Shell.Interop.IVsStatusbar)Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.IVsStatusbar));
                statusBar?.SetText($"✅ Outline 文档已上传: {System.IO.Path.GetFileName(path)}");
            }
        }
        catch (Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            System.Windows.MessageBox.Show($"上传失败: {ex.Message}", "Outline Wiki",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public void Dispose()
    {
        if (_docEvents != null)
        {
            _docEvents.DocumentSaved -= OnDocumentSaved;
        }
    }
}

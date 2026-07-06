using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using OutlineVsix.Models;

namespace OutlineVsix.Services;

public class OutlineEditorService
{
    private static readonly string TempDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OutlineVsix", "docs");

    private readonly OutlineApiService _api;
    private readonly Dictionary<string, TrackedDocument> _trackedDocs = [];

    public OutlineEditorService(OutlineApiService api)
    {
        _api = api;
        Directory.CreateDirectory(TempDir);
    }

    public async Task OpenDocumentInEditorAsync(Models.Document doc)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var localPath = Path.Combine(TempDir, $"{doc.Id}.md");

        if (File.Exists(localPath))
        {
            var localTime = File.GetLastWriteTimeUtc(localPath);
            if (doc.UpdatedAt.HasValue && localTime > doc.UpdatedAt.Value)
            {
                var result = System.Windows.MessageBox.Show(
                    $"本地文件修改时间({localTime:yyyy-MM-dd HH:mm})晚于服务器({doc.UpdatedAt.Value:yyyy-MM-dd HH:mm})\n\n是否用服务器内容覆盖本地？",
                    "文档冲突",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.No)
                {
                    OpenFileInVs(localPath);
                    return;
                }
            }
        }

        var serverDoc = await _api.GetDocumentAsync(doc.Id);
        Directory.CreateDirectory(TempDir);
        File.WriteAllText(localPath, serverDoc.Text);
        if (serverDoc.UpdatedAt.HasValue)
        {
            File.SetLastWriteTimeUtc(localPath, serverDoc.UpdatedAt.Value);
        }

        _trackedDocs[localPath] = new TrackedDocument
        {
            DocumentId = doc.Id,
            Title = serverDoc.Title,
            ServerUpdatedAt = serverDoc.UpdatedAt,
            LocalPath = localPath
        };

        OpenFileInVs(localPath);
    }

    public async Task<bool> UploadIfTrackedAsync(string filePath)
    {
        if (!_trackedDocs.TryGetValue(filePath, out var tracked)) return false;

        if (!File.Exists(filePath)) return false;

        var content = File.ReadAllText(filePath);
        var localTime = File.GetLastWriteTimeUtc(filePath);

        var serverDoc = await _api.GetDocumentAsync(tracked.DocumentId);
        if (serverDoc.UpdatedAt.HasValue && serverDoc.UpdatedAt.Value > localTime)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var result = System.Windows.MessageBox.Show(
                $"服务器文档({serverDoc.UpdatedAt.Value:yyyy-MM-dd HH:mm})比本地({localTime:yyyy-MM-dd HH:mm})更新\n\n是否仍然上传覆盖服务器内容？",
                "上传冲突",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return false;
        }

        var updated = await _api.UpdateDocumentAsync(tracked.DocumentId, tracked.Title, content);
        if (updated.UpdatedAt.HasValue)
        {
            File.SetLastWriteTimeUtc(filePath, updated.UpdatedAt.Value);
            tracked.ServerUpdatedAt = updated.UpdatedAt;
        }

        return true;
    }

    public bool IsTracked(string filePath) => _trackedDocs.ContainsKey(filePath);

    private void OpenFileInVs(string filePath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var dte = (DTE2)Package.GetGlobalService(typeof(DTE));
        dte?.ItemOperations.OpenFile(filePath, EnvDTE.Constants.vsViewKindTextView);
    }
}

public class TrackedDocument
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime? ServerUpdatedAt { get; set; }
    public string LocalPath { get; set; } = string.Empty;
}

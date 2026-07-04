using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OutlineUi.Models;

namespace OutlineUi.Services;

public class DocumentSyncService
{
    private readonly IOutlineApiService _apiService;
    private readonly ConflictResolver _conflictResolver;
    private readonly INotificationService? _notificationService;

    public DocumentSyncService(IOutlineApiService apiService, INotificationService? notificationService = null)
    {
        _apiService = apiService;
        _conflictResolver = new ConflictResolver();
        _notificationService = notificationService;
    }

    public async Task<(int Success, int Skipped, int Failed)> DownloadAsync(
        List<DocumentNode> nodes,
        string outputDir,
        Func<string, DateTime?, DateTime?, Task<ConflictResolver.ConflictResolution?>>? onConflict = null,
        IProgress<(int current, int total, string documentTitle)>? progress = null)
    {
        int success = 0, skipped = 0, failed = 0;
        int current = 0;

        foreach (var node in nodes.Where(n => n.Type == NodeType.Document))
        {
            current++;
            progress?.Report((current, nodes.Count, node.Name));

            try
            {
                if (string.IsNullOrEmpty(node.Id))
                {
                    skipped++;
                    continue;
                }

                var doc = await _apiService.GetDocumentAsync(node.Id);

                var outputDir2 = Path.Combine(outputDir, SanitizeFileName(doc.CollectionId ?? "Root"));
                var outputPath = Path.Combine(outputDir2, SanitizeFileName(doc.Title) + ".md");
                var localFile = new FileInfo(outputPath);
                DateTime? localTime = localFile.Exists ? localFile.LastWriteTimeUtc : null;
                DateTime? serverTime = doc.UpdatedAt;

                if (_conflictResolver.CheckConflict(localTime, serverTime))
                {
                    if (onConflict != null)
                    {
                        var resolution = await onConflict(doc.Title, localTime, serverTime);
                        switch (resolution)
                        {
                            case ConflictResolver.ConflictResolution.Skip:
                                skipped++;
                                continue;
                            case ConflictResolver.ConflictResolution.Cancel:
                                return (success, skipped, failed);
                        }
                    }
                }

                DocumentHelper.SaveDocumentToFile(doc, outputDir2);
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                _notificationService?.ShowError($"下载文档失败: {node.Name} - {ex.Message}");
            }
        }

        return (success, skipped, failed);
    }

    public async Task<(int Success, int Skipped, int Failed)> UploadAsync(
        List<FileInfo> files,
        Func<string, DateTime?, DateTime?, Task<ConflictResolver.ConflictResolution?>>? onConflict = null,
        IProgress<(int current, int total, string documentTitle)>? progress = null)
    {
        int success = 0, skipped = 0, failed = 0;
        int current = 0;

        foreach (var file in files.Where(f => f.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase)))
        {
            current++;
            progress?.Report((current, files.Count, file.Name));

            try
            {
                var content = File.ReadAllText(file.FullName);
                var (metadata, text) = DocumentHelper.ParseFileContent(content);

                if (!metadata.TryGetValue("id", out var documentId) || string.IsNullOrWhiteSpace(documentId))
                {
                    skipped++;
                    continue;
                }

                var serverDoc = await _apiService.GetDocumentAsync(documentId);
                DateTime? localTime = file.LastWriteTimeUtc;
                DateTime? serverTime = serverDoc.UpdatedAt;

                if (_conflictResolver.CheckConflict(localTime, serverTime))
                {
                    if (onConflict != null)
                    {
                        var resolution = await onConflict(file.Name, localTime, serverTime);
                        switch (resolution)
                        {
                            case ConflictResolver.ConflictResolution.Skip:
                                skipped++;
                                continue;
                            case ConflictResolver.ConflictResolution.Cancel:
                                return (success, skipped, failed);
                        }
                    }
                }

                var title = metadata.TryGetValue("title", out var docTitle) ? docTitle : Path.GetFileNameWithoutExtension(file.Name);
                await _apiService.UpdateDocumentAsync(documentId, title, text);
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                _notificationService?.ShowError($"上传文档失败: {file.Name} - {ex.Message}");
            }
        }

        return (success, skipped, failed);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        foreach (var c in invalid)
            name = name.Replace(c, '_');
        name = name.Trim('.', ' ');
        return string.IsNullOrWhiteSpace(name) ? "untitled" : name[..Math.Min(name.Length, 200)];
    }
}

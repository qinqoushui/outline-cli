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
    private readonly INotificationService? _notificationService;

    public DocumentSyncService(IOutlineApiService apiService, INotificationService? notificationService = null)
    {
        _apiService = apiService;
        _notificationService = notificationService;
    }

    public async Task<(int Success, int Skipped, int Failed)> DownloadAsync(
        List<DocumentNode> nodes,
        string outputDir,
        Func<string, DateTime?, DateTime?, Task<bool>>? onConflict = null,
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

                if (localTime.HasValue && serverTime.HasValue && localTime > serverTime)
                {
                    if (onConflict != null)
                    {
                        var shouldOverwrite = await onConflict(doc.Title, localTime, serverTime);
                        if (!shouldOverwrite)
                        {
                            skipped++;
                            continue;
                        }
                    }
                }

                Directory.CreateDirectory(outputDir2);
                File.WriteAllText(outputPath, doc.Text);
                if (doc.UpdatedAt.HasValue)
                {
                    File.SetLastWriteTimeUtc(outputPath, doc.UpdatedAt.Value);
                }
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

    public async Task<(int Success, int Skipped, int Failed)> UploadSingleAsync(
        string documentId,
        string documentTitle,
        string content,
        DateTime localTime,
        Func<string, DateTime?, DateTime?, Task<bool>>? onConflict = null)
    {
        try
        {
            var serverDoc = await _apiService.GetDocumentAsync(documentId);
            DateTime? serverTime = serverDoc.UpdatedAt;

            if (serverTime.HasValue && serverTime.Value > localTime)
            {
                if (onConflict != null)
                {
                    var shouldOverwrite = await onConflict(documentTitle, localTime, serverTime);
                    if (!shouldOverwrite)
                    {
                        return (0, 1, 0);
                    }
                }
            }

            await _apiService.UpdateDocumentAsync(documentId, documentTitle, content);
            return (1, 0, 0);
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"上传文档失败: {documentTitle} - {ex.Message}");
            return (0, 0, 1);
        }
    }

    public async Task<(int Success, int Skipped, int Failed)> UploadModifiedAsync(
        List<DocumentNode> allNodes,
        string localDocDir,
        Func<List<DocumentUploadItem>, Task<List<DocumentUploadItem>>>? onSelectDocuments = null,
        IProgress<(int current, int total, string documentTitle)>? progress = null)
    {
        int success = 0, skipped = 0, failed = 0;
        
        var uploadItems = new List<DocumentUploadItem>();
        
        foreach (var node in allNodes.Where(n => n.Type == NodeType.Document))
        {
            if (string.IsNullOrEmpty(node.Id))
                continue;

            var localFilePath = Path.Combine(localDocDir, $"{node.Id}.md");
            if (!File.Exists(localFilePath))
                continue;

            var localTime = File.GetLastWriteTimeUtc(localFilePath);
            string content;

            try
            {
                content = File.ReadAllText(localFilePath);
            }
            catch
            {
                continue;
            }

            Document? serverDoc = null;
            DateTime? serverTime = null;
            
            try
            {
                serverDoc = await _apiService.GetDocumentAsync(node.Id);
                serverTime = serverDoc.UpdatedAt;
            }
            catch
            {
                continue;
            }

            if (!serverTime.HasValue || localTime > serverTime.Value)
            {
                uploadItems.Add(new DocumentUploadItem
                {
                    DocumentId = node.Id,
                    Title = node.Name,
                    LocalTime = localTime,
                    ServerTime = serverTime,
                    Content = content,
                    Selected = true
                });
            }
        }

        if (uploadItems.Count == 0)
        {
            _notificationService?.ShowInfo("没有需要上传的文档");
            return (0, 0, 0);
        }

        if (onSelectDocuments != null)
        {
            uploadItems = await onSelectDocuments(uploadItems);
        }

        var selectedItems = uploadItems.Where(i => i.Selected).ToList();
        int current = 0;

        foreach (var item in selectedItems)
        {
            current++;
            progress?.Report((current, selectedItems.Count, item.Title));

            try
            {
                await _apiService.UpdateDocumentAsync(item.DocumentId, item.Title, item.Content);
                success++;

                if (item.ServerTime.HasValue)
                {
                    var filePath = Path.Combine(localDocDir, $"{item.DocumentId}.md");
                    File.SetLastWriteTimeUtc(filePath, item.ServerTime.Value);
                }
            }
            catch (Exception ex)
            {
                failed++;
                _notificationService?.ShowError($"上传文档失败: {item.Title} - {ex.Message}");
            }
        }

        skipped = uploadItems.Count - selectedItems.Count;

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

public class DocumentUploadItem
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime LocalTime { get; set; }
    public DateTime? ServerTime { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

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

    public async Task<List<DocumentUploadItem>> RetrieveUploadItemsAsync(
        List<DocumentNode> allNodes,
        string localDocDir)
    {
        return await RetrieveUploadItems(allNodes, localDocDir);
    }

    public async Task<(int Success, int Failed)> UploadDocumentsAsync(
        List<DocumentUploadItem> items,
        string localDocDir,
        IProgress<(int current, int total, string documentTitle)>? progress = null)
    {
        int success = 0, failed = 0;
        int current = 0;

        foreach (var item in items.Where(i => i.Selected))
        {
            current++;
            progress?.Report((current, items.Count, item.Title));

            try
            {
                var updatedDoc = await _apiService.UpdateDocumentAsync(item.DocumentId, item.Title, item.Content);
                success++;

                if (updatedDoc.UpdatedAt.HasValue)
                {
                    var filePath = Path.Combine(localDocDir, $"{item.DocumentId}.md");
                    if (File.Exists(filePath))
                    {
                        File.SetLastWriteTimeUtc(filePath, updatedDoc.UpdatedAt.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService?.ShowError($"上传文档失败: {item.Title} - {ex.Message}");
                failed++;
            }
        }

        return (success, failed);
    }

    private async Task<List<DocumentUploadItem>> RetrieveUploadItems(List<DocumentNode> allNodes, string localDocDir)
    {
        var uploadItems = new List<DocumentUploadItem>();
        var allDocumentNodes = GetAllDocumentNodes(allNodes);

        foreach (var node in allDocumentNodes)
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

            bool hasConflict = serverTime.HasValue && serverTime.Value > localTime;
            bool needsUpload = localTime > serverTime;
            
            if (needsUpload || hasConflict)
            {
                uploadItems.Add(new DocumentUploadItem
                {
                    DocumentId = node.Id,
                    Title = node.Name,
                    LocalTime = localTime,
                    ServerTime = serverTime,
                    Content = content,
                    Selected = needsUpload && !hasConflict,
                    HasConflict = hasConflict
                });
            }
        }

        return uploadItems;
    }

    private List<DocumentNode> GetAllDocumentNodes(List<DocumentNode> nodes)
    {
        var result = new List<DocumentNode>();
        foreach (var node in nodes)
        {
            if (node.Type == NodeType.Document)
            {
                result.Add(node);
                if (node.Children != null && node.Children.Count > 0)
                {
                    result.AddRange(GetAllDocumentNodes(node.Children.ToList()));
                }
            }
            else if (node.Type == NodeType.Collection && node.Children != null)
            {
                result.AddRange(GetAllDocumentNodes(node.Children.ToList()));
            }
        }
        return result;
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
    public bool HasConflict { get; set; }
}

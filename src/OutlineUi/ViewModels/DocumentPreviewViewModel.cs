using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using OutlineUi.Models;
using OutlineUi.Services;

namespace OutlineUi.ViewModels;

public class DocumentPreviewViewModel : ViewModelBase
{
    private readonly IOutlineApiService? _apiService;
    private readonly INotificationService? _notificationService;
    
    private Document? _document;
    public Document? Document
    {
        get => _document;
        set => SetProperty(ref _document, value);
    }
    
    private bool _isPreviewMode = true;
    public bool IsPreviewMode
    {
        get => _isPreviewMode;
        set
        {
            if (SetProperty(ref _isPreviewMode, value))
            {
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(ModeText));
            }
        }
    }
    
    public bool IsEditMode
    {
        get => !_isPreviewMode;
        set
        {
            if (value != IsEditMode)
            {
                IsPreviewMode = !value;
                OnPropertyChanged(nameof(IsEditMode));
            }
        }
    }
    
    public string ModeText => IsPreviewMode ? "编辑" : "预览";
    
    private string _sourceText = string.Empty;
    public string SourceText
    {
        get => _sourceText;
        set
        {
            if (SetProperty(ref _sourceText, value))
            {
                CheckModified();
            }
        }
    }
    
    private bool _isModified;
    public bool IsModified
    {
        get => _isModified;
        set => SetProperty(ref _isModified, value);
    }
    
    public ICommand SaveCommand { get; }
    public ICommand ToggleModeCommand { get; }

    public DocumentPreviewViewModel(IOutlineApiService? apiService = null, INotificationService? notificationService = null)
    {
        _apiService = apiService;
        _notificationService = notificationService;
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => IsModified);
        ToggleModeCommand = new RelayCommand(ToggleMode);
    }

    private void ToggleMode()
    {
        IsPreviewMode = !IsPreviewMode;
    }

    private void CheckModified()
    {
        if (Document != null)
        {
            IsModified = SourceText != Document.Text;
        }
    }

    public async Task LoadDocumentAsync(string documentId)
    {
        if (_apiService == null) return;

        IsLoading = true;
        
        try
        {
            // 从服务器下载文档内容
            var doc = await _apiService.GetDocumentAsync(documentId);
            
            // 检查本地缓存文件是否存在（使用文档ID作为唯一键）
            var docDir = Path.Combine(AppContext.BaseDirectory, "doc");
            var cacheFileName = $"{doc.Id}.md";
            var localFilePath = Path.Combine(docDir, cacheFileName);
            
            if (File.Exists(localFilePath))
            {
                var localModifiedTime = File.GetLastWriteTime(localFilePath);
                var serverModifiedTime = doc.UpdatedAt;
                
                if (localModifiedTime > serverModifiedTime)
                {
                    var shouldDownload = await ShouldRedownloadAsync(doc.Title, localModifiedTime, serverModifiedTime ?? DateTime.MinValue);
                    if (!shouldDownload)
                    {
                        var localContent = await File.ReadAllTextAsync(localFilePath);
                        Document = doc;
                        SourceText = localContent;
                        IsModified = localContent != doc.Text;
                        IsPreviewMode = true;
                        StatusMessage = $"已加载本地缓存: {doc.Title}";
                        _notificationService?.ShowInfo($"已加载本地缓存: {doc.Title}");
                        return;
                    }
                }
            }
            
            // 使用服务器版本
            Document = doc;
            SourceText = doc.Text;
            IsModified = false;
            IsPreviewMode = true;
            
            // 保存到本地缓存（按文档ID命名），并设置文件修改时间为服务器时间
            if (!Directory.Exists(docDir))
            {
                Directory.CreateDirectory(docDir);
            }
            await File.WriteAllTextAsync(localFilePath, doc.Text);
            
            if (doc.UpdatedAt.HasValue)
            {
                File.SetLastWriteTimeUtc(localFilePath, doc.UpdatedAt.Value);
            }
            
            StatusMessage = $"已下载: {doc.Title}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"下载失败: {ex.Message}";
            _notificationService?.ShowError($"下载失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event EventHandler<ConflictCheckEventArgs>? ConflictCheckRequested;

    private async Task<bool> ShouldRedownloadAsync(string title, DateTime localTime, DateTime serverTime)
    {
        var tcs = new TaskCompletionSource<bool>();
        ConflictCheckRequested?.Invoke(this, new ConflictCheckEventArgs
        {
            DocumentTitle = title,
            LocalTime = localTime,
            ServerTime = serverTime,
            Operation = "下载",
            ResultHandler = result => tcs.SetResult(result)
        });
        return await tcs.Task;
    }

    public async Task SaveAsync()
    {
        if (Document == null) return;

        IsLoading = true;
        
        try
        {
            // 保存到本地缓存（使用文档ID作为唯一键）
            var docDir = Path.Combine(AppContext.BaseDirectory, "doc");
            if (!Directory.Exists(docDir))
            {
                Directory.CreateDirectory(docDir);
            }
            
            var cacheFileName = $"{Document.Id}.md";
            var filePath = Path.Combine(docDir, cacheFileName);
            
            await File.WriteAllTextAsync(filePath, SourceText);
            
            Document.Text = SourceText;
            IsModified = false;
            
            _notificationService?.ShowSuccess($"已保存到本地缓存");
            StatusMessage = $"已保存: {Document.Title}";
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"保存失败: {ex.Message}");
            StatusMessage = $"保存失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Cancel()
    {
        if (Document != null)
        {
            SourceText = Document.Text;
            IsModified = false;
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    
    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
}

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
    
    public event EventHandler<string>? MessageRequested;

    public DocumentPreviewViewModel(IOutlineApiService? apiService = null)
    {
        _apiService = apiService;
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
            var doc = await _apiService.GetDocumentAsync(documentId);
            
            // 检查本地文件是否存在
            var docDir = Path.Combine(AppContext.BaseDirectory, "doc");
            var fileName = $"{doc.Title}.md";
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            var localFilePath = Path.Combine(docDir, fileName);
            
            if (File.Exists(localFilePath))
            {
                var localModifiedTime = File.GetLastWriteTime(localFilePath);
                var serverModifiedTime = doc.UpdatedAt;
                
                Console.WriteLine($"[DEBUG] 本地文件时间: {localModifiedTime}");
                Console.WriteLine($"[DEBUG] 服务器文件时间: {serverModifiedTime}");
                
                // 如果本地文件更新时间晚于服务器，询问是否重新下载
                if (localModifiedTime > serverModifiedTime)
                {
                    var shouldDownload = await ShouldRedownloadAsync(doc.Title, localModifiedTime, serverModifiedTime);
                    if (!shouldDownload)
                    {
                        // 使用本地文件
                        var localContent = await File.ReadAllTextAsync(localFilePath);
                        Document = doc;
                        SourceText = localContent;
                        IsModified = localContent != doc.Text;
                        IsPreviewMode = true;
                        return;
                    }
                }
            }
            
            // 使用服务器版本
            Document = doc;
            SourceText = doc.Text;
            IsModified = false;
            IsPreviewMode = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载文档失败: {ex.Message}");
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

    private async Task SaveAsync()
    {
        if (Document == null) return;

        IsLoading = true;
        
        try
        {
            // 只保存到本地文件，不上传到服务器
            var docDir = Path.Combine(AppContext.BaseDirectory, "doc");
            if (!Directory.Exists(docDir))
            {
                Directory.CreateDirectory(docDir);
            }
            
            var fileName = $"{Document.Title}.md";
            // 移除文件名中的非法字符
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(docDir, fileName);
            
            await File.WriteAllTextAsync(filePath, SourceText);
            
            Document.Text = SourceText;
            IsModified = false;
            
            Console.WriteLine($"[DEBUG] SaveAsync: 已保存到本地文件 {fileName}");
            MessageRequested?.Invoke(this, $"已保存到本地: {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] SaveAsync 异常: {ex.Message}");
            MessageRequested?.Invoke(this, $"保存失败: {ex.Message}");
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
}

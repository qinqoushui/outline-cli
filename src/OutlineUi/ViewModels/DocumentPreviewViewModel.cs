using System;
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

    private async Task SaveAsync()
    {
        if (_apiService == null || Document == null) return;

        IsLoading = true;
        
        try
        {
            // 保存到服务器
            await _apiService.UpdateDocumentAsync(Document.Id, Document.Title, SourceText);
            Document.Text = SourceText;
            IsModified = false;
            
            // 保存到本地文件
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
            
            Console.WriteLine($"[DEBUG] SaveAsync: 准备触发 MessageRequested 事件");
            MessageRequested?.Invoke(this, $"保存成功，已保存到: {fileName}");
            Console.WriteLine($"[DEBUG] SaveAsync: MessageRequested 事件已触发");
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

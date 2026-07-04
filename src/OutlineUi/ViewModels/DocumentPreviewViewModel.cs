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
    
    private string _sourceText = string.Empty;
    public string SourceText
    {
        get => _sourceText;
        set => SetProperty(ref _sourceText, value);
    }
    
    private bool _isModified;
    public bool IsModified
    {
        get => _isModified;
        set => SetProperty(ref _isModified, value);
    }
    
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public DocumentPreviewViewModel(IOutlineApiService? apiService = null)
    {
        _apiService = apiService;
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => IsModified);
        CancelCommand = new RelayCommand(Cancel);
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
            await _apiService.UpdateDocumentAsync(Document.Id, Document.Title, SourceText);
            IsModified = false;
            IsPreviewMode = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存文档失败: {ex.Message}");
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using OutlineUi.Models;
using OutlineUi.Services;

namespace OutlineUi.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly Func<ConfigViewModel> _configViewModelFactory;
    private readonly Func<ConflictDialogViewModel> _conflictDialogViewModelFactory;
    private readonly Func<UploadListDialogViewModel> _uploadListDialogViewModelFactory;
    private INotificationService _notificationService;
    
    public IOutlineApiService? ApiService { get; private set; }
    public INotificationService? NotificationService => _notificationService;
    private DocumentSyncService? _syncService;

    private DocumentPreviewViewModel? _currentPreview;
    public DocumentPreviewViewModel? CurrentPreview
    {
        get => _currentPreview;
        set
        {
            if (_currentPreview != null)
            {
                _currentPreview.PropertyChanged -= CurrentPreview_PropertyChanged;
            }
            
            if (SetProperty(ref _currentPreview, value))
            {
                if (_currentPreview != null)
                {
                    _currentPreview.PropertyChanged += CurrentPreview_PropertyChanged;
                }
                (UploadCurrentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    private void CurrentPreview_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentPreviewViewModel.Document))
        {
            (UploadCurrentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private bool _isNavigationVisible = true;
    public bool IsNavigationVisible
    {
        get => _isNavigationVisible;
        set => SetProperty(ref _isNavigationVisible, value);
    }
    
    public ICommand ToggleNavigationCommand { get; }

    public ObservableCollection<DocumentNode> DocumentNodes { get; } = new();
    public ObservableCollection<Collection> Collections { get; } = new();
    
    private DocumentNode? _selectedDocument;
    public DocumentNode? SelectedDocument
    {
        get => _selectedDocument;
        set => SetProperty(ref _selectedDocument, value);
    }
    
    private Collection? _selectedCollection;
    public Collection? SelectedCollection
    {
        get => _selectedCollection;
        set
        {
            if (SetProperty(ref _selectedCollection, value))
            {
                _ = LoadDocumentsAsync();
            }
        }
    }
    
    private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                FilterDocuments();
            }
        }
    }
    
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusMessage = "就绪";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand UploadCommand { get; }
    public ICommand UploadCurrentCommand { get; }
    public ICommand ConfigCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    private bool _isDarkMode;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (SetProperty(ref _isDarkMode, value))
            {
                ApplyTheme(value);
                _configService.SetTheme(value ? "Dark" : "Light");
                OnPropertyChanged(nameof(ThemeIcon));
            }
        }
    }

    public object ThemeIcon => new AtomUI.Icons.AntDesign.AntDesignIconProvider
    {
        Kind = _isDarkMode ? AtomUI.Icons.AntDesign.AntDesignIconKind.MoonOutlined 
                           : AtomUI.Icons.AntDesign.AntDesignIconKind.SunOutlined
    };

    private void ApplyTheme(bool isDark)
    {
        var app = Avalonia.Application.Current;
        if (app != null)
        {
            try
            {
                app.RequestedThemeVariant = isDark 
                    ? Avalonia.Styling.ThemeVariant.Dark 
                    : Avalonia.Styling.ThemeVariant.Light;
            }
            catch (AtomUI.Theme.ThemeNotFoundException)
            {
            }
            catch (Exception)
            {
            }
        }
    }

    public MainViewModel(
        ConfigService configService,
        Func<ConfigViewModel> configViewModelFactory,
        Func<ConflictDialogViewModel> conflictDialogViewModelFactory,
        Func<UploadListDialogViewModel> uploadListDialogViewModelFactory,
        INotificationService? notificationService = null)
    {
        _configService = configService;
        _configViewModelFactory = configViewModelFactory;
        _conflictDialogViewModelFactory = conflictDialogViewModelFactory;
        _uploadListDialogViewModelFactory = uploadListDialogViewModelFactory;
        _notificationService = notificationService ?? new NotificationService(null);
        
        RefreshCommand = new AsyncRelayCommand(LoadDocumentsAsync);
        DownloadCommand = new AsyncRelayCommand(DownloadSelectedAsync, CanDownload);
        UploadCommand = new AsyncRelayCommand(UploadLocalAsync);
        UploadCurrentCommand = new AsyncRelayCommand(UploadCurrentAsync, CanUploadCurrent);
        ConfigCommand = new RelayCommand(() => OpenConfigDialog());
        ToggleNavigationCommand = new RelayCommand(ToggleNavigation);
        ToggleThemeCommand = new RelayCommand(() => IsDarkMode = !IsDarkMode);
        
        _ = InitializeAsync();
        
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            var savedTheme = _configService.GetTheme();
            if (savedTheme == "Dark")
            {
                IsDarkMode = true;
            }
        });
    }

    private void ToggleNavigation()
    {
        IsNavigationVisible = !IsNavigationVisible;
    }
    
    public void InitializeNotificationService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public void SaveLastOpenedDocumentId(string documentId)
    {
        var config = _configService.Load();
        config.LastOpenedDocumentId = documentId;
        _configService.Save(config);
    }

    public string? LoadLastOpenedDocumentId()
    {
        var config = _configService.Load();
        return config.LastOpenedDocumentId;
    }

    private DocumentNode? FindNodeById(List<DocumentNode> nodes, string id)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id)
                return node;
            
            if (node.Type == NodeType.Collection && node.Children != null)
            {
                var found = FindNodeById(node.Children.ToList(), id);
                if (found != null)
                    return found;
            }
        }
        return null;
    }

    private async Task AutoSelectLastOpenedDocument()
    {
        var lastOpenedId = LoadLastOpenedDocumentId();
        if (string.IsNullOrEmpty(lastOpenedId))
            return;

        var node = FindNodeById(DocumentNodes.ToList(), lastOpenedId);
        if (node != null)
        {
            SelectedDocument = node;
            
            var parent = node.Parent;
            while (parent != null)
            {
                parent.IsExpanded = true;
                parent = parent.Parent;
            }
            
            await Task.Delay(100);
            
            if (_notificationService != null)
            {
                _notificationService.ShowInfo($"已自动打开上次文档: {node.Name}");
            }
        }
    }

    private async Task InitializeAsync()
    {
        var config = _configService.Load();
        if (!config.IsValid())
        {
            OpenConfigDialog();
            return;
        }

        ApiService = new OutlineApiService(config.ApiUrl, config.ApiToken);
        _syncService = new DocumentSyncService(ApiService, _notificationService);
        
        await LoadDocumentsAsync();
    }

    private async Task LoadDocumentsAsync()
    {
        if (ApiService == null)
        {
            await InitializeAsync();
            if (ApiService == null) return;
        }

        IsLoading = true;
        StatusMessage = "正在加载文档...";
        
        try
        {
            var collections = await ApiService.GetCollectionsAsync();
            Collections.Clear();
            foreach (var col in collections)
            {
                Collections.Add(col);
            }

            DocumentNodes.Clear();
            
            int totalDocuments = 0;
            
            foreach (var collection in collections)
            {
                var collectionNode = new DocumentNode
                {
                    Name = collection.Name,
                    Type = NodeType.Collection,
                    Id = collection.Id
                };

                var documents = await ApiService.GetDocumentsAsync(collection.Id);
                totalDocuments += documents.Count;
                var documentMap = documents.ToDictionary(d => d.Id);
                var documentNodes = documents.Select(d => new DocumentNode
                {
                    Id = d.Id,
                    Name = d.Title,
                    Type = NodeType.Document
                }).ToList();
                
                foreach (var docNode in documentNodes)
                {
                    var doc = documentMap[docNode.Id!];
                    if (!string.IsNullOrEmpty(doc.ParentDocumentId) && documentMap.TryGetValue(doc.ParentDocumentId, out var parent))
                    {
                        var parentNode = documentNodes.First(n => n.Id == parent.Id);
                        parentNode.Children.Add(docNode);
                        docNode.Parent = parentNode;
                    }
                    else
                    {
                        collectionNode.Children.Add(docNode);
                        docNode.Parent = collectionNode;
                    }
                }

                if (collectionNode.Children.Count > 0)
                {
                    collectionNode.IsExpanded = true;
                    DocumentNodes.Add(collectionNode);
                }
            }
            
            FilterDocuments();
            StatusMessage = $"就绪 | 集合数: {Collections.Count} | 文档数: {totalDocuments}";
            
            await AutoSelectLastOpenedDocument();
        }
        catch (Exception ex)
        {
            StatusMessage = $"错误: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterDocuments()
    {
        if (string.IsNullOrWhiteSpace(SearchKeyword) && SelectedCollection == null)
        {
            foreach (var node in DocumentNodes)
            {
                SetNodeVisibility(node, true);
            }
            return;
        }

        foreach (var node in DocumentNodes)
        {
            SetNodeVisibility(node, false);
            var hasMatch = FilterNode(node, SearchKeyword, SelectedCollection?.Id);
            if (hasMatch)
            {
                SetNodeVisibility(node, true);
            }
        }
    }

    private bool FilterNode(DocumentNode node, string keyword, string? collectionId)
    {
        bool hasMatch = false;

        if (node.Type == NodeType.Collection)
        {
            if (collectionId != null && node.Id != collectionId)
            {
                SetNodeVisibility(node, false);
                return false;
            }

            foreach (var child in node.Children)
            {
                if (FilterNode(child, keyword, null))
                {
                    hasMatch = true;
                    child.IsVisible = true;
                }
                else
                {
                    child.IsVisible = false;
                }
            }
        }
        else
        {
            var matchKeyword = string.IsNullOrWhiteSpace(keyword) || 
                             node.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            hasMatch = matchKeyword;
        }

        node.IsVisible = hasMatch;
        return hasMatch;
    }

    private void SetNodeVisibility(DocumentNode node, bool visible)
    {
        node.IsVisible = visible;
        foreach (var child in node.Children)
        {
            SetNodeVisibility(child, visible);
        }
    }

    private bool CanDownload()
    {
        return DocumentNodes.Any(n => n.IsSelected && n.Type == NodeType.Document);
    }

    private async Task DownloadSelectedAsync()
    {
        if (_syncService == null) return;

        var selectedNodes = DocumentNodes
            .SelectMany(n => GetAllSelectedDocuments(n))
            .ToList();

        if (selectedNodes.Count == 0) return;

        IsLoading = true;
        StatusMessage = $"正在下载 {selectedNodes.Count} 个文档...";
        
        try
        {
            var outputDir = Path.Combine(AppContext.BaseDirectory, "doc");
            
            var progress = new Progress<(int current, int total, string documentTitle)>(p =>
            {
                StatusMessage = $"正在下载: {p.current}/{p.total} - {p.documentTitle}";
            });

            var (success, skipped, failed) = await _syncService.DownloadAsync(
                selectedNodes,
                outputDir,
                async (title, localTime, serverTime) =>
                {
                    var dialog = _conflictDialogViewModelFactory();
                    dialog.DocumentTitle = title;
                    dialog.LocalTime = localTime;
                    dialog.ServerTime = serverTime;
                    dialog.Operation = "下载";
                    var result = await dialog.ShowAsync();
                    return result == Services.ConflictResolver.ConflictResolution.OverwriteLocal;
                },
                progress);

            StatusMessage = $"下载完成: 成功 {success}, 跳过 {skipped}, 失败 {failed}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"下载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UploadLocalAsync()
    {
        if (_syncService == null || ApiService == null) return;

        var docDir = Path.Combine(AppContext.BaseDirectory, "doc");
        if (!Directory.Exists(docDir))
        {
            StatusMessage = $"本地文档目录不存在: {docDir}";
            
            try
            {
                Directory.CreateDirectory(docDir);
                StatusMessage = $"已创建文档目录: {docDir}，请将 .md 文件放入该目录后重试";
            }
            catch (Exception ex)
            {
                StatusMessage = $"无法创建目录: {ex.Message}";
            }
            return;
        }

        IsLoading = true;
        
        try
        {
            var progress = new Progress<(int current, int total, string documentTitle)>(p =>
            {
                StatusMessage = $"正在上传: {p.current}/{p.total} - {p.documentTitle}";
            });

            var uploadItems = await _syncService.RetrieveUploadItemsAsync(DocumentNodes.ToList(), docDir);

            if (uploadItems.Count == 0)
            {
                _notificationService?.ShowInfo("没有需要上传的文档");
                StatusMessage = "上传完成: 没有需要上传的文档";
                return;
            }

            var dialog = _uploadListDialogViewModelFactory();
            foreach (var item in uploadItems)
            {
                dialog.Files.Add(new UploadFileInfo
                {
                    FilePath = Path.Combine(docDir, $"{item.DocumentId}.md"),
                    FileName = $"{item.DocumentId}.md",
                    DocumentId = item.DocumentId,
                    DocumentTitle = item.Title,
                    LocalTime = item.LocalTime,
                    ServerTime = item.ServerTime,
                    HasConflict = item.HasConflict,
                    IsSelected = item.Selected
                });
            }

            dialog.LocalDocDir = docDir;
            dialog.RetrieveUploadItemsCallback = async (_) => 
                await _syncService.RetrieveUploadItemsAsync(DocumentNodes.ToList(), docDir);
            dialog.UploadCallback = async (items) => 
                await _syncService.UploadDocumentsAsync(items, docDir, progress);

            await dialog.ShowAsync();

            StatusMessage = "上传完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"上传失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenConfigDialog()
    {
        var viewModel = _configViewModelFactory();
        viewModel.ConfigSaved += OnConfigSaved;
        _ = viewModel.ShowAsync();
    }
    
    private async void OnConfigSaved()
    {
        await InitializeAsync();
    }

    private bool CanUploadCurrent()
    {
        return CurrentPreview != null && CurrentPreview.Document != null;
    }

    private async Task UploadCurrentAsync()
    {
        if (_syncService == null || CurrentPreview?.Document == null)
        {
            return;
        }

        var document = CurrentPreview.Document;
        
        if (CurrentPreview.IsModified)
        {
            await CurrentPreview.SaveAsync();
        }
        
        var docDir = Path.Combine(AppContext.BaseDirectory, "doc");
        var cacheFileName = $"{document.Id}.md";
        var filePath = Path.Combine(docDir, cacheFileName);

        if (!File.Exists(filePath))
        {
            StatusMessage = $"本地缓存不存在: {document.Title}";
            _notificationService.ShowWarning($"本地缓存不存在: {document.Title}");
            return;
        }

        IsLoading = true;
        StatusMessage = $"正在上传: {document.Title}";
        
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var localTime = File.GetLastWriteTimeUtc(filePath);
            
            var (success, skipped, failed) = await _syncService.UploadSingleAsync(
                document.Id,
                document.Title,
                content,
                localTime,
                async (title, localTime, serverTime) =>
                {
                    var dialog = _conflictDialogViewModelFactory();
                    dialog.DocumentTitle = title;
                    dialog.LocalTime = localTime;
                    dialog.ServerTime = serverTime;
                    dialog.Operation = "上传";
                    dialog.ShowApplyToAll = false;
                    var result = await dialog.ShowAsync();
                    return result == Services.ConflictResolver.ConflictResolution.OverwriteServer;
                });

            if (success > 0)
            {
                StatusMessage = $"上传成功: {document.Title}";
                _notificationService.ShowSuccess($"上传成功: {document.Title}");
            }
            else if (skipped > 0)
            {
                StatusMessage = $"上传跳过: {document.Title} (用户取消)";
                _notificationService.ShowWarning($"上传跳过: {document.Title} (用户取消)");
            }
            else
            {
                StatusMessage = $"上传失败: {document.Title}";
                _notificationService.ShowError($"上传失败: {document.Title}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"上传失败: {ex.Message}";
            _notificationService.ShowError($"上传失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static List<DocumentNode> GetAllSelectedDocuments(DocumentNode node)
    {
        var result = new List<DocumentNode>();
        
        if (node.Type == NodeType.Document && node.IsSelected)
        {
            result.Add(node);
        }
        
        foreach (var child in node.Children)
        {
            result.AddRange(GetAllSelectedDocuments(child));
        }
        
        return result;
    }
}


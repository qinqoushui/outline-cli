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
    
    public IOutlineApiService? ApiService { get; private set; }
    private DocumentSyncService? _syncService;

    private DocumentPreviewViewModel? _currentPreview;
    public DocumentPreviewViewModel? CurrentPreview
    {
        get => _currentPreview;
        set => SetProperty(ref _currentPreview, value);
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

    public MainViewModel(
        ConfigService configService,
        Func<ConfigViewModel> configViewModelFactory,
        Func<ConflictDialogViewModel> conflictDialogViewModelFactory)
    {
        _configService = configService;
        _configViewModelFactory = configViewModelFactory;
        _conflictDialogViewModelFactory = conflictDialogViewModelFactory;
        
        RefreshCommand = new AsyncRelayCommand(LoadDocumentsAsync);
        DownloadCommand = new AsyncRelayCommand(DownloadSelectedAsync, CanDownload);
        UploadCommand = new AsyncRelayCommand(UploadLocalAsync);
        UploadCurrentCommand = new AsyncRelayCommand(UploadCurrentAsync, CanUploadCurrent);
        ConfigCommand = new RelayCommand(() => OpenConfigDialog());
        ToggleNavigationCommand = new RelayCommand(ToggleNavigation);
        
        // 自动初始化
        _ = InitializeAsync();
    }

    private void ToggleNavigation()
    {
        IsNavigationVisible = !IsNavigationVisible;
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
        _syncService = new DocumentSyncService(ApiService);
        
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
                    return await dialog.ShowAsync();
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
        if (_syncService == null) return;

        var docDir = Path.Combine(AppContext.BaseDirectory, "doc");
        if (!Directory.Exists(docDir))
        {
            StatusMessage = $"本地文档目录不存在: {docDir}";
            
            // 可以选择创建目录
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

        var files = Directory.GetFiles(docDir, "*.md", SearchOption.AllDirectories)
            .Select(f => new FileInfo(f))
            .ToList();

        if (files.Count == 0)
        {
            StatusMessage = $"没有在 {docDir} 找到 .md 文件";
            return;
        }

        IsLoading = true;
        StatusMessage = $"正在上传 {files.Count} 个文档...";
        
        try
        {
            var progress = new Progress<(int current, int total, string documentTitle)>(p =>
            {
                StatusMessage = $"正在上传: {p.current}/{p.total} - {p.documentTitle}";
            });

            var (success, skipped, failed) = await _syncService.UploadAsync(
                files,
                async (title, localTime, serverTime) =>
                {
                    var dialog = _conflictDialogViewModelFactory();
                    dialog.DocumentTitle = title;
                    dialog.LocalTime = localTime;
                    dialog.ServerTime = serverTime;
                    dialog.Operation = "上传";
                    return await dialog.ShowAsync();
                },
                progress);

            StatusMessage = $"上传完成: 成功 {success}, 跳过 {skipped}, 失败 {failed}";
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
        _ = viewModel.ShowAsync();
    }

    private bool CanUploadCurrent()
    {
        return CurrentPreview != null && CurrentPreview.Document != null;
    }

    private async Task UploadCurrentAsync()
    {
        Console.WriteLine($"[DEBUG] UploadCurrentAsync 开始");
        
        if (_syncService == null || CurrentPreview?.Document == null)
        {
            Console.WriteLine($"[DEBUG] UploadCurrentAsync 退出: _syncService={_syncService != null}, CurrentPreview={CurrentPreview != null}, Document={CurrentPreview?.Document != null}");
            return;
        }

        var document = CurrentPreview.Document;
        var docDir = Path.Combine(AppContext.BaseDirectory, "doc");
        if (!Directory.Exists(docDir))
        {
            Directory.CreateDirectory(docDir);
        }

        var fileName = $"{document.Title}.md";
        fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(docDir, fileName);

        Console.WriteLine($"[DEBUG] 准备上传文件: {filePath}");

        // 如果当前有修改，先保存
        if (CurrentPreview.IsModified)
        {
            await File.WriteAllTextAsync(filePath, CurrentPreview.SourceText);
            Console.WriteLine($"[DEBUG] 已保存修改到本地文件");
        }
        else if (!File.Exists(filePath))
        {
            // 如果文件不存在，创建它
            await File.WriteAllTextAsync(filePath, document.Text);
            Console.WriteLine($"[DEBUG] 已创建本地文件");
        }

        IsLoading = true;
        StatusMessage = $"正在上传: {document.Title}";
        
        try
        {
            var fileInfo = new FileInfo(filePath);
            var (success, skipped, failed) = await _syncService.UploadAsync(
                new List<FileInfo> { fileInfo },
                async (title, localTime, serverTime) =>
                {
                    var dialog = _conflictDialogViewModelFactory();
                    dialog.DocumentTitle = title;
                    dialog.LocalTime = localTime;
                    dialog.ServerTime = serverTime;
                    dialog.Operation = "上传";
                    return await dialog.ShowAsync();
                },
                null);

            Console.WriteLine($"[DEBUG] 上传结果: success={success}, skipped={skipped}, failed={failed}");

            if (success > 0)
            {
                StatusMessage = $"上传成功: {document.Title}";
                Console.WriteLine($"[DEBUG] 设置状态: 上传成功");
            }
            else if (skipped > 0)
            {
                StatusMessage = $"上传跳过: {document.Title} (服务器版本更新)";
                Console.WriteLine($"[DEBUG] 设置状态: 上传跳过");
            }
            else
            {
                StatusMessage = $"上传失败: {document.Title}";
                Console.WriteLine($"[DEBUG] 设置状态: 上传失败");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"上传失败: {ex.Message}";
            Console.WriteLine($"[DEBUG] 上传异常: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            Console.WriteLine($"[DEBUG] UploadCurrentAsync 完成");
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


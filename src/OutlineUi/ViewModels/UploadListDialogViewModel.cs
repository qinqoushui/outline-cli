using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Windows.Input;
using OutlineUi.Models;
using OutlineUi.Services;

namespace OutlineUi.ViewModels;

public class UploadFileInfo : ViewModelBase
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public DateTime? LocalTime { get; set; }
    public DateTime? ServerTime { get; set; }
    public bool HasConflict { get; set; }
    
    public DateTime? LocalTimeDisplay => LocalTime?.ToLocalTime();
    public DateTime? ServerTimeDisplay => ServerTime?.ToLocalTime();
    
    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public string StatusText => HasConflict ? "⚠️ 冲突" : "正常";
    public string StatusColor => HasConflict ? "#FF5722" : "#4CAF50";
}

public class UploadListDialogViewModel : ViewModelBase
{
    public ObservableCollection<UploadFileInfo> Files { get; } = new();
    
    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public ICommand UploadSelectedCommand { get; }
    public ICommand UploadAllCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    
    private TaskCompletionSource<bool> _tcs = new();
    private Views.UploadListDialog? _dialog;
    
    public Func<List<DocumentUploadItem>, Task<List<DocumentUploadItem>>>? RetrieveUploadItemsCallback { get; set; }
    public Func<List<DocumentUploadItem>, Task<(int Success, int Failed)>>? UploadCallback { get; set; }
    public string LocalDocDir { get; set; } = string.Empty;

    public UploadListDialogViewModel()
    {
        UploadSelectedCommand = new RelayCommand(async () => await UploadSelectedAsync());
        UploadAllCommand = new RelayCommand(async () => await UploadAllAsync());
        CancelCommand = new RelayCommand(Cancel);
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
    }

    private async Task UploadSelectedAsync()
    {
        var selectedFiles = GetSelectedFiles().ToList();
        if (selectedFiles.Count == 0)
            return;

        await UploadAndRefresh(selectedFiles);
    }

    private async Task UploadAllAsync()
    {
        _dialog?.SelectAll();
        var selectedFiles = GetSelectedFiles().ToList();
        if (selectedFiles.Count == 0)
            return;

        await UploadAndRefresh(selectedFiles);
    }

    private async Task UploadAndRefresh(List<UploadFileInfo> selectedFiles)
    {
        StatusMessage = "正在上传...";
        
        var uploadItems = selectedFiles.Select(f => new DocumentUploadItem
        {
            DocumentId = f.DocumentId,
            Title = f.DocumentTitle,
            LocalTime = f.LocalTime ?? DateTime.MinValue,
            ServerTime = f.ServerTime,
            Content = File.ReadAllText(f.FilePath),
            Selected = true,
            HasConflict = f.HasConflict
        }).ToList();

        if (UploadCallback != null)
        {
            var (success, failed) = await UploadCallback(uploadItems);
            StatusMessage = $"上传完成: 成功 {success}, 失败 {failed}";
        }

        await RefreshUploadList();
    }

    private async Task RefreshUploadList()
    {
        if (RetrieveUploadItemsCallback != null)
        {
            var uploadItems = await RetrieveUploadItemsCallback(new List<DocumentUploadItem>());
            
            Files.Clear();
            foreach (var item in uploadItems)
            {
                Files.Add(new UploadFileInfo
                {
                    FilePath = Path.Combine(LocalDocDir, $"{item.DocumentId}.md"),
                    FileName = $"{item.DocumentId}.md",
                    DocumentId = item.DocumentId,
                    DocumentTitle = item.Title,
                    LocalTime = item.LocalTime,
                    ServerTime = item.ServerTime,
                    HasConflict = item.HasConflict,
                    IsSelected = item.Selected
                });
            }

            var conflictCount = Files.Count(f => f.HasConflict);
            StatusMessage = $"共 {Files.Count} 个文件待上传，其中 {conflictCount} 个存在冲突";

            if (Files.Count == 0)
            {
                _tcs.TrySetResult(true);
            }
        }
    }

    private void Cancel()
    {
        _tcs.TrySetResult(false);
    }

    private void SelectAll()
    {
        _dialog?.SelectAll();
    }

    private void DeselectAll()
    {
        _dialog?.DeselectAll();
    }

    public async Task<bool> ShowAsync()
    {
        _tcs = new TaskCompletionSource<bool>();
        
        var conflictCount = Files.Count(f => f.HasConflict);
        StatusMessage = $"共 {Files.Count} 个文件待上传，其中 {conflictCount} 个存在冲突";
        
        _dialog = new Views.UploadListDialog
        {
            DataContext = this
        };
        var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
        if (window != null)
        {
            var _ = _dialog.ShowDialog(window);
            return await _tcs.Task;
        }
        return false;
    }

    public void Close()
    {
        _tcs.TrySetResult(false);
    }

    public IEnumerable<UploadFileInfo> GetSelectedFiles()
    {
        if (_dialog != null)
        {
            var dataGrid = _dialog.UploadDataGrid;
            return dataGrid.SelectedItems.Cast<UploadFileInfo>().ToList();
        }
        return Files.Where(f => f.IsSelected);
    }
}

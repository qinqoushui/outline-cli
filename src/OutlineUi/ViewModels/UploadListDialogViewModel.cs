using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
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
    public bool IsSelected { get; set; } = true;
    
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

    public UploadListDialogViewModel()
    {
        UploadSelectedCommand = new RelayCommand(UploadSelected);
        UploadAllCommand = new RelayCommand(UploadAll);
        CancelCommand = new RelayCommand(Cancel);
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
    }

    private void UploadSelected()
    {
        _tcs.TrySetResult(true);
    }

    private void UploadAll()
    {
        foreach (var file in Files)
        {
            file.IsSelected = true;
        }
        _tcs.TrySetResult(true);
    }

    private void Cancel()
    {
        _tcs.TrySetResult(false);
    }

    private void SelectAll()
    {
        foreach (var file in Files)
        {
            file.IsSelected = true;
        }
    }

    private void DeselectAll()
    {
        foreach (var file in Files)
        {
            file.IsSelected = false;
        }
    }

    public async Task<bool> ShowAsync()
    {
        _tcs = new TaskCompletionSource<bool>();
        
        var conflictCount = Files.Count(f => f.HasConflict);
        StatusMessage = $"共 {Files.Count} 个文件待上传，其中 {conflictCount} 个存在冲突";
        
        var dialog = new Views.UploadListDialog
        {
            DataContext = this
        };
        var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
        if (window != null)
        {
            var _ = dialog.ShowDialog(window);
            return await _tcs.Task;
        }
        return false;
    }

    public void Close()
    {
        _tcs.TrySetResult(false);
    }
}

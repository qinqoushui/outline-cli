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
        _dialog?.SelectAll();
        _tcs.TrySetResult(true);
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

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Windows.Input;
using OutlineUi.Services;
using OutlineUi.Models;

namespace OutlineUi.ViewModels;

public class ConflictDialogViewModel : ViewModelBase
{
    private string _documentTitle = string.Empty;
    public string DocumentTitle
    {
        get => _documentTitle;
        set => SetProperty(ref _documentTitle, value);
    }

    private DateTime? _localTime;
    public DateTime? LocalTime
    {
        get => _localTime;
        set
        {
            if (SetProperty(ref _localTime, value))
            {
                OnPropertyChanged(nameof(LocalTimeDisplay));
            }
        }
    }

    private DateTime? _serverTime;
    public DateTime? ServerTime
    {
        get => _serverTime;
        set
        {
            if (SetProperty(ref _serverTime, value))
            {
                OnPropertyChanged(nameof(ServerTimeDisplay));
            }
        }
    }

    public DateTime? LocalTimeDisplay => _localTime.HasValue ? _localTime.Value.ToLocalTime() : null;
    public DateTime? ServerTimeDisplay => _serverTime.HasValue ? _serverTime.Value.ToLocalTime() : null;

    private bool _showApplyToAll = true;
    public bool ShowApplyToAll
    {
        get => _showApplyToAll;
        set => SetProperty(ref _showApplyToAll, value);
    }

    private bool _applyToAll;
    public bool ApplyToAll
    {
        get => _applyToAll;
        set => SetProperty(ref _applyToAll, value);
    }

    private string _operation = "下载";
    public string Operation
    {
        get => _operation;
        set
        {
            if (SetProperty(ref _operation, value))
            {
                OnPropertyChanged(nameof(IsDownload));
                OnPropertyChanged(nameof(IsUpload));
                OnPropertyChanged(nameof(PrimaryButtonText));
                OnPropertyChanged(nameof(SecondaryButtonText));
            }
        }
    }

    public bool IsDownload => Operation == "下载";
    public bool IsUpload => Operation == "上传";

    public string PrimaryButtonText => IsDownload ? "覆盖本地" : "覆盖服务器";
    public string SecondaryButtonText => IsDownload ? "保留本地" : "取消";
    
    public string PrimaryButtonDescription => IsDownload ? "- 使用服务器版本" : "- 使用本地版本";
    public string SecondaryButtonDescription => IsDownload ? "- 跳过下载" : "- 取消上传";

    public ICommand PrimaryCommand { get; }
    public ICommand SecondaryCommand { get; }

    private TaskCompletionSource<ConflictResolver.ConflictResolution?> _tcs = new();
    private Views.ConflictDialog? _dialog;

    public ConflictDialogViewModel()
    {
        PrimaryCommand = new RelayCommand(() =>
        {
            var resolution = IsDownload 
                ? ConflictResolver.ConflictResolution.OverwriteLocal 
                : ConflictResolver.ConflictResolution.OverwriteServer;
            Resolve(resolution);
        });

        SecondaryCommand = new RelayCommand(() =>
        {
            var resolution = IsDownload 
                ? ConflictResolver.ConflictResolution.Skip 
                : ConflictResolver.ConflictResolution.Cancel;
            Resolve(resolution);
        });
    }

    private void Resolve(ConflictResolver.ConflictResolution resolution)
    {
        _tcs.TrySetResult(resolution);
        _dialog?.Close();
    }

    public async Task<ConflictResolver.ConflictResolution?> ShowAsync()
    {
        _tcs = new TaskCompletionSource<ConflictResolver.ConflictResolution?>();
        
        _dialog = new Views.ConflictDialog
        {
            DataContext = this
        };

        if (!_showApplyToAll)
        {
            _dialog.HideApplyToAll();
        }

        var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
        if (window != null)
        {
            var _ = _dialog.ShowDialog(window);
            return await _tcs.Task;
        }
        return ConflictResolver.ConflictResolution.Cancel;
    }

    public void Close()
    {
        _tcs.TrySetResult(null);
        _dialog?.Close();
    }
}
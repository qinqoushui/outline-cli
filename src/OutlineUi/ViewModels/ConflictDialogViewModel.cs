using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Windows.Input;
using OutlineUi.Models;
using OutlineUi.Services;

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
        set => SetProperty(ref _localTime, value);
    }

    private DateTime? _serverTime;
    public DateTime? ServerTime
    {
        get => _serverTime;
        set => SetProperty(ref _serverTime, value);
    }

    private bool _applyToAll;
    public bool ApplyToAll
    {
        get => _applyToAll;
        set => SetProperty(ref _applyToAll, value);
    }

    public string Operation { get; set; } = "下载";

    public ICommand OverwriteLocalCommand { get; }
    public ICommand OverwriteServerCommand { get; }
    public ICommand SkipCommand { get; }
    public ICommand CancelCommand { get; }

    private TaskCompletionSource<ConflictResolver.ConflictResolution?> _tcs = new();

    public ConflictDialogViewModel()
    {
        OverwriteLocalCommand = new RelayCommand(() => Resolve(ConflictResolver.ConflictResolution.OverwriteLocal));
        OverwriteServerCommand = new RelayCommand(() => Resolve(ConflictResolver.ConflictResolution.OverwriteServer));
        SkipCommand = new RelayCommand(() => Resolve(ConflictResolver.ConflictResolution.Skip));
        CancelCommand = new RelayCommand(() => Resolve(ConflictResolver.ConflictResolution.Cancel));
    }

    private void Resolve(ConflictResolver.ConflictResolution resolution)
    {
        _tcs.TrySetResult(resolution);
    }

    public async Task<ConflictResolver.ConflictResolution?> ShowAsync()
    {
        _tcs = new TaskCompletionSource<ConflictResolver.ConflictResolution?>();
        
        var dialog = new Views.ConflictDialog
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
        return ConflictResolver.ConflictResolution.Cancel;
    }

    public void Close()
    {
        _tcs.TrySetResult(null);
    }
}
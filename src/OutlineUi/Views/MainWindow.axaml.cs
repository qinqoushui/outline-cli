using System;
using System.Threading.Tasks;
using AtomUI.Desktop.Controls;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OutlineUi.ViewModels;
using OutlineUi.Services;

namespace OutlineUi.Views;

public partial class MainWindow : AtomUI.Desktop.Controls.Window
{
    private readonly MainViewModel _viewModel;
    private DocumentPreviewViewModel? _currentPreviewViewModel;
    private Grid? _contentGrid;
    private Border? _navigationBorder;
    private GridSplitter? _gridSplitter;

    public MainWindow()
    {
        InitializeComponent();
        
        var configService = new ConfigService();
        var conflictDialogViewModelFactory = new Func<ConflictDialogViewModel>(() => new ConflictDialogViewModel());
        var configViewModelFactory = new Func<ConfigViewModel>(() => new ConfigViewModel(configService));
        var uploadListDialogViewModelFactory = new Func<UploadListDialogViewModel>(() => new UploadListDialogViewModel());
        _viewModel = new MainViewModel(configService, configViewModelFactory, conflictDialogViewModelFactory, uploadListDialogViewModelFactory);
        DataContext = _viewModel;
        
        Loaded += MainWindow_Loaded;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DocumentTree != null)
        {
            DocumentTree.DoubleTapped += DocumentTree_DoubleTapped;
        }
        
        _contentGrid = this.FindControl<Grid>("ContentGrid");
        _navigationBorder = this.FindControl<Border>("NavigationBorder");
        _gridSplitter = this.FindControl<GridSplitter>("NavigationSplitter");
        
        var notificationService = new NotificationService(TopLevel.GetTopLevel(this));
        _viewModel.InitializeNotificationService(notificationService);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsNavigationVisible))
        {
            UpdateGridLayout();
        }
    }

    private void UpdateGridLayout()
    {
        if (_contentGrid == null || _navigationBorder == null || _gridSplitter == null) return;

        if (_viewModel.IsNavigationVisible)
        {
            _contentGrid.ColumnDefinitions[0].Width = new Avalonia.Controls.GridLength(250);
            _contentGrid.ColumnDefinitions[1].Width = new Avalonia.Controls.GridLength(5);
            _navigationBorder.IsVisible = true;
            _gridSplitter.IsVisible = true;
        }
        else
        {
            _contentGrid.ColumnDefinitions[0].Width = new Avalonia.Controls.GridLength(0);
            _contentGrid.ColumnDefinitions[1].Width = new Avalonia.Controls.GridLength(0);
            _navigationBorder.IsVisible = false;
            _gridSplitter.IsVisible = false;
        }
    }

    private async void DocumentTree_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DocumentTree.SelectedItem is Models.DocumentNode node && node.Type == Models.NodeType.Document && !string.IsNullOrEmpty(node.Id))
        {
            if (_viewModel.ApiService == null)
            {
                _viewModel.StatusMessage = "请先完成配置";
                return;
            }
            
            var viewModel = new DocumentPreviewViewModel(_viewModel.ApiService, _viewModel.NotificationService);
            viewModel.ConflictCheckRequested += ViewModel_ConflictCheckRequested;
            await viewModel.LoadDocumentAsync(node.Id);
            var preview = new DocumentPreview
            {
                DataContext = viewModel
            };
            PreviewArea.Content = preview;
            
            _viewModel.CurrentPreview = viewModel;
            _currentPreviewViewModel = viewModel;
            
            _viewModel.SaveLastOpenedDocumentId(node.Id);
        }
    }

    private async void ViewModel_ConflictCheckRequested(object? sender, ViewModels.ConflictCheckEventArgs e)
    {
        var dialog = new ConflictDialogViewModel();
        dialog.DocumentTitle = e.DocumentTitle;
        dialog.LocalTime = e.LocalTime;
        dialog.ServerTime = e.ServerTime;
        dialog.Operation = e.Operation;
        
        var result = await dialog.ShowAsync();
        e.ResultHandler(result == Services.ConflictResolver.ConflictResolution.OverwriteLocal);
    }

    private void ViewModel_MessageRequested(object? sender, string message)
    {
        if (_viewModel != null)
        {
            _viewModel.StatusMessage = message;
            
            Task.Delay(3000).ContinueWith(_ =>
            {
                if (_viewModel != null)
                {
                    _viewModel.StatusMessage = "就绪";
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}

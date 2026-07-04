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
        _viewModel = new MainViewModel(configService, configViewModelFactory, conflictDialogViewModelFactory);
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
            // TODO: 检查当前文档是否有未保存的更改
            // 暂时直接加载新文档
            
            var viewModel = new DocumentPreviewViewModel(_viewModel.ApiService);
            viewModel.MessageRequested += ViewModel_MessageRequested;
            viewModel.ConflictCheckRequested += ViewModel_ConflictCheckRequested;
            await viewModel.LoadDocumentAsync(node.Id);
            var preview = new DocumentPreview
            {
                DataContext = viewModel
            };
            PreviewArea.Content = preview;
            
            // 设置当前预览，以便工具栏按钮可以绑定
            _viewModel.CurrentPreview = viewModel;
            _currentPreviewViewModel = viewModel;
        }
    }

    private void ViewModel_ConflictCheckRequested(object? sender, ViewModels.ConflictCheckEventArgs e)
    {
        // 使用 ConflictDialogViewModel 显示冲突对话框
        var dialog = new ConflictDialogViewModel();
        dialog.DocumentTitle = e.DocumentTitle;
        dialog.LocalTime = e.LocalTime;
        dialog.ServerTime = e.ServerTime;
        dialog.Operation = e.Operation;
        
        _ = Task.Run(async () =>
        {
            var result = await dialog.ShowAsync();
            e.ResultHandler(result);
        });
    }

    private void ViewModel_MessageRequested(object? sender, string message)
    {
        Console.WriteLine($"[DEBUG] ViewModel_MessageRequested 收到消息: {message}");
        
        // 在状态栏显示消息
        if (_viewModel != null)
        {
            _viewModel.StatusMessage = message;
            Console.WriteLine($"[DEBUG] StatusMessage 已设置为: {message}");
            
            // 3秒后恢复为"就绪"
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

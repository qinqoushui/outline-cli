using System;
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

    public MainWindow()
    {
        InitializeComponent();
        
        var configService = new ConfigService();
        var conflictDialogViewModelFactory = new Func<ConflictDialogViewModel>(() => new ConflictDialogViewModel());
        var configViewModelFactory = new Func<ConfigViewModel>(() => new ConfigViewModel(configService));
        _viewModel = new MainViewModel(configService, configViewModelFactory, conflictDialogViewModelFactory);
        DataContext = _viewModel;
        
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DocumentTree != null)
        {
            DocumentTree.DoubleTapped += DocumentTree_DoubleTapped;
        }
    }

    private async void DocumentTree_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DocumentTree.SelectedItem is Models.DocumentNode node && node.Type == Models.NodeType.Document && !string.IsNullOrEmpty(node.Id))
        {
            var viewModel = new DocumentPreviewViewModel(_viewModel.ApiService);
            await viewModel.LoadDocumentAsync(node.Id);
            var preview = new DocumentPreview
            {
                DataContext = viewModel
            };
            PreviewArea.Content = preview;
        }
    }
}

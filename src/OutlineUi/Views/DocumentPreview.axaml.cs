using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OutlineUi.ViewModels;
using OutlineUi.Services;
using System;

namespace OutlineUi.Views;

public partial class DocumentPreview : UserControl
{
    private TextBlock? _htmlContent;

    public DocumentPreview()
    {
        InitializeComponent();
        Loaded += DocumentPreview_Loaded;
    }

    private void DocumentPreview_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _htmlContent = this.FindControl<TextBlock>("HtmlContent");
        
        if (DataContext is DocumentPreviewViewModel viewModel)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateContent(viewModel.SourceText);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentPreviewViewModel.SourceText) && 
            DataContext is DocumentPreviewViewModel viewModel)
        {
            UpdateContent(viewModel.SourceText);
        }
    }

    private void UpdateContent(string markdown)
    {
        if (_htmlContent == null || string.IsNullOrEmpty(markdown))
            return;

        // 简单转换：保留原始文本，添加基本格式
        var html = MarkdownToHtmlConverter.Convert(markdown);
        _htmlContent.Text = html;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is DocumentPreviewViewModel viewModel)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateContent(viewModel.SourceText);
        }
    }
}

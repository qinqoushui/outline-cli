using Avalonia.Controls;
using OutlineUi.ViewModels;
using System;

namespace OutlineUi.Views;

public partial class DocumentPreview : UserControl
{
    public DocumentPreview()
    {
        InitializeComponent();
        DataContextChanged += DocumentPreview_DataContextChanged;
    }

    private void DocumentPreview_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DocumentPreviewViewModel viewModel)
        {
            viewModel.MessageRequested += ViewModel_MessageRequested;
        }
    }

    private void ViewModel_MessageRequested(object? sender, string message)
    {
        // TODO: 实现消息提示
        Console.WriteLine($"[Message] {message}");
    }
}

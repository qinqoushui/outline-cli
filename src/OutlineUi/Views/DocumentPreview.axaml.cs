using Avalonia.Controls;
using OutlineUi.ViewModels;
using System;

namespace OutlineUi.Views;

public partial class DocumentPreview : UserControl
{
    public DocumentPreview()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
    }
}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OutlineUi.Views;

public partial class DocumentPreview : UserControl
{
    public DocumentPreview()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

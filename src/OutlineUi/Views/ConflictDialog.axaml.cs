using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OutlineUi.Views;

public partial class ConflictDialog : Window
{
    public ConflictDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

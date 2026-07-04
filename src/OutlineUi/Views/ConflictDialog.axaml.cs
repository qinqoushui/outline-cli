using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OutlineUi.Views;

public partial class ConflictDialog : AtomUI.Desktop.Controls.Window
{
    public ConflictDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void HideApplyToAll()
    {
        var panel = this.FindControl<StackPanel>("ApplyToAllPanel");
        if (panel != null)
        {
            panel.IsVisible = false;
        }
    }
}
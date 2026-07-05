using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AtomUI.Icons.AntDesign;

namespace OutlineUi.Controls;

public class ThemeIconControl : ContentControl
{
    public static readonly StyledProperty<bool> IsDarkProperty =
        AvaloniaProperty.Register<ThemeIconControl, bool>(nameof(IsDark));

    public bool IsDark
    {
        get => GetValue(IsDarkProperty);
        set => SetValue(IsDarkProperty, value);
    }

    public ThemeIconControl()
    {
        InitializeComponent();
        UpdateIcon(false);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsDarkProperty)
        {
            UpdateIcon((bool)change.NewValue!);
        }
    }

    private void UpdateIcon(bool isDark)
    {
        Content = new AntDesignIconProvider
        {
            Kind = isDark ? AntDesignIconKind.MoonOutlined : AntDesignIconKind.SunOutlined
        };
    }
}
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using OutlineUi.Views;
using OutlineUi.Services;
using AtomUI;
using AtomUI.Controls;
using AtomUI.Theme;
using AtomUI.Theme.Language;
using AtomUI.Desktop.Controls;
using CodeWF.Markdown.Themes;

namespace OutlineUi;

public partial class App : Application
{
    public static bool IsDark { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        this.UseAtomUI(builder =>
        {
            builder.WithDefaultTheme(IThemeManager.DEFAULT_THEME_ID);
            builder.WithDefaultLanguageVariant(LanguageVariant.zh_CN);
            builder.UseDesktopControls();
            builder.UseDesktopDataGrid();
        });

        Styles.Add(new MarkdownThemes());
        
        var configService = new ConfigService();
        var savedTheme = configService.GetTheme();
        IsDark = savedTheme == "Dark";
        ApplyTheme(IsDark);
    }
    
    public static void ApplyTheme(bool dark)
    {
        IsDark = dark;
        Current!.SetDarkThemeMode(dark);
        ApplyMarkdownBrushResources(dark);
    }

    private static void ApplyMarkdownBrushResources(bool dark)
    {
        var res = Current!.Resources;
        if (dark)
        {
            res["TextBlockDefaultForeground"] = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB));
            res["TextBlockDisabledForeground"] = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
            res["BorderCardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51));
            res["BorderCardBackground"] = new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37));
            res["ButtonSolidPrimaryBackground"] = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
            res["ButtonSolidForeground"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            res["ButtonDefaultBackground"] = new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51));
            res["TextBoxDefaultBackground"] = new SolidColorBrush(Color.FromRgb(0x37, 0x41, 0x51));
            res["TextBlockCodeBackground"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            res["TextBlockSelectionBackground"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x5F));
            res["WindowDefaultBackground"] = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
        }
        else
        {
            res["TextBlockDefaultForeground"] = new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37));
            res["TextBlockDisabledForeground"] = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));
            res["BorderCardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB));
            res["BorderCardBackground"] = new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB));
            res["ButtonSolidPrimaryBackground"] = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
            res["ButtonSolidForeground"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            res["ButtonDefaultBackground"] = new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6));
            res["TextBoxDefaultBackground"] = new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6));
            res["TextBlockCodeBackground"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            res["TextBlockSelectionBackground"] = new SolidColorBrush(Color.FromRgb(0xDB, 0xEA, 0xFE));
            res["WindowDefaultBackground"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
        
        var configService = new ConfigService();
        var savedTheme = configService.GetTheme();
        IsDark = savedTheme == "Dark";
        ApplyTheme(IsDark);
    }
    
    public static void ApplyTheme(bool dark)
    {
        IsDark = dark;
        Current!.SetDarkThemeMode(dark);
        MarkdownThemes.OverrideTypographyResources(
            Current,
            dark ? MarkdownTypographyThemes.Basic
                 : MarkdownTypographyThemes.Basic,
            MarkdownTypographySizes.Normal);
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

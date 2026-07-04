using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OutlineUi.Views;
using AtomUI;
using AtomUI.Theme;
using AtomUI.Theme.Language;
using AtomUI.Desktop.Controls;

namespace OutlineUi;

public partial class App : Application
{
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

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
using Lang.Avalonia;
using Lang.Avalonia.Json;
using System.Globalization;
using System.IO;

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
        
        InitializeI18n();
        
        var configService = new ConfigService();
        var savedTheme = configService.GetTheme();
        IsDark = savedTheme == "Dark";
        ApplyTheme(IsDark);
    }

    private static void InitializeI18n()
    {
        var i18nDir = System.IO.Path.Combine(System.AppContext.BaseDirectory, "I18n");
        if (!Directory.Exists(System.IO.Path.Combine(i18nDir, "CodeWF.Markdown")))
        {
            ExtractEmbeddedI18n(i18nDir);
        }
        
        var langPlugin = new JsonLangPlugin
        {
            ResourceFolder = i18nDir
        };
        I18nManager.Instance.Register(langPlugin, new CultureInfo("zh-CN"), out _);
    }

    private static void ExtractEmbeddedI18n(string targetDir)
    {
        var dir = System.IO.Path.Combine(targetDir, "CodeWF.Markdown");
        Directory.CreateDirectory(dir);
        
        var zhCn = @"{
  ""language"": ""简体中文"",
  ""description"": ""中文（简体）"",
  ""cultureName"": ""zh-CN"",
  ""CodeWF"": {
    ""MarkdownL"": {
      ""ImagePreviewTitle"": ""图片预览"",
      ""ImagePreviewZoomOut"": ""缩小"",
      ""ImagePreviewZoomIn"": ""放大"",
      ""ImagePreviewRotateLeft"": ""左旋"",
      ""ImagePreviewRotateRight"": ""右旋"",
      ""ImagePreviewSaveAs"": ""另存为"",
      ""ImagePreviewImagesFileType"": ""图片"",
      ""ImagePreviewZoomStatus"": ""{0:P0} / {1} 度"",
      ""ImagePreviewActualSize"": ""1:1"",
      ""ImagePreviewFit"": ""适应"",
      ""ImageLoadFailed"": ""图片加载失败：{0}"",
      ""ImageFileNotFound"": ""文件不存在：{0}"",
      ""CopySelectedText"": ""复制选中文本"",
      ""CopyRenderedText"": ""复制渲染文本"",
      ""Copy"": ""复制"",
      ""SocialCopyToolName"": ""Markdown 编辑器"",
      ""SocialCopySuffixFormat"": ""本文使用 {0} 排版""
    }
  }
}";
        File.WriteAllText(System.IO.Path.Combine(dir, "zh-CN.json"), zhCn);
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

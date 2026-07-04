using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OutlineUi.ViewModels;
using System;
using System.Runtime.InteropServices;

namespace OutlineUi.Views;

public partial class DocumentPreview : UserControl
{
    private ContentControl? _previewHost;
    private object? _webView;

    public DocumentPreview()
    {
        InitializeComponent();
        Loaded += DocumentPreview_Loaded;
    }

    private async void DocumentPreview_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _previewHost = this.FindControl<ContentControl>("PreviewHost");
        
        if (DataContext is DocumentPreviewViewModel viewModel)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // 初始化 WebView
            await InitializeWebViewAsync();
            UpdateContent(viewModel.SourceText);
        }
    }

    private async System.Threading.Tasks.Task InitializeWebViewAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // 非 Windows 平台，使用简单文本显示
            if (_previewHost != null)
            {
                _previewHost.Content = new TextBlock 
                { 
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 14 
                };
            }
            return;
        }

        try
        {
            // Windows 平台：尝试创建 WebView2
            var webView2Type = Type.GetType("Microsoft.Web.WebView2.WinForms.WebView2, Microsoft.Web.WebView2.WinForms");
            if (webView2Type != null)
            {
                _webView = Activator.CreateInstance(webView2Type);
                
                // 配置 WebView2
                var coreWebView2Property = webView2Type.GetProperty("CoreWebView2");
                if (coreWebView2Property != null)
                {
                    // 等待初始化
                    var ensureMethod = webView2Type.GetMethod("EnsureCoreWebView2Async");
                    if (ensureMethod != null)
                    {
                        await (System.Threading.Tasks.Task)ensureMethod.Invoke(_webView, new object[] { null });
                    }
                }
                
                // 使用 WindowsFormsHost 嵌入
                var hostType = Type.GetType("Avalonia.Controls.WindowsFormsHost, Avalonia.Desktop");
                if (hostType != null && _previewHost != null)
                {
                    var host = Activator.CreateInstance(hostType, _webView);
                    _previewHost.Content = host;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebView2 初始化失败: {ex.Message}");
            // 回退到简单文本显示
            if (_previewHost != null)
            {
                _previewHost.Content = new TextBlock 
                { 
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 14 
                };
            }
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentPreviewViewModel.SourceText) && 
            DataContext is DocumentPreviewViewModel viewModel)
        {
            UpdateContent(viewModel.SourceText);
        }
    }

    private void UpdateContent(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return;

        var html = GenerateMarkdownHtml(markdown);

        if (_webView != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                // WebView2 导航到 HTML
                var navigateToStringMethod = _webView.GetType().GetMethod("NavigateToString");
                navigateToStringMethod?.Invoke(_webView, new object[] { html });
            }
            catch
            {
                // 失败则使用简单显示
                if (_previewHost?.Content is TextBlock textBlock)
                {
                    textBlock.Text = markdown;
                }
            }
        }
        else
        {
            // 非 Windows 或 WebView 不可用，使用简单显示
            if (_previewHost?.Content is TextBlock textBlock)
            {
                textBlock.Text = markdown;
            }
        }
    }

    private string GenerateMarkdownHtml(string markdown)
    {
        // 使用 marked.js 渲染 Markdown
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            padding: 20px;
            color: #333;
            max-width: 900px;
            margin: 0 auto;
            background-color: #fff;
        }}
        h1, h2, h3, h4, h5, h6 {{
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
            line-height: 1.25;
        }}
        h1 {{ font-size: 2em; border-bottom: 1px solid #eaecef; padding-bottom: .3em; }}
        h2 {{ font-size: 1.5em; border-bottom: 1px solid #eaecef; padding-bottom: .3em; }}
        h3 {{ font-size: 1.25em; }}
        code {{
            padding: 0.2em 0.4em;
            margin: 0;
            font-size: 85%;
            background-color: rgba(27,31,35,0.05);
            border-radius: 3px;
            font-family: Consolas, Monaco, 'Courier New', monospace;
        }}
        pre {{
            padding: 16px;
            overflow: auto;
            font-size: 85%;
            line-height: 1.45;
            background-color: #f6f8fa;
            border-radius: 3px;
        }}
        pre code {{
            background-color: transparent;
            padding: 0;
        }}
        blockquote {{
            padding: 0 1em;
            color: #6a737d;
            border-left: 0.25em solid #dfe2e5;
            margin: 0 0 16px 0;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin-bottom: 16px;
        }}
        table th, table td {{
            padding: 6px 13px;
            border: 1px solid #dfe2e5;
        }}
        table th {{
            font-weight: 600;
            background-color: #f6f8fa;
        }}
        table tr:nth-child(2n) {{
            background-color: #f6f8fa;
        }}
        img {{
            max-width: 100%;
            box-sizing: content-box;
            background-color: #fff;
        }}
        a {{
            color: #0366d6;
            text-decoration: none;
        }}
        a:hover {{
            text-decoration: underline;
        }}
        ul, ol {{
            padding-left: 2em;
            margin-bottom: 16px;
        }}
        hr {{
            height: 0.25em;
            padding: 0;
            margin: 24px 0;
            background-color: #e1e4e8;
            border: 0;
        }}
    </style>
    <script src=""https://cdn.jsdelivr.net/npm/marked/marked.min.js""></script>
</head>
<body>
    <div id=""content"">正在加载...</div>
    <script>
        try {{
            var markdown = {System.Text.Json.JsonSerializer.Serialize(markdown)};
            document.getElementById('content').innerHTML = marked.parse(markdown);
        }} catch(e) {{
            document.getElementById('content').innerText = 'Markdown 渲染失败: ' + e.message;
        }}
    </script>
</body>
</html>";
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is DocumentPreviewViewModel viewModel)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateContent(viewModel.SourceText);
        }
    }
}

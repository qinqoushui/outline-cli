using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OutlineUi.ViewModels;
using System;
using System.Runtime.InteropServices;

namespace OutlineUi.Views;

public partial class DocumentPreview : UserControl
{
    private ContentControl? _previewHost;
    private bool _isWebViewInitialized;

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
            
            // 延迟初始化，确保控件已加载
            await System.Threading.Tasks.Task.Delay(100);
            
            // 更新内容
            UpdateContent(viewModel.SourceText);
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
        if (_previewHost == null || string.IsNullOrEmpty(markdown))
            return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !_isWebViewInitialized)
            {
                // Windows 平台：尝试使用外部浏览器
                _isWebViewInitialized = true;
                
                // 创建一个按钮，点击后用浏览器打开
                var panel = new StackPanel 
                { 
                    Orientation = Avalonia.Layout.Orientation.Vertical,
                    Spacing = 10,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                
                panel.Children.Add(new TextBlock 
                { 
                    Text = "📄 Markdown 文档",
                    FontSize = 18,
                    FontWeight = Avalonia.Media.FontWeight.Bold
                });
                
                var openButton = new Button 
                { 
                    Content = "在浏览器中打开预览",
                    Padding = new Avalonia.Thickness(20, 10),
                    Background = Avalonia.Media.Brushes.LightBlue
                };
                
                openButton.Click += async (s, e) =>
                {
                    var html = GenerateMarkdownHtml(markdown);
                    var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"markdown_preview_{Guid.NewGuid():N}.html");
                    await System.IO.File.WriteAllTextAsync(tempFile, html);
                    
                    // 用系统默认浏览器打开
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = tempFile,
                            UseShellExecute = true
                        });
                    }
                };
                
                panel.Children.Add(openButton);
                panel.Children.Add(new TextBlock 
                { 
                    Text = $"文档长度: {markdown.Length} 字符",
                    FontSize = 12,
                    Foreground = Avalonia.Media.Brushes.Gray
                });
                
                _previewHost.Content = panel;
            }
            else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // 非 Windows：直接显示文本
                var textBlock = new TextBlock
                {
                    Text = markdown,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 14
                };
                _previewHost.Content = textBlock;
            }
        }
        catch (Exception ex)
        {
            // 错误处理
            _previewHost.Content = new TextBlock 
            { 
                Text = $"预览错误: {ex.Message}\n\n原始内容:\n{markdown}",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };
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
    <title>Markdown 预览</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, 'Noto Sans', sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol', 'Noto Color Emoji';
            line-height: 1.6;
            padding: 40px;
            color: #24292f;
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
        h1 {{ font-size: 2em; border-bottom: 1px solid #d0d7de; padding-bottom: .3em; }}
        h2 {{ font-size: 1.5em; border-bottom: 1px solid #d0d7de; padding-bottom: .3em; }}
        h3 {{ font-size: 1.25em; }}
        h4 {{ font-size: 1em; }}
        code {{
            padding: 0.2em 0.4em;
            margin: 0;
            font-size: 85%;
            background-color: rgba(175,184,193,0.2);
            border-radius: 6px;
            font-family: ui-monospace,SFMono-Regular,SF Mono,Menlo,Consolas,Liberation Mono,monospace;
        }}
        pre {{
            padding: 16px;
            overflow: auto;
            font-size: 85%;
            line-height: 1.45;
            background-color: #f6f8fa;
            border-radius: 6px;
        }}
        pre code {{
            background-color: transparent;
            padding: 0;
        }}
        blockquote {{
            padding: 0 1em;
            color: #57606a;
            border-left: 0.25em solid #d0d7de;
            margin: 0 0 16px 0;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin-bottom: 16px;
        }}
        table th, table td {{
            padding: 6px 13px;
            border: 1px solid #d0d7de;
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
            color: #0969da;
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
            background-color: #d0d7de;
            border: 0;
        }}
        .task-list-item {{
            list-style-type: none;
        }}
        .task-list-item input {{
            margin: 0 0.5em 0.25em -1.6em;
            vertical-align: middle;
        }}
    </style>
    <script src=""https://cdn.jsdelivr.net/npm/marked/marked.min.js""></script>
</head>
<body>
    <div id=""content"" style=""display:none;"">Loading...</div>
    <noscript>
        <div style=""padding: 20px; background: #fff3cd; border: 1px solid #ffc107; border-radius: 4px; margin: 20px 0;"">
            <strong>JavaScript 已禁用</strong><br>
            请启用 JavaScript 以正确渲染 Markdown 内容。
        </div>
    </noscript>
    <script>
        document.addEventListener('DOMContentLoaded', function() {{
            try {{
                var markdown = {System.Text.Json.JsonSerializer.Serialize(markdown)};
                var htmlContent = marked.parse(markdown, {{
                    breaks: true,
                    gfm: true
                }});
                document.getElementById('content').innerHTML = htmlContent;
                document.getElementById('content').style.display = 'block';
            }} catch(e) {{
                document.getElementById('content').innerHTML = '<div style=""padding: 20px; background: #f8d7da; border: 1px solid #f5c6cb; border-radius: 4px;""><strong>Markdown 渲染错误</strong><br>' + e.message + '</div>';
                document.getElementById('content').style.display = 'block';
            }}
        }});
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

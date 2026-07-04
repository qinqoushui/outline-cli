using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OutlineUi.ViewModels;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OutlineUi.Views;

public partial class DocumentPreview : UserControl
{
    private ContentControl? _previewHost;
    private string? _currentTempFile;

    public DocumentPreview()
    {
        InitializeComponent();
        Loaded += DocumentPreview_Loaded;
    }

    private void DocumentPreview_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _previewHost = this.FindControl<ContentControl>("PreviewHost");
        
        if (DataContext is DocumentPreviewViewModel viewModel)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
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

    private async void UpdateContent(string markdown)
    {
        if (_previewHost == null || string.IsNullOrEmpty(markdown))
            return;

        try
        {
            // 生成 HTML
            var html = GenerateMarkdownHtml(markdown);
            
            // 写入临时文件
            if (_currentTempFile != null && File.Exists(_currentTempFile))
            {
                File.Delete(_currentTempFile);
            }
            
            _currentTempFile = Path.Combine(Path.GetTempPath(), $"outline_preview_{Guid.NewGuid():N}.html");
            await File.WriteAllTextAsync(_currentTempFile, html);
            
            // 自动在浏览器中打开
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _currentTempFile,
                    UseShellExecute = true
                });
            }
            
            // 显示提示
            var panel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Vertical,
                Spacing = 15,
                Margin = new Avalonia.Thickness(20),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock
            {
                Text = "✅ 已在浏览器中打开预览",
                FontSize = 16,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brushes.Green
            });

            panel.Children.Add(new TextBlock
            {
                Text = "编辑内容后会自动更新浏览器预览",
                FontSize = 13,
                Foreground = Avalonia.Media.Brushes.Gray
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"文档长度: {markdown.Length} 字符",
                FontSize = 12,
                Foreground = Avalonia.Media.Brushes.Gray,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });

            _previewHost.Content = panel;
        }
        catch (Exception ex)
        {
            _previewHost.Content = new TextBlock
            {
                Text = $"预览错误: {ex.Message}",
                Foreground = Avalonia.Media.Brushes.Red,
                Padding = new Avalonia.Thickness(10)
            };
        }
    }

    private string GenerateMarkdownHtml(string markdown)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Outline Markdown 预览</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; padding: 40px; color: #24292f; max-width: 900px; margin: 0 auto; }}
        h1, h2, h3, h4, h5, h6 {{ margin-top: 24px; margin-bottom: 16px; font-weight: 600; }}
        h1 {{ font-size: 2em; border-bottom: 1px solid #d0d7de; padding-bottom: .3em; }}
        h2 {{ font-size: 1.5em; border-bottom: 1px solid #d0d7de; padding-bottom: .3em; }}
        h3 {{ font-size: 1.25em; }}
        code {{ padding: 0.2em 0.4em; background-color: rgba(175,184,193,0.2); border-radius: 6px; font-family: Consolas, Monaco, monospace; font-size: 85%; }}
        pre {{ padding: 16px; background-color: #f6f8fa; border-radius: 6px; overflow: auto; }}
        pre code {{ background-color: transparent; padding: 0; }}
        blockquote {{ padding: 0 1em; color: #57606a; border-left: 0.25em solid #d0d7de; margin: 0 0 16px 0; }}
        table {{ border-collapse: collapse; width: 100%; margin: 16px 0; }}
        table th, table td {{ padding: 6px 13px; border: 1px solid #d0d7de; }}
        table th {{ font-weight: 600; background-color: #f6f8fa; }}
        a {{ color: #0969da; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
        ul, ol {{ padding-left: 2em; margin: 16px 0; }}
        hr {{ border: 0; height: 0.25em; background-color: #d0d7de; margin: 24px 0; }}
        img {{ max-width: 100%; }}
    </style>
    <script src=""https://cdn.jsdelivr.net/npm/marked/marked.min.js""></script>
</head>
<body>
    <div id=""content"">加载中...</div>
    <script>
        try {{
            var markdown = {System.Text.Json.JsonSerializer.Serialize(markdown)};
            document.getElementById('content').innerHTML = marked.parse(markdown, {{ breaks: true, gfm: true }});
        }} catch(e) {{
            document.getElementById('content').innerHTML = '<div style=""padding: 20px; background: #f8d7da; border-radius: 4px;""><strong>渲染错误</strong><br>' + e.message + '</div>';
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

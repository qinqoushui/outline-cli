using System;
using System.Text;
using System.Text.RegularExpressions;

namespace OutlineUi.Services;

public static class MarkdownToHtmlConverter
{
    public static string Convert(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var html = new StringBuilder(markdown);
        
        // 转义 HTML
        html.Replace("&", "&amp;");
        html.Replace("<", "&lt;");
        html.Replace(">", "&gt;");
        
        // 代码块
        html = ReplaceCodeBlocks(html);
        
        // 标题
        html = ReplaceHeaders(html);
        
        // 粗体和斜体
        var htmlStr = html.ToString();
        htmlStr = htmlStr.Replace("**", "<strong>");
        htmlStr = htmlStr.Replace("**", "</strong>");
        htmlStr = htmlStr.Replace("*", "<em>");
        htmlStr = htmlStr.Replace("*", "</em>");
        html = new StringBuilder(htmlStr);
        
        // 链接
        html = ReplaceLinks(html);
        
        // 列表
        html = ReplaceLists(html);
        
        // 段落
        html = ReplaceParagraphs(html);

        return html.ToString();
    }

    private static StringBuilder ReplaceCodeBlocks(StringBuilder html)
    {
        // 代码块 ```code```
        var pattern = new Regex(@"```(\w*)\n([\s\S]*?)```", RegexOptions.Multiline);
        var result = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in pattern.Matches(html.ToString()))
        {
            result.Append(html.ToString().Substring(lastIndex, match.Index - lastIndex));
            var lang = match.Groups[1].Value;
            var code = match.Groups[2].Value;
            result.Append($"<pre><code class=\"language-{lang}\">{code}</code></pre>");
            lastIndex = match.Index + match.Length;
        }

        result.Append(html.ToString().Substring(lastIndex));
        
        // 行内代码 `code`
        result = new StringBuilder(Regex.Replace(result.ToString(), @"`([^`]+)`", "<code>$1</code>"));

        return result;
    }

    private static StringBuilder ReplaceHeaders(StringBuilder html)
    {
        var lines = html.ToString().Split('\n');
        var result = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("# "))
            {
                result.AppendLine($"<h1>{trimmed.Substring(2)}</h1>");
            }
            else if (trimmed.StartsWith("## "))
            {
                result.AppendLine($"<h2>{trimmed.Substring(3)}</h2>");
            }
            else if (trimmed.StartsWith("### "))
            {
                result.AppendLine($"<h3>{trimmed.Substring(4)}</h3>");
            }
            else if (trimmed.StartsWith("#### "))
            {
                result.AppendLine($"<h4>{trimmed.Substring(5)}</h4>");
            }
            else if (trimmed.StartsWith("##### "))
            {
                result.AppendLine($"<h5>{trimmed.Substring(6)}</h5>");
            }
            else if (trimmed.StartsWith("###### "))
            {
                result.AppendLine($"<h6>{trimmed.Substring(7)}</h6>");
            }
            else
            {
                result.AppendLine(line);
            }
        }

        return result;
    }

    private static StringBuilder ReplaceLinks(StringBuilder html)
    {
        // [text](url)
        var pattern = new Regex(@"\[([^\]]+)\]\(([^)]+)\)");
        return new StringBuilder(pattern.Replace(html.ToString(), "<a href=\"$2\">$1</a>"));
    }

    private static StringBuilder ReplaceLists(StringBuilder html)
    {
        var lines = html.ToString().Split('\n');
        var result = new StringBuilder();
        var inList = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                if (!inList)
                {
                    result.AppendLine("<ul>");
                    inList = true;
                }
                result.AppendLine($"<li>{trimmed.Substring(2)}</li>");
            }
            else
            {
                if (inList)
                {
                    result.AppendLine("</ul>");
                    inList = false;
                }
                result.AppendLine(line);
            }
        }

        if (inList)
        {
            result.AppendLine("</ul>");
        }

        return result;
    }

    private static StringBuilder ReplaceParagraphs(StringBuilder html)
    {
        var lines = html.ToString().Split('\n');
        var result = new StringBuilder();
        var inParagraph = false;
        var paragraphContent = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // 跳过已经在其他标签中的内容
            if (trimmed.StartsWith("<h") || trimmed.StartsWith("<ul") || 
                trimmed.StartsWith("<li") || trimmed.StartsWith("<pre") ||
                trimmed.StartsWith("<code") || trimmed.StartsWith("</"))
            {
                if (inParagraph)
                {
                    result.AppendLine($"<p>{paragraphContent.ToString().Trim()}</p>");
                    paragraphContent.Clear();
                    inParagraph = false;
                }
                result.AppendLine(line);
            }
            else if (string.IsNullOrEmpty(trimmed))
            {
                if (inParagraph)
                {
                    result.AppendLine($"<p>{paragraphContent.ToString().Trim()}</p>");
                    paragraphContent.Clear();
                    inParagraph = false;
                }
            }
            else
            {
                if (!inParagraph)
                {
                    inParagraph = true;
                }
                paragraphContent.AppendLine(trimmed);
            }
        }

        if (inParagraph)
        {
            result.AppendLine($"<p>{paragraphContent.ToString().Trim()}</p>");
        }

        return result;
    }
}

using System.Text.RegularExpressions;
using OutlineCli.Models;

namespace OutlineCli.Utils;

public static class DocumentHelper
{
    public static string SanitizeFileName(string name)
    {
        var invalid = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        foreach (var c in invalid)
            name = name.Replace(c, '_');
        name = name.Trim('.', ' ');
        return string.IsNullOrWhiteSpace(name) ? "untitled" : name[..Math.Min(name.Length, 200)];
    }

    public static (string? DocumentId, string? ShareId) ParseDocumentUrl(string source)
    {
        // 匹配 /doc/id-slug
        var docMatch = Regex.Match(source, @"/doc/([a-zA-Z0-9-]+)");
        if (docMatch.Success)
        {
            var id = docMatch.Groups[1].Value;
            return (id.Split('-')[0], null);
        }

        // 匹配 /s/shareId
        var shareMatch = Regex.Match(source, @"/s/([a-zA-Z0-9-]+)");
        if (shareMatch.Success)
            return (null, shareMatch.Groups[1].Value);

        // 匹配 UUID
        var uuidMatch = Regex.Match(source, @"([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})", RegexOptions.IgnoreCase);
        if (uuidMatch.Success)
            return (uuidMatch.Groups[1].Value, null);

        return (null, null);
    }

    public static (Dictionary<string, string> Metadata, string Content) ParseFileContent(string fileContent)
    {
        var metadata = new Dictionary<string, string>();
        var content = fileContent;

        if (fileContent.StartsWith("---\n"))
        {
            var parts = fileContent.Split(new[] { "---\n" }, 3, StringSplitOptions.None);
            if (parts.Length >= 3)
            {
                foreach (var line in parts[1].Split('\n'))
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var key = line[..colonIndex].Trim();
                        var value = line[(colonIndex + 1)..].Trim().Trim('"').Trim('\'');
                        metadata[key] = value;
                    }
                }
                content = parts[2];
            }
        }

        return (metadata, content);
    }

    public static string CreateFrontMatter(Document doc)
    {
        var lines = new List<string>
        {
            "---",$"id: {doc.Id}",
            $"title: \"{doc.Title}\"",$"url: {doc.Url}"
        };
        
        if (!string.IsNullOrWhiteSpace(doc.CollectionId))
            lines.Add($"collection_id: {doc.CollectionId}");
        if (!string.IsNullOrWhiteSpace(doc.ParentDocumentId))
            lines.Add($"parent_id: {doc.ParentDocumentId}");
        
        lines.Add("---");
        lines.Add("");
        
        return string.Join(Environment.NewLine, lines);
    }

    public static string SaveDocumentToFile(Document doc, string? outputPath, bool withFrontMatter = true)
    {
        var fileName = SanitizeFileName(doc.Title) + ".md";
        var filePath = string.IsNullOrWhiteSpace(outputPath)
            ? fileName
            : Directory.Exists(outputPath)
                ? Path.Combine(outputPath, fileName)
                : outputPath;

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var content = withFrontMatter
            ? CreateFrontMatter(doc) + doc.Text
            : doc.Text;

        File.WriteAllText(filePath, content);
        return Path.GetFullPath(filePath);
    }
}

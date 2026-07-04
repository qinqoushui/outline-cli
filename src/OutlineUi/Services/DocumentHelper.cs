using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OutlineUi.Models;

namespace OutlineUi.Services;

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

    public static (Dictionary<string, string> Metadata, string Content) ParseFileContent(string fileContent)
    {
        var metadata = new Dictionary<string, string>();
        var content = fileContent;

        if (fileContent.StartsWith($"---{Environment.NewLine}"))
        {
            var parts = fileContent.Split(new[] { $"---{Environment.NewLine}" }, 3, StringSplitOptions.None);
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
        if (doc.UpdatedAt.HasValue)
            lines.Add($"updated_at: {doc.UpdatedAt.Value:yyyy-MM-ddTHH:mm:ssZ}");
        
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

        if (doc.UpdatedAt.HasValue)
        {
            File.SetLastWriteTimeUtc(filePath, doc.UpdatedAt.Value);
        }

        return Path.GetFullPath(filePath);
    }
}

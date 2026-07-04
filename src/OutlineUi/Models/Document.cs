using System;
using System.Text.Json.Serialization;

namespace OutlineUi.Models;

public class Document
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("collectionId")]
    public string? CollectionId { get; set; }
    
    [JsonPropertyName("parentDocumentId")]
    public string? ParentDocumentId { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

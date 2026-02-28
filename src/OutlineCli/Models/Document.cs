using System.Text.Json.Serialization;

namespace OutlineCli.Models;

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

public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
    
    [JsonPropertyName("ok")]
    public bool Ok { get; set; } = true;
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

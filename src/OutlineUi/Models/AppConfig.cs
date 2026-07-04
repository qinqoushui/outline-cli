using System.Text.Json.Serialization;

namespace OutlineUi.Models;

public class AppConfig
{
    [JsonPropertyName("api_url")]
    public string ApiUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("api_token")]
    public string ApiToken { get; set; } = string.Empty;
    
    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
    
    [JsonPropertyName("default_collection_id")]
    public string? DefaultCollectionId { get; set; }
    
    [JsonPropertyName("last_opened_document_id")]
    public string? LastOpenedDocumentId { get; set; }
    
    public bool IsValid() => !string.IsNullOrWhiteSpace(ApiUrl) && !string.IsNullOrWhiteSpace(ApiToken);
}

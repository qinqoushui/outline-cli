using System.Text.Json.Serialization;

namespace OutlineCli.Models;

public class AppConfig
{
    [JsonPropertyName("api_url")]
    public string ApiUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("api_token")]
    public string ApiToken { get; set; } = string.Empty;
    
    [JsonPropertyName("default_collection_id")]
    public string? DefaultCollectionId { get; set; }
    
    public bool IsValid() => !string.IsNullOrWhiteSpace(ApiUrl) && !string.IsNullOrWhiteSpace(ApiToken);
}

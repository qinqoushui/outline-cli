using System.Text.Json.Serialization;

namespace OutlineVsix.Models;

public class AppConfig
{
    [JsonPropertyName("api_url")]
    public string ApiUrl { get; set; } = string.Empty;

    [JsonPropertyName("api_token")]
    public string ApiToken { get; set; } = string.Empty;

    public bool IsValid() => !string.IsNullOrWhiteSpace(ApiUrl) && !string.IsNullOrWhiteSpace(ApiToken);
}

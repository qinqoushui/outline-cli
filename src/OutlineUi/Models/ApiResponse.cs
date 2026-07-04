using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace OutlineUi.Models;

public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
    
    [JsonPropertyName("ok")]
    public bool Ok { get; set; } = true;
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

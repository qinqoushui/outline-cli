using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OutlineVsix.Models;

namespace OutlineVsix.Services;

public class OutlineApiService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public OutlineApiService(AppConfig config)
    {
        _baseUrl = config.ApiUrl.TrimEnd('/');
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiToken}");
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
        _http.DefaultRequestHeaders.Add("User-Agent", "OutlineVsix/1.0");
    }

    public async Task<List<Collection>> GetCollectionsAsync()
    {
        var resp = await _http.PostAsync($"{_baseUrl}/api/collections.list", new StringContent("{}", Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        return JsonSerializer.Deserialize<List<Collection>>(data.GetRawText()) ?? [];
    }

    public async Task<List<Document>> GetDocumentsAsync(string? collectionId = null)
    {
        var body = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(collectionId)) body["collectionId"] = collectionId;
        body["limit"] = 100;

        var resp = await _http.PostAsync($"{_baseUrl}/api/documents.list",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        return JsonSerializer.Deserialize<List<Document>>(data.GetRawText()) ?? [];
    }

    public async Task<Document> GetDocumentAsync(string documentId)
    {
        var body = new Dictionary<string, object> { ["id"] = documentId };
        var resp = await _http.PostAsync($"{_baseUrl}/api/documents.info",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        return JsonSerializer.Deserialize<Document>(data.GetRawText())!;
    }

    public async Task<Document> UpdateDocumentAsync(string documentId, string title, string text)
    {
        var body = new Dictionary<string, object>
        {
            ["id"] = documentId,
            ["title"] = title,
            ["text"] = text,
            ["publish"] = true
        };
        var resp = await _http.PostAsync($"{_baseUrl}/api/documents.update",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        return JsonSerializer.Deserialize<Document>(data.GetRawText())!;
    }
}

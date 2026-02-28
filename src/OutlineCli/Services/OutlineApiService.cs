using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OutlineCli.Models;

namespace OutlineCli.Services;

public class OutlineApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public OutlineApiService(string apiUrl, string apiToken)
    {
        _apiUrl = apiUrl.TrimEnd('/');
        _httpClient = new HttpClient
        {
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", apiToken)
            }
        };
    }

    public async Task<Document> GetDocumentAsync(string documentId)
    {
        var result = await PostAsync<ApiResponse<Document>>("/api/documents.info", new { id = documentId });
        return result.Data ?? throw new Exception("文档不存在");
    }

    public async Task<Document> GetDocumentByShareIdAsync(string shareId)
    {
        var result = await PostAsync<ApiResponse<Document>>("/api/documents.info", new { shareId });
        return result.Data ?? throw new Exception("文档不存在");
    }

    public async Task<Document> CreateDocumentAsync(string title, string text, string collectionId, string? parentDocumentId = null, bool publish = true)
    {
        var payload = new Dictionary<string, object>
        {
            ["title"] = title,
            ["text"] = text,
            ["collectionId"] = collectionId,
            ["publish"] = publish
        };
        if (parentDocumentId != null)
            payload["parentDocumentId"] = parentDocumentId;

        var result = await PostAsync<ApiResponse<Document>>("/api/documents.create", payload);
        return result.Data ?? throw new Exception("创建文档失败");
    }

    public async Task<Document> UpdateDocumentAsync(string documentId, string? title, string? text, bool publish = true)
    {
        var payload = new Dictionary<string, object>
        {
            ["id"] = documentId,
            ["publish"] = publish
        };
        if (title != null)
            payload["title"] = title;
        if (text != null)
            payload["text"] = text;

        var result = await PostAsync<ApiResponse<Document>>("/api/documents.update", payload);
        return result.Data ?? throw new Exception("更新文档失败");
    }

    public async Task<List<Document>> ListDocumentsAsync(string? collectionId = null, int limit = 100)
    {
        var payload = new Dictionary<string, object> { ["limit"] = limit };
        if (collectionId != null)
            payload["collectionId"] = collectionId;

        var result = await PostAsync<ApiResponse<List<Document>>>("/api/documents.list", payload);
        return result.Data ?? new List<Document>();
    }

    public async Task<List<Document>> SearchDocumentsAsync(string query, int limit = 25)
    {
        var result = await PostAsync<ApiResponse<List<Document>>>("/api/documents.search", new { query, limit });
        return result.Data ?? new List<Document>();
    }

    public async Task<List<Collection>> ListCollectionsAsync()
    {
        var result = await PostAsync<ApiResponse<List<Collection>>>("/api/collections.list", new { });
        return result.Data ?? new List<Collection>();
    }

    private async Task<T> PostAsync<T>(string endpoint, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{_apiUrl}{endpoint}", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new Exception("认证失败，请检查 API Token");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new Exception("权限不足");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new Exception("资源不存在");
        if (!response.IsSuccessStatusCode)
            throw new Exception($"API 请求失败: {(int)response.StatusCode} - {responseBody}");

        var result = JsonSerializer.Deserialize<T>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return result ?? throw new Exception("解析响应失败");
    }
}

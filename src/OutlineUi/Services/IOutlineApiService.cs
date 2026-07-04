using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OutlineUi.Models;

namespace OutlineUi.Services;

public interface IOutlineApiService
{
    Task<List<Collection>> GetCollectionsAsync();
    Task<List<Document>> GetDocumentsAsync(string? collectionId = null, int limit = 100);
    Task<Document> GetDocumentAsync(string documentId);
    Task<string> GetDocumentContentAsync(string documentId);
    Task<Document> UpdateDocumentAsync(string documentId, string? title, string? text, bool publish = true);
    Task<Document> CreateDocumentAsync(string title, string text, string collectionId, string? parentDocumentId = null, bool publish = true);
    Task<List<Document>> SearchDocumentsAsync(string query, int limit = 25);
}

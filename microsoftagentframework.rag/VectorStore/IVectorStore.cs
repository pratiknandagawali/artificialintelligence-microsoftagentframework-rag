using MicrosoftAgentFramework.Rag.Models;

namespace MicrosoftAgentFramework.Rag.VectorStore;

/// <summary>
/// Interface for vector store operations
/// </summary>
public interface IVectorStore
{
    Task AddDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task<List<SearchResult>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
    Task<List<SearchResult>> SearchWithFilterAsync(string query, Dictionary<string, string> filters, int topK = 5, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default);
}   
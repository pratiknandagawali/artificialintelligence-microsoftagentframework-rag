namespace MicrosoftAgentFramework.Rag.Models;

/// <summary>
/// Represents a document in the RAG system
/// </summary>
public class Document
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a chunk of a document for embedding
/// </summary>
public class DocumentChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DocumentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public ReadOnlyMemory<float> Embedding { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a search result with similarity score
/// </summary>
public class SearchResult
{
    public DocumentChunk Chunk { get; set; } = new();
    public double SimilarityScore { get; set; }
}
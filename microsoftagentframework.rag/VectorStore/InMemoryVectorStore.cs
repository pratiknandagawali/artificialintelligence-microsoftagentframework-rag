using Microsoft.SemanticKernel.Embeddings;
using MicrosoftAgentFramework.Rag.Models;
using System.Collections.Concurrent;

namespace MicrosoftAgentFramework.Rag.VectorStore;

/// <summary>
/// In-memory implementation of vector store using cosine similarity
/// </summary>
public class InMemoryVectorStore : IVectorStore
{
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly ConcurrentDictionary<string, List<DocumentChunk>> _storage = new();
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    public InMemoryVectorStore(
    ITextEmbeddingGenerationService embeddingService,
    int chunkSize = 1000,
    int chunkOverlap = 200)
    {
        _embeddingService = embeddingService;
        _chunkSize = chunkSize;
        _chunkOverlap = chunkOverlap;
    }

    public async Task AddDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        var chunks = ChunkDocument(document);
        var documentChunks = new List<DocumentChunk>();

        foreach (var (content, index) in chunks.Select((c, i) => (c, i)))
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(content, cancellationToken: cancellationToken);

            var chunk = new DocumentChunk
            {
                DocumentId = document.Id,
                Content = content,
                ChunkIndex = index,
                Embedding = embedding,
                Metadata = new Dictionary<string, string>(document.Metadata)
                {
                    ["title"] = document.Title,
                    ["createdAt"] = document.CreatedAt.ToString("O")
                }
            };

            documentChunks.Add(chunk);
        }

        _storage[document.Id] = documentChunks;
    }

    public async Task<List<SearchResult>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);

        var allChunks = _storage.Values.SelectMany(chunks => chunks).ToList();
        var results = new List<SearchResult>();

        foreach (var chunk in allChunks)
        {
            var similarity = CosineSimilarity(queryEmbedding, chunk.Embedding);
            results.Add(new SearchResult
            {
                Chunk = chunk,
                SimilarityScore = similarity
            });
        }

        return results
        .OrderByDescending(r => r.SimilarityScore)
        .Take(topK)
        .ToList();
    }

    public async Task<List<SearchResult>> SearchWithFilterAsync(
    string query,
    Dictionary<string, string> filters,
    int topK = 5,
    CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);

        var allChunks = _storage.Values
        .SelectMany(chunks => chunks)
        .Where(chunk => filters.All(f => chunk.Metadata.TryGetValue(f.Key, out var value) && value == f.Value))
        .ToList();

        var results = new List<SearchResult>();

        foreach (var chunk in allChunks)
        {
            var similarity = CosineSimilarity(queryEmbedding, chunk.Embedding);
            results.Add(new SearchResult
            {
                Chunk = chunk,
                SimilarityScore = similarity
            });
        }

        return results
        .OrderByDescending(r => r.SimilarityScore)
        .Take(topK)
        .ToList();
    }

    public Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_storage.TryRemove(documentId, out _));
    }

    public Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_storage.Count);
    }

    private List<string> ChunkDocument(Document document)
    {
        var chunks = new List<string>();
        var content = document.Content;
        var position = 0;

        while (position < content.Length)
        {
            var length = Math.Min(_chunkSize, content.Length - position);
            var chunk = content.Substring(position, length);
            chunks.Add(chunk);
            position += _chunkSize - _chunkOverlap;
        }

        return chunks;
    }

    private static double CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var aSpan = a.Span;
        var bSpan = b.Span;

        if (aSpan.Length != bSpan.Length)
            throw new ArgumentException("Vectors must have the same length");

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < aSpan.Length; i++)
        {
            dotProduct += aSpan[i] * bSpan[i];
            magnitudeA += aSpan[i] * aSpan[i];
            magnitudeB += bSpan[i] * bSpan[i];
        }

        return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}
using Microsoft.Azure.Cosmos;
using Microsoft.SemanticKernel.Embeddings;
using MicrosoftAgentFramework.Rag.Models;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Container = Microsoft.Azure.Cosmos.Container;

namespace MicrosoftAgentFramework.Rag.VectorStore;

/// <summary>
/// Azure Cosmos DB implementation of IVectorStore
/// NOTE: This implementation uses standard Cosmos DB queries.
/// For production, consider using Cosmos DB with DiskANN vector indexing (preview feature).
/// </summary>
public class CosmosDBVectorStore : IVectorStore, IDisposable
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly ITextEmbeddingGenerationService _embeddingService;
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly string _databaseName;
    private readonly string _containerName;
    private readonly int _vectorDimensions;
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    public CosmosDBVectorStore(
    string endpoint,
    string key,
    string databaseName,
    string containerName,
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    ITextEmbeddingGenerationService embeddingService,
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    int vectorDimensions = 1536,
    int chunkSize = 1000,
    int chunkOverlap = 200)
    {
        _databaseName = databaseName;
        _containerName = containerName;
        _embeddingService = embeddingService;
        _vectorDimensions = vectorDimensions;
        _chunkSize = chunkSize;
        _chunkOverlap = chunkOverlap;

        var options = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Direct,
            MaxRetryAttemptsOnRateLimitedRequests = 3,
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        _cosmosClient = new CosmosClient(endpoint, key, options);
        InitializeAsync().Wait();
        _container = _cosmosClient.GetContainer(_databaseName, _containerName);
    }

    private async Task InitializeAsync()
    {
        try
        {
            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
            var database = databaseResponse.Database;

            // Create container with standard partitioning
            // Note: Vector search policies require preview SDK versions
            var containerProperties = new ContainerProperties
            {
                Id = _containerName,
                PartitionKeyPath = "/documentId"
            };

            await database.CreateContainerIfNotExistsAsync(containerProperties);
            Console.WriteLine($"? Cosmos DB initialized: {_databaseName}/{_containerName}");
            Console.WriteLine($"??  Note: Using client-side vector similarity. For production, enable Cosmos DB vector indexing.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"??  Cosmos DB initialization error: {ex.Message}");
            throw;
        }
    }

    public async Task AddDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        var chunks = ChunkDocument(document);
        var tasks = new List<Task>();

        foreach (var (content, index) in chunks.Select((c, i) => (c, i)))
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(content, cancellationToken: cancellationToken);

            var cosmosDocument = new CosmosDocumentChunk
            {
                Id = $"{document.Id}_{index}",
                DocumentId = document.Id,
                Content = content,
                ChunkIndex = index,
                Embedding = embedding.ToArray(),
                Metadata = new CosmosMetadata
                {
                    Title = document.Title,
                    Category = document.Metadata.GetValueOrDefault("category", ""),
                    Topic = document.Metadata.GetValueOrDefault("topic", ""),
                    Level = document.Metadata.GetValueOrDefault("level", ""),
                    CreatedAt = document.CreatedAt
                }
            };

            tasks.Add(_container.UpsertItemAsync(cosmosDocument, new PartitionKey(document.Id), cancellationToken: cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    public async Task<List<SearchResult>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        // Generate query embedding
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);
        var queryVector = queryEmbedding.ToArray();

        // Fetch all documents and compute similarity client-side
        // Note: For large datasets, consider implementing pagination or server-side filtering
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var allDocuments = new List<CosmosDocumentChunk>();

        using var iterator = _container.GetItemQueryIterator<CosmosDocumentChunk>(queryDefinition);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            allDocuments.AddRange(response);
        }

        // Calculate similarity scores client-side
        var resultsWithScores = allDocuments.Select(doc => new
        {
            Document = doc,
            Score = CosineSimilarity(queryVector, doc.Embedding)
        })
 .OrderByDescending(x => x.Score)
 .Take(topK)
 .ToList();

        // Convert to SearchResult
        var results = resultsWithScores.Select(item =>
        {
            return new SearchResult
            {
                Chunk = new DocumentChunk
                {
                    Id = item.Document.Id,
                    DocumentId = item.Document.DocumentId,
                    Content = item.Document.Content,
                    ChunkIndex = item.Document.ChunkIndex,
                    Metadata = new Dictionary<string, string>
                    {
                        ["title"] = item.Document.Metadata.Title ?? "",
                        ["category"] = item.Document.Metadata.Category ?? "",
                        ["topic"] = item.Document.Metadata.Topic ?? "",
                        ["level"] = item.Document.Metadata.Level ?? "",
                    },
                    Embedding = ReadOnlyMemory<float>.Empty
                },
                SimilarityScore = item.Score
            };
        }).ToList();

        return results;
    }

    public async Task<List<SearchResult>> SearchWithFilterAsync(string query, Dictionary<string, string> filters, int topK = 5, CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);
        var queryVector = queryEmbedding.ToArray();

        // Build filter query
        var filterConditions = new List<string>();
        var parameters = new Dictionary<string, object>();

        int paramIndex = 0;
        foreach (var filter in filters)
        {
            var paramName = $"@filter{paramIndex++}";
       
 filterConditions.Add($"c.metadata.{filter.Key} = {paramName}");
            parameters[paramName] = filter.Value;
        }

        var whereClause = filterConditions.Count > 0 ? $"WHERE {string.Join(" AND ", filterConditions)}" : "";
        var queryText = $"SELECT * FROM c {whereClause}";

        var queryDefinition = new QueryDefinition(queryText);
        foreach (var param in parameters)
            queryDefinition.WithParameter(param.Key, param.Value);

        // Fetch filtered documents
        var filteredDocuments = new List<CosmosDocumentChunk>();
        using var iterator = _container.GetItemQueryIterator<CosmosDocumentChunk>(queryDefinition);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            filteredDocuments.AddRange(response);
        }

        // Calculate similarity scores client-side
        var resultsWithScores = filteredDocuments.Select(doc => new
        {
            Document = doc,
            Score = CosineSimilarity(queryVector, doc.Embedding)
        })
 .OrderByDescending(x => x.Score)
 .Take(topK)
 .ToList();

        // Convert to SearchResult
        var results = resultsWithScores.Select(item =>
        {
            return new SearchResult
            {
                Chunk = new DocumentChunk
                {
                    Id = item.Document.Id,
                    DocumentId = item.Document.DocumentId,
                    Content = item.Document.Content,
                    ChunkIndex = item.Document.ChunkIndex,
                    Metadata = new Dictionary<string, string>
                    {
                        ["title"] = item.Document.Metadata.Title ?? "",
                        ["category"] = item.Document.Metadata.Category ?? "",
                        ["topic"] = item.Document.Metadata.Topic ?? "",
                        ["level"] = item.Document.Metadata.Level ?? ""
                    },
                    Embedding = ReadOnlyMemory<float>.Empty
                },
                SimilarityScore = item.Score
            };
        }).ToList();

        return results;
    }

    public async Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT c.id FROM c WHERE c.documentId = @documentId")
        .WithParameter("@documentId", documentId);

        var chunks = new List<string>();
        using var iterator = _container.GetItemQueryIterator<dynamic>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            chunks.AddRange(response.Select(x => (string)x.id));
        }

        var deleteTasks = chunks.Select(chunkId =>
        _container.DeleteItemAsync<CosmosDocumentChunk>(chunkId, new PartitionKey(documentId), cancellationToken: cancellationToken));

        await Task.WhenAll(deleteTasks);
        return chunks.Count > 0;
    }

    public async Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
        using var iterator = _container.GetItemQueryIterator<int>(query);

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            return response.FirstOrDefault();
        }

        return 0;
    }

    private List<string> ChunkDocument(Document document)
    {
        var chunks = new List<string>();
        var content = document.Content;

        if (content.Length <= _chunkSize)
        {
            chunks.Add(content);
            return chunks;
        }

        var position = 0;
        while (position < content.Length)
        {
            var length = Math.Min(_chunkSize, content.Length - position);
            chunks.Add(content.Substring(position, length));
            position += _chunkSize - _chunkOverlap;
            if (position + _chunkSize > content.Length && position < content.Length)
                break;
        }

        return chunks;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have the same length");

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;

        return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }

    public void Dispose()
    {
        _cosmosClient?.Dispose();
    }
}

internal class CosmosDocumentChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("chunkIndex")]
    public int ChunkIndex { get; set; }

    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = Array.Empty<float>();

    [JsonPropertyName("metadata")]
    public CosmosMetadata Metadata { get; set; } = new();
}

internal class CosmosMetadata
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("level")]
    public string? Level { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Embeddings;
using MicrosoftAgentFramework.Rag.Configuration;

namespace MicrosoftAgentFramework.Rag.VectorStore;

/// <summary>
/// Factory for creating vector store instances based on configuration
/// </summary>
public static class VectorStoreFactory
{
    public static IVectorStore CreateVectorStore(
    IConfiguration configuration,
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    ITextEmbeddingGenerationService embeddingService)
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        var vectorStoreConfig = configuration.GetSection("VectorStore").Get<VectorStoreConfig>();

        if (vectorStoreConfig == null)
        {
            Console.WriteLine("??  No VectorStore configuration found, using InMemory");
            return new InMemoryVectorStore(embeddingService);
        }

        switch (vectorStoreConfig.Provider.ToLower())
        {
            case "cosmosdb":
                return CreateCosmosDBVectorStore(vectorStoreConfig, embeddingService);

            case "inmemory":
            default:
                return CreateInMemoryVectorStore(vectorStoreConfig, embeddingService);
        }
    }

    private static IVectorStore CreateInMemoryVectorStore(
    VectorStoreConfig config,
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    ITextEmbeddingGenerationService embeddingService)
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        Console.WriteLine("? Vector Store: InMemory");
        return new InMemoryVectorStore(
        embeddingService,
        config.Chunking.ChunkSize,
        config.Chunking.ChunkOverlap);
    }

    private static IVectorStore CreateCosmosDBVectorStore(
    VectorStoreConfig config,
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    ITextEmbeddingGenerationService embeddingService)
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        if (config.CosmosDB == null)
        {
            throw new InvalidOperationException("CosmosDB configuration is required when Provider is 'CosmosDB'");
        }

        if (string.IsNullOrEmpty(config.CosmosDB.Endpoint) ||
        string.IsNullOrEmpty(config.CosmosDB.Key))
        {
            throw new InvalidOperationException("CosmosDB Endpoint and Key are required");
        }

        Console.WriteLine($"? Vector Store: Cosmos DB ({config.CosmosDB.DatabaseName})");

        return new CosmosDBVectorStore(
        config.CosmosDB.Endpoint,
        config.CosmosDB.Key,
        config.CosmosDB.DatabaseName,
        config.CosmosDB.ContainerName,
        embeddingService,
        config.CosmosDB.VectorDimensions,
        config.Chunking.ChunkSize,
        config.Chunking.ChunkOverlap);
    }
}
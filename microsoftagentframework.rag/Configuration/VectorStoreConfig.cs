namespace MicrosoftAgentFramework.Rag.Configuration;

/// <summary>
/// Vector store configuration settings
/// </summary>
public class VectorStoreConfig
{
    /// <summary>
    /// Provider type: "InMemory" or "CosmosDB"
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>
        /// Cosmos DB configuration (used when Provider = "CosmosDB")
        /// </summary>
    public CosmosDBConfig? CosmosDB { get; set; }

    /// <summary>
        /// Chunking configuration
        /// </summary>
    public ChunkingConfig Chunking { get; set; } = new();
}
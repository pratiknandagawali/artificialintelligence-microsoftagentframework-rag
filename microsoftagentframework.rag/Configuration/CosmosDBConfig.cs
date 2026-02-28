/// <summary>
/// Cosmos DB specific configuration
/// </summary>
public class CosmosDBConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "RAGDatabase";
    public string ContainerName { get; set; } = "DocumentChunks";
    public int VectorDimensions { get; set; } = 1536;
}

/// <summary>
/// Document chunking configuration
/// </summary>
public class ChunkingConfig
{
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
}
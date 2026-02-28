namespace MicrosoftAgentFramework.Rag.Core.Domain.ValueObjects;

/// <summary>
/// Value object representing document chunking configuration
/// Replaces magic numbers for chunk size and overlap
/// </summary>
public record ChunkingOptions
{
    public int ChunkSize { get; init; }
    public int ChunkOverlap { get; init; }

    public ChunkingOptions()
    {
        ChunkSize = 1000; // Default
        ChunkOverlap = 200; // Default
    }

    public ChunkingOptions(int chunkSize, int chunkOverlap)
    {
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be positive", nameof(chunkSize));
        if (chunkOverlap < 0)
            throw new ArgumentException("Chunk overlap cannot be negative", nameof(chunkOverlap));
        if (chunkOverlap >= chunkSize)
            throw new ArgumentException("Chunk overlap must be less than chunk size", nameof(chunkOverlap));

        ChunkSize = chunkSize;
        ChunkOverlap = chunkOverlap;
    }

    /// <summary>
        /// Small chunks: Better precision, more API calls
        /// </summary>
    public static ChunkingOptions Small => new(500, 100);

    /// <summary>
        /// Default chunks: Balanced approach
        /// </summary>
    public static ChunkingOptions Default => new(1000, 200);

    /// <summary>
        /// Large chunks: Better context, fewer API calls
        /// </summary>
    public static ChunkingOptions Large => new(2000, 400);

    public int CalculateChunkCount(int contentLength)
    {
        if (contentLength <= 0) return 0;
        if (contentLength <= ChunkSize) return 1;

        var effectiveChunkSize = ChunkSize - ChunkOverlap;
        return (int)Math.Ceiling((double)(contentLength - ChunkOverlap) / effectiveChunkSize);
    }
}
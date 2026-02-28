using MicrosoftAgentFramework.Rag.Core.Domain.ValueObjects;

namespace MicrosoftAgentFramework.Rag.Core.Application.Common.Processors;

/// <summary>
/// Interface for document chunking operations
/// Single Responsibility: Only handles splitting documents into chunks
/// </summary>
public interface IDocumentChunker
{
    /// <summary>
    /// Chunks a document's content into smaller pieces based on options
    /// </summary>
    IEnumerable<string> ChunkDocument(string content, ChunkingOptions options);

    /// <summary>
        /// Chunks multiple documents in parallel
        /// </summary>
    Task<Dictionary<string, IEnumerable<string>>> ChunkDocumentsAsync(
 IEnumerable<(string Id, string Content)> documents,
 ChunkingOptions options,
 CancellationToken cancellationToken = default);
}
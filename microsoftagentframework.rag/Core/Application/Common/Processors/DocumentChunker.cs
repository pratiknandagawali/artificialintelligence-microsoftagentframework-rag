using MicrosoftAgentFramework.Rag.Core.Domain.Exceptions;
using MicrosoftAgentFramework.Rag.Core.Domain.ValueObjects;

namespace MicrosoftAgentFramework.Rag.Core.Application.Common.Processors;

/// <summary>
/// Default implementation of IDocumentChunker
/// Uses overlapping window strategy for chunking
/// </summary>
public class DocumentChunker : IDocumentChunker
{
    public IEnumerable<string> ChunkDocument(string content, ChunkingOptions options)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ChunkingException("Content cannot be null or whitespace");
        }

        var chunks = new List<string>();
        var contentLength = content.Length;

        if (contentLength <= options.ChunkSize)
        {
            chunks.Add(content);
            return chunks;
        }

        var position = 0;
        while (position < contentLength)
        {
            var remainingLength = contentLength - position;
            var chunkLength = Math.Min(options.ChunkSize, remainingLength);

            var chunk = content.Substring(position, chunkLength);
            chunks.Add(chunk);

            // Move forward by (ChunkSize - Overlap)
            position += options.ChunkSize - options.ChunkOverlap;

            // Ensure we don't create duplicate chunks
            if (position + options.ChunkSize > contentLength && position < contentLength)
            {
                // Last chunk
                break;
            }
        }

        return chunks;
    }

    public async Task<Dictionary<string, IEnumerable<string>>> ChunkDocumentsAsync(
    IEnumerable<(string Id, string Content)> documents,
    ChunkingOptions options,
    CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, IEnumerable<string>>();

        var tasks = documents.Select(async doc =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunks = await Task.Run(() => ChunkDocument(doc.Content, options), cancellationToken);
            return (doc.Id, Chunks: chunks);
        });

        var completedTasks = await Task.WhenAll(tasks);

        foreach (var (id, chunks) in completedTasks)
        {
            results[id] = chunks;
        }

        return results;
    }
}
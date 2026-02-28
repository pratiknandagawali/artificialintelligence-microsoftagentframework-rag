using MicrosoftAgentFramework.Rag.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when document chunking fails
/// </summary>
public class ChunkingException : RAGException
{
    public ChunkingException(string message) : base(message)
    {
    }

    public ChunkingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
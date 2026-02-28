using MicrosoftAgentFramework.Rag.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when vector search fails
/// </summary>
public class VectorSearchException : RAGException
{
    public VectorSearchException(string message) : base(message)
    {
    }

    public VectorSearchException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
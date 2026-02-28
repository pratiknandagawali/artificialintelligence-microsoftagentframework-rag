using MicrosoftAgentFramework.Rag.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when embedding generation fails
/// </summary>
public class EmbeddingException : RAGException
{
    public EmbeddingException(string message) : base(message)
    {
    }

    public EmbeddingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
namespace MicrosoftAgentFramework.Rag.Core.Domain.Exceptions;

/// <summary>
/// Base exception for all RAG-related errors
/// </summary>
public class RAGException : Exception
{
    public RAGException(string message) : base(message)
    {
    }

    public RAGException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
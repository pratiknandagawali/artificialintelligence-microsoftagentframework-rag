using MicrosoftAgentFramework.Rag.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when a document is not found
/// </summary>
public class DocumentNotFoundException : RAGException
{
    public string DocumentId { get; }

    public DocumentNotFoundException(string documentId)
    : base($"Document with ID '{documentId}' was not found")
    {
        DocumentId = documentId;
    }
}
namespace MicrosoftAgentFramework.Rag.Services;

/// <summary>
/// Interface for RAG (Retrieval Augmented Generation) service
/// </summary>
public interface IRAGService
{
    Task<string> AskQuestionAsync(string question, CancellationToken cancellationToken = default);
    Task<string> AskQuestionWithContextAsync(string question, Dictionary<string, string> filters, CancellationToken cancellationToken = default);
    Task IndexDocumentAsync(string title, string content, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);
}
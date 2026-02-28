using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using MicrosoftAgentFramework.Rag.Models;
using MicrosoftAgentFramework.Rag.VectorStore;

namespace MicrosoftAgentFramework.Rag.Services;

/// <summary>
/// Traditional RAG implementation: Retrieve ? Generate
/// Simple pattern: Search for relevant context and use it to generate an answer
/// </summary>
public class TraditionalRAGService : IRAGService
{
    private readonly IVectorStore _vectorStore;
    private readonly Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService _chatService;
    private readonly ILogger<TraditionalRAGService> _logger;

    public TraditionalRAGService(
    IVectorStore vectorStore,
    Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService chatService,
    ILogger<TraditionalRAGService> logger)
    {
        _vectorStore = vectorStore;
        _chatService = chatService;
        _logger = logger;
    }

    public async Task<string> AskQuestionAsync(string question, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing question: {Question}", question);

        // Step 1: Retrieve relevant documents
        var searchResults = await _vectorStore.SearchAsync(question, topK: 5, cancellationToken);

        if (!searchResults.Any())
        {
            return "I don't have enough information to answer that question.";
        }

        // Step 2: Build context from retrieved documents
        var context = BuildContext(searchResults);

        _logger.LogInformation("Retrieved {Count} relevant chunks", searchResults.Count);

        // Step 3: Generate answer using context
        var prompt = BuildPrompt(question, context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a helpful AI assistant that answers questions based on the provided context. Always cite the source information.");
        chatHistory.AddUserMessage(prompt);

        var response = await _chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);

        _logger.LogInformation("Generated response for question");

        return response.Content ?? "Unable to generate a response.";
    }

    public async Task<string> AskQuestionWithContextAsync(
    string question,
    Dictionary<string, string> filters,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing question with filters: {Question}", question);

        // Step 1: Retrieve relevant documents with filters
        var searchResults = await _vectorStore.SearchWithFilterAsync(question, filters, topK: 5, cancellationToken);

        if (!searchResults.Any())
        {
            return "I don't have enough information matching your criteria to answer that question.";
        }

        // Step 2: Build context from retrieved documents
        var context = BuildContext(searchResults);

        // Step 3: Generate answer using context
        var prompt = BuildPrompt(question, context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a helpful AI assistant that answers questions based on the provided context. Always cite the source information.");
        chatHistory.AddUserMessage(prompt);

        var response = await _chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);

        return response.Content ?? "Unable to generate a response.";
    }

    public async Task IndexDocumentAsync(
    string title,
    string content,
    Dictionary<string, string>? metadata = null,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Indexing document: {Title}", title);

        var document = new Document
        {
            Title = title,
            Content = content,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        await _vectorStore.AddDocumentAsync(document, cancellationToken);

        _logger.LogInformation("Document indexed successfully: {DocumentId}", document.Id);
    }

    private static string BuildContext(List<SearchResult> searchResults)
    {
        var contextBuilder = new System.Text.StringBuilder();

        for (int i = 0; i < searchResults.Count; i++)
        {
            var result = searchResults[i];
            contextBuilder.AppendLine($"[Source {i + 1}] (Relevance: {result.SimilarityScore:F2})");

            if (result.Chunk.Metadata.TryGetValue("title", out var title))
            {
                contextBuilder.AppendLine($"Title: {title}");
            }

            contextBuilder.AppendLine($"Content: {result.Chunk.Content}");
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }

    private static string BuildPrompt(string question, string context)
    {
        return $"""
            Context Information:
            {context}
 
            Question: {question}
 
            Please answer the question based on the context provided above. If the context doesn't contain enough information to answer the question, say so clearly.
            """;
    }
}
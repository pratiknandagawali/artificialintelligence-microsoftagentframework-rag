using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using MicrosoftAgentFramework.Rag.Models;
using MicrosoftAgentFramework.Rag.VectorStore;

namespace MicrosoftAgentFramework.Rag.Services;

/// <summary>
/// Agentic RAG implementation using Microsoft Semantic Kernel Agents
/// Advanced pattern: Multiple specialized agents work together with planning and tool use
/// </summary>
public class AgenticRAGService : IRAGService
{
    private readonly IVectorStore _vectorStore;
    private readonly Kernel _kernel;
    private readonly ILogger<AgenticRAGService> _logger;
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly ChatCompletionAgent _researchAgent;
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly ChatCompletionAgent _analysisAgent;
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly ChatCompletionAgent _synthesisAgent;
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    public AgenticRAGService(
    IVectorStore vectorStore,
    Kernel kernel,
    ILogger<AgenticRAGService> logger)
    {
        _vectorStore = vectorStore;
        _kernel = kernel;
        _logger = logger;

        // Initialize specialized agents
        _researchAgent = CreateResearchAgent();
        _analysisAgent = CreateAnalysisAgent();
        _synthesisAgent = CreateSynthesisAgent();
    }

    public async Task<string> AskQuestionAsync(string question, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing question with Agentic RAG: {Question}", question);

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(question);

        // Phase 1: Research Agent - Query decomposition and retrieval
        _logger.LogInformation("Phase 1: Research Agent analyzing query");
        var researchResults = await PerformResearchAsync(question, cancellationToken);

        if (!researchResults.Any())
        {
            return "I don't have enough information to answer that question.";
        }

        // Phase 2: Analysis Agent - Deep analysis of retrieved content
        _logger.LogInformation("Phase 2: Analysis Agent processing retrieved information");
        var analysisResult = await PerformAnalysisAsync(question, researchResults, cancellationToken);

        // Phase 3: Synthesis Agent - Final answer generation
        _logger.LogInformation("Phase 3: Synthesis Agent generating final answer");
        var finalAnswer = await PerformSynthesisAsync(question, analysisResult, cancellationToken);

        return finalAnswer;
    }

    public async Task<string> AskQuestionWithContextAsync(
    string question,
    Dictionary<string, string> filters,
    CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing question with filters using Agentic RAG: {Question}", question);

        // Phase 1: Research with filters
        var researchResults = await PerformFilteredResearchAsync(question, filters, cancellationToken);

        if (!researchResults.Any())
        {
            return "I don't have enough information matching your criteria to answer that question.";
        }

        // Phase 2: Analysis
        var analysisResult = await PerformAnalysisAsync(question, researchResults, cancellationToken);

        // Phase 3: Synthesis
        var finalAnswer = await PerformSynthesisAsync(question, analysisResult, cancellationToken);

        return finalAnswer;
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

    private async Task<List<SearchResult>> PerformResearchAsync(string question, CancellationToken cancellationToken)
    {
        // Research agent breaks down the question and performs multiple searches
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(_researchAgent.Instructions);
        chatHistory.AddUserMessage($"Analyze this question and identify key search terms: {question}");

        await foreach (var message in _researchAgent.InvokeAsync(chatHistory, cancellationToken: cancellationToken))
        {
            _logger.LogDebug("Research Agent: {Content}", message.Content);
        }

        // Perform vector search with multiple strategies
        var primaryResults = await _vectorStore.SearchAsync(question, topK: 5, cancellationToken);

        // Expand search with related terms (simplified for demonstration)
        var expandedResults = await _vectorStore.SearchAsync(question, topK: 3, cancellationToken);

        // Combine and deduplicate results
        var allResults = primaryResults
 .Concat(expandedResults)
 .DistinctBy(r => r.Chunk.Id)
 .OrderByDescending(r => r.SimilarityScore)
 .Take(7)
 .ToList();

        return allResults;
    }

    private async Task<List<SearchResult>> PerformFilteredResearchAsync(
    string question,
    Dictionary<string, string> filters,
    CancellationToken cancellationToken)
    {
        return await _vectorStore.SearchWithFilterAsync(question, filters, topK: 7, cancellationToken);
    }

    private async Task<string> PerformAnalysisAsync(
    string question,
    List<SearchResult> searchResults,
    CancellationToken cancellationToken)
    {
        var context = BuildDetailedContext(searchResults);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(_analysisAgent.Instructions);
        chatHistory.AddUserMessage($"""
            Question: {question}
 
            Retrieved Information:
            {context}
 
            Analyze this information in relation to the question. Identify:
            1. Key facts and insights
            2. Relevance to the question
            3. Any gaps or inconsistencies
            4. Supporting evidence
            """);

        var analysisBuilder = new System.Text.StringBuilder();

        await foreach (var message in _analysisAgent.InvokeAsync(chatHistory, cancellationToken: cancellationToken))
        {
            analysisBuilder.Append(message.Content);
            _logger.LogDebug("Analysis Agent: {Content}", message.Content);
        }

        return analysisBuilder.ToString();
    }

    private async Task<string> PerformSynthesisAsync(
    string question,
    string analysis,
    CancellationToken cancellationToken)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(_synthesisAgent.Instructions);
        chatHistory.AddUserMessage($"""
            Original Question: {question}
 
            Analysis:
            {analysis}
 
            Generate a comprehensive, well-structured answer that:
            1. Directly addresses the question
            2. Incorporates insights from the analysis
            3. Is clear and concise
            4. Cites sources where appropriate
            """);

        var synthesisBuilder = new System.Text.StringBuilder();

        await foreach (var message in _synthesisAgent.InvokeAsync(chatHistory, cancellationToken: cancellationToken))
        {
            synthesisBuilder.Append(message.Content);
            _logger.LogDebug("Synthesis Agent: {Content}", message.Content);
        }

        return synthesisBuilder.ToString();
    }

    private ChatCompletionAgent CreateResearchAgent()
    {
        return new ChatCompletionAgent
        {
            Name = "ResearchAgent",
            Instructions = """
                You are a specialized research agent. Your role is to:
                1. Analyze user questions to understand information needs
                2. Identify key concepts and search terms
                3. Plan effective retrieval strategies
                4. Evaluate the quality and relevance of retrieved information
               
                Be thorough and systematic in your approach.
                """,
            Kernel = _kernel
        };
    }

    private ChatCompletionAgent CreateAnalysisAgent()
    {
        return new ChatCompletionAgent
        {
            Name = "AnalysisAgent",
            Instructions = """
                You are a specialized analysis agent. Your role is to:
                1. Examine retrieved information critically
                2. Identify key facts, patterns, and relationships
                3. Assess relevance to the question
                4. Detect contradictions or gaps in information
                5. Extract supporting evidence
               
                Provide detailed, structured analysis.
                """,
            Kernel = _kernel
        };
    }

    private ChatCompletionAgent CreateSynthesisAgent()
    {
        return new ChatCompletionAgent
        {
            Name = "SynthesisAgent",
            Instructions = """
                You are a specialized synthesis agent. Your role is to:
                1. Create coherent, comprehensive answers
                2. Integrate insights from analysis
                3. Present information clearly and concisely
                4. Maintain accuracy and cite sources
                5. Acknowledge limitations when information is incomplete
               
                Generate polished, professional responses.
                """,
            Kernel = _kernel
        };
    }

    private static string BuildDetailedContext(List<SearchResult> searchResults)
    {
        var contextBuilder = new System.Text.StringBuilder();

        for (int i = 0; i < searchResults.Count; i++)
        {
            var result = searchResults[i];
            contextBuilder.AppendLine($"--- Source {i + 1} ---");
            contextBuilder.AppendLine($"Relevance Score: {result.SimilarityScore:F4}");

            if (result.Chunk.Metadata.TryGetValue("title", out var title))
            {
                contextBuilder.AppendLine($"Title: {title}");
            }

            if (result.Chunk.Metadata.TryGetValue("createdAt", out var createdAt))
            {
                contextBuilder.AppendLine($"Created: {createdAt}");
            }

            contextBuilder.AppendLine($"Content:\n{result.Chunk.Content}");
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }
}
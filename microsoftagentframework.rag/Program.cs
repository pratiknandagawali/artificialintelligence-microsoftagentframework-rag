using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Embeddings;
using MicrosoftAgentFramework.Rag.Configuration;
using MicrosoftAgentFramework.Rag.Data;
using MicrosoftAgentFramework.Rag.Services;
using MicrosoftAgentFramework.Rag.VectorStore;

Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║     Microsoft Agent Framework - RAG & Agentic RAG Demo       ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Build configuration
var configuration = new ConfigurationBuilder()
 .SetBasePath(Directory.GetCurrentDirectory())
 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
 .AddEnvironmentVariables()
 .Build();

var azureOpenAIConfig = configuration.GetSection("AzureOpenAI").Get<AzureOpenAIConfig>();

if (azureOpenAIConfig == null ||
 string.IsNullOrEmpty(azureOpenAIConfig.ChatEndpoint) ||
 azureOpenAIConfig.ChatEndpoint == "YOUR_AZURE_OPENAI_ENDPOINT")
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: Please configure Azure OpenAI settings in appsettings.json");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Required settings:");
    Console.WriteLine("  For separate endpoints:");
    Console.WriteLine("    - AzureOpenAI:ChatEndpoint: Your chat endpoint URL");
    Console.WriteLine("    - AzureOpenAI:ChatApiKey: Your chat API key");
    Console.WriteLine("    - AzureOpenAI:ChatDeploymentName: Your chat deployment name (e.g., gpt-4)");
    Console.WriteLine("    - AzureOpenAI:EmbeddingEndpoint: Your embedding endpoint URL");
    Console.WriteLine("    - AzureOpenAI:EmbeddingApiKey: Your embedding API key");
    Console.WriteLine("    - AzureOpenAI:EmbeddingDeploymentName: Your embedding deployment name");
    Console.WriteLine();
    Console.WriteLine("  For same endpoint (legacy):");
    Console.WriteLine("    - AzureOpenAI:Endpoint: Your Azure OpenAI endpoint URL");
    Console.WriteLine("    - AzureOpenAI:ApiKey: Your Azure OpenAI API key");
    return;
}

// Setup Dependency Injection
var services = new ServiceCollection();

// Configure logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Create HttpClient with SSL configuration to handle certificate issues
var httpClientHandler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
    {
        // In production, you should properly validate certificates
        // For development/testing with self-signed certs or corporate proxies, this helps
        // Only use this if you're experiencing SSL issues
        return true;
    },
    // Enable automatic decompression
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
};

// Create separate HttpClients for chat and embedding if needed
var chatHttpClient = new HttpClient(httpClientHandler)
{
    Timeout = TimeSpan.FromMinutes(5)
};

var embeddingHttpClient = new HttpClient(new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
})
{
    Timeout = TimeSpan.FromMinutes(5)
};

// Build Semantic Kernel with separate endpoints and keys
var kernelBuilder = Kernel.CreateBuilder();

// Add Chat Completion with its own endpoint and key
kernelBuilder.AddAzureOpenAIChatCompletion(
 deploymentName: azureOpenAIConfig.ChatDeploymentName,
 endpoint: azureOpenAIConfig.ChatEndpoint,
 apiKey: azureOpenAIConfig.ChatApiKey,
 httpClient: chatHttpClient);

// Add Text Embedding with its own endpoint and key
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
 deploymentName: azureOpenAIConfig.EmbeddingDeploymentName,
 endpoint: azureOpenAIConfig.EmbeddingEndpoint,
 apiKey: azureOpenAIConfig.EmbeddingApiKey,
 httpClient: embeddingHttpClient);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var kernel = kernelBuilder.Build();

// Register services
services.AddSingleton(kernel);
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
services.AddSingleton(kernel.GetRequiredService<ITextEmbeddingGenerationService>());
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
services.AddSingleton(kernel.GetRequiredService<IChatCompletionService>());

// Register Vector Store using Factory Pattern
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var vectorStore = VectorStoreFactory.CreateVectorStore(configuration, embeddingService);
services.AddSingleton<IVectorStore>(vectorStore);

services.AddTransient<TraditionalRAGService>();
services.AddTransient<AgenticRAGService>();

var serviceProvider = services.BuildServiceProvider();

Console.WriteLine("✓ Configuration loaded successfully");
Console.WriteLine($"✓ Chat Endpoint: {azureOpenAIConfig.ChatEndpoint}");
Console.WriteLine($"✓ Chat Deployment: {azureOpenAIConfig.ChatDeploymentName}");
Console.WriteLine($"✓ Embedding Endpoint: {azureOpenAIConfig.EmbeddingEndpoint}");
Console.WriteLine($"✓ Embedding Deployment: {azureOpenAIConfig.EmbeddingDeploymentName}");
Console.WriteLine();

// Initialize RAG services
var traditionalRag = serviceProvider.GetRequiredService<TraditionalRAGService>();
var agenticRag = serviceProvider.GetRequiredService<AgenticRAGService>();

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine("Phase 1: Loading Sample Documents");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine();

await SampleDataLoader.LoadSampleDocumentsAsync(traditionalRag);
Console.WriteLine();

// Interactive demo
while (true)
{
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine("Select an option:");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine("1. Ask a question (Traditional RAG)");
    Console.WriteLine("2. Ask a question (Agentic RAG)");
    Console.WriteLine("3. Run comparison demo");
    Console.WriteLine("4. Add a new document");
    Console.WriteLine("5. Ask with filters");
    Console.WriteLine("6. Exit");
    Console.WriteLine();
    Console.Write("Enter your choice (1-6): ");

    var choice = Console.ReadLine();
    Console.WriteLine();

    try
    {
        switch (choice)
        {
            case "1":
                await AskTraditionalRAG(traditionalRag);
                break;
            case "2":
                await AskAgenticRAG(agenticRag);
                break;
            case "3":
                await RunComparison(traditionalRag, agenticRag);
                break;
            case "4":
                await AddDocument(traditionalRag);
                break;
            case "5":
                await AskWithFilters(traditionalRag);
                break;
            case "6":
                Console.WriteLine("Thank you for using the RAG demo!");
                return;
            default:
                Console.WriteLine("Invalid choice. Please try again.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine();
}

static async Task AskTraditionalRAG(IRAGService ragService)
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                    Traditional RAG                            ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.Write("Enter your question: ");
    var question = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
    {
        Console.WriteLine("Question cannot be empty.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Processing... (Retrieve → Generate)");
    Console.WriteLine();

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var answer = await ragService.AskQuestionAsync(question);
    stopwatch.Stop();

    Console.WriteLine("─────────────────────────────────────────────────────────────");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Answer:");
    Console.ResetColor();
    Console.WriteLine(answer);
    Console.WriteLine();
    Console.WriteLine($"⏱️  Time taken: {stopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine("─────────────────────────────────────────────────────────────");
}

static async Task AskAgenticRAG(IRAGService ragService)
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                      Agentic RAG                              ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.Write("Enter your question: ");
    var question = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
    {
        Console.WriteLine("Question cannot be empty.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Processing... (Research → Analyze → Synthesize)");
    Console.WriteLine();

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var answer = await ragService.AskQuestionAsync(question);
    stopwatch.Stop();

    Console.WriteLine("─────────────────────────────────────────────────────────────");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Answer:");
    Console.ResetColor();
    Console.WriteLine(answer);
    Console.WriteLine();
    Console.WriteLine($"⏱️  Time taken: {stopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine("─────────────────────────────────────────────────────────────");
}

static async Task RunComparison(IRAGService traditionalRag, IRAGService agenticRag)
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║              Traditional vs Agentic RAG Comparison            ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
    Console.WriteLine();

    var sampleQuestions = new[]
    {
 "What is RAG and how does it work?",
 "What are the differences between traditional RAG and agentic RAG?",
 "How do I implement a RAG system with Azure OpenAI?"
 };

    Console.WriteLine("Select a sample question or enter your own:");
    for (int i = 0; i < sampleQuestions.Length; i++)
    {
        Console.WriteLine($"{i + 1}. {sampleQuestions[i]}");
    }
    Console.WriteLine($"{sampleQuestions.Length + 1}. Enter custom question");
    Console.WriteLine();
    Console.Write("Enter your choice: ");

    var choiceStr = Console.ReadLine();
    string question;

    if (int.TryParse(choiceStr, out int choice) && choice > 0 && choice <= sampleQuestions.Length)
    {
        question = sampleQuestions[choice - 1];
    }
    else if (choice == sampleQuestions.Length + 1)
    {
        Console.Write("Enter your question: ");
        question = Console.ReadLine() ?? "";
    }
    else
    {
        Console.WriteLine("Invalid choice.");
        return;
    }

    if (string.IsNullOrWhiteSpace(question))
    {
        Console.WriteLine("Question cannot be empty.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine($"Question: {question}");
    Console.WriteLine();

    // Traditional RAG
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine("Traditional RAG Response:");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    var sw1 = System.Diagnostics.Stopwatch.StartNew();
    var answer1 = await traditionalRag.AskQuestionAsync(question);
    sw1.Stop();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(answer1);
    Console.ResetColor();
    Console.WriteLine($"\n⏱️  Time: {sw1.ElapsedMilliseconds}ms");
    Console.WriteLine();

    // Agentic RAG
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine("Agentic RAG Response:");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    var sw2 = System.Diagnostics.Stopwatch.StartNew();
    var answer2 = await agenticRag.AskQuestionAsync(question);
    sw2.Stop();
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine(answer2);
    Console.ResetColor();
    Console.WriteLine($"\n⏱️  Time: {sw2.ElapsedMilliseconds}ms");
    Console.WriteLine();
}

static async Task AddDocument(IRAGService ragService)
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                    Add New Document                           ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
    Console.WriteLine();

    Console.Write("Document Title: ");
    var title = Console.ReadLine();

    Console.WriteLine("Document Content (press Enter twice to finish):");
    var contentLines = new List<string>();
    string? line;
    int emptyLineCount = 0;

    while ((line = Console.ReadLine()) != null)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            emptyLineCount++;
            if (emptyLineCount >= 2)
                break;
        }
        else
        {
            emptyLineCount = 0;
        }
        contentLines.Add(line);
    }

    var content = string.Join(Environment.NewLine, contentLines);

    Console.Write("Category (optional): ");
    var category = Console.ReadLine();

    var metadata = new Dictionary<string, string>();
    if (!string.IsNullOrWhiteSpace(category))
    {
        metadata["category"] = category;
    }

    Console.WriteLine();
    Console.WriteLine("Indexing document...");

    await ragService.IndexDocumentAsync(title ?? "Untitled", content, metadata);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✓ Document added successfully!");
    Console.ResetColor();
}

static async Task AskWithFilters(IRAGService ragService)
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                   Ask with Filters                            ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine("Available filters: category, topic, level");
    Console.WriteLine("Example: category=AI, level=beginner");
    Console.WriteLine();

    Console.Write("Enter your question: ");
    var question = Console.ReadLine();

    Console.Write("Enter filters (key=value, comma-separated): ");
    var filterStr = Console.ReadLine();

    var filters = new Dictionary<string, string>();
    if (!string.IsNullOrWhiteSpace(filterStr))
    {
        foreach (var pair in filterStr.Split(','))
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
            {
                filters[parts[0].Trim()] = parts[1].Trim();
            }
        }
    }

    if (string.IsNullOrWhiteSpace(question))
    {
        Console.WriteLine("Question cannot be empty.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Processing with filters...");
    Console.WriteLine();

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var answer = await ragService.AskQuestionWithContextAsync(question, filters);
    stopwatch.Stop();

    Console.WriteLine("─────────────────────────────────────────────────────────────");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Answer:");
    Console.ResetColor();
    Console.WriteLine(answer);
    Console.WriteLine();
    Console.WriteLine($"⏱️  Time taken: {stopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine("─────────────────────────────────────────────────────────────");
}
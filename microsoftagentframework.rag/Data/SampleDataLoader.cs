using MicrosoftAgentFramework.Rag.Services;

namespace MicrosoftAgentFramework.Rag.Data;

/// <summary>
/// Loads sample documents into the RAG system for demonstration
/// </summary>
public static class SampleDataLoader
{
    private const string V = """
                Agentic RAG represents an evolution beyond traditional RAG systems. While traditional
                RAG follows a simple retrieve-then-generate pattern, agentic RAG employs multiple
                specialized AI agents that collaborate to process queries more intelligently.

                Key components of agentic RAG include:

                1. Research Agents: Break down complex queries, identify key information needs, and plan retrieval strategies. They may perform multiple searches with different                 strategies to gather comprehensive information.

                2. Analysis Agents: Examine retrieved content critically, identify patterns, assess
                   relevance, and detect contradictions or gaps. They provide structured analysis
                   of the information.

                3. Synthesis Agents: Combine insights from multiple sources to create coherent,
                   well-structured answers. They ensure the final response is accurate and properly
                   cited.

                4. Planning and Orchestration: A coordinator manages the workflow between agents,
                   determining when each agent should act and how their outputs feed into subsequent
                   steps.

                This multi-agent approach enables more sophisticated reasoning, better handling of
                complex queries, and higher quality responses compared to traditional RAG.
                """;

    public static async Task LoadSampleDocumentsAsync(IRAGService ragService)
    {
        var documents = GetSampleDocuments();

        foreach (var (title, content, metadata) in documents)
        {
            await ragService.IndexDocumentAsync(title, content, metadata);
        }

        Console.WriteLine($"Loaded {documents.Count} sample documents into the RAG system.");
    }

    private static List<(string Title, string Content, Dictionary<string, string> Metadata)> GetSampleDocuments()
    {
        return new List<(string, string, Dictionary<string, string>)>
 {
 (
 "Introduction to RAG",
 """
                Retrieval-Augmented Generation (RAG) is an AI framework that combines the strengths of
                large language models with external knowledge retrieval. The basic RAG pipeline consists
                of two main components: a retrieval system that finds relevant documents from a knowledge
                base, and a generation system that uses those documents to produce accurate, contextual
                responses.

                The retrieval component typically uses vector embeddings to find semantically similar
                content. Documents are chunked, embedded into high-dimensional vectors, and stored in a
                vector database. When a query comes in, it's also embedded and compared against stored
                vectors using similarity metrics like cosine similarity.

                The generation component takes the retrieved context and the original query to produce
                a response. This approach helps ground the language model's responses in factual
                information, reducing hallucinations and improving accuracy.
                """,
 new Dictionary<string, string> { { "category", "AI" }, { "topic", "RAG" }, { "level", "beginner" } }
 ),
 (
 "Agentic RAG Systems",
 V,
 new Dictionary<string, string> { { "category", "AI" }, { "topic", "Agentic RAG" }, { "level", "advanced" } }
 ),
 (
 "Vector Embeddings Explained",
 """
                Vector embeddings are numerical representations of text that capture semantic meaning
                in high-dimensional space. Modern embedding models transform words, sentences, or
                documents into dense vectors (typically 768 to 1536 dimensions) where semantically
                similar content has similar vector representations.

                Popular embedding models include:
                - OpenAI's text-embedding-ada-002 (1536 dimensions)
                - Sentence-BERT models (768 dimensions)
                - Azure OpenAI embedding models

                The quality of embeddings directly impacts RAG system performance. Better embeddings
                lead to more accurate retrieval, which in turn produces better generated responses.

                When implementing a RAG system, consider:
                1. Embedding model selection based on your domain
                2. Chunk size optimization (typically 500-1500 tokens)
                3. Overlap between chunks to maintain context
                4. Similarity threshold tuning for retrieval
                """,
 new Dictionary<string, string> { { "category", "AI" }, { "topic", "Embeddings" }, { "level", "intermediate" } }
 ),
 (
 "Microsoft Semantic Kernel",
 """
                Microsoft Semantic Kernel is an open-source SDK that enables developers to integrate
                AI capabilities into their applications. It provides a framework for orchestrating
                AI models, plugins, and agents in a cohesive way.

                Key features include:
                - Integration with multiple AI providers (Azure OpenAI, OpenAI, Hugging Face)
                - Plugin system for extending functionality
                - Agent framework for building autonomous AI systems
                - Memory and context management
                - Templating for prompts

                Semantic Kernel is particularly well-suited for building RAG systems because it
                provides:
                1. Built-in connectors for embedding generation
                2. Memory stores for vector storage
                3. Agent framework for agentic RAG implementations
                4. Prompt templating and management
                5. Planning capabilities for complex workflows

                The framework abstracts many low-level details while remaining flexible enough
                for advanced use cases.
                """,
 new Dictionary<string, string> { { "category", "Framework" }, { "topic", "Semantic Kernel" }, { "level", "intermediate" } }
 ),
 (
 "Azure OpenAI Service",
 """
                Azure OpenAI Service provides REST API access to OpenAI's powerful language models
                including GPT-4, GPT-3.5-Turbo, and embedding models. It combines OpenAI's cutting-edge
                models with Azure's enterprise-grade security, compliance, and regional availability.

                Key advantages for RAG systems:
                1. Enterprise Security: Data stays within Azure's secure infrastructure with
                   compliance certifications (SOC 2, ISO 27001, HIPAA, etc.)
                
                2. Embedding Models: Azure OpenAI provides text-embedding-ada-002 for generating
                   high-quality vector embeddings.
                
                3. Chat Completion Models: GPT-4 and GPT-3.5-Turbo for generating responses based
                   on retrieved context.
                
                4. Content Filtering: Built-in content moderation and safety features.
                
                5. Private Networking: Deploy in VNETs with private endpoints for maximum security.

                When building RAG systems with Azure OpenAI:
                - Use dedicated deployments for consistent performance
                - Implement retry logic for transient failures
                - Monitor token usage for cost optimization
                - Cache embeddings to reduce API calls
                - Use managed identity for secure authentication
                """,
 new Dictionary<string, string> { { "category", "Azure" }, { "topic", "Azure OpenAI" }, { "level", "intermediate" } }
 ),
 (
 "RAG Best Practices",
 """
                Building effective RAG systems requires careful attention to multiple aspects:

                1. Document Processing:
                   - Clean and normalize text before indexing
                   - Choose appropriate chunk sizes (balance between context and specificity)
                   - Implement chunk overlap to preserve context boundaries
                   - Add metadata for filtering and ranking

                2. Retrieval Optimization:
                   - Use hybrid search (combining vector and keyword search)
                   - Implement re-ranking for better relevance
                   - Set appropriate similarity thresholds
                   - Consider query expansion techniques

                3. Generation Quality:
                   - Craft effective system prompts
                   - Provide clear context to the model
                   - Implement citation mechanisms
                   - Handle cases with insufficient information gracefully

                4. Performance:
                   - Cache frequently accessed embeddings
                   - Use batch processing for bulk operations
                   - Implement async patterns for scalability
                   - Monitor and optimize latency

                5. Evaluation:
                   - Establish metrics (relevance, accuracy, latency)
                   - Create test sets with ground truth
                   - Conduct A/B testing for improvements
                   - Collect user feedback

                Regular monitoring and iteration based on real-world usage is essential for
                maintaining high-quality RAG systems.
                """,
 new Dictionary<string, string> { { "category", "Best Practices" }, { "topic", "RAG" }, { "level", "advanced" } }
 )
 };
    }
}
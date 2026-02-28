using Microsoft.Extensions.DependencyInjection;
using MicrosoftAgentFramework.Rag.Core.Application.Common.Builders;
using MicrosoftAgentFramework.Rag.Core.Application.Common.Processors;

namespace MicrosoftAgentFramework.Rag.Composition.DependencyInjection;

/// <summary>
/// Extension methods for registering core application services
/// Follows Open/Closed Principle - extend functionality without modifying
/// </summary>
public static class CoreServicesRegistration
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Register builders
        services.AddTransient<IPromptBuilder, RAGPromptBuilder>();
        services.AddTransient<IContextBuilder, ContextBuilder>();

        // Register processors
        services.AddSingleton<IDocumentChunker, DocumentChunker>();

        return services;
    }
}
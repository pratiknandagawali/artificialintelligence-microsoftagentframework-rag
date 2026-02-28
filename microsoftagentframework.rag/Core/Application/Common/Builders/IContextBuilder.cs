using MicrosoftAgentFramework.Rag.Models;

namespace MicrosoftAgentFramework.Rag.Core.Application.Common.Builders;

/// <summary>
/// Builder interface for constructing context from search results
/// </summary>
public interface IContextBuilder
{
    IContextBuilder AddSearchResults(IEnumerable<SearchResult> results);
    IContextBuilder WithFormatting(ContextFormatting formatting);
    IContextBuilder WithMaxLength(int maxLength);
    string Build();
}

/// <summary>
/// Context formatting options
/// </summary>
public enum ContextFormatting
{
    Simple,
    WithSources,
    WithRelevanceScores,
    WithMetadata
}
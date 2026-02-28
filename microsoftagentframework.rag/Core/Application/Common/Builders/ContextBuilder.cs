using System.Text;
using MicrosoftAgentFramework.Rag.Models;

namespace MicrosoftAgentFramework.Rag.Core.Application.Common.Builders;

/// <summary>
/// Concrete implementation of IContextBuilder
/// </summary>
public class ContextBuilder : IContextBuilder
{
    private List<SearchResult> _results = new();
    private ContextFormatting _formatting = ContextFormatting.WithSources;
    private int _maxLength = int.MaxValue;

    public IContextBuilder AddSearchResults(IEnumerable<SearchResult> results)
    {
        _results.AddRange(results);
        return this;
    }

    public IContextBuilder WithFormatting(ContextFormatting formatting)
    {
        _formatting = formatting;
        return this;
    }

    public IContextBuilder WithMaxLength(int maxLength)
    {
        _maxLength = maxLength;
        return this;
    }

    public string Build()
    {
        if (_results.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();

        for (int i = 0; i < _results.Count; i++)
        {
            var result = _results[i];

            switch (_formatting)
            {
                case ContextFormatting.Simple:
                    builder.AppendLine(result.Chunk.Content);
                    break;

                case ContextFormatting.WithSources:
                    builder.AppendLine($"[Source {i + 1}]");
                    if (result.Chunk.Metadata.TryGetValue("title", out var title))
                    {
                        builder.AppendLine($"Title: {title}");
                    }
                    builder.AppendLine($"Content: {result.Chunk.Content}");
                    break;

                case ContextFormatting.WithRelevanceScores:
                    builder.AppendLine($"[Source {i + 1}] (Relevance: {result.SimilarityScore:F2})");
                    builder.AppendLine(result.Chunk.Content);
                    break;

                case ContextFormatting.WithMetadata:
                    builder.AppendLine($"[Source {i + 1}]");
                    foreach (var meta in result.Chunk.Metadata)
                    {
                        builder.AppendLine($"  {meta.Key}: {meta.Value}");
                    }
                    builder.AppendLine($"Content: {result.Chunk.Content}");
                    break;
            }

            builder.AppendLine();

            // Check max length
            if (builder.Length > _maxLength)
            {
                return builder.ToString(0, Math.Min(_maxLength, builder.Length));
            }
        }

        return builder.ToString();
    }
}
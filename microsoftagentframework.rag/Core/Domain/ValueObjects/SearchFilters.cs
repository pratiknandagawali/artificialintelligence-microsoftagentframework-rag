namespace MicrosoftAgentFramework.Rag.Core.Domain.ValueObjects;

/// <summary>
/// Value object representing search filters
/// Replaces Dictionary&lt;string, string&gt; for filter parameters
/// </summary>
public record SearchFilters
{
    public IReadOnlyList<FilterCriterion> Criteria { get; init; } = Array.Empty<FilterCriterion>();

    public SearchFilters()
    {
    }

    public SearchFilters(params FilterCriterion[] criteria)
    {
        Criteria = criteria.ToList().AsReadOnly();
    }

    public static SearchFilters FromDictionary(Dictionary<string, string>? dict)
    {
        if (dict == null || dict.Count == 0)
            return new SearchFilters();

        var criteria = dict.Select(kvp => new FilterCriterion(kvp.Key, kvp.Value, FilterOperator.Equals)).ToArray();
        return new SearchFilters(criteria);
    }

    public Dictionary<string, string> ToDictionary()
    {
        return Criteria.ToDictionary(c => c.FieldName, c => c.Value);
    }

    public bool Matches(Dictionary<string, string> metadata)
    {
        return Criteria.All(criterion => criterion.Matches(metadata));
    }
}
namespace MicrosoftAgentFramework.Rag.Core.Domain.ValueObjects;

/// <summary>
/// Value object representing document metadata
/// Replaces Dictionary&lt;string, string&gt; primitive obsession
/// </summary>
public record DocumentMetadata
{
    public string? Category { get; init; }
    public string? Topic { get; init; }
    public string? Level { get; init; }
    public string? Author { get; init; }
    public DateTime? CreatedDate { get; init; }
    public Dictionary<string, string> CustomFields { get; init; } = new();

    public static DocumentMetadata FromDictionary(Dictionary<string, string>? dict)
    {
        if (dict == null || dict.Count == 0)
            return new DocumentMetadata();

        return new DocumentMetadata
        {
            Category = dict.TryGetValue("category", out var cat) ? cat : null,
            Topic = dict.TryGetValue("topic", out var topic) ? topic : null,
            Level = dict.TryGetValue("level", out var level) ? level : null,
            Author = dict.TryGetValue("author", out var author) ? author : null,
            CreatedDate = dict.TryGetValue("created", out var created) && DateTime.TryParse(created, out var date) ? date : null,
            CustomFields = dict.Where(kvp => !IsWellKnownField(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }

    public Dictionary<string, string> ToDictionary()
    {
        var dict = new Dictionary<string, string>(CustomFields);

        if (!string.IsNullOrEmpty(Category))
            dict["category"] = Category;
        if (!string.IsNullOrEmpty(Topic))
            dict["topic"] = Topic;
        if (!string.IsNullOrEmpty(Level))
            dict["level"] = Level;
        if (!string.IsNullOrEmpty(Author))
            dict["author"] = Author;
        if (CreatedDate.HasValue)
            dict["created"] = CreatedDate.Value.ToString("O");

        return dict;
    }

    private static bool IsWellKnownField(string key) =>
    key.Equals("category", StringComparison.OrdinalIgnoreCase) ||
    key.Equals("topic", StringComparison.OrdinalIgnoreCase) ||
    key.Equals("level", StringComparison.OrdinalIgnoreCase) ||
    key.Equals("author", StringComparison.OrdinalIgnoreCase) ||
    key.Equals("created", StringComparison.OrdinalIgnoreCase);
}
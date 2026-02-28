/// <summary>
/// Represents a single filter criterion
/// </summary>
public record FilterCriterion(string FieldName, string Value, FilterOperator Operator = FilterOperator.Equals)
{
    public bool Matches(Dictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue(FieldName, out var actualValue))
            return false;

        return Operator switch
        {
            FilterOperator.Equals => string.Equals(actualValue, Value, StringComparison.OrdinalIgnoreCase),
            FilterOperator.Contains => actualValue.Contains(Value, StringComparison.OrdinalIgnoreCase),
            FilterOperator.StartsWith => actualValue.StartsWith(Value, StringComparison.OrdinalIgnoreCase),
            FilterOperator.EndsWith => actualValue.EndsWith(Value, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}

/// <summary>
/// Filter comparison operators
/// </summary>
public enum FilterOperator
{
    Equals,
    Contains,
    StartsWith,
    EndsWith
}
using System.Collections.Generic;

namespace Causality.Shared.Features.Querying.Domain;

/// <summary>
/// Supported filter operators in Abstract Queries
/// </summary>
public static class FilterOperators
{
    // Comparison operators
    public const string Equal = "eq";
    public const string NotEquals = "neq";
    public const string LessThan = "lt";
    public const string LessThanOrEqual = "lte";
    public const string GreaterThan = "gt";
    public const string GreaterThanOrEqual = "gte";
    public const string In = "in";

    // String operators  
    public const string Contains = "contains";
    public const string StartsWith = "startsWith";
    public const string EndsWith = "endsWith";
    public const string EqualsIgnoreCase = "equalsIgnoreCase";

    // Null handling
    public const string IsNull = "isNull";
    public const string IsNotNull = "isNotNull";

    // Date operators (optional - for future implementation)
    public const string DateEquals = "dateEq";
    public const string DateBetween = "dateBetween";

    // Collection operators (optional - for future implementation)  
    public const string Any = "any";
    public const string All = "all";

    // Logical operators
    public const string And = "and";
    public const string Or = "or";

    /// <summary>
    /// Get all supported operators
    /// </summary>
    public static readonly HashSet<string> SupportedOperators = new()
    {
        Equal, NotEquals, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual, In,
        Contains, StartsWith, EndsWith, EqualsIgnoreCase,
        IsNull, IsNotNull,
        DateEquals, DateBetween,
        Any, All,
        And, Or
    };

    /// <summary>
    /// Operators that require a value
    /// </summary>
    public static readonly HashSet<string> ValueRequiredOperators = new()
    {
        Equal, NotEquals, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual, In,
        Contains, StartsWith, EndsWith, EqualsIgnoreCase,
        DateEquals, DateBetween
    };

    /// <summary>
    /// Operators that don't require a value
    /// </summary>
    public static readonly HashSet<string> ValueNotRequiredOperators = new()
    {
        IsNull, IsNotNull
    };

    /// <summary>
    /// String-specific operators
    /// </summary>
    public static readonly HashSet<string> StringOperators = new()
    {
        Contains, StartsWith, EndsWith, EqualsIgnoreCase
    };

    /// <summary>
    /// Date-specific operators
    /// </summary>
    public static readonly HashSet<string> DateOperators = new()
    {
        DateEquals, DateBetween
    };

    /// <summary>
    /// Collection operators
    /// </summary>
    public static readonly HashSet<string> CollectionOperators = new()
    {
        Any, All
    };
}
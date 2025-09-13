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

    // Collection operators
    public const string Any = "any";
    public const string All = "all";

    // Logical operators
    public const string And = "and";
    public const string Or = "or";

    // Projection operators
    public const string SelectMany = "selectMany";
    public const string Select = "select";
    public const string Distinct = "distinct";

    // Grouping operators  
    public const string GroupBy = "groupBy";

    // Ordering operators
    public const string OrderBy = "orderBy";
    public const string OrderByDescending = "orderByDescending";
    public const string ThenBy = "thenBy";  
    public const string ThenByDescending = "thenByDescending";
    public const string Reverse = "reverse";

    // Pagination operators
    public const string Skip = "skip";
    public const string Take = "take";

    // Aggregation operators
    public const string Count = "count";
    public const string Sum = "sum";
    public const string Average = "average";
    public const string Min = "min";
    public const string Max = "max";

    // Element operators
    public const string First = "first";
    public const string FirstOrDefault = "firstOrDefault";
    public const string Single = "single";
    public const string SingleOrDefault = "singleOrDefault";
    public const string Last = "last";
    public const string LastOrDefault = "lastOrDefault";

    // Set operators
    public const string Union = "union";
    public const string Intersect = "intersect";
    public const string Except = "except";
    
    // Join operators
    public const string Join = "join";
    public const string GroupJoin = "groupJoin";

    // Sequence operators
    public const string Concat = "concat";
    public const string Zip = "zip";

    /// <summary>
    /// Get all supported operators
    /// </summary>
    public static readonly HashSet<string> SupportedOperators = new()
    {
        // Comparison operators
        Equal, NotEquals, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual, In,
        
        // String operators
        Contains, StartsWith, EndsWith, EqualsIgnoreCase,
        
        // Null handling
        IsNull, IsNotNull,
        
        // Date operators
        DateEquals, DateBetween,
        
        // Collection operators
        Any, All,
        
        // Logical operators
        And, Or,
        
        // Projection operators
        SelectMany, Select, Distinct,
        
        // Grouping operators
        GroupBy,
        
        // Ordering operators
        OrderBy, OrderByDescending, ThenBy, ThenByDescending, Reverse,
        
        // Pagination operators
        Skip, Take,
        
        // Aggregation operators
        Count, Sum, Average, Min, Max,
        
        // Element operators
        First, FirstOrDefault, Single, SingleOrDefault, Last, LastOrDefault,
        
        // Set operators
        Union, Intersect, Except,
        
        // Join operators
        Join, GroupJoin,
        
        // Sequence operators
        Concat, Zip
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

    /// <summary>
    /// Projection operators  
    /// </summary>
    public static readonly HashSet<string> ProjectionOperators = new()
    {
        SelectMany, Select, Distinct
    };

    /// <summary>
    /// Grouping operators
    /// </summary>
    public static readonly HashSet<string> GroupingOperators = new()
    {
        GroupBy
    };

    /// <summary>
    /// Ordering operators
    /// </summary>
    public static readonly HashSet<string> OrderingOperators = new()
    {
        OrderBy, OrderByDescending, ThenBy, ThenByDescending, Reverse
    };

    /// <summary>
    /// Pagination operators
    /// </summary>
    public static readonly HashSet<string> PaginationOperators = new()
    {
        Skip, Take
    };

    /// <summary>
    /// Aggregation operators
    /// </summary>
    public static readonly HashSet<string> AggregationOperators = new()
    {
        Count, Sum, Average, Min, Max
    };

    /// <summary>
    /// Element selection operators
    /// </summary>
    public static readonly HashSet<string> ElementOperators = new()
    {
        First, FirstOrDefault, Single, SingleOrDefault, Last, LastOrDefault
    };

    /// <summary>
    /// Set operations
    /// </summary>
    public static readonly HashSet<string> SetOperators = new()
    {
        Union, Intersect, Except
    };

    /// <summary>
    /// Join operators
    /// </summary>
    public static readonly HashSet<string> JoinOperators = new()
    {
        Join, GroupJoin
    };

    /// <summary>
    /// Sequence operators
    /// </summary>
    public static readonly HashSet<string> SequenceOperators = new()
    {
        Concat, Zip
    };
}
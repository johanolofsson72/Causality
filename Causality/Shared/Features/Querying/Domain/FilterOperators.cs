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
    public const string UnionBy = "unionBy";
    public const string IntersectBy = "intersectBy";
    public const string ExceptBy = "exceptBy";
    public const string DistinctBy = "distinctBy";
    
    // Join operators
    public const string Join = "join";
    public const string GroupJoin = "groupJoin";
    public const string DefaultIfEmpty = "defaultIfEmpty";

    // Sequence operators
    public const string Concat = "concat";
    public const string Zip = "zip";
    public const string SequenceEqual = "sequenceEqual";

    // Filtering operators
    public const string OfType = "ofType";
    
    // Partitioning operators  
    public const string SkipWhile = "skipWhile";
    public const string TakeWhile = "takeWhile";
    public const string SkipLast = "skipLast";
    public const string TakeLast = "takeLast";
    
    // Element operators (additional)
    public const string ElementAt = "elementAt";
    public const string ElementAtOrDefault = "elementAtOrDefault";
    
    // Aggregation operators (additional)
    public const string LongCount = "longCount";
    public const string Aggregate = "aggregate";
    
    // Quantifier operators (additional)
    public const string ContainsElement = "containsElement";
    
    // Grouping operators (additional)
    public const string ToLookup = "toLookup";
    
    // Generation operators
    public const string Range = "range";
    public const string Repeat = "repeat";
    public const string Empty = "empty";
    
    // Projection operators (additional)
    public const string Chunk = "chunk";
    public const string SelectWithIndex = "selectWithIndex";

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
        Union, Intersect, Except, UnionBy, IntersectBy, ExceptBy, DistinctBy,
        
        // Join operators
        Join, GroupJoin, DefaultIfEmpty,
        
        // Sequence operators
        Concat, Zip, SequenceEqual,
        
        // Filtering operators
        OfType,
        
        // Partitioning operators
        SkipWhile, TakeWhile, SkipLast, TakeLast,
        
        // Element operators (additional)
        ElementAt, ElementAtOrDefault,
        
        // Aggregation operators (additional)
        LongCount, Aggregate,
        
        // Quantifier operators (additional)
        ContainsElement,
        
        // Grouping operators (additional)
        ToLookup,
        
        // Generation operators
        Range, Repeat, Empty,
        
        // Projection operators (additional)  
        Chunk, SelectWithIndex
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
        Union, Intersect, Except, UnionBy, IntersectBy, ExceptBy, DistinctBy
    };

    /// <summary>
    /// Join operators
    /// </summary>
    public static readonly HashSet<string> JoinOperators = new()
    {
        Join, GroupJoin, DefaultIfEmpty
    };

    /// <summary>
    /// Sequence operators
    /// </summary>
    public static readonly HashSet<string> SequenceOperators = new()
    {
        Concat, Zip, SequenceEqual
    };

    /// <summary>
    /// Filtering operators
    /// </summary>
    public static readonly HashSet<string> FilteringOperators = new()
    {
        OfType
    };

    /// <summary>
    /// Partitioning operators (additional)
    /// </summary>
    public static readonly HashSet<string> PartitioningOperators = new()
    {
        Skip, Take, SkipWhile, TakeWhile, SkipLast, TakeLast
    };

    /// <summary>
    /// Element operators (extended)
    /// </summary>
    public static readonly HashSet<string> ExtendedElementOperators = new()
    {
        First, FirstOrDefault, Single, SingleOrDefault, Last, LastOrDefault,
        ElementAt, ElementAtOrDefault
    };

    /// <summary>
    /// Aggregation operators (extended)
    /// </summary>
    public static readonly HashSet<string> ExtendedAggregationOperators = new()
    {
        Count, LongCount, Sum, Average, Min, Max, Aggregate
    };

    /// <summary>
    /// Quantifier operators (extended)
    /// </summary>
    public static readonly HashSet<string> QuantifierOperators = new()
    {
        Any, All, ContainsElement, SequenceEqual
    };

    /// <summary>
    /// Grouping operators (extended)
    /// </summary>
    public static readonly HashSet<string> ExtendedGroupingOperators = new()
    {
        GroupBy, ToLookup
    };

    /// <summary>
    /// Generation operators
    /// </summary>
    public static readonly HashSet<string> GenerationOperators = new()
    {
        Range, Repeat, Empty
    };

    /// <summary>
    /// Projection operators (extended)
    /// </summary>
    public static readonly HashSet<string> ExtendedProjectionOperators = new()
    {
        SelectMany, Select, SelectWithIndex, Distinct, DistinctBy, Chunk
    };
}
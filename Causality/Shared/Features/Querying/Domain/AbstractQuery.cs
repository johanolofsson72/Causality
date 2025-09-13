using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Causality.Shared.Features.Querying.Domain;

/// <summary>
/// Abstract Query (AQ) - Language-agnostic query representation for client-to-server communication
/// </summary>
public class AbstractQuery
{
    /// <summary>
    /// Target entity name to query
    /// </summary>
    [JsonPropertyName("entity")]
    public string Entity { get; set; } = string.Empty;

    /// <summary>
    /// Collection of filter conditions
    /// </summary>
    [JsonPropertyName("filters")]
    public List<FilterCondition> Filters { get; set; } = new();

    /// <summary>
    /// Sorting specifications
    /// </summary>
    [JsonPropertyName("sort")]
    public List<SortSpecification> Sort { get; set; } = new();

    /// <summary>
    /// Fields to select for projection
    /// </summary>
    [JsonPropertyName("select")]
    public List<string> Select { get; set; } = new();

    /// <summary>
    /// Pagination configuration
    /// </summary>
    [JsonPropertyName("page")]
    public PageRequest? Page { get; set; }

    /// <summary>
    /// Query execution hints
    /// </summary>
    [JsonPropertyName("hints")]
    public QueryHints? Hints { get; set; }
}

/// <summary>
/// Represents a single filter condition
/// </summary>
public class FilterCondition
{
    /// <summary>
    /// Field name to filter on
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Filter operator (eq, neq, lt, lte, gt, gte, in, contains, startsWith, endsWith, etc.)
    /// </summary>
    [JsonPropertyName("op")]
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// Value to compare against
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// For complex conditions - nested conditions with logical operators
    /// </summary>
    [JsonPropertyName("conditions")]
    public List<FilterCondition>? Conditions { get; set; }

    /// <summary>
    /// Logical operator for combining conditions (and, or)
    /// </summary>
    [JsonPropertyName("logic")]
    public string? Logic { get; set; }
}

/// <summary>
/// Sort specification for ordering results
/// </summary>
public class SortSpecification
{
    /// <summary>
    /// Field name to sort by
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    [JsonPropertyName("dir")]
    public string Direction { get; set; } = "asc";
}

/// <summary>
/// Pagination request parameters
/// </summary>
public class PageRequest
{
    /// <summary>
    /// Number of items per page (max 200)
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; } = 50;

    /// <summary>
    /// Opaque cursor for pagination
    /// </summary>
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}

/// <summary>
/// Query execution hints
/// </summary>
public class QueryHints
{
    /// <summary>
    /// Whether to include total count in results
    /// </summary>
    [JsonPropertyName("includeCount")]
    public bool IncludeCount { get; set; } = false;

    /// <summary>
    /// Cache TTL override in seconds
    /// </summary>
    [JsonPropertyName("cacheTtlSeconds")]
    public int? CacheTtlSeconds { get; set; }
}
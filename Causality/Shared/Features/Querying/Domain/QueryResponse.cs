using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Causality.Shared.Features.Querying.Domain;

/// <summary>
/// Query execution response
/// </summary>
/// <typeparam name="T">DTO type being returned</typeparam>
public class QueryResponse<T>
{
    /// <summary>
    /// Query result items
    /// </summary>
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Pagination metadata
    /// </summary>
    [JsonPropertyName("page")]
    public PageMetadata Page { get; set; } = new();

    /// <summary>
    /// Query execution metadata
    /// </summary>
    [JsonPropertyName("meta")]
    public QueryMetadata Meta { get; set; } = new();
}

/// <summary>
/// Pagination metadata in response
/// </summary>
public class PageMetadata
{
    /// <summary>
    /// Cursor for next page (if any)
    /// </summary>
    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    /// <summary>
    /// Page size used
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// Total count (if requested)
    /// </summary>
    [JsonPropertyName("totalCount")]
    public long? TotalCount { get; set; }
}

/// <summary>
/// Query execution metadata
/// </summary>
public class QueryMetadata
{
    /// <summary>
    /// Execution time in milliseconds
    /// </summary>
    [JsonPropertyName("elapsedMs")]
    public long ElapsedMs { get; set; }

    /// <summary>
    /// Whether result was served from cache
    /// </summary>
    [JsonPropertyName("fromCache")]
    public bool FromCache { get; set; }

    /// <summary>
    /// Query hash for caching/auditing
    /// </summary>
    [JsonPropertyName("queryHash")]
    public string QueryHash { get; set; } = string.Empty;

    /// <summary>
    /// Number of database rows examined
    /// </summary>
    [JsonPropertyName("rowsExamined")]
    public long? RowsExamined { get; set; }
}
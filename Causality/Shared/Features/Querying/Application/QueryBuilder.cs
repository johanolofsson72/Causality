using System;
using Causality.Shared.Features.Querying.Domain;

namespace Causality.Shared.Features.Querying.Application;

/// <summary>
/// Fluent builder for constructing Abstract Queries
/// </summary>
public class QueryBuilder
{
    private readonly AbstractQuery _query = new();

    /// <summary>
    /// Set the target entity to query
    /// </summary>
    public QueryBuilder Entity(string entityName)
    {
        _query.Entity = entityName ?? throw new ArgumentNullException(nameof(entityName));
        return this;
    }

    /// <summary>
    /// Add an equality filter
    /// </summary>
    public QueryBuilder Where(string field, object? value)
    {
        return AddFilter(field, FilterOperators.Equal, value);
    }

    /// <summary>
    /// Add a filter with specific operator
    /// </summary>
    public QueryBuilder Where(string field, string op, object? value)
    {
        return AddFilter(field, op, value);
    }

    /// <summary>
    /// Add equality filter
    /// </summary>
    public QueryBuilder Equals(string field, object? value)
    {
        return AddFilter(field, FilterOperators.Equal, value);
    }

    /// <summary>
    /// Add not equals filter
    /// </summary>
    public QueryBuilder NotEquals(string field, object? value)
    {
        return AddFilter(field, FilterOperators.NotEquals, value);
    }

    /// <summary>
    /// Add less than filter
    /// </summary>
    public QueryBuilder LessThan(string field, object value)
    {
        return AddFilter(field, FilterOperators.LessThan, value);
    }

    /// <summary>
    /// Add less than or equal filter
    /// </summary>
    public QueryBuilder LessThanOrEqual(string field, object value)
    {
        return AddFilter(field, FilterOperators.LessThanOrEqual, value);
    }

    /// <summary>
    /// Add greater than filter
    /// </summary>
    public QueryBuilder GreaterThan(string field, object value)
    {
        return AddFilter(field, FilterOperators.GreaterThan, value);
    }

    /// <summary>
    /// Add greater than or equal filter
    /// </summary>
    public QueryBuilder GreaterThanOrEqual(string field, object value)
    {
        return AddFilter(field, FilterOperators.GreaterThanOrEqual, value);
    }

    /// <summary>
    /// Add contains filter for string fields
    /// </summary>
    public QueryBuilder Contains(string field, string value)
    {
        return AddFilter(field, FilterOperators.Contains, value);
    }

    /// <summary>
    /// Add starts with filter for string fields
    /// </summary>
    public QueryBuilder StartsWith(string field, string value)
    {
        return AddFilter(field, FilterOperators.StartsWith, value);
    }

    /// <summary>
    /// Add ends with filter for string fields
    /// </summary>
    public QueryBuilder EndsWith(string field, string value)
    {
        return AddFilter(field, FilterOperators.EndsWith, value);
    }

    /// <summary>
    /// Add case-insensitive equals filter
    /// </summary>
    public QueryBuilder EqualsIgnoreCase(string field, string value)
    {
        return AddFilter(field, FilterOperators.EqualsIgnoreCase, value);
    }

    /// <summary>
    /// Add in filter for multiple values
    /// </summary>
    public QueryBuilder In(string field, params object[] values)
    {
        return AddFilter(field, FilterOperators.In, values);
    }

    /// <summary>
    /// Add is null filter
    /// </summary>
    public QueryBuilder IsNull(string field)
    {
        return AddFilter(field, FilterOperators.IsNull, null);
    }

    /// <summary>
    /// Add is not null filter
    /// </summary>
    public QueryBuilder IsNotNull(string field)
    {
        return AddFilter(field, FilterOperators.IsNotNull, null);
    }

    /// <summary>
    /// Add sorting by field (ascending)
    /// </summary>
    public QueryBuilder OrderBy(string field)
    {
        _query.Sort.Add(new SortSpecification { Field = field, Direction = "asc" });
        return this;
    }

    /// <summary>
    /// Add sorting by field (descending)
    /// </summary>
    public QueryBuilder OrderByDescending(string field)
    {
        _query.Sort.Add(new SortSpecification { Field = field, Direction = "desc" });
        return this;
    }

    /// <summary>
    /// Add secondary sort (ascending) - for ThenBy scenarios
    /// </summary>
    public QueryBuilder ThenBy(string field)
    {
        return OrderBy(field);
    }

    /// <summary>
    /// Add secondary sort (descending) - for ThenBy scenarios
    /// </summary>
    public QueryBuilder ThenByDescending(string field)
    {
        return OrderByDescending(field);
    }

    /// <summary>
    /// Select specific fields for projection
    /// </summary>
    public QueryBuilder Select(params string[] fields)
    {
        _query.Select.Clear();
        _query.Select.AddRange(fields);
        return this;
    }

    /// <summary>
    /// Add a field to the selection
    /// </summary>
    public QueryBuilder AddSelect(string field)
    {
        if (!_query.Select.Contains(field))
        {
            _query.Select.Add(field);
        }
        return this;
    }

    /// <summary>
    /// Set page size
    /// </summary>
    public QueryBuilder PageSize(int size)
    {
        _query.Page ??= new PageRequest();
        _query.Page.Size = Math.Min(size, 200); // Enforce max page size
        return this;
    }

    /// <summary>
    /// Set cursor for pagination
    /// </summary>
    public QueryBuilder Cursor(string? cursor)
    {
        _query.Page ??= new PageRequest();
        _query.Page.Cursor = cursor;
        return this;
    }

    /// <summary>
    /// Include total count in response
    /// </summary>
    public QueryBuilder IncludeCount(bool include = true)
    {
        _query.Hints ??= new QueryHints();
        _query.Hints.IncludeCount = include;
        return this;
    }

    /// <summary>
    /// Set cache TTL override
    /// </summary>
    public QueryBuilder CacheTtl(int seconds)
    {
        _query.Hints ??= new QueryHints();
        _query.Hints.CacheTtlSeconds = seconds;
        return this;
    }

    /// <summary>
    /// Build the final AbstractQuery
    /// </summary>
    public AbstractQuery Build()
    {
        if (string.IsNullOrEmpty(_query.Entity))
        {
            throw new InvalidOperationException("Entity must be specified");
        }

        return _query;
    }

    /// <summary>
    /// Create a new QueryBuilder for the specified entity
    /// </summary>
    public static QueryBuilder For(string entityName)
    {
        return new QueryBuilder().Entity(entityName);
    }

    private QueryBuilder AddFilter(string field, string op, object? value)
    {
        if (string.IsNullOrEmpty(field))
            throw new ArgumentException("Field name cannot be null or empty", nameof(field));

        if (!FilterOperators.SupportedOperators.Contains(op))
            throw new ArgumentException($"Unsupported operator: {op}", nameof(op));

        _query.Filters.Add(new FilterCondition
        {
            Field = field,
            Operator = op,
            Value = value
        });

        return this;
    }
}
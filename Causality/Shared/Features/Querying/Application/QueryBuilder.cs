using System;
using System.Linq;
using System.Collections.Generic;
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

    // =============== Advanced LINQ Operators ===============

    /// <summary>
    /// Add SelectMany operation for flattening collections
    /// </summary>
    public QueryBuilder SelectMany(string collectionField, string? alias = null)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.SelectMany,
            Parameters = new Dictionary<string, object?>
            {
                ["field"] = collectionField,
                ["alias"] = alias
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Distinct operation to remove duplicates
    /// </summary>
    public QueryBuilder Distinct(params string[] fields)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Distinct,
            Parameters = new Dictionary<string, object?>
            {
                ["fields"] = fields.Length > 0 ? fields : null
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Skip operation for pagination
    /// </summary>
    public QueryBuilder Skip(int count)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Skip,
            Parameters = new Dictionary<string, object?>
            {
                ["count"] = count
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Take operation for limiting results
    /// </summary>
    public QueryBuilder Take(int count)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Take,
            Parameters = new Dictionary<string, object?>
            {
                ["count"] = Math.Min(count, 200) // Enforce max limit
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add GroupBy operation for analytics
    /// </summary>
    public QueryBuilder GroupBy(params string[] fields)
    {
        if (fields == null || fields.Length == 0)
            throw new ArgumentException("At least one field must be specified for GroupBy", nameof(fields));

        _query.GroupBy = new GroupBySpecification
        {
            Fields = fields.ToList()
        };
        return this;
    }

    /// <summary>
    /// Add Having condition (filters applied after GroupBy)
    /// </summary>
    public QueryBuilder Having(string field, string op, object? value)
    {
        _query.GroupBy ??= new GroupBySpecification();
        
        _query.GroupBy.Having.Add(new FilterCondition
        {
            Field = field,
            Operator = op,
            Value = value
        });
        return this;
    }

    /// <summary>
    /// Add Count aggregation
    /// </summary>
    public QueryBuilder Count(string? alias = null)
    {
        return AddAggregation(FilterOperators.Count, null, alias);
    }

    /// <summary>
    /// Add Sum aggregation
    /// </summary>
    public QueryBuilder Sum(string field, string? alias = null)
    {
        return AddAggregation(FilterOperators.Sum, field, alias);
    }

    /// <summary>
    /// Add Average aggregation
    /// </summary>
    public QueryBuilder Average(string field, string? alias = null)
    {
        return AddAggregation(FilterOperators.Average, field, alias);
    }

    /// <summary>
    /// Add Min aggregation
    /// </summary>
    public QueryBuilder Min(string field, string? alias = null)
    {
        return AddAggregation(FilterOperators.Min, field, alias);
    }

    /// <summary>
    /// Add Max aggregation
    /// </summary>
    public QueryBuilder Max(string field, string? alias = null)
    {
        return AddAggregation(FilterOperators.Max, field, alias);
    }

    /// <summary>
    /// Add Join operation
    /// </summary>
    public QueryBuilder Join(string entity, string sourceKey, string targetKey, params string[] selectFields)
    {
        return AddJoin(FilterOperators.Join, entity, sourceKey, targetKey, selectFields);
    }

    /// <summary>
    /// Add GroupJoin operation (LEFT JOIN)
    /// </summary>
    public QueryBuilder GroupJoin(string entity, string sourceKey, string targetKey, params string[] selectFields)
    {
        return AddJoin(FilterOperators.GroupJoin, entity, sourceKey, targetKey, selectFields);
    }

    /// <summary>
    /// Add First operation (returns first element)
    /// </summary>
    public QueryBuilder First()
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.First,
            Parameters = new Dictionary<string, object?>()
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add FirstOrDefault operation
    /// </summary>
    public QueryBuilder FirstOrDefault()
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.FirstOrDefault,
            Parameters = new Dictionary<string, object?>()
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Single operation (expects exactly one element)
    /// </summary>
    public QueryBuilder Single()
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Single,
            Parameters = new Dictionary<string, object?>()
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add SingleOrDefault operation
    /// </summary>
    public QueryBuilder SingleOrDefault()
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.SingleOrDefault,
            Parameters = new Dictionary<string, object?>()
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Reverse operation to reverse the order
    /// </summary>
    public QueryBuilder Reverse()
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Reverse,
            Parameters = new Dictionary<string, object?>()
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Any operation for collection predicate evaluation
    /// </summary>
    public QueryBuilder Any(string collectionField, string predicateField, string op, object? value)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Any,
            Parameters = new Dictionary<string, object?>
            {
                ["collection"] = collectionField,
                ["predicate"] = new FilterCondition
                {
                    Field = predicateField,
                    Operator = op,
                    Value = value
                }
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add All operation for collection predicate evaluation
    /// </summary>
    public QueryBuilder All(string collectionField, string predicateField, string op, object? value)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.All,
            Parameters = new Dictionary<string, object?>
            {
                ["collection"] = collectionField,
                ["predicate"] = new FilterCondition
                {
                    Field = predicateField,
                    Operator = op,
                    Value = value
                }
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Last operation (returns last element)
    /// </summary>
    public QueryBuilder Last()
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Last,
            Parameters = new Dictionary<string, object?>()
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add LastOrDefault operation
    /// </summary>
    public QueryBuilder LastOrDefault()
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.LastOrDefault,
            Parameters = new Dictionary<string, object?>()
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add ElementAt operation (returns element at specific index)
    /// </summary>
    public QueryBuilder ElementAt(int index)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.ElementAt,
            Parameters = new Dictionary<string, object?>
            {
                ["index"] = index
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add ElementAtOrDefault operation
    /// </summary>
    public QueryBuilder ElementAtOrDefault(int index)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.ElementAtOrDefault,
            Parameters = new Dictionary<string, object?>
            {
                ["index"] = index
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Union operation (set union)
    /// </summary>
    public QueryBuilder Union(AbstractQuery other)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Union,
            Parameters = new Dictionary<string, object?>
            {
                ["other"] = other
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add UnionBy operation (set union by key selector)
    /// </summary>
    public QueryBuilder UnionBy(AbstractQuery other, string keyField)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.UnionBy,
            Parameters = new Dictionary<string, object?>
            {
                ["other"] = other,
                ["keyField"] = keyField
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Intersect operation (set intersection)
    /// </summary>
    public QueryBuilder Intersect(AbstractQuery other)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Intersect,
            Parameters = new Dictionary<string, object?>
            {
                ["other"] = other
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add IntersectBy operation (set intersection by key selector)
    /// </summary>
    public QueryBuilder IntersectBy(AbstractQuery other, string keyField)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.IntersectBy,
            Parameters = new Dictionary<string, object?>
            {
                ["other"] = other,
                ["keyField"] = keyField
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Except operation (set difference)
    /// </summary>
    public QueryBuilder Except(AbstractQuery other)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Except,
            Parameters = new Dictionary<string, object?>
            {
                ["other"] = other
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add ExceptBy operation (set difference by key selector)
    /// </summary>
    public QueryBuilder ExceptBy(AbstractQuery other, string keyField)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.ExceptBy,
            Parameters = new Dictionary<string, object?>
            {
                ["other"] = other,
                ["keyField"] = keyField
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add DistinctBy operation (distinct by key selector)
    /// </summary>
    public QueryBuilder DistinctBy(string keyField)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.DistinctBy,
            Parameters = new Dictionary<string, object?>
            {
                ["keyField"] = keyField
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Concat operation (sequence concatenation)
    /// </summary>
    public QueryBuilder Concat(AbstractQuery other)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Concat,
            Parameters = new Dictionary<string, object?>
            {
                ["other"] = other
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Zip operation (pair elements from two sequences)
    /// </summary>
    public QueryBuilder Zip(AbstractQuery other, string resultSelector)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Zip,
            Parameters = new Dictionary<string, object?>
            {
                ["other"] = other,
                ["resultSelector"] = resultSelector
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add SequenceEqual operation (compare sequences for equality)
    /// </summary>
    public QueryBuilder SequenceEqual(AbstractQuery other)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.SequenceEqual,
            Parameters = new Dictionary<string, object?>
            {
                ["other"] = other
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add OfType operation (filter by type)
    /// </summary>
    public QueryBuilder OfType(string typeName)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.OfType,
            Parameters = new Dictionary<string, object?>
            {
                ["typeName"] = typeName
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add SkipWhile operation (skip elements while condition is true)
    /// </summary>
    public QueryBuilder SkipWhile(string field, string op, object? value)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.SkipWhile,
            Parameters = new Dictionary<string, object?>
            {
                ["predicate"] = new FilterCondition
                {
                    Field = field,
                    Operator = op,
                    Value = value
                }
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add TakeWhile operation (take elements while condition is true)
    /// </summary>
    public QueryBuilder TakeWhile(string field, string op, object? value)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.TakeWhile,
            Parameters = new Dictionary<string, object?>
            {
                ["predicate"] = new FilterCondition
                {
                    Field = field,
                    Operator = op,
                    Value = value
                }
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add SkipLast operation (skip last N elements)
    /// </summary>
    public QueryBuilder SkipLast(int count)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.SkipLast,
            Parameters = new Dictionary<string, object?>
            {
                ["count"] = count
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add TakeLast operation (take last N elements)
    /// </summary>
    public QueryBuilder TakeLast(int count)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.TakeLast,
            Parameters = new Dictionary<string, object?>
            {
                ["count"] = Math.Min(count, 200) // Enforce limit
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add LongCount aggregation
    /// </summary>
    public QueryBuilder LongCount(string? alias = null)
    {
        return AddAggregation(FilterOperators.LongCount, null, alias);
    }

    /// <summary>
    /// Add Aggregate operation (fold/reduce)
    /// </summary>
    public QueryBuilder Aggregate(string field, string aggregateFunction, string? alias = null)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Aggregate,
            Parameters = new Dictionary<string, object?>
            {
                ["field"] = field,
                ["function"] = aggregateFunction,
                ["alias"] = alias
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Contains operation (check if sequence contains element)
    /// </summary>
    public QueryBuilder ContainsElement(object value)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.ContainsElement,
            Parameters = new Dictionary<string, object?>
            {
                ["value"] = value
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add ToLookup operation (create lookup/dictionary)
    /// </summary>
    public QueryBuilder ToLookup(string keyField, string? elementField = null)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.ToLookup,
            Parameters = new Dictionary<string, object?>
            {
                ["keyField"] = keyField,
                ["elementField"] = elementField
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add DefaultIfEmpty operation (returns default elements if sequence is empty)
    /// </summary>
    public QueryBuilder DefaultIfEmpty(object? defaultValue = null)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.DefaultIfEmpty,
            Parameters = new Dictionary<string, object?>
            {
                ["defaultValue"] = defaultValue
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add Chunk operation (split sequence into chunks)
    /// </summary>
    public QueryBuilder Chunk(int size)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.Chunk,
            Parameters = new Dictionary<string, object?>
            {
                ["size"] = Math.Min(size, 200) // Enforce limit
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Add SelectWithIndex operation (select with element index)
    /// </summary>
    public QueryBuilder SelectWithIndex(params string[] fields)
    {
        var operation = new QueryOperation
        {
            Type = FilterOperators.SelectWithIndex,
            Parameters = new Dictionary<string, object?>
            {
                ["fields"] = fields,
                ["includeIndex"] = true
            }
        };
        _query.Operations.Add(operation);
        return this;
    }

    /// <summary>
    /// Create Range operation (generate sequence of numbers)
    /// </summary>
    public static QueryBuilder Range(int start, int count)
    {
        var builder = new QueryBuilder();
        var operation = new QueryOperation
        {
            Type = FilterOperators.Range,
            Parameters = new Dictionary<string, object?>
            {
                ["start"] = start,
                ["count"] = Math.Min(count, 200) // Enforce limit
            }
        };
        builder._query.Operations.Add(operation);
        builder._query.Entity = "Range"; // Special entity for generation
        return builder;
    }

    /// <summary>
    /// Create Repeat operation (repeat element N times)
    /// </summary>
    public static QueryBuilder Repeat(object element, int count)
    {
        var builder = new QueryBuilder();
        var operation = new QueryOperation
        {
            Type = FilterOperators.Repeat,
            Parameters = new Dictionary<string, object?>
            {
                ["element"] = element,
                ["count"] = Math.Min(count, 200) // Enforce limit
            }
        };
        builder._query.Operations.Add(operation);
        builder._query.Entity = "Repeat"; // Special entity for generation
        return builder;
    }

    /// <summary>
    /// Create Empty operation (empty sequence)
    /// </summary>
    public static QueryBuilder Empty(string entityType)
    {
        var builder = new QueryBuilder();
        var operation = new QueryOperation
        {
            Type = FilterOperators.Empty,
            Parameters = new Dictionary<string, object?>
            {
                ["entityType"] = entityType
            }
        };
        builder._query.Operations.Add(operation);
        builder._query.Entity = "Empty"; // Special entity for generation
        return builder;
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

    private QueryBuilder AddAggregation(string function, string? field, string? alias)
    {
        _query.Aggregations.Add(new AggregationSpecification
        {
            Function = function,
            Field = field,
            Alias = alias
        });
        return this;
    }

    private QueryBuilder AddJoin(string joinType, string entity, string sourceKey, string targetKey, string[] selectFields)
    {
        var join = new JoinSpecification
        {
            Type = joinType,
            Entity = entity,
            On = new Dictionary<string, string> { [sourceKey] = targetKey },
            Select = selectFields.ToList()
        };
        _query.Joins.Add(join);
        return this;
    }
}
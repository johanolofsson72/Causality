using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Causality.Shared.Features.Querying.Domain;
using Causality.Server.Features.Querying.Application;
using Causality.Server.Data;

namespace Causality.Server.Features.Querying.Infrastructure;

/// <summary>
/// Service for translating Abstract Queries to EF Core IQueryable and executing them
/// Implements projection-first approach per ADR-0004
/// </summary>
public class QueryTranslator : IQueryTranslator
{
    private readonly ApplicationDbContext _context;
    private readonly IProjectionMapProvider _projectionMapProvider;
    private readonly ICursorPagingService _cursorPagingService;
    private readonly ILogger<QueryTranslator> _logger;

    public QueryTranslator(
        ApplicationDbContext context,
        IProjectionMapProvider projectionMapProvider,
        ICursorPagingService cursorPagingService,
        ILogger<QueryTranslator> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _projectionMapProvider = projectionMapProvider ?? throw new ArgumentNullException(nameof(projectionMapProvider));
        _cursorPagingService = cursorPagingService ?? throw new ArgumentNullException(nameof(cursorPagingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<QueryResponse<T>> ExecuteAsync<T>(AbstractQuery query, CancellationToken cancellationToken = default) 
        where T : class
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Get the entity type based on the query
            var entityType = GetEntityType(query.Entity);
            if (entityType == null)
            {
                throw new ArgumentException($"Unknown entity: {query.Entity}");
            }

            // Get projection mapping from entity to DTO
            var projectionExpression = _projectionMapProvider.GetProjectionExpression<T>(entityType);
            if (projectionExpression == null)
            {
                throw new InvalidOperationException($"No projection mapping found from {entityType.Name} to {typeof(T).Name}");
            }

            // Build the query using reflection (since we don't know the entity type at compile time)
            var method = GetType().GetMethod(nameof(ExecuteTypedQueryAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var genericMethod = method.MakeGenericMethod(entityType, typeof(T));
            
            var task = (Task<QueryResponse<T>>)genericMethod.Invoke(this, new object[] { query, projectionExpression, cancellationToken })!;
            return await task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query for entity {Entity}", query.Entity);
            throw;
        }
    }

    private async Task<QueryResponse<TDto>> ExecuteTypedQueryAsync<TEntity, TDto>(
        AbstractQuery query, 
        Expression<Func<TEntity, TDto>> projection,
        CancellationToken cancellationToken) 
        where TEntity : class
        where TDto : class
    {
        // Start with the entity DbSet
        IQueryable<TEntity> queryable = _context.Set<TEntity>();

        // Apply filters
        queryable = ApplyFilters(queryable, query.Filters);

        // Apply advanced operations before projection
        queryable = ApplyOperations(queryable, query.Operations);

        // Handle GroupBy operations
        if (query.GroupBy != null && query.GroupBy.Fields.Any())
        {
            // For GroupBy queries, we need different handling
            return await ExecuteGroupByQuery<TEntity, TDto>(queryable, query, projection, cancellationToken);
        }

        // Apply projection FIRST (ADR-0004: Projection-First)
        var projectedQuery = queryable.Select(projection);

        // Apply sorting to the projected query
        projectedQuery = ApplySort(projectedQuery, query.Sort);

        // Apply post-projection operations
        projectedQuery = ApplyPostProjectionOperations(projectedQuery, query.Operations);

        // Apply pagination
        var (results, nextCursor, totalCount) = await _cursorPagingService.ApplyPagingAsync(
            projectedQuery, query.Page, cancellationToken);

        return new QueryResponse<TDto>
        {
            Items = results.ToList(),
            Page = new PageMetadata
            {
                Size = query.Page?.Size ?? 50,
                NextCursor = nextCursor,
                TotalCount = totalCount
            },
            Meta = new QueryMetadata
            {
                FromCache = false, // TODO: Implement caching
                RowsExamined = results.Count() // This is approximate
            }
        };
    }

    private IQueryable<TEntity> ApplyFilters<TEntity>(IQueryable<TEntity> queryable, List<FilterCondition> filters)
    {
        foreach (var filter in filters)
        {
            queryable = ApplyFilter(queryable, filter);
        }
        return queryable;
    }

    private IQueryable<TEntity> ApplyFilter<TEntity>(IQueryable<TEntity> queryable, FilterCondition filter)
    {
        // Handle nested conditions with logical operators
        if (filter.Conditions != null && filter.Conditions.Any())
        {
            return ApplyLogicalFilter(queryable, filter);
        }

        // Build expression for individual filter
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var property = Expression.Property(parameter, filter.Field);
        var filterExpression = BuildFilterExpression(property, filter.Operator, filter.Value);
        
        if (filterExpression != null)
        {
            var lambda = Expression.Lambda<Func<TEntity, bool>>(filterExpression, parameter);
            queryable = queryable.Where(lambda);
        }

        return queryable;
    }

    private IQueryable<TEntity> ApplyLogicalFilter<TEntity>(IQueryable<TEntity> queryable, FilterCondition filter)
    {
        if (filter.Conditions == null || !filter.Conditions.Any())
            return queryable;

        var parameter = Expression.Parameter(typeof(TEntity), "x");
        Expression? combinedExpression = null;

        foreach (var condition in filter.Conditions)
        {
            var property = Expression.Property(parameter, condition.Field);
            var conditionExpression = BuildFilterExpression(property, condition.Operator, condition.Value);
            
            if (conditionExpression != null)
            {
                if (combinedExpression == null)
                {
                    combinedExpression = conditionExpression;
                }
                else
                {
                    combinedExpression = filter.Logic?.ToLowerInvariant() == "or"
                        ? Expression.OrElse(combinedExpression, conditionExpression)
                        : Expression.AndAlso(combinedExpression, conditionExpression);
                }
            }
        }

        if (combinedExpression != null)
        {
            var lambda = Expression.Lambda<Func<TEntity, bool>>(combinedExpression, parameter);
            queryable = queryable.Where(lambda);
        }

        return queryable;
    }

    private Expression? BuildFilterExpression(MemberExpression property, string op, object? value)
    {
        var constant = value != null ? Expression.Constant(value, value.GetType()) : null;

        return op switch
        {
            FilterOperators.Equal => constant != null ? Expression.Equal(property, constant) : null,
            FilterOperators.NotEquals => constant != null ? Expression.NotEqual(property, constant) : null,
            FilterOperators.LessThan => constant != null ? Expression.LessThan(property, constant) : null,
            FilterOperators.LessThanOrEqual => constant != null ? Expression.LessThanOrEqual(property, constant) : null,
            FilterOperators.GreaterThan => constant != null ? Expression.GreaterThan(property, constant) : null,
            FilterOperators.GreaterThanOrEqual => constant != null ? Expression.GreaterThanOrEqual(property, constant) : null,
            FilterOperators.Contains => BuildStringMethod(property, "Contains", value),
            FilterOperators.StartsWith => BuildStringMethod(property, "StartsWith", value),
            FilterOperators.EndsWith => BuildStringMethod(property, "EndsWith", value),
            FilterOperators.IsNull => Expression.Equal(property, Expression.Constant(null)),
            FilterOperators.IsNotNull => Expression.NotEqual(property, Expression.Constant(null)),
            FilterOperators.In => BuildInExpression(property, value),
            _ => throw new NotSupportedException($"Filter operator {op} is not supported")
        };
    }

    private Expression? BuildStringMethod(MemberExpression property, string methodName, object? value)
    {
        if (value == null) return null;

        var method = typeof(string).GetMethod(methodName, new[] { typeof(string) });
        if (method == null) return null;

        var constant = Expression.Constant(value.ToString(), typeof(string));
        return Expression.Call(property, method, constant);
    }

    private Expression? BuildInExpression(MemberExpression property, object? value)
    {
        if (value == null) return null;

        // Handle array/list values for IN operator
        if (value is Array array)
        {
            var values = array.Cast<object>().ToArray();
            var constant = Expression.Constant(values);
            var containsMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(property.Type);
            
            return Expression.Call(containsMethod, constant, property);
        }

        return null;
    }

    private IQueryable<T> ApplySort<T>(IQueryable<T> queryable, List<SortSpecification> sortSpecs)
    {
        if (!sortSpecs.Any()) return queryable;

        // Apply first sort
        var firstSort = sortSpecs[0];
        var orderedQuery = firstSort.Direction.ToLowerInvariant() == "desc"
            ? queryable.OrderByDescending(BuildSortExpression<T>(firstSort.Field))
            : queryable.OrderBy(BuildSortExpression<T>(firstSort.Field));

        // Apply subsequent sorts using ThenBy
        for (int i = 1; i < sortSpecs.Count; i++)
        {
            var sort = sortSpecs[i];
            orderedQuery = sort.Direction.ToLowerInvariant() == "desc"
                ? orderedQuery.ThenByDescending(BuildSortExpression<T>(sort.Field))
                : orderedQuery.ThenBy(BuildSortExpression<T>(sort.Field));
        }

        return orderedQuery;
    }

    private Expression<Func<T, object>> BuildSortExpression<T>(string fieldName)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, fieldName);
        var convert = Expression.Convert(property, typeof(object));
        return Expression.Lambda<Func<T, object>>(convert, parameter);
    }

    private Type? GetEntityType(string entityName)
    {
        return entityName.ToLowerInvariant() switch
        {
            "user" => typeof(User), // Replace with actual entity types
            "product" => typeof(Product), // Replace with actual entity types
            _ => null
        };
    }

    private IQueryable<TEntity> ApplyOperations<TEntity>(IQueryable<TEntity> queryable, List<QueryOperation> operations)
    {
        foreach (var operation in operations)
        {
            queryable = ApplyOperation(queryable, operation);
        }
        return queryable;
    }

    private IQueryable<TEntity> ApplyOperation<TEntity>(IQueryable<TEntity> queryable, QueryOperation operation)
    {
        return operation.Type switch
        {
            FilterOperators.SelectMany => ApplySelectMany(queryable, operation),
            FilterOperators.Distinct => ApplyDistinct(queryable, operation),
            FilterOperators.Skip => ApplySkip(queryable, operation),
            FilterOperators.Take => ApplyTake(queryable, operation),
            FilterOperators.Any => ApplyAnyOperation(queryable, operation),
            FilterOperators.All => ApplyAllOperation(queryable, operation),
            FilterOperators.Reverse => queryable.Reverse(),
            _ => queryable
        };
    }

    private IQueryable<T> ApplyPostProjectionOperations<T>(IQueryable<T> queryable, List<QueryOperation> operations)
    {
        foreach (var operation in operations)
        {
            queryable = ApplyPostProjectionOperation(queryable, operation);
        }
        return queryable;
    }

    private IQueryable<T> ApplyPostProjectionOperation<T>(IQueryable<T> queryable, QueryOperation operation)
    {
        return operation.Type switch
        {
            FilterOperators.First => queryable.Take(1),
            FilterOperators.FirstOrDefault => queryable.Take(1),
            FilterOperators.Single => queryable.Take(2), // Take 2 to validate single
            FilterOperators.SingleOrDefault => queryable.Take(2),
            FilterOperators.Distinct => queryable.Distinct(),
            _ => queryable
        };
    }

    private IQueryable<TEntity> ApplyDistinct<TEntity>(IQueryable<TEntity> queryable, QueryOperation operation)
    {
        // For entity-level distinct, we can use the standard Distinct()
        // Field-specific distinct would require more complex handling
        return queryable.Distinct();
    }

    private IQueryable<TEntity> ApplySkip<TEntity>(IQueryable<TEntity> queryable, QueryOperation operation)
    {
        if (operation.Parameters.TryGetValue("count", out var countObj) && countObj is int count)
        {
            return queryable.Skip(count);
        }
        return queryable;
    }

    private IQueryable<TEntity> ApplyTake<TEntity>(IQueryable<TEntity> queryable, QueryOperation operation)
    {
        if (operation.Parameters.TryGetValue("count", out var countObj) && countObj is int count)
        {
            // Enforce maximum limit for security
            var safeCount = Math.Min(count, 200);
            return queryable.Take(safeCount);
        }
        return queryable;
    }

    private async Task<QueryResponse<TDto>> ExecuteGroupByQuery<TEntity, TDto>(
        IQueryable<TEntity> queryable,
        AbstractQuery query,
        Expression<Func<TEntity, TDto>> projection,
        CancellationToken cancellationToken)
        where TEntity : class
        where TDto : class
    {
        // GroupBy queries require special handling since they return grouped data
        // This is a simplified implementation - real implementation would be more complex
        
        throw new NotImplementedException("GroupBy queries require specialized implementation based on specific requirements");
    }

    private IQueryable<TEntity> ApplySelectMany<TEntity>(IQueryable<TEntity> queryable, QueryOperation operation)
    {
        if (!operation.Parameters.TryGetValue("field", out var fieldObj) || fieldObj == null)
        {
            throw new InvalidOperationException("SelectMany operation requires a 'field' parameter");
        }

        var fieldName = fieldObj.ToString()!;
        
        // Build SelectMany expression: x => x.CollectionProperty
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var property = Expression.Property(parameter, fieldName);
        
        // Check if this is a collection property
        var propertyType = property.Type;
        if (!propertyType.IsGenericType)
        {
            throw new InvalidOperationException($"SelectMany field '{fieldName}' must be a collection property");
        }

        var elementType = propertyType.GetGenericArguments()[0];
        
        // Create SelectMany expression
        var selectorType = typeof(Func<,>).MakeGenericType(typeof(TEntity), propertyType);
        var selector = Expression.Lambda(selectorType, property, parameter);
        
        // Call SelectMany method via reflection
        var selectManyMethod = typeof(Queryable).GetMethods()
            .Where(m => m.Name == "SelectMany" && m.GetParameters().Length == 2)
            .First()
            .MakeGenericMethod(typeof(TEntity), elementType);
            
        var result = selectManyMethod.Invoke(null, new object[] { queryable, selector });
        
        // This is a simplified approach - in reality, we'd need to handle type conversion
        // For now, we'll return the original queryable and let the actual implementation
        // handle the SelectMany logic appropriately
        
        _logger.LogWarning("SelectMany operation detected but not fully implemented. Field: {Field}", fieldName);
        return queryable;
    }

    private IQueryable<TEntity> ApplyAnyOperation<TEntity>(IQueryable<TEntity> queryable, QueryOperation operation)
    {
        if (!operation.Parameters.TryGetValue("collection", out var collectionObj) || collectionObj == null)
        {
            throw new InvalidOperationException("Any operation requires a 'collection' parameter");
        }

        if (!operation.Parameters.TryGetValue("predicate", out var predicateObj) || predicateObj is not FilterCondition predicate)
        {
            throw new InvalidOperationException("Any operation requires a 'predicate' parameter");
        }

        var collectionName = collectionObj.ToString()!;
        
        // Build Any expression: x => x.Collection.Any(y => y.Field == Value)
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var collection = Expression.Property(parameter, collectionName);
        
        // Get collection element type
        var collectionType = collection.Type;
        if (!collectionType.IsGenericType)
        {
            throw new InvalidOperationException($"Any collection '{collectionName}' must be a generic collection");
        }

        var elementType = collectionType.GetGenericArguments()[0];
        var innerParameter = Expression.Parameter(elementType, "y");
        var innerProperty = Expression.Property(innerParameter, predicate.Field);
        
        // Build predicate expression
        var predicateExpression = BuildFilterExpression(innerProperty, predicate.Operator, predicate.Value);
        if (predicateExpression == null)
        {
            throw new InvalidOperationException($"Could not build predicate for Any operation");
        }

        var innerLambda = Expression.Lambda(predicateExpression, innerParameter);
        
        // Call Any method
        var anyMethod = typeof(Enumerable).GetMethods()
            .Where(m => m.Name == "Any" && m.GetParameters().Length == 2)
            .First()
            .MakeGenericMethod(elementType);

        var anyCall = Expression.Call(anyMethod, collection, innerLambda);
        var outerLambda = Expression.Lambda<Func<TEntity, bool>>(anyCall, parameter);
        
        return queryable.Where(outerLambda);
    }

    private IQueryable<TEntity> ApplyAllOperation<TEntity>(IQueryable<TEntity> queryable, QueryOperation operation)
    {
        // Similar to ApplyAnyOperation but uses All instead of Any
        if (!operation.Parameters.TryGetValue("collection", out var collectionObj) || collectionObj == null)
        {
            throw new InvalidOperationException("All operation requires a 'collection' parameter");
        }

        if (!operation.Parameters.TryGetValue("predicate", out var predicateObj) || predicateObj is not FilterCondition predicate)
        {
            throw new InvalidOperationException("All operation requires a 'predicate' parameter");
        }

        var collectionName = collectionObj.ToString()!;
        
        // Build All expression: x => x.Collection.All(y => y.Field == Value)
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var collection = Expression.Property(parameter, collectionName);
        
        var collectionType = collection.Type;
        if (!collectionType.IsGenericType)
        {
            throw new InvalidOperationException($"All collection '{collectionName}' must be a generic collection");
        }

        var elementType = collectionType.GetGenericArguments()[0];
        var innerParameter = Expression.Parameter(elementType, "y");
        var innerProperty = Expression.Property(innerParameter, predicate.Field);
        
        var predicateExpression = BuildFilterExpression(innerProperty, predicate.Operator, predicate.Value);
        if (predicateExpression == null)
        {
            throw new InvalidOperationException($"Could not build predicate for All operation");
        }

        var innerLambda = Expression.Lambda(predicateExpression, innerParameter);
        
        var allMethod = typeof(Enumerable).GetMethods()
            .Where(m => m.Name == "All" && m.GetParameters().Length == 2)
            .First()
            .MakeGenericMethod(elementType);

        var allCall = Expression.Call(allMethod, collection, innerLambda);
        var outerLambda = Expression.Lambda<Func<TEntity, bool>>(allCall, parameter);
        
        return queryable.Where(outerLambda);
    }
}

// Placeholder interfaces and classes - these would be implemented separately

/// <summary>
/// Interface for providing projection expressions from entities to DTOs
/// </summary>
public interface IProjectionMapProvider
{
    Expression<Func<TEntity, TDto>>? GetProjectionExpression<TDto>(Type entityType) where TDto : class;
}

/// <summary>
/// Interface for cursor-based pagination
/// </summary>
public interface ICursorPagingService
{
    Task<(IQueryable<T> results, string? nextCursor, long? totalCount)> ApplyPagingAsync<T>(
        IQueryable<T> queryable, PageRequest? pageRequest, CancellationToken cancellationToken = default);
}

// Placeholder entity classes - replace with actual entities
public class User { }
public class Product { }
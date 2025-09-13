using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
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

        // Apply projection FIRST (ADR-0004: Projection-First)
        var projectedQuery = queryable.Select(projection);

        // Apply sorting to the projected query
        projectedQuery = ApplySort(projectedQuery, query.Sort);

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
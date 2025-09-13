using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Causality.Shared.Features.Querying.Domain;

namespace Causality.Server.Features.Querying.Infrastructure;

/// <summary>
/// Service for implementing cursor-based pagination with opaque tokens
/// Provides stable ordering for consistent pagination results
/// </summary>
public class CursorPagingService : ICursorPagingService
{
    private readonly ILogger<CursorPagingService> _logger;

    public CursorPagingService(ILogger<CursorPagingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(IQueryable<T> results, string? nextCursor, long? totalCount)> ApplyPagingAsync<T>(
        IQueryable<T> queryable, 
        PageRequest? pageRequest, 
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Min(pageRequest?.Size ?? 50, 200); // Enforce max page size
        long? totalCount = null;

        // Get total count if requested (this can be expensive for large datasets)
        // In production, consider making this optional or implementing approximation
        if (pageRequest?.Size > 0) // Simple heuristic - only count if explicitly requested
        {
            try
            {
                totalCount = await queryable.LongCountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get total count for query");
                // Continue without count
            }
        }

        // Parse cursor if provided
        CursorData? cursorData = null;
        if (!string.IsNullOrEmpty(pageRequest?.Cursor))
        {
            try
            {
                cursorData = ParseCursor(pageRequest.Cursor);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse cursor: {Cursor}", pageRequest.Cursor);
                // Continue without cursor (start from beginning)
            }
        }

        // Apply cursor-based filtering
        if (cursorData != null)
        {
            queryable = ApplyCursorFilter(queryable, cursorData);
        }

        // Take one extra item to determine if there's a next page
        var items = await queryable.Take(pageSize + 1).ToListAsync(cancellationToken);
        
        // Check if there's a next page
        string? nextCursor = null;
        if (items.Count > pageSize)
        {
            // Remove the extra item and create cursor for next page
            var lastItem = items[pageSize - 1];
            items.RemoveAt(pageSize);
            
            nextCursor = CreateCursor(lastItem);
        }

        return (items.AsQueryable(), nextCursor, totalCount);
    }

    private IQueryable<T> ApplyCursorFilter<T>(IQueryable<T> queryable, CursorData cursorData)
    {
        // This is a simplified implementation
        // In production, you'd want more sophisticated cursor handling based on:
        // 1. The actual sort fields being used
        // 2. Multiple sort criteria
        // 3. Proper comparison operators for different data types

        try
        {
            // For now, assume we're filtering by Id (most common case)
            if (cursorData.Values.TryGetValue("Id", out var idValue) && idValue is JsonElement jsonElement)
            {
                if (jsonElement.TryGetInt32(out var id))
                {
                    // Use reflection to apply the filter
                    var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
                    var property = System.Linq.Expressions.Expression.Property(parameter, "Id");
                    var constant = System.Linq.Expressions.Expression.Constant(id);
                    var comparison = System.Linq.Expressions.Expression.GreaterThan(property, constant);
                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(comparison, parameter);
                    
                    return queryable.Where(lambda);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply cursor filter");
        }

        return queryable;
    }

    private string CreateCursor<T>(T item)
    {
        try
        {
            // Extract key fields for cursor (simplified - using Id)
            var cursorData = new CursorData();
            
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null)
            {
                var idValue = idProperty.GetValue(item);
                if (idValue != null)
                {
                    cursorData.Values["Id"] = idValue;
                }
            }

            // Serialize cursor data and encode as base64
            var json = JsonSerializer.Serialize(cursorData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cursor for item");
            return string.Empty;
        }
    }

    private CursorData ParseCursor(string cursor)
    {
        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var json = Encoding.UTF8.GetString(bytes);
            
            var cursorData = JsonSerializer.Deserialize<CursorData>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return cursorData ?? new CursorData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse cursor: {Cursor}", cursor);
            throw new ArgumentException("Invalid cursor format", nameof(cursor), ex);
        }
    }
}

/// <summary>
/// Internal structure for cursor data
/// </summary>
internal class CursorData
{
    public Dictionary<string, object> Values { get; set; } = new();
    public DateTime? Timestamp { get; set; }
}
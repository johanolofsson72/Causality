# Abstract Query (AQ) Infrastructure

This document describes the comprehensive Abstract Query infrastructure implemented in Causality, which enables secure, type-safe querying from client to server following all architectural decision records (ADRs).

## Overview

The Abstract Query system allows clients to build queries using a fluent API, serialize them to a language-agnostic JSON format, send them to the server for validation and execution, and receive paginated DTO results. This replaces raw LINQ serialization with a secure, controlled approach.

## Architecture

```
Client (Blazor WASM)
├── QueryBuilder (Fluent API)
└── AbstractQuery (JSON)
    │
    │ HTTP POST /api/query/execute
    │
    ▼
Server (ASP.NET Core)
├── QueryController
├── QueryValidationService (ADR-0002 Guardrails)
├── QueryTranslator (AQ → EF Core IQueryable)
├── ProjectionMapProvider (ADR-0004 Projection-First)
├── CursorPagingService (Stable pagination)
└── QueryMetrics (ADR-0005 Monitoring)
```

## Client Usage

### Basic Query Building

```csharp
using Causality.Shared.Features.Querying.Application;

// Simple equality filter
var query = QueryBuilder
    .For("User")
    .Where("IsActive", true)
    .OrderBy("Name")
    .Select("Id", "Name", "Email")
    .PageSize(25)
    .Build();

// Complex query with multiple conditions
var advancedQuery = QueryBuilder
    .For("Product")
    .Contains("Name", searchTerm)
    .GreaterThan("Price", minPrice)
    .LessThanOrEqual("Price", maxPrice)
    .In("Category", "Electronics", "Books", "Clothing")
    .OrderByDescending("CreatedAt")
    .ThenBy("Name")
    .Select("Id", "Name", "Brand", "Price", "Category")
    .PageSize(50)
    .IncludeCount(true)
    .Build();
```

### Supported Filter Operators

- **Comparison**: `eq`, `neq`, `lt`, `lte`, `gt`, `gte`, `in`
- **String**: `contains`, `startsWith`, `endsWith`, `equalsIgnoreCase`
- **Null handling**: `isNull`, `isNotNull`
- **Logical**: `and`, `or` (with nested conditions)

### Fluent API Methods

```csharp
QueryBuilder
    .For("EntityName")              // Set target entity
    .Where(field, value)            // Add equality filter
    .Where(field, op, value)        // Add filter with specific operator
    .Equals(field, value)           // Equality filter
    .NotEquals(field, value)        // Not equals filter
    .LessThan(field, value)         // Less than filter
    .GreaterThan(field, value)      // Greater than filter
    .Contains(field, value)         // String contains filter
    .StartsWith(field, value)       // String starts with filter
    .In(field, values...)           // IN filter for multiple values
    .IsNull(field)                  // Null check filter
    .OrderBy(field)                 // Sort ascending
    .OrderByDescending(field)       // Sort descending
    .ThenBy(field)                  // Secondary sort ascending
    .ThenByDescending(field)        // Secondary sort descending
    .Select(fields...)              // Project specific fields
    .PageSize(size)                 // Set page size (max 200)
    .Cursor(cursor)                 // Set pagination cursor
    .IncludeCount(true)             // Include total count in response
    .Build();                       // Create AbstractQuery
```

## Server Configuration

### Entity Whitelisting (Required)

```csharp
// Configure allowed operations per entity
var config = new QueryValidationConfiguration
{
    MaxDepth = 4,
    MaxNodes = 200,
    MaxPageSize = 200,
    ExecutionTimeout = TimeSpan.FromSeconds(5),
    EntityConfigurations = new Dictionary<string, EntityConfiguration>
    {
        ["user"] = new EntityConfiguration
        {
            Name = "User",
            FilterableFields = new HashSet<string> 
            { 
                "Id", "Name", "Email", "CreatedAt", "IsActive" 
                // Note: PasswordHash NOT included for security
            },
            SortableFields = new HashSet<string> 
            { 
                "Id", "Name", "Email", "CreatedAt" 
            },
            SelectableFields = new HashSet<string> 
            { 
                "Id", "Name", "Email", "CreatedAt", "LastLoginAt", "IsActive" 
            },
            FieldOperators = new Dictionary<string, HashSet<string>>
            {
                ["Name"] = new HashSet<string> 
                { 
                    "eq", "neq", "contains", "startsWith", "endsWith" 
                },
                ["IsActive"] = new HashSet<string> 
                { 
                    "eq", "neq" 
                }
            }
        }
    }
};
```

### Service Registration

```csharp
// In Program.cs or Startup.cs
services.AddSingleton(config);
services.AddScoped<IQueryValidationService, QueryValidationService>();
services.AddScoped<IQueryTranslator, QueryTranslator>();
services.AddScoped<IProjectionMapProvider, ProjectionMapProvider>();
services.AddScoped<ICursorPagingService, CursorPagingService>();
services.AddSingleton<QueryMetrics>();
services.AddScoped<QueryAuditLogger>();
```

### Projection Maps (Required)

```csharp
// Define entity → DTO mappings
public class ProjectionMapProvider : IProjectionMapProvider
{
    private readonly Dictionary<(Type entityType, Type dtoType), LambdaExpression> _projectionMaps;

    private LambdaExpression CreateUserProjection(Type userEntityType)
    {
        // user => new UserDto { Id = user.Id, Name = user.Name, ... }
        var parameter = Expression.Parameter(userEntityType, "user");
        // ... build projection expression
        return Expression.Lambda(memberInit, parameter);
    }
}
```

## API Endpoint

### Request Format

```http
POST /api/query/execute
Content-Type: application/json

{
  "entity": "User",
  "filters": [
    {
      "field": "IsActive",
      "op": "eq",
      "value": true
    },
    {
      "field": "CreatedAt",
      "op": "gt",
      "value": "2024-08-01T00:00:00Z"
    }
  ],
  "sort": [
    {
      "field": "CreatedAt",
      "dir": "desc"
    }
  ],
  "select": [
    "Id",
    "Name",
    "Email",
    "CreatedAt"
  ],
  "page": {
    "size": 25,
    "cursor": "eyJpZCI6MTAwfQ=="
  },
  "hints": {
    "includeCount": true
  }
}
```

### Response Format

```json
{
  "items": [
    {
      "id": 1,
      "name": "John Doe",
      "email": "john@example.com",
      "createdAt": "2024-08-15T10:30:00Z"
    }
  ],
  "page": {
    "nextCursor": "eyJpZCI6MTAxfQ==",
    "size": 25,
    "totalCount": 1250
  },
  "meta": {
    "elapsedMs": 45,
    "fromCache": false,
    "queryHash": "abc123def456",
    "rowsExamined": 25
  }
}
```

## Security Features (ADR-0002 Compliance)

### Validation Guardrails

- **MaxDepth**: 4 (prevents deeply nested conditions)
- **MaxNodes**: 200 (limits query complexity)
- **MaxPageSize**: 200 (prevents large result sets)
- **ExecutionTimeout**: 5 seconds (prevents long-running queries)

### Field-Level Access Control

- **FilterableFields**: Only whitelisted fields can be used in WHERE clauses
- **SortableFields**: Only whitelisted fields can be used in ORDER BY clauses
- **SelectableFields**: Only whitelisted fields can be returned in results
- **FieldOperators**: Specific operators allowed per field

### Projection-First Security (ADR-0004)

- Entities are NEVER returned directly to clients
- All results go through explicit DTO projection
- Sensitive fields (passwords, internal notes, etc.) are never exposed
- Projection maps are defined server-side and cannot be overridden

## Monitoring & Metrics (ADR-0005)

### Prometheus Metrics

- `query_latency_ms`: Histogram of execution times
- `query_executed_total`: Counter of successful queries
- `query_blocked_total`: Counter of blocked queries
- `query_result_count`: Histogram of result counts
- `active_concurrent_queries`: Current executing queries

### Audit Logging

All query activity is logged with:
- Query hash (for correlation)
- User ID and Tenant ID
- Entity being queried
- Execution time and result count
- Validation outcome (allowed/blocked)
- Error details if failed

### Metrics Endpoint

```http
GET /metrics
```

Returns Prometheus-compatible metrics for scraping.

## Pagination

### Cursor-Based Paging

The system uses opaque cursor tokens for stable pagination:

```csharp
// First page
var query = QueryBuilder.For("User").PageSize(50).Build();

// Next page using cursor from previous response
var nextPageQuery = QueryBuilder.For("User")
    .PageSize(50)
    .Cursor(previousResponse.Page.NextCursor)
    .Build();
```

### Benefits

- **Stable**: Results don't shift when data is added/removed
- **Performant**: No OFFSET-based queries that get slower with page depth
- **Opaque**: Cursor implementation details are hidden from client

## Example Usage Scenarios

### Dashboard Query

```csharp
// Active users in last 30 days
var activeUsersQuery = QueryBuilder
    .For("User")
    .Where("IsActive", true)
    .GreaterThan("LastLoginAt", DateTime.Now.AddDays(-30))
    .OrderByDescending("LastLoginAt")
    .Select("Id", "Name", "Email", "LastLoginAt")
    .PageSize(100)
    .IncludeCount(true)
    .Build();
```

### Product Search

```csharp
// Products matching search with price range
var productSearchQuery = QueryBuilder
    .For("Product")
    .Contains("Name", searchTerm)
    .GreaterThanOrEqual("Price", minPrice)
    .LessThanOrEqual("Price", maxPrice)
    .Where("IsAvailable", true)
    .OrderBy("Brand")
    .ThenBy("Name")
    .Select("Id", "Name", "Brand", "Price", "Category")
    .PageSize(50)
    .Build();
```

### Complex Filtering

```csharp
// Products in specific categories with availability
var categoryQuery = QueryBuilder
    .For("Product")
    .In("Category", "Electronics", "Books", "Clothing")
    .Where("IsAvailable", true)
    .GreaterThan("Price", 0)
    .OrderByDescending("CreatedAt")
    .Select("Id", "Name", "Brand", "Price", "Category", "CreatedAt")
    .PageSize(75)
    .Build();
```

## Best Practices

### Client Side

1. **Always use QueryBuilder**: Don't construct AbstractQuery objects manually
2. **Specify projections**: Always use `.Select()` to limit returned fields
3. **Set reasonable page sizes**: Default is 50, max is 200
4. **Handle pagination**: Use cursor tokens for consistent results
5. **Cache queries**: Reuse QueryBuilder instances when possible

### Server Side

1. **Configure whitelists carefully**: Only expose necessary fields and operations
2. **Keep projections minimal**: Only map fields that should be exposed
3. **Monitor metrics**: Track query performance and blocked attempts
4. **Audit everything**: Log all query activity for compliance
5. **Test guardrails**: Verify validation rules prevent unauthorized access

### Security

1. **Never expose entities directly**: Always use DTO projection
2. **Whitelist, don't blacklist**: Explicitly allow operations, don't rely on blocking
3. **Validate everything**: Check all inputs against configuration
4. **Log security events**: Track attempted unauthorized access
5. **Regular reviews**: Audit entity configurations and projection maps

## Error Handling

### Validation Errors

```json
{
  "error": "Query validation failed",
  "details": [
    "Field 'PasswordHash' is not allowed for filtering on entity 'User'",
    "Operator 'arbitrary' is not supported"
  ]
}
```

### Execution Errors

```json
{
  "error": "Internal server error during query execution",
  "queryHash": "abc123def456",
  "elapsedMs": 1250
}
```

## Performance Considerations

1. **Index strategy**: Ensure database indexes support common filter patterns
2. **Result limits**: Enforce reasonable page sizes to prevent memory issues  
3. **Cursor efficiency**: Design cursors to use indexed fields for pagination
4. **Caching layer**: Implement result caching for frequently executed queries
5. **Query timeout**: Set appropriate timeout values to prevent resource exhaustion

## Future Extensions

The infrastructure is designed to support future enhancements:

- **Date operations**: `dateEq`, `dateBetween` with UTC normalization
- **Collection operations**: `any`, `all` for nested data with security limits
- **Aggregations**: `count`, `sum`, `avg`, `min`, `max` with grouping
- **Faceted search**: Category counts and filters
- **Full-text search**: Integration with search engines
- **Advanced caching**: Distributed cache with intelligent invalidation
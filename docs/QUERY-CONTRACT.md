# Query Contract (Abstract Query – AQ) - Comprehensive LINQ Support

This document defines the wire-level contract for how client applications express queries to the server.  
It replaces the idea of serializing raw LINQ expressions with a structured, language-agnostic JSON format that supports the full range of LINQ operators in a secure, validated manner.

---

## Request Format

### Basic Query Structure
```json
{
  "entity": "Product",
  "filters": [
    { "field": "Brand", "op": "eq", "value": "Volvo" },
    { "field": "Price", "op": "lte", "value": 5000 }
  ],
  "sort": [
    { "field": "Name", "dir": "asc" }
  ],
  "select": [ "Id", "Name", "Brand", "Price" ],
  "page": {
    "size": 50,
    "cursor": "eyJpZCI6MTAwfQ=="
  }
}
```

### Advanced Operations
```json
{
  "entity": "User",
  "filters": [
    { "field": "IsActive", "op": "eq", "value": true }
  ],
  "operations": [
    {
      "type": "selectMany",
      "parameters": {
        "field": "Orders",
        "alias": "userOrders"
      }
    },
    {
      "type": "distinct"
    },
    {
      "type": "take",
      "parameters": { "count": 100 }
    }
  ],
  "groupBy": {
    "fields": ["Category"],
    "having": [
      { "field": "TotalRevenue", "op": "gt", "value": 1000 }
    ]
  },
  "aggregations": [
    { "function": "count", "alias": "TotalOrders" },
    { "function": "sum", "field": "Amount", "alias": "TotalRevenue" },
    { "function": "avg", "field": "Amount", "alias": "AvgOrderValue" }
  ],
  "joins": [
    {
      "type": "join",
      "entity": "Product",
      "on": { "ProductId": "Id" },
      "select": ["Name", "Category", "Price"]
    }
  ]
}
```

## Response Format
```json
{
  "items": [
    { "Id": 1, "Name": "Propeller", "Brand": "Volvo", "Price": 4999 }
  ],
  "page": {
    "nextCursor": "eyJpZCI6MTAxfQ==",
    "size": 50,
    "totalCount": 1250
  },
  "meta": {
    "elapsedMs": 12,
    "fromCache": false,
    "rowsExamined": 1250,
    "operationsApplied": ["selectMany", "distinct", "groupBy"]
  }
}
```

## Comprehensive Operator Support

### Filter Operators
- **Comparison**: `eq`, `neq`, `lt`, `lte`, `gt`, `gte`, `in`
- **String Operations**: `contains`, `startsWith`, `endsWith`, `equalsIgnoreCase`
- **Null Handling**: `isNull`, `isNotNull`
- **Date Operations**: `dateEq`, `dateBetween`
- **Logical Combinations**: `and`, `or` (with nested conditions)

### Projection Operations
- **SelectMany**: `selectMany` - Flatten collections (one-to-many relationships)
- **Select**: Standard field selection with DTO projection
- **Distinct**: `distinct` - Remove duplicate results

### Collection Predicates
- **Any**: `any` - True if any collection element matches condition
- **All**: `all` - True if all collection elements match condition

### Ordering Operations
- **OrderBy**: `orderBy`, `orderByDescending`
- **ThenBy**: `thenBy`, `thenByDescending` 
- **Reverse**: `reverse` - Reverse the entire result sequence

### Pagination Operations
- **Cursor-Based**: Standard cursor pagination with opaque tokens
- **Skip/Take**: `skip`, `take` - Offset-based pagination for specific use cases

### Aggregation Operations
- **Count**: `count` - Count of items
- **Sum**: `sum` - Sum of numeric field values
- **Average**: `avg` - Average of numeric field values  
- **Min**: `min` - Minimum field value
- **Max**: `max` - Maximum field value

### Element Selection
- **First**: `first` - First element (throws if empty)
- **FirstOrDefault**: `firstOrDefault` - First element or null
- **Single**: `single` - Single element (throws if empty or multiple)
- **SingleOrDefault**: `singleOrDefault` - Single element or null
- **Last**: `last` - Last element (throws if empty)
- **LastOrDefault**: `lastOrDefault` - Last element or null

### Grouping Operations
- **GroupBy**: Group results by one or more fields
- **Having**: Apply conditions after grouping

### Join Operations
- **Join**: Inner join with related entities
- **GroupJoin**: Left join (outer join) with related entities

### Set Operations (Future)
- **Union**: `union` - Combine two result sets (removes duplicates)
- **Intersect**: `intersect` - Common elements between sets
- **Except**: `except` - Elements in first set but not second
- **Concat**: `concat` - Combine sequences (keeps duplicates)

## Security & Validation (ADR-0002 Compliance)

All operations are subject to strict validation:

### Entity-Level Controls
- **Allowed Entities**: Only whitelisted entities can be queried
- **Field Access Control**: Separate permissions for filterable, sortable, and selectable fields
- **Operation Restrictions**: Field-specific allowed operations

### Complexity Limits
- **MaxDepth**: 4 levels of nested conditions
- **MaxNodes**: 200 total filter/operation nodes per query
- **MaxPageSize**: 200 items per page maximum
- **ExecutionTimeout**: 5-second default with cancellation tokens

### Operator-Specific Validation
- **SelectMany**: Collection fields must be explicitly whitelisted
- **Joins**: Target entities must be in allowed relationships
- **Aggregations**: Only allowed on numeric/countable fields
- **GroupBy**: Grouping fields must be selectable

## Usage Examples

### Simple Filter Query
```json
{
  "entity": "Product",
  "filters": [
    { "field": "IsActive", "op": "eq", "value": true },
    { "field": "Price", "op": "gte", "value": 50 }
  ],
  "sort": [{ "field": "Name", "dir": "asc" }],
  "select": ["Id", "Name", "Price"],
  "page": { "size": 25 }
}
```

### SelectMany for Flattening
```json
{
  "entity": "User",
  "operations": [
    {
      "type": "selectMany", 
      "parameters": { "field": "Orders" }
    }
  ],
  "filters": [
    { "field": "Status", "op": "eq", "value": "Completed" }
  ],
  "select": ["UserId", "UserName", "OrderId", "Amount"]
}
```

### Analytics with GroupBy and Aggregations
```json
{
  "entity": "Order",
  "filters": [
    { "field": "Status", "op": "eq", "value": "Completed" }
  ],
  "groupBy": {
    "fields": ["ProductCategory"],
    "having": [
      { "field": "TotalRevenue", "op": "gt", "value": 1000 }
    ]
  },
  "aggregations": [
    { "function": "count", "alias": "OrderCount" },
    { "function": "sum", "field": "Amount", "alias": "TotalRevenue" },
    { "function": "avg", "field": "Amount", "alias": "AvgOrderValue" }
  ],
  "sort": [{ "field": "TotalRevenue", "dir": "desc" }]
}
```

### Collection Predicates
```json
{
  "entity": "User",
  "operations": [
    {
      "type": "any",
      "parameters": {
        "collection": "Orders",
        "predicate": {
          "field": "Amount",
          "op": "gt", 
          "value": 1000
        }
      }
    }
  ],
  "select": ["Id", "Name", "Email"]
}
```

### Multi-Entity Join
```json
{
  "entity": "User",
  "joins": [
    {
      "type": "join",
      "entity": "Order", 
      "on": { "Id": "UserId" },
      "select": ["Id", "Amount", "Status"]
    },
    {
      "type": "join",
      "entity": "Product",
      "on": { "Order.ProductId": "Id" },
      "select": ["Name", "Category"]
    }
  ],
  "filters": [
    { "field": "Order.Status", "op": "eq", "value": "Completed" }
  ],
  "select": ["User.Name", "Order.Amount", "Product.Name"]
}
```

## Versioning & Compatibility

- **Current Version**: v3 (comprehensive LINQ support)
- **Backward Compatibility**: v2 queries remain supported
- **Version Detection**: Server auto-detects query version based on structure
- **Future Extensions**: Additional operators can be added while maintaining compatibility

## Performance Considerations

- **Projection-First**: All queries use DTO projection before materialization (ADR-0004)
- **Index Optimization**: Server validates queries against available indexes
- **Complexity Analysis**: Query cost estimation prevents expensive operations
- **Caching Integration**: Results cached based on query signature and TTL settings

## Monitoring & Observability (ADR-0005)

Every query execution includes comprehensive metrics:
- **Execution Time**: Total processing duration  
- **Operations Applied**: Which LINQ operators were used
- **Rows Examined**: Database efficiency metrics
- **Cache Status**: Hit/miss information
- **Validation Results**: Security check outcomes

## References
- **PRD** – High-level requirements for comprehensive LINQ support
- **ADR-0001** – Decision to replace raw LINQ with AQ
- **ADR-0002** – Security guardrails and validation framework  
- **ADR-0003** – Multi-tenant security filter injection
- **ADR-0004** – Projection-first architecture
- **ADR-0005** – Comprehensive monitoring and audit requirements
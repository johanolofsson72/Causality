# Query Contract (Abstract Query – AQ)

This document defines the wire-level contract for how client applications express queries to the server.  
It replaces the idea of serializing raw LINQ expressions with a structured, language-agnostic JSON format.

---

## Request Format

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
    "cursor": "eyJpZCI6MTAwfQ=="   // optional: cursor-based pagination
  },
  "hints": {
    "includeCount": false
  }
}
Response Format
{
  "items": [
    { "Id": 1, "Name": "Propeller", "Brand": "Volvo", "Price": 4999 }
  ],
  "page": {
    "nextCursor": "eyJpZCI6MTAxfQ==",
    "size": 50
  },
  "meta": {
    "elapsedMs": 12,
    "fromCache": false
  }
}
Operators
Supported filter operators (op):
eq – equals
neq – not equals
lt – less than
lte – less than or equal
gt – greater than
gte – greater than or equal
in – value is in list
contains – string contains
startsWith – string starts with
endsWith – string ends with
Guardrails
All queries must adhere to the validation rules in ADR-0002:
MaxDepth: 4
MaxNodes: 200
MaxPageSize: 200
ExecutionTimeout: 5s (default)
ProjectionWhitelist: only fields explicitly allowed in DTOs
Versioning
Current version: v2 (used with ExpressionSerializer v2).
Version field may be added to support breaking changes ("version": 2).
Clients must specify version; server defaults to latest if not provided.
Extensibility
Future ADRs may introduce:
Aggregations (count, sum, avg, min, max).
Facets and group-by.
Advanced search scoring.
Each extension must update this contract with examples and guardrails.
References
PRD – high-level requirements
ADR-0001 – decision to replace raw LINQ with AQ
ADR-0002 – guardrail enforcement
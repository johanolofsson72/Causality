# Product Requirements Document (PRD) – Causality

## Overview
Causality is a proof-of-concept system designed to **build LINQ-like queries in the client** and safely **execute them on the server**.  
The project demonstrates how expressions can be constructed in Blazor WASM, serialized into an abstract query (AQ), validated, and then executed against a database (SQL Server/MySQL) via Entity Framework Core.

The goal is to evolve this prototype into a **secure, extensible query engine** that can support enterprise scenarios such as reporting, dashboards, and ad-hoc analysis.

---

## Goals
- Provide a **client API** (QueryBuilder) for constructing queries without exposing server internals.
- Ensure **secure query serialization** and transmission (Abstract Query Contract).
- Execute validated queries **server-side** using EF Core with full control over:
  - Security (whitelisting, tenant enforcement, RBAC).
  - Performance (max depth, max nodes, pagination, caching).
  - Reliability (timeouts, audit logs, error handling).
- Keep the system **extensible** for new operators, entities, and projections.

---

## Scope
- Blazor WASM client builds queries using QueryBuilder.
- Queries are serialized into JSON (AQ v2) and sent to the server.
- Server validates, translates, and executes queries.
- Results are returned as DTOs with paging metadata.
- Support for SQL Server and MySQL backends.

---

## Non-Goals
- No execution of raw LINQ/Expressions from client without validation.
- No support for arbitrary method calls or client-side evaluation.
- No direct entity exposure – always project to DTOs.
- Not intended to replace full BI tools, but to serve as a secure, embedded query layer.

---

## Key Features
1. **Abstract Query (AQ) Contract**  
   - Entity name  
   - Filters (eq, neq, lt, lte, gt, gte, in, contains, startsWith, endsWith)  
   - Sort order  
   - Projection (whitelisted fields only)  
   - Paging (cursor-based)

2. **Validation & Guardrails**  
   - AllowedMembers & AllowedMethods whitelist  
   - MaxDepth, MaxNodes, MaxPageSize, ExecutionTimeout  
   - Tenant and RBAC filter injection  
   - Audit logging for all queries

3. **Server Execution**  
   - Translation to EF Core IQueryable  
   - Projection to DTOs before materialization  
   - Cursor-based paging  
   - Result caching (hash-based)

4. **Extensibility**  
   - Easy to add new filter operators  
   - Support for facets, counts, and aggregate queries (future ADR)  
   - Multiple backends (SQL Server, MySQL)

---

## Success Criteria
- Queries from client can be expressed using QueryBuilder and executed securely on the server.
- Security guardrails prevent unsafe or costly queries.
- System can handle large datasets with stable performance.
- Clear audit trail for executed queries.
- Extensibility for future operators and advanced analytics.

---

## References
- `.augment/system-prompt.md` – structural rules  
- `docs/QUERY-CONTRACT.md` – wire-level contract  
- `adr-0001-client-to-server-linq-serialization.md` – decision on Abstract Query  

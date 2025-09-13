# Project Rules for GitHub Copilot

This repository uses a **feature-based structure**.  
The following files in the project root define how Copilot must generate code:

- `.augment/state.json` – AI state (restart safe)
- `.augment/system-prompt.md` – system rules
- `.augment/rules.md` – coding standards
- `.augment/Claude.md` and `.augment/GPT.md` – model-specific notes
- `/docs/prd.md` – product requirements
- `/docs/adr-*.md` – architecture decision records
- `/docs/INSTRUCTIONS.md` – implementation guidelines
- `/docs/INTEGRATION.md` – integration details

✅ **Copilot must always follow these files when suggesting code.**

# Causality Documentation

This folder contains the product requirements, architectural decisions, and contracts that guide the evolution of the Causality project.

---

## Contents
- [prd.md](prd.md) – Product Requirements Document (high-level goals and scope)
- [QUERY-CONTRACT.md](QUERY-CONTRACT.md) – JSON contract for client-to-server queries
- [adr-0001-client-to-server-linq-serialization.md](adr-0001-client-to-server-linq-serialization.md) – Why we use Abstract Query instead of raw LINQ
- [adr-0002-query-guard-rails.md](adr-0002-query-guard-rails.md) – Guardrails (whitelisting, depth/size limits, timeouts)
- [adr-0003-multi-tenant-filter-injection.md](adr-0003-multi-tenant-filter-injection.md) – Always inject Tenant and RBAC filters server-side
- [adr-0004-projection-first.md](adr-0004-projection-first.md) – DTO projection before returning data
- [adr-0005-caching-and-metrics.md](adr-0005-caching-and-metrics.md) – Result caching, metrics, and auditing

---

## Query Flow (Summary)

1. **Client** builds an Abstract Query (AQ) using QueryBuilder.
2. AQ is serialized to JSON according to [QUERY-CONTRACT.md](QUERY-CONTRACT.md).
3. **Server** validates AQ against guardrails (ADR-0002).
4. Mandatory filters (Tenant, RBAC) are injected (ADR-0003).
5. Server translates AQ into EF Core IQueryable.
6. Projection to DTOs is applied (ADR-0004).
7. Query is executed, results optionally cached, and metrics recorded (ADR-0005).

---

## Cursor-Based Paging

Causality uses **cursor-based paging** instead of simple page/size when possible.

- **Page/Size (offset-based)**  
  Example: `page=5&size=50` → "Skip 200 rows, take 50".  
  ❌ Expensive on large datasets, inconsistent if data changes between requests.

- **Cursor-Based Paging**  
  The server returns a `"nextCursor"` token in each response, encoding where the next page should start.  
  Example:
  ```json
  "page": {
    "size": 50,
    "nextCursor": "eyJpZCI6MTAxfQ=="
  }
On the next request, the client sends "cursor": "eyJpZCI6MTAxfQ==".
✅ Efficient (no large skips), consistent (stable ordering), secure (opaque token).
Rules:

Cursors are opaque strings; clients must not interpret them.
Default page size = 50, max page size = 200 (ADR-0002).
Supported sort fields must be deterministic to allow stable cursoring.
How to Extend
When adding new features:
Update PRD if scope/goals change.
Add a new ADR if a major architectural decision is made.
Update QUERY-CONTRACT.md with new operators or fields.
Ensure tests cover new operators and guardrails.
References
.augment/system-prompt.md – AI/coding agent rules
.augment/rules.md – coding conventions
/tests – automated validation of contract, guardrails, and projection


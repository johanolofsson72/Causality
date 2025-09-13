# Copilot Instructions

- Always consult the following files in the project root before generating code:
  - `.augment/system-prompt.md`
  - `.augment/rules.md`
  - `/docs/INSTRUCTIONS.md`
  - `/docs/INTEGRATION.md`

- All new features must follow the feature-based structure:
  `/src/<FeatureName>/{Domain,Application,Infrastructure}`

- Never place code directly in the project root.
- Always update `/docs/INSTRUCTIONS.md` and `/docs/INTEGRATION.md` if you generate new code that affects structure.


Copilot Prompt – Upgrade to .NET 9 + Rich LINQ Over AQ
Context (must read):
This repo is Causality. Client builds queries in Blazor and sends them to server for execution.
You MUST follow these docs before generating code:
docs/prd.md
docs/QUERY-CONTRACT.md
docs/adr-0001-client-to-server-linq-serialization.md
docs/adr-0002-query-guard-rails.md
docs/adr-0003-multi-tenant-filter-injection.md
docs/adr-0004-projection-first.md
docs/adr-0005-caching-and-metrics.md
Also follow .augment/system-prompt.md and .augment/rules.md.
Goal
Upgrade solution to .NET 9 (Client/Server/Shared).
Expand the Abstract Query (AQ) pipeline to support the widest safe set of LINQ operators end-to-end (client → AQ JSON → server validation → EF Core translation → DTO projection → cursor paging).
Keep strict security/guardrails (whitelist, tenant/RBAC injection, timeouts, DTO-only).
Scope (do exactly this)
Update all projects to TargetFramework: net9.0.
Bump EF Core to latest for .NET 9 and align packages.
Implement/extend:
Shared/Features/Querying/QueryBuilder.cs (fluent builder for AQ).
Server/Controllers/QueryController.cs (POST /api/query/execute).
Server/Services/QueryValidationService.cs (whitelist, limits).
Server/Infrastructure/QueryTranslator.cs (AQ → IQueryable with EF Core).
Server/Infrastructure/ProjectionMaps/* (entity → DTO expressions).
Server/Infrastructure/CursorPaging.cs (opaque cursor, stable ordering).
Ensure Projection-First: always Select → DTO before materialization.
Add Prometheus/Grafana metrics and audit logging per ADR-0005.
LINQ operator set (implement end-to-end)
Filters (required now):
eq, neq, lt, lte, gt, gte, in
string: contains, startsWith, endsWith, equalsIgnoreCase (map till EF.Functions.Collate/lowercase jämförelse där lämpligt)
nullable handling: isNull, isNotNull
Sorting: multi-column stable OrderBy/ThenBy (asc/desc).
Projection: select explicit whitelisted fields → DTO.
Paging: cursor-based (opaque token), default size 50, max 200.
Optional (nice to have if time):
date ops: dateEq, dateBetween (utc normalisering),
collections: any, all (begränsat och säkrat),
boolean groups: explicit and/or med maxdjup enligt ADR-0002.
Do NOT implement arbitrary methods, client-side evaluation, reflection access, slumpvisa Invoke, eller dynamiska uttryck utanför whitelist. Blockera icke tillåtna medlemmar/metoder och logga orsak.
Validation & Guardrails (enforce)
Whitelist per entity: fields (filterable, sortable, selectable) + allowed operators.
Limits: MaxDepth=4, MaxNodes=200, MaxPageSize=200, Timeout=5s.
Mandatory filters: inject TenantId == User.TenantId + RBAC predicate.
Deny-list: blockera farliga medlemmar/metoder (t.ex. DateTime.Now om ej uttryckligen tillåtet).
Audit: logga query-hash, entity, userId, elapsedMs, rowCount, allowed/blocked.
DTO & Projection Maps
Lägg DTO:er i Shared/DTOs/*.
Skapa uttryck Expression<Func<TEntity, TDto>> i Server/Infrastructure/ProjectionMaps.
Validera select mot DTO-fält (inte entity-fält).
Tests (must add)
Happy path: kombinerade filter + multi-sort + projektion + cursor.
Guards: blockerat fält/op, för djup/noder, pageSize > max, okänd entity.
Security: tenant/RBAC alltid injiceras (kan ej kringgås).
Perf: timeout, metrics counters, cache hit/miss.
Packaging & Config
appsettings.*: QueryLimitsOptions (sizes, timeout, allowed fields/operators).
Add /metrics för Prometheus (kvar i dev bakom auth om behövs).
CI: bygg/test för alla projekt.
Acceptance Criteria
dotnet build & dotnet test passerar på .NET 9.
Postman/HTTPie: POST /api/query/execute med AQ enligt docs/QUERY-CONTRACT.md returnerar korrekta, projicerade DTO:er med cursor.
Blockerade queries returnerar 400 med tydliga felkoder och auditlogg skrivs.
Metrics och audit är aktiva; cache fungerar med AQ-hash.
File/Namespace rules (must follow)
Namespaces: Causality.Client|Server|Shared.
Ingen kod i rot; följ feature-struktur.
Uppdatera docs/ (PRD/ADR/Contract) om du ändrar format eller operators.
Start now:
Uppgradera csproj → net9.0, bump EF Core.
Implementera QueryValidator + Translator för operatorerna ovan.
Lägg DTO-projektioner och cursor-paging.
Skriv tester för happy path + guards.
Visa diffar och berätta vilka filer som skapats/ändrats.

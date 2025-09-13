# Rules for this repository (Causality)
- Always follow feature-based structure under /src:
  /src/Causality.{Client|Server|Shared}/<FeatureName>/{Domain,Application,Infrastructure,UI}
- Never create new files in root; docs under /docs.
- Update /docs/INSTRUCTIONS.md and ADRs on structural changes.
- All client→server queries MUST use the Query Contract defined in /docs/QUERY-CONTRACT.md.
- Enforce AllowedMembers/AllowedMethods whitelist in server before translating to EF Core.
- Tests live under /tests with mirror feature structure.

Tech focus: .NET 9, Blazor WASM, ASP.NET Core Minimal APIs/Controllers, EF Core, SQL Server/MySQL.
Security focus: input validation, expression whitelisting, query cost guards, tenancy filters, audit logs.

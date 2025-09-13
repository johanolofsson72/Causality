# Causality + Augment Code – Working Instructions
- Build: .NET 9, Blazor WASM (Client), ASP.NET Core (Server), EF Core.
- Client builds an Abstract Query (AQ) via QueryBuilder (fluent API) OR captured Expression.
- AQ is serialized (JSON) using ExpressionSerializer v2 and sent to Server.
- Server validates AQ against a whitelist (AllowedMembers/AllowedMethods/MaxDepth/MaxNodes/Timeout).
- Server composes IQueryable with mandatory filters (tenant, RBAC) and projects to DTO before execution.
- All responses are paged (cursor-based default).
- NEVER expose raw Expression or entity models to client.

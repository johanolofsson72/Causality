# Codegen & Namespaces
- Root namespaces:
  - Causality.Client
  - Causality.Server
  - Causality.Shared
- New features go in /src/Causality.Shared/Features/<Name>, same name mirrored in Client/Server.
- DTOs live in Shared. Server never returns entities directly; always project to DTOs.

# Testing
- xUnit + FluentAssertions. One test project per assembly.
- Mandatory tests: Query translation happy path, guard rails (blocked member/method), pagination limits, multi-tenant filter injection.

# Causality

A Blazor WebAssembly demo that caches data at two levels: an in-memory cache on the server and a localStorage + IndexedDB cache in the browser. The client talks to the server over gRPC-Web, and queries are built as LINQ expressions on the client, serialized, and run against the database server-side. The sample data model is a small causality graph: event, class, cause, effect, exclude, meta, and user.

This is a proof of concept, not a product. The oldest code (the EF Core migration) dates to January 2021 and the last commit was September 2025, so treat it as a reference for the patterns rather than something to ship.

## What it does

The point of the project is to show how a WASM client can avoid hitting the server when it doesn't need to, and how the server can avoid hitting the database when it doesn't need to. Both sides cache, and the caches are keyed by the actual query so identical reads are cheap.

There are two query paths in the repo:

- The original path used by the demo pages: the client builds a LINQ predicate (`Expression<Func<T, bool>>`), serializes it with `Serialize.Linq`, sends the filter over gRPC, and the server materializes it with `System.Linq.Dynamic.Core`.
- A newer "abstract query" path under `Server/Features/Querying` and `Shared/Features/Querying` with a `QueryBuilder`, server-side guardrails, projection, and cursor paging, exposed over REST at `api/query/execute`. The design notes live in `docs/adr-0001.md` through `docs/adr-0005.md` and `docs/QUERY-CONTRACT.md`.

## Architecture: the two-level cache

```
Browser (Blazor WASM client)                 Server (ASP.NET Core)
┌───────────────────────────────┐            ┌──────────────────────────────┐
│  Razor page / component        │            │  gRPC service (Cause, Event, │
│        │                        │            │  Class, Effect, Exclude,     │
│        ▼                        │            │  Meta, User)                 │
│  Client service                 │            │        │                      │
│   1. build LINQ filter          │            │        ▼                      │
│   2. check IndexedDB "Blobs"    │            │   IMemoryCache               │
│      keyed by query             │            │   keyed by query             │
│        │ miss                   │            │     │ miss                    │
│        ▼                        │  gRPC-Web  │     ▼                         │
│   call gRPC ───────────────────────────────▶│   Repository → EF Core        │
│        │                        │            │     │                         │
│   3. store result in IndexedDB  │            │   SQLite (Causality.db)       │
└───────────────────────────────┘            └──────────────────────────────┘

localStorage holds only the app-state JSON ("Causality_AppState"),
including the toggle that turns the IndexedDB cache on or off.
```

Server side. Each gRPC service holds an `IMemoryCache`. A read builds a cache key from the filter, order-by, and direction (for example `Cause.Get::<filter>::Id::True`), and on a hit returns the cached rows tagged as coming from `MemoryCache` instead of `Database`. Entries use a sliding expiration set by `AppSettings:DataCacheInSeconds` (60 seconds in `appsettings.json`). Writes (insert, update, delete) drop every key with the entity's prefix, so a `Cause` mutation clears all `Cause.*` cache entries. The REST `GenericController` uses the same memory-cache pattern.

Client side. Two browser stores do two different jobs:

- localStorage holds one thing: the serialized application state (`Causality_AppState`), which carries flags like `UseIndexedDB` and `OfflineMode`. It is read once on startup in `CascadingAppStateProvider` and written back when state changes.
- IndexedDB (database `Causality`, object store `Blobs`) holds the cached query results. When `UseIndexedDB` is on, a client service first looks for a `Blob` whose `key` matches the serialized query; on a hit it deserializes the stored JSON and skips the network entirely. On a miss it calls the server over gRPC and writes the result back as a new `Blob`. Mutations clear the `Blobs` store. There is also a second store, `Outbox`, declared for offline writes.

So a read can be served from three places, cheapest first: IndexedDB in the browser, the server's memory cache, or the database. The status string returned with each response says which one answered.

## Tech stack

- .NET 8 (`net8.0`) across all three projects
- Blazor WebAssembly, ASP.NET Core hosted
- gRPC-Web — `Grpc.AspNetCore` / `Grpc.AspNetCore.Web` on the server, `Grpc.Net.Client.Web` on the client
- Entity Framework Core with SQLite (`Causality.db`); the SQL Server EF provider is referenced but not the wired connection
- `Serialize.Linq` and `System.Linq.Dynamic.Core` for the client-to-server expression path
- `Blazored.LocalStorage` for app state, `TG.Blazor.IndexedDB` for the result cache, `BlazorOnlineState` for online/offline detection
- `prometheus-net` for metrics on the server

Telerik UI for Blazor was removed (the package references are commented out across the projects) so the repo builds without a Telerik license.

## Project structure

```
Causality.sln
Causality/
  Client/    Blazor WASM app — Pages, Components, Services (per-entity gRPC clients + caching), ViewModels
  Server/    ASP.NET Core host — gRPC Services, REST Controllers, EF Core Data, Migrations, Features/Querying
  Shared/    Contracts/causality.proto, DTOs, Models (Blob, AppUser), Features/Querying (QueryBuilder, AbstractQuery)
docs/        PRD, ADRs, query contract and infrastructure notes
```

The proto in `Shared/Contracts/causality.proto` defines all seven services and their messages; the same five-method shape (`Get`, `GetById`, `Insert`, `Update`, `Delete`) repeats per entity.

## Getting started

You need the .NET 8 SDK.

```bash
git clone https://github.com/johanolofsson72/Causality.git
cd Causality
dotnet build Causality.sln
dotnet run --project Causality/Server/Causality.Server.csproj
```

The server hosts the WASM client, so once it's running open the URL it prints and use the demo pages (`/`, `/democausality`, `/demolinq`, `/demoappuser`). The SQLite file `Causality.db` ships in the Server project and is copied to the output directory, so there is no separate database setup step.

To watch the cache toggles do their job: turn IndexedDB on, run a query, and look at the status line on the response. It tells you whether the rows came from IndexedDB, the server's memory cache, or the database.

## Status

Demo and proof of concept. The newer abstract-query feature has design docs (PRD and ADRs under `docs/`) that describe goals like multi-tenant filter injection and RBAC, but those are aspirational notes on the prototype, not implemented guarantees, so read the code before relying on any of it. There is no automated test project in the repo despite older docs referencing a `/tests` folder, and there is no license file, so the usage terms are undefined.

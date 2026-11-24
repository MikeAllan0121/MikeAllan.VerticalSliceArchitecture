# MikeAllan.VerticalSliceArchitecture

A template that demonstrates modern C# (.NET 11, C# 15) and vertical slice architecture through a small
invoicing API: customers, and invoices that go Draft → Issued → Paid. People read this code to learn from
it and copy it, so follow the existing patterns exactly and keep the "why" comments accurate.

## Commands

```bash
dotnet build                     # Debug. Release (-c Release) treats every warning as an error
dotnet test                      # all tests; the integration tests need Docker running
dotnet test --project src/Domain/Domain.UnitTests --filter-class "*.InvoiceTests"
aspire run                       # PostgreSQL container, migrations, seed data, API and dashboard
```

Migrations (run `dotnet tool restore` once to get `dotnet ef`):

```bash
dotnet ef migrations add <Name> --project src/Domain/Domain --startup-project src/WebApi/WebApi --output-dir Database/Migrations
dotnet ef migrations has-pending-model-changes --project src/Domain/Domain --startup-project src/WebApi/WebApi
```

## Layout

| Path | Contents |
| --- | --- |
| `src/Domain/Domain` | Class library: aggregates, `ApplicationDbContext` and migrations, domain events, errors, paging. No ASP.NET Core. |
| `src/WebApi/WebApi` | Minimal API host. `Api/<Feature>/<Slice>/` holds the slices; sibling folders hold cross-cutting concerns (`ErrorHandling`, `Validation`, `EventualConsistency`, ...). |
| `src/ServiceDefaults` | Aspire defaults: OpenTelemetry, health checks, HTTP resilience. |
| `tools/AppHost` | Aspire orchestration: a PostgreSQL container locally; Azure App Service and PostgreSQL Flexible Server on deploy. |
| `tools/Seeder` | Dev-only Bogus data. The AppHost adds it in run mode only. |
| `src/**/*Tests` | Domain unit tests, request-validation unit tests, and integration tests against PostgreSQL in Testcontainers. |

## Architecture

- **One folder per use case.** `Api/Invoices/RecordPayment/` holds that endpoint's handler, request, response
  and route mapping. Behaviour and invariants live on the aggregate in the Domain library, so a handler is
  load, call the domain, save.
- **Explicit wiring.** Each folder that registers anything has a `Setup.cs` of extension members. A `Setup.cs`
  calls only its direct children's, and `Program.cs` calls only the top-level one. Nothing is
  assembly-scanned: endpoints, event handlers, entity configurations and strongly typed IDs are each listed
  where they are wired. Middleware is added only in the top-level `Setup.UseWebApi`, which owns the order.
- **Expected failures are values.** Domain methods return `Result` / `Result<T>` (C# unions) holding a
  `DomainError`, and the API maps it with `error.ToProblem()`. Invalid input is a bug in the caller: the
  domain throws for it, so request validation must reject it first. Every error response is RFC 9457
  problem details.
- **Domain events** are raised by aggregates and dispatched after `SaveChanges`. The WebApi defers them until
  after the response (eventual consistency); anywhere else (the Seeder, tests seeding data) they run at once.

## Conventions

- Folders name a feature (`Api/Customers`) or a concern (`Validation`), never a kind of code: no `Services/`,
  `Models/`, `Helpers/` or `Extensions/` folders.
- Classes and records are `sealed` unless they are designed as a base.
- Extension methods are extension members (`extension(IServiceCollection services) { ... }`). The only
  `this`-parameter methods are `[LoggerMessage]` partials, which is how all logging is written.
- Use explicit types where `var` would hide what a call returns, and collection expressions (`[]`, `[.. items]`).
- Guard a property in its setter with `field` and the `ThrowIf*` helpers (statically imported in `GlobalUsings.cs`).
- Comments and XML docs say why, not what. Update them when the code they describe changes.

More detail loads from `.claude/rules/` when you work on matching files: `api.md` (WebApi), `domain.md`
(Domain library), `testing.md` (test projects) and `dependencies.md` (packages and build props).

## Before calling a change done

1. `dotnet build -c Release` passes: warnings are errors there, including analyzers and code style.
2. `dotnet test` passes. It needs Docker running for the integration tests; if you couldn't run them, say so.
3. If an entity or its EF configuration changed, a migration was added and `has-pending-model-changes` is clean.

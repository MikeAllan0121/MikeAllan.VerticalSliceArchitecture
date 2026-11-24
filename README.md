# MikeAllan.VerticalSliceArchitecture

A .NET 11 template that shows vertical slice architecture and modern C# in a small but complete invoicing
API. It is meant to be read and copied: every pattern appears in working code, with tests, and comments
explain why it is built that way.

## What it demonstrates

- **Vertical slices.** Each use case is one folder holding its endpoint, request, response and route. There
  are no layers to thread a change through.
- **A rich domain model.** Aggregates enforce their own invariants, with strongly typed IDs, value objects
  and domain events. It lives in its own class library, shared by the API and the seeding tool.
- **Errors as values.** `Result` and `DomainError` are C# unions. Every switch over them is exhaustive, so a
  new kind of error won't compile until the API maps it to a status code. Every error response is RFC 9457
  problem details.
- **Minimal APIs** with `TypedResults`, built-in DataAnnotations validation, an OpenAPI document and the
  Scalar UI.
- **Eventual consistency.** Domain events raised during a request are handled after the response is sent.
- **EF Core 11 on PostgreSQL**, with composable query extension members, sort allow-lists, stable paging and
  automatic auditing.
- **Aspire 17.** One command runs everything locally, and the same AppHost deploys to Azure with managed
  identity instead of passwords.
- **Tests.** xUnit v3 unit tests, and integration tests that drive the real app against PostgreSQL in a
  container.

### Modern C# and .NET on show

| Feature | Where to look |
| --- | --- |
| Union types | `src/Domain/Domain/Errors/Result.cs`, `DomainError.cs` |
| Extension members (`extension(...) { }`) | every `Setup.cs`, `Customers/CustomerQueryableExtensions.cs` |
| `field`-backed property guards | `Customers/Customer.cs`, `Invoices/InvoiceLine.cs` |
| Static abstract interface members | `Aggregates/IGuidId.cs` |
| Primary constructors | `Database/ApplicationDbContext.cs`, the event handler and middleware |
| Collection expressions | throughout |
| Version 7 GUIDs (`Guid.CreateVersion7()`) | the aggregates' `Create` factories |

## The domain

Two features, **Customers** and **Invoices**. An invoice starts as a **Draft**, whose lines can be replaced.
Issuing it makes it **Issued**, and it is owed. Once payments cover the total it becomes **Paid**. A
customer's outstanding balance is the amount still due on their issued invoices. The Invoices feature raises
`InvoiceAmountDueChangedEvent`, and the Customers feature handles it to keep the balance up to date.

| Method | Route | Operation |
| --- | --- | --- |
| `POST` | `/api/customers` | Create a customer |
| `GET` | `/api/customers` | Page through customers (`page`, `pageSize`, `sortBy`, `sortDirection`) |
| `GET` | `/api/customers/{customerId}` | Get a customer with their invoices |
| `PUT` | `/api/customers/{customerId}` | Update a customer |
| `POST` | `/api/invoices` | Create a draft invoice |
| `GET` | `/api/invoices` | Page through invoices |
| `PUT` | `/api/invoices/{invoiceId}` | Replace a draft invoice's lines |
| `POST` | `/api/invoices/{invoiceId}/issue` | Issue a draft invoice |
| `POST` | `/api/invoices/{invoiceId}/payments` | Record a payment |

## Getting started

You need:

- the .NET 11 SDK (the version is pinned in `global.json`)
- Docker, running: Aspire runs PostgreSQL in it, and so do the integration tests
- the Aspire CLI 17: `dotnet tool install -g aspire.cli`

Then, from the repository root:

```bash
aspire run
```

The Aspire dashboard opens. The AppHost starts a PostgreSQL container (kept between runs), applies the
migrations, seeds 20 customers and 40 invoices, and then starts the API. Open the API's URL from the dashboard
and go to `/scalar` to try the endpoints. The OpenAPI document is at `/openapi/v1.json`.

To start again from an empty database, run the **Drop Database** command on the `AppDb` resource in the
dashboard, then restart.

## Tests

```bash
dotnet test
```

The integration tests start their own PostgreSQL container, so Docker must be running. Without it they fail
at startup while the unit tests still pass.

## Project structure

```text
src/
├── Domain/
│   ├── Domain/                    # aggregates, ApplicationDbContext and migrations, events, errors, paging
│   └── Domain.UnitTests/
├── WebApi/
│   ├── WebApi/
│   │   ├── Api/
│   │   │   ├── Customers/         # one folder per slice: CreateCustomer/, GetCustomer/, ...
│   │   │   └── Invoices/
│   │   ├── ErrorHandling/         # cross-cutting concerns, each with its own Setup.cs
│   │   ├── EventualConsistency/
│   │   ├── Validation/
│   │   ├── ...
│   │   ├── Setup.cs               # the top of the wiring tree; Program.cs calls only this
│   │   └── Program.cs
│   ├── WebApi.UnitTests/          # request validation
│   └── WebApi.IntegrationTests/   # the real app against PostgreSQL (Testcontainers)
└── ServiceDefaults/               # OpenTelemetry, health checks, resilience
tools/
├── AppHost/                       # Aspire: local orchestration and Azure deployment
└── Seeder/                        # development seed data, never deployed
```

A slice looks like this:

```text
Api/Invoices/RecordPayment/
├── RecordPaymentEndpoint.cs           # Handle(): load the invoice, call invoice.RecordPayment(), save
├── RecordPaymentRequest.cs            # the body, with its validation rules
└── RouteGroupBuilderExtensions.cs     # MapRecordPayment(): route and OpenAPI metadata
```

Everything is wired explicitly. Each `Setup.cs` calls its direct children's, and nothing is found by
assembly scanning, so the wiring can be read top-down from `Program.cs`.

## Extending it

To add an endpoint, copy the closest existing slice, map it from its feature's `Setup.cs`, and add its tests.
To add an aggregate, register it in `ApplicationDbContext` and add a migration. The full conventions and
checklists are in [`CLAUDE.md`](CLAUDE.md) and [`.claude/rules/`](.claude/rules). Claude Code loads them
automatically, and they work just as well as a guide for people.

### Migrations

The migrations live in the Domain library and the WebApi is the startup project:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Domain/Domain --startup-project src/WebApi/WebApi --output-dir Database/Migrations
```

`aspire run` applies them on start.

## Deploying to Azure

```bash
aspire deploy
```

This provisions an App Service plan (B1) for the API, Azure Database for PostgreSQL Flexible Server with
Microsoft Entra ID authentication through the app's managed identity (no passwords), and Application Insights
backed by Log Analytics. The Seeder is never added outside local runs, so seed data can't reach Azure. Azure
runs PostgreSQL 16, while the local container runs PostgreSQL 18.

**Migrations are a deployment step.** When published, the AppHost doesn't migrate the database. It emits an
EF Core migration bundle under `efmigrations/` (`aspire publish` writes the artifacts without deploying).
Run that bundle against the Azure database, as an identity allowed to change its schema, before the new
version of the API takes traffic.

## License

[MIT](LICENSE)

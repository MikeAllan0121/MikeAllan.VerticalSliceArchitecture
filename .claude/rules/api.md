---
paths:
  - "src/WebApi/WebApi/**"
---

# WebApi: slices and concerns

## Layout and visibility

`Program.cs`, `GlobalUsings.cs`, `appsettings*.json`, `Properties/` and the csproj sit at the project root.
Everything else lives under one `Internals/` folder (never a nested one), and every type there is `internal`:
an executable has no consumers, so nothing in it is public API. `Program.cs` calls only
`Internals/Setup.cs`. The test projects see the internals through `<InternalsVisibleTo Include="..." />` items
in `WebApi.csproj`. `Program` itself is public because ASP.NET Core's source generator emits
`public partial class Program` for top-level statements, which `WebApplicationFactory<Program>` needs.

Namespaces follow folders, so a slice is in `MikeAllan.VerticalSliceArchitecture.Internals.Api.<Feature>.<Slice>`.
Minimal API binding, the validation source generator (`AddValidation`) and the OpenAPI document all work with
`internal` request and response types; nothing needs to be public for them.

## Anatomy of a slice

`Internals/Api/<Feature>/<Slice>/`, named for the use case (`RecordPayment`, not `PaymentEndpoint`):

| File | Shape |
| --- | --- |
| `<Slice>Endpoint.cs` | `internal static class` with one `internal static async Task<...> Handle(...)`. |
| `<Slice>Request.cs` | `internal sealed record` with DataAnnotations; nested line DTOs are `sealed record`s too. Only when there is a body or query. |
| `<Slice>Response.cs` | `internal sealed record` returned in the 2xx, with `sealed` nested records. Only when the slice returns data. |
| `RouteGroupBuilderExtensions.cs` | `internal static class` with `extension(RouteGroupBuilder group)` and `Map<Slice>()`: the route, `WithName("<Slice>")`, `WithSummary`, `WithDescription`, `ProducesValidationProblem()`. |

To add one:

1. Create the folder and files above. Copy the closest existing slice: `CreateCustomer` (create),
   `GetAllCustomers` (paged list), `GetCustomer` (read with projection), `RecordPayment` (change an aggregate).
2. Call `group.Map<Slice>()` from `Internals/Api/<Feature>/Setup.cs`.
3. For a new feature, add `Internals/Api/<Feature>/Setup.cs` with `Map<Feature>()` (its route group,
   `WithTags`, `ProducesProblem(500)`), plus `Add<Feature>()` only if it registers services. Call both from
   `Internals/Api/Setup.cs`.
4. Add the operation name to the expected list in `WebApi.IntegrationTests/OpenApi/ApiDocsTests.cs`, and write
   the request's validation tests and the endpoint's integration tests.

## Handlers

- Inject `ApplicationDbContext` straight into `Handle`. There are no repositories, mediators or application
  services.
- Load an aggregate through its query extension members (`dbContext.Invoices.ById(id).FirstOrDefaultAsync(ct)`),
  call one domain method, then `SaveChangesAsync`.
- Declare every outcome in the return type (`Task<Results<NoContent, ProblemHttpResult>>`) and return
  `TypedResults`, so the OpenAPI document lists each response.
- Not found: `return InvoiceErrors.NotFound.ToProblem();`. Domain failure:
  `if (invoice.Issue() is DomainError error) return error.ToProblem();`.
- Don't catch `DbUpdateConcurrencyException`. `ErrorHandling/ConcurrencyConflictExceptionHandler` turns it
  into a 409 problem with `code` `Concurrency.Conflict` for every endpoint (see `domain.md`).
- Depend on concrete classes. Add an interface only when there are two or more implementations, a test needs a
  fake, a library inverts a dependency on its host, or it is an extension point.
- Route IDs bind as `[NotEmptyGuid] Guid invoiceId` and become strongly typed at once with `InvoiceId.From(...)`.
- Reads project into the response record inside the query (`Select`). Paged lists inherit
  `PagedRequest<TEntity>`, bind with `[AsParameters]`, and return `PagedList<T>` from
  `SortedBy(...).ToPagedListAsync(...)`.

## Request validation

Validation runs before the handler and answers with a 400. Anything the domain throws on that validation
lets through becomes a 500, so:

- Reuse the domain's constants (`[MaxLength(Customer.NameMaxLength)]`), never repeat the number.
- Decimal ranges take string limits parsed in the invariant culture; money also needs `[MaxDecimalPlaces]`.
- Cross-field and query-string rules go in `IValidatableObject.Validate`.
- State-dependent rules ("the invoice is already issued") are the domain's: a 409 from a `DomainError`,
  never a 400 from validation.

## Domain event handlers

A handler lives in the feature that reacts, not the one that raised the event
(`Internals/Api/Customers/InvoiceAmountDueChanged/`). It implements `IDomainEventHandler<TEvent>`, is registered with
`services.AddDomainEventHandler<TEvent, THandler>()` in that feature's `Setup.cs`, gets its own
`ApplicationDbContext` from a new `IServiceScopeFactory` scope, and throws `EventualConsistencyException` for an
expected failure. It runs after the response has been sent, so the only place a failure surfaces is the log.

There is no client to retry a concurrency conflict for a handler, so a handler that saves an aggregate
retries it itself: catch `DbUpdateConcurrencyException`, open a new scope (a fresh context, so it reloads),
and run load, change, save again, up to a fixed number of attempts, then throw
`EventualConsistencyException(ConcurrencyErrors.Conflict)`. `InvoiceAmountDueChangedEventHandler` is the example.

## Cross-cutting concerns

Each concern folder (`Internals/ErrorHandling`, `Internals/Validation`, `Internals/OpenApi`, ...) has its own
`Setup.cs`, called from the top-level `Internals/Setup.cs`. A concern that needs middleware exposes a `Use<Concern>()` that only the top-level
`UseWebApi()` calls.

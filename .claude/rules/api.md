---
paths:
  - "src/WebApi/WebApi/**"
---

# WebApi: slices and concerns

## Anatomy of a slice

`Api/<Feature>/<Slice>/`, named for the use case (`RecordPayment`, not `PaymentEndpoint`):

| File | Shape |
| --- | --- |
| `<Slice>Endpoint.cs` | `public static class` with one `internal static async Task<...> Handle(...)`. |
| `<Slice>Request.cs` | `sealed record` with DataAnnotations. Only when there is a body or query. |
| `<Slice>Response.cs` | `record` returned in the 2xx. Only when the slice returns data. |
| `RouteGroupBuilderExtensions.cs` | `extension(RouteGroupBuilder group)` with `Map<Slice>()`: the route, `WithName("<Slice>")`, `WithSummary`, `WithDescription`, `ProducesValidationProblem()`. |

To add one:

1. Create the folder and files above. Copy the closest existing slice: `CreateCustomer` (create),
   `GetAllCustomers` (paged list), `GetCustomer` (read with projection), `RecordPayment` (change an aggregate).
2. Call `group.Map<Slice>()` from `Api/<Feature>/Setup.cs`.
3. For a new feature, add `Api/<Feature>/Setup.cs` with `Map<Feature>()` (its route group,
   `WithTags`, `ProducesProblem(500)`), plus `Add<Feature>()` only if it registers services. Call both from
   `Api/Setup.cs`.
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
(`Api/Customers/InvoiceAmountDueChanged/`). It implements `IDomainEventHandler<TEvent>`, is registered with
`services.AddDomainEventHandler<TEvent, THandler>()` in that feature's `Setup.cs`, gets its own
`ApplicationDbContext` from a new `IServiceScopeFactory` scope, and throws `EventualConsistencyException` for an
expected failure. It runs after the response has been sent, so the only place a failure surfaces is the log.

## Cross-cutting concerns

Each concern folder (`ErrorHandling`, `Validation`, `OpenApi`, ...) has its own `Setup.cs`, called from the
top-level `Setup.cs`. A concern that needs middleware exposes a `Use<Concern>()` that only the top-level
`UseWebApi()` calls.

---
paths:
  - "src/**/*Tests/**"
---

# Tests

xUnit v3 on Microsoft.Testing.Platform (`global.json`), so test projects are executables. Assert with xUnit's
`Assert` only: no assertion library and no mocking library.

## Shape

- A test file's folder and namespace mirror the code it tests, from below `Internals/`:
  `WebApi.IntegrationTests/Api/Invoices/RecordPayment/RecordPaymentEndpointTests.cs` tests
  `WebApi/Internals/Api/Invoices/RecordPayment/` and is in `...IntegrationTests.Api.Invoices.RecordPayment`.
  `Internals/` is a visibility boundary, not a feature, so test projects don't repeat it.
- The WebApi's types are `internal`; its csproj grants the test projects access with `InternalsVisibleTo`.
  Helpers in a test project are `internal` too, unless xUnit needs them public (test classes, fixtures,
  collection definitions, `IntegrationTestBase`).
- Classes are `public sealed`. Names read `Subject_Condition_ShouldOutcome`
  (`RecordPayment_WhenDraft_ShouldError`, `Post_WhenInvoiceDoesNotExist_ShouldReturnNotFoundProblem`).
- Bodies are marked `// Arrange`, `// Act`, `// Assert`. A comment above a test says why the case matters
  when the name doesn't.

## Which project

| Project | Tests | Helpers |
| --- | --- | --- |
| `Domain.UnitTests` | Aggregate behaviour: state, results, raised events. | `result.ShouldBeSuccess()`; `Assert.Equal(InvoiceErrors.NotDraft, result.ShouldBeError())`. Compare with the catalogue entry, never the leaf record. |
| `WebApi.UnitTests` | Request validation, especially the values at each domain guard's boundary. | `RequestValidation.InvalidMembers(request)`. It doesn't recurse, so validate nested DTOs on their own. |
| `WebApi.IntegrationTests` | Each endpoint through real HTTP against real PostgreSQL: status, body, what was saved. | See below. |

## Integration tests

- Inherit `IntegrationTestBase(fixture)`. All tests share one PostgreSQL container (Testcontainers, so
  Docker must be running) and one in-memory app, and Respawn empties the database before each test.
- Call the API with `Client`. Seed with `AddAsync` / `AddRangeAsync` and `CustomerFactory` / `InvoiceFactory`,
  and read back with `GetQueryable<T>()`, which bypasses the change tracker. `GetPage<T>(url)` reads a paged
  list.
- Assert errors with `ShouldBeProblemAsync(status, "Invoice.NotDraft", ct)` or
  `ShouldBeValidationProblemAsync(ct)`, which returns the `errors` map.
- Pass `CancellationToken` (from `TestContext`) to every async call.
- Domain events raised during a request run after the response, so poll for their effect with
  `Wait.UntilAsync`. Events raised while seeding run inside that `SaveChanges`, so seed the customer before an
  issued invoice.
- To test a lost concurrency race deterministically, create a client with
  `fixture.CreateClient(new ConcurrentWriteInterceptor<TEntity>(fixture.ConnectionString, write))`. Just before
  the app's first save that modifies a `TEntity`, it commits `write` (an `ExecuteUpdateAsync` on the same row)
  from another connection, then lets the save fail on the row version. It fires once, so a retry goes
  through. For the real thing, also race requests with `Task.WhenAll` and assert invariants that hold
  whichever request wins (nothing overpaid, every 204 saved, balances settle).

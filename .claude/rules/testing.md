---
paths:
  - "src/**/*Tests/**"
---

# Tests

xUnit v3 on Microsoft.Testing.Platform (`global.json`), so test projects are executables. Assert with xUnit's
`Assert` only: no assertion library and no mocking library.

## Shape

- A test file's folder and namespace mirror the code it tests:
  `WebApi.IntegrationTests/Api/Invoices/RecordPayment/RecordPaymentEndpointTests.cs` is in
  `...IntegrationTests.Api.Invoices.RecordPayment`.
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

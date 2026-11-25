---
paths:
  - "src/Domain/Domain/**"
---

# Domain library

The WebApi and the Seeder both use this library, so it never references ASP.NET Core. Behaviour that
depends on the host comes in through `ICurrentUserService` (who is saving) and `IDomainEventDispatcher` (when
events are published).

It is a library, so visibility is the reverse of the executables': there is no `Internals/` folder. Its
public types are the surface the WebApi and the Seeder use (aggregates, IDs, error catalogues, query
extension members, `ApplicationDbContext`, the top-level `Setup`), and its infrastructure is `internal`
(EF configurations, interceptors, the immediate dispatcher, `Database/Setup`). Keep new infrastructure
`internal`. Interfaces exist only where one is needed: `ICurrentUserService` and `IDomainEventDispatcher`
invert dependencies on the host, `IDomainEventHandler<TEvent>` is an extension point, and `IGuidId<TSelf>`
gives the ID converter its static `From`.

## Aggregates and entities

- An aggregate is `sealed class X : AggregateRoot<XId>` with a private parameterless constructor for EF
  Core and a static `Create(...)` factory that assigns `XId.From(Guid.CreateVersion7())`.
- IDs are strongly typed: `public readonly record struct XId(Guid Value) : IGuidId<XId>` with
  `public static XId From(Guid value) => new(value);`.
- A child entity (`Payment`) derives from `Entity<TId>` and has an `internal` factory, so only its aggregate
  can create it. Value objects (`InvoiceLine`) are `sealed record`s.
- Collections are a private `List<T>` exposed as `IReadOnlyList<T>`. State changes go through methods.
- Limits are `public const` on the type (`Customer.NameMaxLength`). The EF configuration and the WebApi's
  request validation both read them.
- `CreatedAt/By` and `UpdatedAt/By` are stamped on save by `EntitySaveChangesInterceptor`. Never set them.

## Throw or return

- **Invalid input throws.** Guard it in the property setter with `field` and `ThrowIf*`. The caller should
  never have sent it, so it is a bug, and request validation must reject it first.
- **A business rule that depends on state returns.** `Result` (no value) or `Result<T>` holds `Success` /
  the value, or a `DomainError` from the aggregate's static catalogue (`InvoiceErrors.NotDraft`). Catalogue
  entries are typed `DomainError`, not the leaf record, so they convert to either result.
- To add an error kind, declare a `sealed record XError(string Code, string Description)` in `Errors/` and add
  it to the `DomainError` union. Every switch over `DomainError` then fails to compile (CS8509) until it maps
  the new kind, including the WebApi's `Internals/ErrorHandling/DomainErrorExtensions`. Keep those switches free of `_`
  and `var` arms so this keeps working.
- `Result<T>` stores its case as `object`, so a value-type `T` (an `int`, a strongly typed ID) is boxed: one
  allocation per call. That is nothing per request. Don't return a struct `Result<T>` per item inside a loop;
  return a class, or check the rule once before the loop.

## Queries

- Every query over an aggregate is an extension member on `IQueryable<X>` in `XQueryableExtensions`
  (`dbContext.Invoices.ById(id)`). It returns a query, not results, so the caller composes it and EF Core
  translates the whole chain once. Look there before writing a new one.
- `ById` includes what the aggregate's methods need. List queries skip `Include`: they project, and EF Core
  ignores includes under a projection.
- Sortable columns are the allow-list in `XQueries.SortColumns`.
- A computed property that isn't mapped (`Invoice.AmountDue`) can't appear in a translated query. Spell out
  the expression there (`Total - AmountPaid`).

## Persistence

- Configuration is `internal sealed class XConfiguration : AuditableConfiguration<X>`, overriding
  `PostConfigure`. Money is `HasPrecision(18, 2)`, and the domain guards the matching two decimal places.
- `InvoiceLine` is an owned JSON collection (`OwnsMany(...).ToJson()`), which EF Core 11 reports as obsolete
  (warning 20613). Map a new JSON collection as a complex type collection instead.
- An aggregate's configuration calls `builder.HasRowVersion()` (see Concurrency below).
- Nothing is scanned. A new aggregate needs, in `Database/ApplicationDbContext.cs`: a
  `DbSet<X> Xs => AggregateRootSet<X>();`, an `ApplyConfiguration` line for it and each child entity, and a
  `ConfigureConventions` converter line for every new ID type. A missing converter only fails at startup.
- Then add a migration (commands in `CLAUDE.md`). Never hand-edit the generated `Designer.cs` or the model
  snapshot.

## Concurrency

Handlers load an aggregate, change it in memory and save, so two requests on the same aggregate can both
pass a rule against the same stale state (two payments both under the amount due) and both save. Every
aggregate's configuration therefore calls `builder.HasRowVersion()` (`Concurrency/EntityTypeBuilderExtensions`).

- **Mechanism: optimistic, on PostgreSQL's `xmin`.** It maps a `uint` shadow property `Version` with
  `IsRowVersion()`, which Npgsql binds to the `xmin` system column (the id of the transaction that last wrote
  the row). EF Core adds `AND xmin = @original` to every `UPDATE` of the root row. If another transaction
  changed the row since it was loaded, the update matches nothing and `SaveChanges` throws
  `DbUpdateConcurrencyException`. `SaveChanges` is one transaction, so nothing else from that save (a new
  `Payment` row, say) is committed, and `SavedChanges` never runs, so no domain events are raised.
- **Why optimistic, not `SELECT ... FOR UPDATE`.** Conflicts are rare, it needs no explicit transaction
  around load-change-save (which the retrying execution strategy would also have to wrap), handlers keep
  their plain shape, and `xmin` needs no column or trigger. The migration that added it is model-only:
  Npgsql emits no DDL for a system column.
- **Root rows only.** The token guards the aggregate's own row. An aggregate method must change a root
  property whenever it changes the aggregate (as `RecordPayment` changes `AmountPaid`), not only add a child
  entity, or a concurrent change to the children wouldn't be detected.
- **What callers see.** In the WebApi, `ConcurrencyConflictExceptionHandler` answers it with
  `ConcurrencyErrors.Conflict`: `409 Conflict`, `application/problem+json`, `title` "The resource was changed
  by another request; nothing was saved, so retry the request", `code` `Concurrency.Conflict`. The client may
  resend the same request unchanged; it is re-run against the current state, so it can then succeed or fail
  on a domain rule (e.g. 409 `Invoice.Overpayment`). The server doesn't retry an HTTP request itself.
- **Event handlers retry.** No caller is waiting, so a handler that saves an aggregate catches the
  exception, reloads in a fresh scope and tries again (`InvoiceAmountDueChangedEventHandler`, up to 5
  attempts, then `EventualConsistencyException(ConcurrencyErrors.Conflict)`, which is logged). Because the
  handler that saves last must have loaded after every competing save, the customer's balance ends correct.

## Domain events

An event is a `sealed record XEvent(...) : IDomainEvent`, raised with `AddDomainEvent` inside the aggregate
method, after the state has changed. It is dispatched after `SaveChanges`, and handlers live in the WebApi
feature that reacts to it.

using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

public sealed record InvoiceAmountDueChangedEvent(Invoice Invoice) : IDomainEvent;

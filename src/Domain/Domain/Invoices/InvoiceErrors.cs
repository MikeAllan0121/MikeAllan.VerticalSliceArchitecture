namespace MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

// Entries are typed DomainError, not the leaf record, so each one converts straight to Result and Result<T>.
public static class InvoiceErrors
{
    public static readonly DomainError NotFound = new NotFoundError(
        "Invoice.NotFound",
        "Invoice is not found");

    public static readonly DomainError NotDraft = new ConflictError(
        "Invoice.NotDraft",
        "The invoice has already been issued");

    public static readonly DomainError NoLines = new ConflictError(
        "Invoice.NoLines",
        "The invoice has no lines");

    public static readonly DomainError NotIssued = new ConflictError(
        "Invoice.NotIssued",
        "The invoice is not issued, so it can't take a payment");

    public static readonly DomainError Overpayment = new ConflictError(
        "Invoice.Overpayment",
        "The payment is more than the amount due");
}

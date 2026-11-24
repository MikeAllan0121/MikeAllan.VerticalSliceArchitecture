using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;
using MikeAllan.VerticalSliceArchitecture.Domain.Errors;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Domain.UnitTests.Errors;

namespace MikeAllan.VerticalSliceArchitecture.Domain.UnitTests.Invoices;

public sealed class InvoiceTests
{
    private static readonly CustomerId CustomerId = CustomerId.From(Guid.CreateVersion7());

    [Theory]
    [InlineData("a3ac0d5a-2555-46fa-be04-75a57476e9dd", "d405d1b3-cd2e-428d-9c64-fd06dbb4f9d7", false)]
    [InlineData("a3ac0d5a-2555-46fa-be04-75a57476e9dd", "a3ac0d5a-2555-46fa-be04-75a57476e9dd", true)]
    public void InvoiceId_ShouldBeComparable(string stringGuid1, string stringGuid2, bool isEqual)
    {
        // Arrange
        Guid guid1 = Guid.Parse(stringGuid1);
        Guid guid2 = Guid.Parse(stringGuid2);
        InvoiceId id1 = InvoiceId.From(guid1);
        InvoiceId id2 = InvoiceId.From(guid2);

        // Act
        bool areEqual = id1 == id2;

        // Assert
        Assert.Equal(isEqual, areEqual);
        Assert.Equal(guid1, id1.Value);
        Assert.Equal(guid2, id2.Value);
    }

    [Fact]
    public void Create_ShouldBeADraftWithTheLineTotal()
    {
        // Act
        Invoice invoice = Invoice.Create(CustomerId, [new("Consulting", 2, 150m), new("Hosting", 1, 49.99m)]);

        // Assert
        Assert.Equal(CustomerId, invoice.CustomerId);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Equal(349.99m, invoice.Total);
        Assert.Equal(0m, invoice.AmountPaid);
        Assert.Equal(349.99m, invoice.AmountDue);
        Assert.Equal(2, invoice.Lines.Count);
    }

    [Fact]
    public void Create_WithNoLines_ShouldBeAnEmptyDraft()
    {
        // Act
        Invoice invoice = Invoice.Create(CustomerId, []);

        // Assert
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Empty(invoice.Lines);
        Assert.Equal(0m, invoice.Total);
    }

    // Identical lines are legitimate (two of the same item billed separately), so none is dropped.
    [Fact]
    public void Create_WithIdenticalLines_ShouldKeepEveryLine()
    {
        // Act
        Invoice invoice = Invoice.Create(CustomerId, [new("Consulting", 1, 100m), new("Consulting", 1, 100m)]);

        // Assert
        Assert.Equal(2, invoice.Lines.Count);
        Assert.Equal(200m, invoice.Total);
    }

    [Fact]
    public void Create_ShouldRaiseNoDomainEvent()
    {
        // Act
        Invoice invoice = Invoice.Create(CustomerId, [new("Consulting", 1, 100m)]);

        // Assert — a draft isn't owed, so no balance has changed
        Assert.Empty(invoice.PopDomainEvents());
    }

    [Fact]
    public void ReplaceLines_OnADraft_ShouldReplaceTheLinesAndTotal()
    {
        // Arrange
        Invoice invoice = Invoice.Create(CustomerId, [new("Consulting", 2, 150m), new("Hosting", 1, 50m)]);

        // Act
        Result result = invoice.ReplaceLines([new("Consulting", 1, 150m)]);

        // Assert
        result.ShouldBeSuccess();
        Assert.Equal(150m, invoice.Total);
        Assert.Equal("Consulting", Assert.Single(invoice.Lines).Description);
    }

    [Fact]
    public void ReplaceLines_WhenIssued_ShouldError()
    {
        // Arrange
        Invoice invoice = CreateIssuedInvoice(100m);

        // Act
        Result result = invoice.ReplaceLines([new("Consulting", 1, 999m)]);

        // Assert
        Assert.Equal(InvoiceErrors.NotDraft, result.ShouldBeError());
        Assert.Equal(100m, invoice.Total);
    }

    [Fact]
    public void Issue_ShouldUpdateTheStatus()
    {
        // Arrange
        Invoice invoice = Invoice.Create(CustomerId, [new("Consulting", 1, 100m)]);

        // Act
        Result result = invoice.Issue();

        // Assert
        result.ShouldBeSuccess();
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal(100m, invoice.AmountDue);
    }

    [Fact]
    public void Issue_ShouldRaiseInvoiceAmountDueChangedEvent()
    {
        // Arrange
        Invoice invoice = Invoice.Create(CustomerId, [new("Consulting", 1, 100m)]);

        // Act
        invoice.Issue();

        // Assert
        IReadOnlyList<IDomainEvent> domainEvents = invoice.PopDomainEvents();
        InvoiceAmountDueChangedEvent changed = Assert.IsType<InvoiceAmountDueChangedEvent>(Assert.Single(domainEvents));
        Assert.Equal(invoice.Id, changed.Invoice.Id);
        Assert.Equal(CustomerId, changed.Invoice.CustomerId);
        Assert.Equal(100m, changed.Invoice.AmountDue);
    }

    [Fact]
    public void Issue_WhenAlreadyIssued_ShouldError()
    {
        // Arrange
        Invoice invoice = CreateIssuedInvoice(100m);

        // Act
        Result result = invoice.Issue();

        // Assert
        Assert.Equal(InvoiceErrors.NotDraft, result.ShouldBeError());
    }

    [Fact]
    public void Issue_WithNoLines_ShouldError()
    {
        // Arrange
        Invoice invoice = Invoice.Create(CustomerId, []);

        // Act
        Result result = invoice.Issue();

        // Assert
        Assert.Equal(InvoiceErrors.NoLines, result.ShouldBeError());
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Empty(invoice.PopDomainEvents());
    }

    [Fact]
    public void Issue_AfterAddingLines_ShouldSucceed()
    {
        // Arrange
        Invoice invoice = Invoice.Create(CustomerId, []);
        invoice.ReplaceLines([new("Consulting", 1, 100m)]);

        // Act
        Result result = invoice.Issue();

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public void RecordPayment_WithPartOfTheAmountDue_ShouldReduceItAndStayIssued()
    {
        // Arrange
        Invoice invoice = CreateIssuedInvoice(100m);

        // Act
        Result result = invoice.RecordPayment(40m);

        // Assert
        result.ShouldBeSuccess();
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal(40m, invoice.AmountPaid);
        Assert.Equal(60m, invoice.AmountDue);
        Assert.Equal(40m, Assert.Single(invoice.Payments).Amount);
    }

    [Fact]
    public void RecordPayment_WithTheRestOfTheAmountDue_ShouldMarkTheInvoicePaid()
    {
        // Arrange
        Invoice invoice = CreateIssuedInvoice(100m);
        invoice.RecordPayment(40m);

        // Act
        Result result = invoice.RecordPayment(60m);

        // Assert
        result.ShouldBeSuccess();
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(0m, invoice.AmountDue);
        Assert.Equal([40m, 60m], invoice.Payments.Select(p => p.Amount));
    }

    [Fact]
    public void RecordPayment_ShouldRaiseInvoiceAmountDueChangedEvent()
    {
        // Arrange
        Invoice invoice = CreateIssuedInvoice(100m);
        invoice.PopDomainEvents();

        // Act
        invoice.RecordPayment(25m);

        // Assert
        IReadOnlyList<IDomainEvent> domainEvents = invoice.PopDomainEvents();
        InvoiceAmountDueChangedEvent changed = Assert.IsType<InvoiceAmountDueChangedEvent>(Assert.Single(domainEvents));
        Assert.Equal(75m, changed.Invoice.AmountDue);
    }

    [Fact]
    public void RecordPayment_WhenDraft_ShouldError()
    {
        // Arrange
        Invoice invoice = Invoice.Create(CustomerId, [new("Consulting", 1, 100m)]);

        // Act
        Result result = invoice.RecordPayment(10m);

        // Assert
        Assert.Equal(InvoiceErrors.NotIssued, result.ShouldBeError());
        Assert.Empty(invoice.Payments);
    }

    [Fact]
    public void RecordPayment_WhenAlreadyPaid_ShouldError()
    {
        // Arrange
        Invoice invoice = CreateIssuedInvoice(100m);
        invoice.RecordPayment(100m);

        // Act
        Result result = invoice.RecordPayment(0.01m);

        // Assert
        Assert.Equal(InvoiceErrors.NotIssued, result.ShouldBeError());
    }

    [Fact]
    public void RecordPayment_WithMoreThanTheAmountDue_ShouldErrorAndRecordNothing()
    {
        // Arrange
        Invoice invoice = CreateIssuedInvoice(100m);
        invoice.PopDomainEvents();

        // Act
        Result result = invoice.RecordPayment(100.01m);

        // Assert
        Assert.Equal(InvoiceErrors.Overpayment, result.ShouldBeError());
        Assert.Empty(invoice.Payments);
        Assert.Equal(100m, invoice.AmountDue);
        Assert.Empty(invoice.PopDomainEvents());
    }

    // Invalid input, not a business rule: the request validates the amount first, so this is a throw.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RecordPayment_WithANonPositiveAmount_ShouldThrow(int amount)
    {
        // Arrange
        Invoice invoice = CreateIssuedInvoice(100m);

        // Act
        Exception? exception = Record.Exception(() => invoice.RecordPayment(amount));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public void RecordPayment_WithMoreThanTwoDecimalPlaces_ShouldThrow()
    {
        // Arrange
        Invoice invoice = CreateIssuedInvoice(100m);

        // Act
        Exception? exception = Record.Exception(() => invoice.RecordPayment(10.005m));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public void PopDomainEvents_ShouldDrainTheEvents()
    {
        // Arrange
        Invoice invoice = CreateIssuedInvoice(100m);
        invoice.PopDomainEvents();

        // Act
        IReadOnlyList<IDomainEvent> second = invoice.PopDomainEvents();

        // Assert
        Assert.Empty(second);
    }

    private static Invoice CreateIssuedInvoice(decimal total)
    {
        Invoice invoice = Invoice.Create(CustomerId, [new InvoiceLine("Consulting", 1, total)]);
        invoice.Issue();
        return invoice;
    }
}

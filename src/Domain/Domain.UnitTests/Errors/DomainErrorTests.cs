using MikeAllan.VerticalSliceArchitecture.Domain.Errors;

namespace MikeAllan.VerticalSliceArchitecture.Domain.UnitTests.Errors;

public sealed class DomainErrorTests
{
    [Fact]
    public void CodeAndDescription_ShouldComeFromTheKind()
    {
        // Arrange
        DomainError notFound = new NotFoundError("Thing.NotFound", "Thing is not found");
        DomainError conflict = new ConflictError("Thing.Busy", "Thing is busy");

        // Act & Assert
        Assert.Equal("Thing.NotFound", notFound.Code);
        Assert.Equal("Thing is not found", notFound.Description);
        Assert.Equal("Thing.Busy", conflict.Code);
        Assert.Equal("Thing is busy", conflict.Description);
    }

    [Fact]
    public void Code_OfADefaultError_ShouldThrow()
    {
        // Arrange
        DomainError error = default;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => error.Code);
    }

    [Fact]
    public void Errors_WithTheSameKindAndFields_ShouldBeEqual()
    {
        // Arrange
        DomainError first = new ConflictError("Thing.Busy", "Thing is busy");
        DomainError second = new ConflictError("Thing.Busy", "Thing is busy");
        DomainError otherKind = new NotFoundError("Thing.Busy", "Thing is busy");

        // Act & Assert
        Assert.Equal(first, second);
        Assert.NotEqual(first, otherKind);
    }

    [Fact]
    public void ResultOfT_ShouldHoldEitherTheValueOrTheError()
    {
        // Arrange
        Result<int> success = 42;
        Result<int> failure = (DomainError)new NotFoundError("Thing.NotFound", "Thing is not found");

        // Act & Assert
        Assert.Equal(42, success.ShouldBeSuccess());
        Assert.Equal("Thing.NotFound", failure.ShouldBeError().Code);
    }
}

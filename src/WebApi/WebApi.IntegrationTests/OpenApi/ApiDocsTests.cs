using System.Net;
using System.Text.Json;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.OpenApi;

/// <summary>
/// The OpenAPI document and the Scalar UI are mapped in every environment, and every endpoint appears
/// in the document under its operation ID — a missing one means it was never mapped from a <c>Setup.cs</c>.
/// </summary>
public sealed class ApiDocsTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task OpenApiDocument_ShouldListEveryOperation()
    {
        // Arrange
        string[] expected =
        [
            "CreateCustomer", "CreateInvoice", "GetAllCustomers", "GetAllInvoices", "GetCustomer",
            "IssueInvoice", "RecordPayment", "UpdateCustomer", "UpdateInvoice"
        ];

        // Act
        HttpResponseMessage response = await Client.GetAsync("/openapi/v1.json", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));

        string[] operationIds =
        [
            .. document.RootElement.GetProperty("paths").EnumerateObject()
                .SelectMany(path => path.Value.EnumerateObject())
                .Where(operation => operation.Value.ValueKind == JsonValueKind.Object &&
                                    operation.Value.TryGetProperty("operationId", out _))
                .Select(operation => operation.Value.GetProperty("operationId").GetString()!)
                .Order(StringComparer.Ordinal)
        ];
        Assert.Equal(expected, operationIds);
    }

    [Fact]
    public async Task ScalarUi_ShouldBeServed()
    {
        // Act
        HttpResponseMessage response = await Client.GetAsync("/scalar", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerUi_ShouldBeGone()
    {
        // Act
        HttpResponseMessage response = await Client.GetAsync("/swagger", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

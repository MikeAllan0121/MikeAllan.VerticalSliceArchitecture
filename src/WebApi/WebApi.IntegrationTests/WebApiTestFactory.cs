using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests;

/// <summary>
/// Hosts the real app in memory, wired exactly as <c>Program.cs</c> wires it, pointed at the test database.
/// </summary>
/// <remarks>
/// Only the connection string is overridden. <c>AppDb</c> is the name the domain library's database
/// <c>Setup</c> reads, the same one Aspire injects when the app runs under the AppHost.
/// </remarks>
internal sealed class WebApiTestFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:AppDb", connectionString);
    }
}

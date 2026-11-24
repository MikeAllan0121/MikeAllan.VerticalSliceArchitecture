using MikeAllan.VerticalSliceArchitecture;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddWebApi();

WebApplication app = builder.Build();

app.UseWebApi();

app.Run();

namespace MikeAllan.VerticalSliceArchitecture
{
    // Public so integration tests can host the app with WebApplicationFactory<Program>.
    public partial class Program;
}

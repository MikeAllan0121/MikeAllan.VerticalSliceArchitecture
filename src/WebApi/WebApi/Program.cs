using MikeAllan.VerticalSliceArchitecture.Internals;

// Program is public without a declaration here: for top-level statements ASP.NET Core's source generator
// emits `public partial class Program`, which is what lets the integration tests host the app with
// WebApplicationFactory<Program>.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddWebApi();

WebApplication app = builder.Build();

app.UseWebApi();

app.Run();

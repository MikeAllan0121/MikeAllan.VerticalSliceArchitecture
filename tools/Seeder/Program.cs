using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Seeder;
using Seeder.Initializers;
using MikeAllan.VerticalSliceArchitecture.Domain;
using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;
using MikeAllan.VerticalSliceArchitecture.ServiceDefaults;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<Worker>();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.Services.AddScoped<ApplicationDbContextInitializer>();

// The domain library audits every save with the current user; for the seeder that's a fixed identity.
builder.Services.AddSingleton<ICurrentUserService, SeederUserService>();

// The DbContext, its interceptors and domain-event dispatch. No HTTP request here, so events raised
// while seeding are published immediately (and nothing in the seeder handles them).
builder.AddDomain();

IHost host = builder.Build();

await host.RunAsync();

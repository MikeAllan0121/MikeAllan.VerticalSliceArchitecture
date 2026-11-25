using Microsoft.Extensions.Hosting;
using MikeAllan.VerticalSliceArchitecture.Seeder.Internals;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddSeeder();

IHost host = builder.Build();

await host.RunAsync();

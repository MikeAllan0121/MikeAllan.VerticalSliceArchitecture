---
paths:
  - "Directory.Packages.props"
  - "Directory.Build.props"
  - "global.json"
  - ".config/dotnet-tools.json"
  - "**/*.csproj"
---

# Packages and build settings

- Versions live only in `Directory.Packages.props`. A `.csproj` has `<PackageReference Include="..." />` with no
  version. Put a new version in the item group for the projects that use it (`IsAspireSharedProject`,
  `IsAspireHost`, `IsTestProject`, or the unconditional group).
- Shared properties (target framework, nullable, analyzers, warnings) live only in `Directory.Build.props`.
  Don't repeat them in a project.
- Prefer stable releases. Any version that isn't the obvious latest stable (a prerelease, a pin, a version held
  in step with another) gets an XML comment saying why and what lets it be removed or bumped.
- To override a vulnerable or conflicting transitive package, add a `PackageVersion` for it.
  `CentralPackageTransitivePinningEnabled` applies it without a direct `PackageReference`.
- Bump these together:
  - The `dotnet-ef` tool (`.config/dotnet-tools.json`), `Microsoft.EntityFrameworkCore.Design`, and the EF Core
    version `Npgsql.EntityFrameworkCore.PostgreSQL` is built against.
  - All `Aspire.*` packages, and the Aspire CLI that runs the AppHost. The preview-only Aspire packages carry
    the stable version's build number.
- Release builds treat warnings as errors, and nullable warnings are errors in every configuration. Fix a
  warning rather than suppress it. If a suppression is right, make it local (`#pragma warning disable` around
  the line, with the reason), not a project-wide `NoWarn`.
- After a change: `dotnet build -c Release`, `dotnet test`, and
  `dotnet list package --vulnerable --include-transitive`.

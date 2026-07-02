# Agent Instructions

Guidance for AI coding agents (e.g. GitHub Copilot CLI) working in this repository.

## Build

SDK-style .NET 10 solution (`SHLAnalytics.sln`) using Central Package Management.

- `dotnet restore` — restore packages. NuGetAudit runs here and **fails the restore** on
  any known-vulnerable package (warnings are treated as errors).
- `dotnet build` — build the whole solution.
- `dotnet build --no-restore -c Debug` — build without re-restoring (after a restore).
- `dotnet build <Project>/<Project>.csproj` — build a single project.

## Test

Tests live in `Shuttle.Tests` (xunit.v3, run via the Microsoft Testing Platform runner).

- `dotnet test` — run the full test suite.
- `dotnet test Shuttle.Tests/Shuttle.Tests.csproj` — run just the test project.
- `dotnet test --filter "FullyQualifiedName~<Namespace.Class>"` — run a targeted subset.

## Dependency management

This solution uses **Central Package Management** — all package versions are declared
in `Directory.Packages.props` via `<PackageVersion>` elements, and projects reference
packages with version-less `<PackageReference Include="..." />`.

### Prefer upgrading parent packages over pinning transitive dependencies

When a vulnerable or outdated package is pulled in **transitively**, fix it by upgrading
the package that depends on it (the parent) rather than adding an explicit pinned
`<PackageReference>` for the transitive package.

- Trace the dependency chain first (e.g. `dotnet nuget why <project> <package>`).
- Upgrade the nearest parent whose newer version references a patched/updated dependency.
- Only pin a transitive package directly when **no parent upgrade resolves the issue**
  (e.g. the parent is version-locked and still ships the vulnerable dependency). Treat
  pins as a temporary exception and revisit them once the parent ships a fix.

### Make related package versions explicit with a shared MSBuild property

When several packages must move together (same version family), declare a single
version property in the `<PropertyGroup>` of `Directory.Packages.props` and reference it
from each `<PackageVersion>`, instead of repeating the literal version.

Example:

```xml
<PropertyGroup>
  <EntityFrameworkVersion>10.0.9</EntityFrameworkVersion>
  <MicrosoftIdentityWebVersion>4.12.0</MicrosoftIdentityWebVersion>
  <MudBlazorVersion>9.5.0</MudBlazorVersion>
</PropertyGroup>
<ItemGroup>
  <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="$(EntityFrameworkVersion)" />
  <PackageVersion Include="Microsoft.Identity.Web" Version="$(MicrosoftIdentityWebVersion)" />
  <PackageVersion Include="Microsoft.Identity.Web.UI" Version="$(MicrosoftIdentityWebVersion)" />
  <PackageVersion Include="MudBlazor" Version="$(MudBlazorVersion)" />
  <PackageVersion Include="MudBlazor.Extensions" Version="$(MudBlazorVersion)" />
</ItemGroup>
```

When declaring or updating package versions, introduce a property for any group of
related packages so the relationship is explicit and updates stay consistent.

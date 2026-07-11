# Agent Instructions

Guidance for AI coding agents (e.g. GitHub Copilot CLI) working in this repository.

## Solution architecture

SHLAnalytics provides information and statistics — through the `Shuttle.WebClient` front
end — about players and teams in the **Simulation Hockey League (SHL)**, a roleplaying
simulation hockey league that uses **Franchise Hockey Manager** (a hockey management game)
to simulate matches between teams. The backend ingests data from the league's public
websites, stores it in Azure SQL, and serves it to the front end.

### Upstream SHL data sources

- **Forum** — `simulationhockey.com`: the community forum where the roleplaying league
  activity happens.
- **Portal** — `portal.simulationhockey.com`: the player portal.
- **Index** — `index.simulationhockey.com`: season-by-season competition/statistics data.

`Shuttle.Shl.Api.Client` provides typed clients for the **Index** and **Portal** APIs, and
`Shuttle.Api` periodically pulls from them (via its background Quartz jobs) to keep the
database current.

### Competitions

The broader game includes several competitions that this solution reports on:

- **SHL** — the top-level Simulation Hockey League. Its draft and roster management
  processes work similarly to its real-world analogue, the NHL.
- **SMJHL** — the junior league, which acts as a feeder league for the SHL. Newly created
  players are drafted into the SMJHL first; after their first SMJHL season they become
  eligible for the SHL draft. A player can play at most 4 seasons in the SMJHL.
- **IIHF** — an international competition, similar to the real-world IIHF World
  Championship. Available to SHL players.
- **WJC** — the World Junior Championship, an international competition. Available to
  SMJHL-eligible players.

For both the IIHF and WJC, player eligibility is determined at player creation by selecting
an international nation to represent, though this selection may change under a few
circumstances.

### Domain concepts / glossary

- **TPE** — the core progression currency for a player. Members
  earn TPE by completing on-site activities and spend it to improve the attributes of their
  in-game (Franchise Hockey Manager) player.
- **In-game currency** — a separate currency also earned through on-site activity. It can be
  spent on things like training, which in turn earns more TPE.
- **Player** — a member's roleplayed athlete, whose simulated performance and progression
  (via TPE) this solution reports on alongside team statistics.

### Project goals / use cases

A key goal of this project is to provide **statistical analysis of how likely a player is to
succeed if drafted**, by analyzing their in-game stats together with collected information
about their income and site activity. Much of this underlying information is available
programmatically through the SHL APIs (Index and Portal).

Another goal is to provide **"at a glance" stat cards** for players that reflect their
current impact and potential.

A primary use case is providing information useful to teams **scouting players
and planning their draft lists**. Scouting draftees draws on several sources of information:

- **Member interviews** — typically conducted over Discord.
- **Player stats** — reviews of on-ice statistical performance.
- **Community activity** — member activity in the community forums and Discord servers.
- **Onsite income** — reviews of a member's onsite income and cash reserves.

Because income can be spent to improve players, members with high income or large cash
reserves are typically viewed as more likely to succeed at the SHL level, making income a
meaningful scouting signal.

Determining which **player archetype** a player best fits also helps drafting teams judge
whether a player suits their roster. For example, a team may be looking for a *two-way
player* who is responsible both offensively and defensively, or a *playmaker* with
excellent passing and speed.

Teams also consider whether a player is a **first-gen** (a new member with their first-ever
player) or a **recreate** (a new player created by a member who has usually already been in
the league for a few seasons).

There are two **shipped** projects, backed by several shared libraries and an Aspire
orchestration host.

### Shipped projects

- **`Shuttle.Api`** — ASP.NET Core Web API (`Microsoft.NET.Sdk.Web`) that exposes the
  backend API consumed by `Shuttle.WebClient` **and** hosts the scheduled background jobs.
  Controllers live in `Shuttle.Api/Controllers`; supporting logic in `Shuttle.Api/Services`.
  Authenticates incoming API requests as a protected API using JWT bearer tokens via
  `Microsoft.Identity.Web` (Entra ID, `AzureAd` config section) — this is the **default**
  authentication scheme. CORS origins are read from `Shuttle:AllowedCorsOrigins` and are
  **required in production**. Reads data through `Shuttle.EFCore` (EF Core + linq2db).

  Background jobs run in-process via **Quartz.NET** with a persistent SQL Server job store
  (table prefix in `ShuttleEfCoreConstants.QuartzTablePrefix`). Jobs live in
  `Shuttle.Api/Jobs` (namespace `Shuttle.Api.Jobs.Jobs`) and self-register through the
  `ISelfRegisteringJob` interface (see `DbUpdateJob`, which runs every 6 hours and drives
  `IndexUpdater`/`PortalUpdater`). Ships the **CrystalQuartz** dashboard at `/quartz`, gated
  by the `Shuttle.Jobs.Admin` role via interactive OpenID Connect sign-in (`QuartzDashAuth`
  config section, registered as a non-default scheme so it does not affect JWT or anonymous
  access to the API endpoints).
- **`Shuttle.WebClient`** — Blazor WebAssembly **Standalone** front end
  (`Microsoft.NET.Sdk.BlazorWebAssembly`). Built with **MudBlazor**. Authenticates users
  with MSAL (`Microsoft.Authentication.WebAssembly.Msal`, `AzureAd` config) and calls
  `Shuttle.Api` over `HttpClient`. Pages are in `Shuttle.WebClient/Pages`, reusable UI in
  `Components`/`Layout`, client-side state (e.g. local-storage-backed options) in
  `Services`.

### Shared / supporting projects

- **`Shuttle.EFCore`** — EF Core data layer: `ShlDbContext`, entities, migrations,
  stored-procedure/updater logic (`Procedures/`), and the `AddShuttleDatabase` /
  `EnsureShuttleDatabaseConnectivity` host extensions. Uses **Azure SQL** with
  `ActiveDirectoryDefault` auth and layers **linq2db** (`linq2db.EntityFrameworkCore`)
  on top of EF Core. Both server-side entry points live in `Shuttle.Api`, which depends on it.
- **`Shuttle.ServiceDefaults`** — shared Aspire service defaults: OpenTelemetry wiring,
  health endpoints (`/health`, `/alive`), and `MapDefaultEndpoints`/`AddServiceDefaults`.
  Referenced by the server project.
- **`Shuttle.Shl.Api.Client`** / **`Shuttle.Shl.Api.Models`** — typed clients and DTOs for
  the upstream SHL Index and Portal APIs.
- **`Shuttle.Models`**, **`Shuttle.Core`**, **`Shuttle.EloCalc`**, **`Shuttle.Math`** —
  domain models and shared utility/calculation libraries.
- **`Shuttle.Backend.Aspire`** — .NET Aspire AppHost that orchestrates `shuttle-api`,
  wiring it to Azure SQL and Application Insights and publishing it
  as an Azure App Service website. Run this project for local orchestration.
- **`Shuttle.Tests`** — the test project (see Test section below).

### Cross-cutting notes

- Authentication everywhere is **Entra ID via `Microsoft.Identity.Web`**: the API validates
  JWTs, the Jobs dashboard uses interactive OpenID Connect, and the WebClient uses MSAL.
- Observability is via **OpenTelemetry**, configured centrally in `Shuttle.ServiceDefaults`
  and per-project `ActivitySources`.

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

## Skills

When asked to create a skill, place it in the repository (e.g.
`.github/skills/<skill-name>/SKILL.md`) so it is version-controlled and shared with
collaborators, unless the request explicitly says otherwise.

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

A key goal of this project is to provide \*\*statistical analysis of how likely a player is to
succeed if drafted\*\*, by analyzing their in-game stats together with collected information
about their income and site activity. Much of this underlying information is available
programmatically through the SHL APIs (Index and Portal).

Another goal is to provide **"at a glance" stat cards** for players that reflect their
current impact and potential.

A primary use case is providing information useful to teams \*\*scouting players
and planning their draft lists\*\*. Scouting draftees draws on several sources of information:

- **Member interviews** — typically conducted over Discord.
- **Player stats** — reviews of on-ice statistical performance.
- **Community activity** — member activity in the community forums and Discord servers.
- **Onsite income** — reviews of a member's onsite income and cash reserves.

Because income can be spent to improve players, members with high income or large cash
reserves are typically viewed as more likely to succeed at the SHL level, making income a
meaningful scouting signal.

Determining which **player archetype** a player best fits also helps drafting teams judge
whether a player suits their roster. For example, a team may be looking for a \*two-way
player\* who is responsible both offensively and defensively, or a *playmaker* with
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
  (`Microsoft.NET.Sdk.BlazorWebAssembly`). Built with **Fluent UI Blazor**
  (`Microsoft.FluentUI.AspNetCore.Components`). Authenticates users
  with MSAL (`Microsoft.Authentication.WebAssembly.Msal`, `AzureAd` config) and calls
  `Shuttle.Api` over `HttpClient`. Pages are in `Shuttle.WebClient/Pages`, reusable UI in
  `Components`/`Layout`, client-side state (e.g. local-storage-backed options) in
  `Services`. See the [Shuttle.WebClient structure](#shuttlewebclient-structure) section
  below for a full breakdown.

  **Styling:** avoid adding custom CSS (e.g. new rules in `wwwroot/css/app.css` or
  scoped `.razor.css` files) — prefer Fluent UI component parameters and built-in
  tokens/utilities. Small amounts of inline CSS (e.g. a `style="..."` for minor
  spacing/alignment tweaks) are permissible when a component parameter doesn't cover it.

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
- **`Shuttle.WebClient.Shared`** — a **Razor Class Library** shared by the WebClient (WASM) and
  `Shuttle.Api` (server). Owns the reusable SEO / social-embed head component
  (`Meta/MetaTags.razor` + `Meta/MetaDocument.razor`, driven by the `Meta/PageMetadata` record)
  and the blog engine (`Blogs/BlogService`, `IBlogService`, `BlogEntry`, plus the embedded
  `BlogEntries/*.md`). The API server-side-renders `MetaDocument` to an HTML string (via
  `HtmlRenderer`) for the anonymous `GET /meta/{**path}` endpoint (see `Shuttle.Api/Meta/`),
  which crawlers / Discord's unfurler hit; the WebClient reuses `MetaTags` via `<HeadContent>`.
  Uses the WASM-compatible `Microsoft.AspNetCore.Components.Web` package (not a framework
  reference) so it builds for `browser-wasm`.
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

### Shuttle.WebClient structure

The front end is a **standalone Blazor WebAssembly** app — there is no server-side host;
everything runs in the browser and talks to `Shuttle.Api` over HTTP. Component code lives
in `.razor` files, most with a matching `.razor.cs` code-behind and (where needed) a scoped
`.razor.css`.

- **`Program.cs`** — WebAssembly host bootstrap. Reads the API base URL from `Api:BaseUrl`
  (supplied by `wwwroot/appsettings*.json`), registers a base-address `HttpClient`, the
  typed Refit API client (`AddShuttleApiClient`), MSAL authentication
  (`AddMsalAuthentication`, roles from the `roles` claim via `ArrayClaimsPrincipalFactory`),
  Fluent UI (`AddFluentUIComponents`), local-storage services, and the singleton app
  services (`IBlogService`, `IShuttleOptionsStorage`, `IPlayerDirectoryService`).
- **`App.razor`** — root router wrapped in `CascadingAuthenticationState`. Uses
  `AuthorizeRouteView` with `MainLayout`; unauthenticated users hit `RedirectToLogin`, and
  authenticated-but-unauthorized users get an alert. `NotFoundPage` routes to `Pages/NotFound`.
- **`Pages/`** — routable page components (each with an `@page` route). Top level:
  `Home`, `Blogs`/`BlogPost`, `Privacy`, `Authentication` (MSAL callback handling),
  `Claims`/`Roles` (auth debug), `NotFound`. Subfolders: `Pages/Players/`
  (`PlayerProfile`, `PlayerSearch`) and `Pages/Admin/` (`HelloApi`).
- **`Components/`** — reusable, non-routable UI. Top level: `RoleBadge`, `SeasonNumber`,
  `ShuttleLogo`, `BackToTopButton` (with a collocated `.razor.js` interop module).
  Subfolders: `Options/` (the `ShuttleOptions*` dialog/button/context for user preferences),
  `Players/` (`PlayerCardTable`, `PlayerSearchFilters`), and `Dev/` (`AccountView`,
  `HelloApiView`).
- **`Layout/`** — app shell: `MainLayout`, `ShuttleNavMenu`, `LoginDisplay`, and
  `RedirectToLogin`.
- **`Services/`** — client-side services and state. `PlayerDirectoryService` fetches the slim
  player suggestion directory once and caches it in memory + `localStorage` for local
  autocomplete; `ShuttleOptionsLocalStorage`/`IShuttleOptionsStorage` persist user options;
  `ArrayClaimsPrincipalFactory` expands the array-valued `roles` claim. (The blog engine —
  `IBlogService`/`BlogService`, `BlogEntry`, and the `BlogEntries/*.md` articles — lives in the
  shared `Shuttle.WebClient.Shared` library so the API can render blog meta too.)
- **`Models/`** — client-side models and constants: `Routes` (typed route/URL constants —
  prefer these over hard-coded paths), `KnownRoles`, and `Options/`
  (`IShuttleOptions`, `ShuttleOptions`, `ShuttleOptionsModel`).
- **`Extensions/`** — `ClaimsPrincipalExtensions` and other small helpers.
- **`wwwroot/`** — static web assets: `index.html`, `appsettings.json` +
  `appsettings.Development.json` (the latter points `Api:BaseUrl` at the local dev API),
  `css/`, `js/`, and icons.

**Data access** goes exclusively through the shared `Shuttle.Api.Client` project's typed
Refit `IShuttlePlayerClient` (registered in `Program.cs`); the WebClient does not build
requests by hand. DTOs are the shared `Shuttle.Models` types.

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

### WebClient testing (no Azure auth)

The Blazor WebClient can be exercised **without Entra ID / MSAL or a live `Shuttle.Api`** via a
shared, deterministic in-memory backend. Three projects support this:

- **`Shuttle.WebClient.Testing`** — the single source of offline truth, reused by the app's
  run mode and both test projects:
    - `SeedData` — a fixed set of `PlayerCard` / `PlayerSuggestion` records (stable IDs and
    ordering, `PlayerId` starting at 1001, ordered by name).
    - `InMemoryShuttlePlayerClient : IShuttlePlayerClient` — a hand-written fake that mirrors the
    server's filter/sort/paging semantics (text `Contains` on name/username, position short
    codes, page size clamped to 100, `PlayerId` tiebreak).
    - `FakeAuthenticationStateProvider` + `FakeAuthOptions` — a configurable signed-in principal
    (name/roles, defaults to the `Shuttle.Admin` role) with no token round trip.
    - `AddShuttleFakeBackend(...)` — DI extension registering the fake client and fake auth.

- **`Shuttle.WebClient.Tests`** — **bUnit** component/page tests (headless, offline, no browser).
  `WebClientTestContext` wires FluentUI services, the in-memory client, and
  `PlayerDirectoryService`; bUnit's `AddAuthorization()` drives auth-gated UI. Fast to run:
    - `dotnet test Shuttle.WebClient.Tests/Shuttle.WebClient.Tests.csproj`

- **`Shuttle.WebClient.E2E`** — **Playwright** browser smoke tests that drive the fully rendered
  app in its offline **fake-backend run mode**. `WebAppFixture` boots the WebClient via
  `dotnet run --launch-profile TestServer` (fixed at `http://localhost:5099`), waits for
  readiness, and tears the process down. Set `SHUTTLE_E2E_BASEURL` to point the tests at an
  already-running server instead of starting one.
    - One-time browser install:
    `pwsh Shuttle.WebClient.E2E/bin/Debug/net10.0/playwright.ps1 install chromium`
    - `dotnet test Shuttle.WebClient.E2E/Shuttle.WebClient.E2E.csproj`

**Fake-backend run mode.** `Program.cs` reads the `Testing:FakeBackend` flag; when `true` it calls
`AddShuttleFakeBackend` and **skips** MSAL/Refit registration. The flag lives only in
`wwwroot/appsettings.Testing.json` and is loaded when the app's environment is `Testing`. Because a
standalone .NET 10 WASM app resolves its environment from the boot manifest baked at build time (the
dev server does not emit a reliable `Blazor-Environment` header), the WebClient csproj bakes
`WasmApplicationEnvironmentName=Testing` whenever `-p:ShuttleFakeBackend=true` is passed **or**
`ASPNETCORE_ENVIRONMENT=Testing` is set (the `TestServer` launch profile). Run it locally with:

````javascript
dotnet run --project Shuttle.WebClient/Shuttle.WebClient.csproj --launch-profile TestServer -p:ShuttleFakeBackend=true
```

The property is never set in normal Debug/Release builds, so the production app is unaffected even
though the fake code ships in the bundle.


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
````
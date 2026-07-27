# Copilot instructions for SHLAnalytics

Analytics and research tooling for the **Simulation Hockey League (SHL)**, a roleplaying
league that uses *Franchise Hockey Manager* to simulate matches. The backend ingests data
from the league's public sites (Forum, Portal, Index), stores it in Azure SQL, and serves it
to a Blazor front end that helps scouts research and draft players.

`AGENTS.md` is the authoritative deep-dive (full architecture, domain glossary, per-project
breakdown, WebClient structure). Read it for anything not covered here; keep the two files
consistent when you change one. `Shuttle.Analysis/README.md` documents the CLI tool.

## Build, test, lint

.NET 10 SDK-style solution (`SHLAnalytics.sln`) using **Central Package Management**.

- `dotnet restore` — restore. NuGetAudit runs here and **fails restore** on any known
  vulnerable package (audit warnings are errors).
- `dotnet build` / `dotnet build --no-restore -c Debug` — build all / after a restore.
- `dotnet build <Project>/<Project>.csproj` — build one project.
- `dotnet test` — full suite (`Shuttle.Tests`, xunit.v3 via Microsoft Testing Platform).
- `dotnet test Shuttle.Tests/Shuttle.Tests.csproj` — just the main test project.
- **Single test / subset:** `dotnet test --filter "FullyQualifiedName~<Namespace.Class>"`.
- **Always run the full test suite (`dotnet test`) immediately before pushing** — regardless of
  how targeted your changes were.
- **WebClient tests (offline, no Azure auth):**
  - bUnit: `dotnet test Shuttle.WebClient.Tests/Shuttle.WebClient.Tests.csproj`
  - Playwright E2E: `dotnet test Shuttle.WebClient.E2E/Shuttle.WebClient.E2E.csproj`
    (one-time: `pwsh Shuttle.WebClient.E2E/bin/Debug/net10.0/playwright.ps1 install chromium`).
- **`TreatWarningsAsErrors=true` solution-wide** (`Directory.Build.props`) — builds fail on
  any warning. `Nullable` is enabled and `LangVersion=latest`.

## Architecture essentials

Two shipped apps over shared libraries, orchestrated by an Aspire AppHost.

- **`Shuttle.Api`** — ASP.NET Core Web API **and** the host for background ingestion jobs.
  Jobs run in-process via **Quartz.NET** (persistent SQL job store) in `Jobs/`, self-register
  through `ISelfRegisteringJob`, and pull from the upstream SHL Index/Portal APIs
  (`Shuttle.Shl.Api.Client`) to keep the DB current. Ships the CrystalQuartz dashboard at
  `/quartz`. Authenticates API requests with JWT bearer (Entra ID, default scheme).
- **`Shuttle.WebClient`** — standalone Blazor WebAssembly front end using **Fluent UI Blazor**
  and MSAL. No server host; it calls `Shuttle.Api` over HTTP.
- **`Shuttle.EFCore`** — data layer: `ShlDbContext`, entities, migrations. **Azure SQL** with
  `ActiveDirectoryDefault` auth, layering **linq2db** on top of EF Core.
- **`Shuttle.WebClient.Shared`** — Razor Class Library shared by WASM client and API (SEO meta
  component + blog engine); must stay `browser-wasm`-compatible.
- **`Shuttle.Analysis`** — `System.CommandLine` console tool: exports `PlayerInformation` and
  runs pluggable ML.NET **analysis flows** (see its README and `.github/skills/`).

Cross-cutting: auth everywhere is **Entra ID via `Microsoft.Identity.Web`**; observability is
**OpenTelemetry**, configured centrally in `Shuttle.ServiceDefaults`.

## Conventions

- **WebClient data access goes exclusively through the typed Refit `IShuttlePlayerClient`**
  (in `Shuttle.Api.Client`); never hand-build HTTP requests. DTOs are `Shuttle.Models` types.
- **Use the typed `Routes` constants** in `Shuttle.WebClient/Models` rather than hard-coded paths.
- **Avoid custom CSS** in the WebClient — prefer Fluent UI component parameters and built-in
  tokens/utilities; small inline `style="..."` tweaks are acceptable when no parameter fits.
- **Dependency versions:** declare all versions in `Directory.Packages.props`; projects use
  version-less `<PackageReference>`. Fix transitive vulnerabilities by upgrading the parent
  package, not by pinning the transitive one. Group related packages behind a shared MSBuild
  version property (see `AGENTS.md`).
- **Style (`.editorconfig`):** accessibility modifiers are required; no `this.` qualification;
  prefer framework type keywords (`int`, not `Int32`); modifier order per
  `csharp_preferred_modifier_order`. Concrete types are typically `sealed`.
- **Skills** go in `.github/skills/<name>/SKILL.md` so they are version-controlled.

## Tooling

Prefer purpose-built tooling over raw shell commands:

- **C# LSP / code intelligence** for navigation and edits (definitions, references, call
  hierarchy, rename, symbol search) instead of `grep`/text search for symbols.
- **Installed skills** — use any available skill (repository `.github/skills` and plugin/user
  skills, e.g. testing, coverage/CRAP, MSBuild, EF Core, performance, testability) for its
  specialized workflow instead of ad-hoc commands. In particular, use the **`querying-json`**
  skill when manipulating or analyzing JSON data (querying, filtering, or extracting fields)
  rather than reading whole JSON files.
- **Relevant MCP servers** — Fluent UI Blazor, NuGet, Microsoft Learn, MSBuild binlog, GitHub —
  when they fit the task, before falling back to raw `dotnet`/`git`/HTTP commands.

Fall back to raw commands only when no LSP/skill/MCP capability covers the need.

## Memory

After completing a complex analysis or investigation, in addition to storing any memories you
decide to record yourself, **ask the user whether a repository-scoped memory should be stored**
to capture durable findings (architecture facts, non-obvious conventions, verified commands) for
future sessions.

## Analysis flows (`Shuttle.Analysis/Flows`)

Add a scenario by deriving from `AnalysisFlowBase` and registering it in
`AnalysisFlowRegistry.CreateDefault()`. A flow's `DataSource` (`FlowDataSource`) selects its
input: `Csv` consumes the pre-ingested `--input` export via `AnalysisContext.Data`; `Database`
pulls from `ShlDbContext` during `RunAsync` via the scoped `AnalysisContext.Services`
(`GetRequiredService<T>()`). Only `Database` flows require Azure SQL access / `az login`.

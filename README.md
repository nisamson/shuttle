# The Shuttle Project

![GitHub License](https://img.shields.io/github/license/nisamson/shuttle)

The **Sh**utt**l**e project is a set of analytics and research tooling for the **Simulation Hockey League (SHL)** — a roleplaying
simulation hockey league that uses *Franchise Hockey Manager* to simulate matches. The
backend ingests data from the league's public sites (Forum, Portal, and Index), stores it
in Azure SQL, and serves it to a web front end.

## But for why?

Hyperfocus project.

## But for what?

### Data analysis

Pull player and team data from the SHL **Index** and **Portal** APIs and turn it into
statistical insight — for example, estimating how likely a player is to succeed if drafted,
generating "at a glance" stat cards, and running ML.NET **analysis flows** over exported
data (see [`Shuttle.Analysis`](Shuttle.Analysis/README.md)).

### League research portal

A Blazor front end for browsing and researching the league, built to make **drafting easier
for scouts**. It surfaces the signals teams care about when building draft lists — player
stats and archetypes, community activity, onsite income, and whether a player is a first-gen
or a recreate — so scouts can evaluate draftees at a glance.

## But for how?

**Shipped apps**

- **`Shuttle.Api`** — ASP.NET Core Web API that serves the backend and hosts scheduled
  background ingestion jobs (Quartz.NET, persistent SQL job store). A scheduled job
  mirrors public game files from `https://simulationhockey.com/games/` into
  [`shuttle-shl/shl-games-archive`](https://github.com/shuttle-shl/shl-games-archive).
- **`Shuttle.WebClient`** — Blazor WebAssembly (standalone) front end built with Fluent UI Blazor.

**Shared libraries**

- **`Shuttle.EFCore`** — EF Core + linq2db data layer over Azure SQL (`ShlDbContext`,
  entities, migrations, updater logic).
- **`Shuttle.Shl.Api.Client`** / **`Shuttle.Shl.Api.Models`** — typed clients and DTOs for
  the upstream SHL Index and Portal APIs.
- **`Shuttle.Analysis`** — ML.NET analysis flows over exported player data.
- **`Shuttle.Models`**, **`Shuttle.Core`**, **`Shuttle.EloCalc`**, **`Shuttle.Math`** —
  domain models and shared calculation utilities.
- **`Shuttle.ServiceDefaults`** — shared Aspire service defaults (OpenTelemetry, health
  endpoints).
- **`Shuttle.GameArchive`** — shared crawling, Key Vault credentials, and Git snapshot
  synchronization for the production job and local runner.

**Orchestration & tests**

- **`Shuttle.Backend.Aspire`** — .NET Aspire AppHost that orchestrates the backend and
  wires it to Azure SQL and Application Insights.
- **`Shuttle.Tests`** — the test suite (xUnit).
- **`Shuttle.GameArchive.Runner`** — one-shot local archive executable, launched by the
  AppHost's `ArchiveOnly` profile without the API or SQL stack.

Authentication throughout uses Entra ID via `Microsoft.Identity.Web`, and observability is
provided by OpenTelemetry.

### Game-file archive

The game-file archive runs by default in the production AppHost; the ordinary local
API requires explicit enablement. For an isolated, one-shot sync of the real SHL
source to local Gitea, with Docker running:

```powershell
dotnet run --project Shuttle.Backend.Aspire\Shuttle.Backend.Aspire.csproj --launch-profile ArchiveOnly
```

This profile needs no Azure login or SQL. See
[`docs/game-archive.md`](docs/game-archive.md) for local Gitea + Key Vault Emulator
development, production Key Vault setup, and safe activation.
Non-Apache HTML directories are documented in the archive's
`manifest.json`'s `skippedDirectories` and excluded from the current snapshot;
previously archived content remains in Git history.

## But for where?

Here!

## But for–

Why do you ask so many questions? Are you a cop?
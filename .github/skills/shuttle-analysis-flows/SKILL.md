---
name: shuttle-analysis-flows
description: >
  Use the Shuttle.Analysis CLI to export SHL PlayerInformation data to JSON/CSV
  and to build ML.NET "analysis flows" over the exported CSV or straight from
  the database.
  USE FOR: running the download-players export (JSON/CSV, normalization,
  position filters), running the analyze command, and adding a new
  IDataAnalysisFlow scenario (CSV-ingested or database-backed ML.NET analysis).
  DO NOT USE FOR: server API work in Shuttle.Api, Blazor WebClient UI, EF Core
  schema/migrations, or upstream SHL API client changes.
license: MIT
---

# Shuttle.Analysis: data export & ML.NET analysis flows

`Shuttle.Analysis` is a `System.CommandLine` console app that (1) exports the
`PlayerInformation` table to local files and (2) runs those files through pluggable
ML.NET **analysis flows**. This skill covers both.

## Prerequisites (database access)

The export command reads Azure SQL. Before running it:

- Ensure `Shuttle.EFCore/.env` sets `SHUTTLESQLSERVER_DATABASE` and
  `SHUTTLESQLSERVER_HOST` (loaded via `ShuttleEnvironment.LoadDotEnv()`), or provide those
  env vars directly.
- Sign in with an Azure identity that can reach the DB (`az login`) — Azure SQL uses
  `ActiveDirectoryDefault`.

The `analyze` command works purely on local files for **CSV flows** and needs **no** database
or Azure login. **Database flows** (`DataSource = Database`) instead read Azure SQL and require
the same env vars + `az login` as the export above.

## 1. Export data (`download-players`)

Downloads the `PlayerInformation` table to JSON or CSV.

```
dotnet run --project Shuttle.Analysis -- download-players [options]
```

Options (`Shuttle.Analysis/Program.cs`):

- `--output, -o <file>` — output path. Defaults to `player-information.<json|csv>`.
- `--format, -f <json|csv>` — defaults to the `--output` extension (`.csv` => csv), else json.
- `--norm, -n <none|l1|l2>` — replace each player's stat vector with its L1/L2-normalized
  form in place (same column names). Default `none`.
- `--positions, -p <spec>` — comma list of `G, C, LW, RW, LD, RD`; group aliases `F`
  (forwards) and `D` (defense). Case-insensitive. Omit to export all.
- `--database, -d <name>` — override `SHUTTLESQLSERVER_DATABASE`.
- `--pretty` — indented JSON (ignored for CSV; default true).

Example — forwards + goalies to a CSV:

```
dotnet run --project Shuttle.Analysis -- download-players -p F,G -o players.csv
```

### Exported CSV shape (what flows ingest)

- UTF-8 **BOM**, RFC-4180 quoting, `\r\n` line endings.
- Nested attributes flatten into **dotted columns** (e.g. `skaterAttributes.checking`).
- Columns are the **union** of skater and goaltender attributes; a row only fills its own
  set, so the other set's cells are **empty** (ingested as `null`).
- Constant "mental" attributes (always 15) are dropped (`PlayerExportJson`).
- Enums use their SHL string form (e.g. position `RD`).

## 2. Run an analysis flow (`analyze`)

```
dotnet run --project Shuttle.Analysis -- analyze --flow <name> -i players.csv [-o <dir>] [--arg k=v]...
dotnet run --project Shuttle.Analysis -- analyze --list
```

Options:

- `--flow, -f <name>` — flow to run (case-insensitive; see `--list`).
- `--input, -i <file>` — exported **CSV** to ingest. **Required for CSV flows**; ignored by
  database flows.
- `--database, -d <name>` — override `SHUTTLESQLSERVER_DATABASE` (database flows only).
- `--output, -o <dir>` — artifact directory. Default `./analysis-output`.
- `--arg, -a <key=value>` — repeatable flow-specific argument (e.g. `--arg k=3 --arg seed=42`).
  Keys are case-insensitive; the value may contain `=`; duplicate keys are rejected.
- `--list` — print registered flows and exit.

Exit codes (mirror the exporter): `0` success, `130` cancelled, `1` failure (including
unknown flow, a missing required `--input` for a CSV flow, malformed input, or a bad/missing
required argument).

### Data sources: CSV vs database

Each flow declares a `FlowDataSource` (via `AnalysisFlowBase.DataSource`, default `Csv`):

- **`Csv`** — the runner ingests `--input` into `IngestedData` (surfaced as
  `AnalysisContext.Data`); no database is touched. Read data with `context.RequireData()`.
- **`Database`** — the runner sets up Azure SQL (env + `az login`, honoring `--database`),
  runs the connectivity check, and hands the flow a **scoped** `IServiceProvider`
  (`AnalysisContext.Services`); `Data`/`Input` are `null`. The flow pulls what it needs during
  analysis, e.g. `var db = context.GetRequiredService<ShlDbContext>();` and runs its own
  EF/linq2db queries. See `PlayerSummaryFlow` for the reference implementation.

## 3. Framework internals (`Shuttle.Analysis/Flows/`)

- `IngestedData` — immutable table: ordered `Columns` + `Rows`
  (`IReadOnlyDictionary<string,string?>`, empty cell => `null`). Schema-flexible so flows
  read only the columns they need and stay decoupled from the export schema.
- `CsvDataIngestor.IngestAsync(FileInfo, ct)` — parses the export CSV (BOM, quoting, union
  header) via `CsvHelper` into `IngestedData`. Throws `FileNotFoundException` /
  `InvalidDataException` for missing/empty/headerless files.
- `IDataAnalysisFlow` — `Name`, `Description`, `DataSource` (`FlowDataSource`), and
  `Task<AnalysisFlowResult> RunAsync(AnalysisContext, ct)`.
- `AnalysisFlowBase` — abstract base flows derive from: abstract `Name`/`Description`/`RunAsync`
  and `virtual DataSource => FlowDataSource.Csv`. Override `DataSource` for database flows.
- `FlowDataSource` — `Csv` (ingest `--input`) or `Database` (pull via scoped `Services`).
- `AnalysisContext` — shared `MLContext`, output `DirectoryInfo` (created before the flow runs),
  an `ILogger`, and `Arguments` (`IReadOnlyDictionary<string,string>`) with helpers
  `TryGetArgument`, `GetRequiredInt`, `GetOptionalInt`. Source-specific: `Data` (`IngestedData?`)
  + `Input` (`FileInfo?`) for CSV flows; `Services` (scoped `IServiceProvider?`) for database
  flows. Guards: `RequireData()`, `RequireServices()`, `GetRequiredService<T>()` throw a clear
  error if used by the wrong flow kind.
- `FlowArguments` — `Parse(tokens)` turns `--arg` tokens into the case-insensitive map;
  `GetRequiredInt`/`GetOptionalInt` parse integer args (invariant culture).
- `AnalysisFlowResult` — `Success(summary?)` / `Failure(summary)` (extensible).
- `CsvResultWriter` — reusable CSV writer (UTF-8 BOM, CRLF, RFC-4180 quoting) taking ordered
  columns + `string?` rows; use it for flow output.
- `AnalysisFlowRegistry` — case-insensitive name→flow map; `CreateDefault()` is the single
  place to register flows. Duplicate names throw.
- `AnalysisFlowRunner` — resolves the flow, then branches on `flow.DataSource`: a CSV flow
  ingests `--input`, builds an `MLContext` + `AnalysisContext`, and runs; a database flow loads
  the `.env`, calls `AddShuttleDatabase` (+ linq2db init + connectivity check), creates a scope,
  and passes its `ServiceProvider` as `AnalysisContext.Services`. Maps the outcome to the exit
  code above.

`MLContext` comes from the `Microsoft.ML` package (added centrally in
`Directory.Packages.props`; referenced version-less per Central Package Management).

## 3a. Built-in flow: `kmeans-centroids`

Clusters players by their stat-attribute vectors with ML.NET k-means and reports each
cluster's **centroid** (mean vector) and **medoid** (the real player nearest that centroid).

```
dotnet run --project Shuttle.Analysis -- analyze --flow kmeans-centroids -i players.csv --arg k=4
```

- Arguments: `k` (required, integer ≥ 1) clusters per group; `seed` (optional, integer) for
  reproducible runs. `k=1` means one cluster over the whole group (centroid = mean, medoid =
  nearest player); it is computed directly since ML.NET k-means needs ≥ 2 clusters.
- Features are the `skaterAttributes.*` / `goaltenderAttributes.*` columns. **Skaters and
  goaltenders are clustered separately**; only the group(s) present are reported. A group with
  fewer than `k` usable players is skipped with a warning; if no group can be clustered the
  flow fails.
- Values are clustered as provided — pre-scale with the export's `--norm l1|l2` if desired.
- Outputs (per processed group, into the output dir):
  - `{group}-medoids.csv` — `clusterId, clusterSize, playerId, name, <stats…>`
  - `{group}-centroids.csv` — `clusterId, clusterSize, <stats…>`
  - `{group}` ∈ `skater` / `goaltender`.

## 3b. Built-in flow: `player-summary` (database)

A reference **database-backed** flow. It reads `PlayerInformation` straight from the DB (no
CSV) and writes `player-summary.csv` (`position, count, avgTotalTpe`). Use it as the template
for database flows.

```
dotnet run --project Shuttle.Analysis -- analyze --flow player-summary [-d <db>] [-o <dir>]
```

Needs the DB env vars + `az login` (see Prerequisites); it resolves `ShlDbContext` from
`context.GetRequiredService<ShlDbContext>()`.

## 4. Add a new scenario

1. Create a class deriving from `AnalysisFlowBase` in `Shuttle.Analysis/Flows/` with a
   kebab-case `Name` and short `Description`.
2. Choose the data source:
   - **CSV flow** (default): leave `DataSource` as `Csv`. Read the ingested table via
     `context.RequireData()` and project the columns you need from its `Rows`.
   - **Database flow**: `override FlowDataSource DataSource => FlowDataSource.Database;` and pull
     data during `RunAsync` via `var db = context.GetRequiredService<ShlDbContext>();` (see
     `PlayerSummaryFlow`). The runner sets up Azure SQL only for these flows.
3. Read any parameters from `context.Arguments` (e.g. `context.GetRequiredInt("k")`), returning
   `AnalysisFlowResult.Failure(...)` on bad input.
4. In `RunAsync`, build an `IDataView` with `context.MLContext.Data.LoadFromEnumerable(...)` and
   train/evaluate/predict. Write artifacts under `context.Output` (use `CsvResultWriter` for
   CSV). Log via `context.Logger`. Honor `cancellationToken`. Return
   `AnalysisFlowResult.Success("...")`.
5. Register the flow in `AnalysisFlowRegistry.CreateDefault()`.
6. It's now selectable via `analyze --flow <name>` and shown by `analyze --list`.

Guidance:
- Parse cells with `CultureInfo.InvariantCulture`; treat `null` cells as missing (e.g. a
  goaltender column on a skater row).
- Prefer `LoadFromEnumerable` over `LoadFromTextFile` since ingestion already parsed the CSV
  and the union/dotted schema doesn't map cleanly to a single strongly-typed loader. For a
  fixed-size feature vector, set the column type via `SchemaDefinition` (see
  `KMeansCentroidFlow`).

## Build & test

```
dotnet build Shuttle.Analysis/Shuttle.Analysis.csproj
dotnet test Shuttle.Tests/Shuttle.Tests.csproj --filter "FullyQualifiedName~Shuttle.Tests.Analysis"
```

Framework/flow tests: `Shuttle.Tests/Analysis/CsvDataIngestorTests.cs`,
`AnalysisFlowRegistryTests.cs`, `AnalysisFlowContextTests.cs`, `FlowArgumentsTests.cs`,
`KMeansCentroidFlowTests.cs`.

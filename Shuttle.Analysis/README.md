# Shuttle.Analysis

Command-line tools for offline analysis of the Shuttle database. It can **export** the
`PlayerInformation` table to local files and **run analysis flows** (including ML.NET
scenarios) over those files.

## How it works

The app is a `System.CommandLine` console tool (`Program.cs`) with two commands:

1. **`download-players`** — reads the `PlayerInformation` table from Azure SQL (via
   `Shuttle.EFCore`) and writes it to a local JSON or CSV file. Supports position filtering
   and in-place stat-vector normalization.
2. **`analyze`** — ingests a previously exported **CSV** file and runs it through a named
   analysis flow. Flows are pluggable; see `Flows/` and the framework overview below.

Exported records are a flattened, analysis-friendly projection of a player
(`PlayerExportRecord`): nested skater/goaltender attributes become dotted columns
(e.g. `skaterAttributes.checking`), constant "mental" attributes (always 15) are dropped,
and the CSV takes the union of skater and goaltender columns.

## Prerequisites

- **.NET 10 SDK**.
- The export command needs database access:
  - Set `SHUTTLESQLSERVER_DATABASE` and `SHUTTLESQLSERVER_HOST` (via the shared
    `Shuttle.EFCore/.env`, loaded automatically, or the environment).
  - Sign in with an Azure identity that can reach the DB (`az login`) — Azure SQL uses
    `ActiveDirectoryDefault`.
- The `analyze` command works purely on local files and needs **no** database or Azure login.

## Running

### Export players

```
dotnet run --project Shuttle.Analysis -- download-players [options]
```

Options:

- `--output, -o <file>` — output path (default `player-information.<json|csv>`).
- `--format, -f <json|csv>` — defaults to the `--output` extension, else JSON.
- `--norm, -n <none|l1|l2>` — normalize each player's stat vector in place (default `none`).
- `--positions, -p <spec>` — filter to `G, C, LW, RW, LD, RD`; group aliases `F` (forwards),
  `D` (defense). Case-insensitive; omit for all players.
- `--database, -d <name>` — override `SHUTTLESQLSERVER_DATABASE`.
- `--pretty` — indented JSON (ignored for CSV; default true).

Example — export forwards and goalies to CSV:

```
dotnet run --project Shuttle.Analysis -- download-players -p F,G -o players.csv
```

### Run an analysis flow

```
dotnet run --project Shuttle.Analysis -- analyze --flow <name> -i players.csv [-o <dir>] [--arg k=v]...
dotnet run --project Shuttle.Analysis -- analyze --list
```

Options:

- `--flow, -f <name>` — flow to run (case-insensitive; see `--list`).
- `--input, -i <file>` — exported **CSV** to ingest.
- `--output, -o <dir>` — artifact directory (default `./analysis-output`).
- `--arg, -a <key=value>` — repeatable flow-specific argument (e.g. `--arg k=3 --arg seed=42`).
- `--list` — list registered flows and exit.

Exit codes: `0` success, `130` cancelled, `1` failure (unknown flow, malformed input, or a
bad/missing required argument).

#### Built-in flow: `kmeans-centroids`

Clusters players by their stat vectors with ML.NET k-means and reports, per cluster, the
**centroid** (mean vector) and the **medoid** (real player nearest that centroid). Skaters and
goaltenders are clustered separately.

```
dotnet run --project Shuttle.Analysis -- analyze --flow kmeans-centroids -i players.csv --arg k=4
```

- Arguments: `k` (required, integer ≥ 1) clusters per group; `seed` (optional) for
  reproducibility. `k=1` yields one cluster over the whole group.
- Outputs per processed group (`skater` / `goaltender`): `{group}-medoids.csv`
  (`clusterId, clusterSize, playerId, name, <stats…>`) and `{group}-centroids.csv`
  (`clusterId, clusterSize, <stats…>`).

## Analysis-flow framework (`Flows/`)

- `CsvDataIngestor` — parses the exported CSV into `IngestedData` (a schema-flexible table).
- `IDataAnalysisFlow` — a named scenario: `RunAsync(AnalysisContext, ct)`.
- `AnalysisContext` — shared `MLContext`, the ingested data, input/output paths, an `ILogger`,
  and parsed `Arguments` (from `--arg`).
- `AnalysisFlowRegistry.CreateDefault()` — the single place to register flows.
- `AnalysisFlowRunner` — ingests, builds the context, and runs the selected flow.
- `CsvResultWriter` — reusable CSV output writer (UTF-8 BOM, RFC-4180 quoting).

To add a scenario: implement `IDataAnalysisFlow` in `Flows/`, read parameters from
`context.Arguments`, write artifacts under `context.Output`, and register it in
`AnalysisFlowRegistry.CreateDefault()`.

## Build & test

```
dotnet build Shuttle.Analysis/Shuttle.Analysis.csproj
dotnet test Shuttle.Tests/Shuttle.Tests.csproj --filter "FullyQualifiedName~Shuttle.Tests.Analysis"
```

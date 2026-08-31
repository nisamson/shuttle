# Shuttle.Fhm.Serde.Sqlite.Impex

Imports FHM 10 save folders into local SQLite databases and exports those
databases back to FHM save folders using `Shuttle.Fhm.Serde.Sqlite`.

```powershell
dotnet run --project Shuttle.Fhm.Serde.Sqlite.Impex -- `
  import `
  --source "C:\Users\me\Documents\Out of the Park Developments\Franchise Hockey Manager 10\saved_games\Example.lg" `
  --output "C:\work\example.sqlite"
```

The Impex refuses to replace an existing database. Pass `--overwrite` only to
replace an existing SQLite database created by this adapter:

```powershell
dotnet run --project Shuttle.Fhm.Serde.Sqlite.Impex -- `
  import `
  --source "C:\saves\Example.lg" `
  --output "C:\work\example.sqlite" `
  --overwrite
```

Export a database to a new or empty save folder:

```powershell
dotnet run --project Shuttle.Fhm.Serde.Sqlite.Impex -- `
  export `
  --source "C:\work\example.sqlite" `
  --output "C:\saves\Example-copy.lg"
```

The export command will not replace files in a non-empty destination folder.
Pass `--timings` to print the elapsed database reconstruction and save-folder
write phases.

Write a human-readable JSON report for a team:

```powershell
dotnet run --project Shuttle.Fhm.Serde.Sqlite.Impex -- `
  team-report `
  --source "C:\work\example.sqlite" `
  --team "Example City Example"
```

The report resolves players, active lineups, tactical-system selections, and
documented tactical settings into display names. The same projection is
available to other .NET projects through
`ITeamReportService` / `TeamReportService` in `Shuttle.Fhm.Serde.Sqlite`.

Export a report for every team to separate JSON files:

```powershell
dotnet run --project Shuttle.Fhm.Serde.Sqlite.Impex -- `
  team-report `
  --source "C:\work\example.sqlite" `
  --all `
  --output "C:\work\team-reports"
```

Bulk export refuses to replace an existing report file. Pass `--overwrite` to
replace previously generated reports.

Pass `--timings` to `import` to print the elapsed time for each streaming
SQLite import phase. Pass `--progress` to print each source read and decode
operation.

Run `dotnet run --project Shuttle.Fhm.Serde.Sqlite.Impex -- --help` for all

options.

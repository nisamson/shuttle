# Shuttle.Fhm.Serde.Sqlite

`Shuttle.Fhm.Serde.Sqlite` is a local SQLite adapter for an FHM 10
save-folder `FhmSave`. It uses EF Core SQLite and is intentionally separate
from the binary codec library and the production analytics database.

To import a save folder into a database or export one back to a save folder
without writing code, use
[`Shuttle.Fhm.Serde.Sqlite.Impex`](../Shuttle.Fhm.Serde.Sqlite.Impex/README.md).

```csharp
var save = new FhmSaveReader().Read(@"C:\saves\Example.lg");
await new FhmSaveSqliteWriter().WriteAsync(save, @"C:\work\example.sqlite");

// For a memory-efficient folder import, stream source files directly to SQLite.
await new FhmSaveSqliteWriter().WriteFromDirectoryAsync(
    @"C:\saves\Example.lg",
    @"C:\work\example.sqlite");

// Edit the projection tables through FhmSaveSqliteContext or SQLite tooling.
await new FhmSaveSqliteReader().ExportAsync(
    @"C:\work\example.sqlite",
    @"C:\saves\Example-copy.lg");
```

The writer refuses to replace a database by default. To explicitly replace
one, pass `new FhmSaveSqliteWriteOptions { Overwrite = true }`.
`FhmSaveSqliteContext.OpenAsync(...)` applies pending schema migrations before
returning a context; the reader and writer do the same.

Every source file is retained in `SaveFiles` as an exact baseline blob. Large
files use ordered `SaveFileChunks`; `WriteFromDirectoryAsync` streams each
chunk directly into SQLite, so unsupported large files are not retained as
`FhmOpaqueFile` byte arrays during import. `ExportAsync` streams opaque
content from SQLite directly to the destination and reconstructs documented
files one at a time. An unedited import/export returns raw documented content
as well as opaque files, so a folder round trip is byte-equivalent.
`ReadAsync` remains available for callers that require an in-memory
`FhmSave`.

Editable projections cover `names.dat`, player profiles/rating vectors,
team top-level fields and embedded tactical settings, fixed-order game
settings, stored-line slots, `team_tactics.dat`, `tactics.dat`,
`tactic_templates.dat`, `set_play_*.dat`, `shot_type_mod.dat`,
`tactical_settings_mod.dat`, and `zone_event_mod.dat`.

The 32 built-in player roles from `player_roles.dat` are represented by the
`InGameRole` enum (for example, `InGameRole.GretzkysOffice`) in the tactical
role definitions, assignments, weights, and index entries. SQLite stores the
enum's unchanged integer values.

This is an **update-only** format. Export rejects inserted, deleted, reordered,
or identity-changing projected rows; invalid dates, position ratings, setting
type tags, references, vector sizes, and malformed tactical payload lengths
also fail before a save is returned. Serialized player backing is retained for
validation and is not an editable field. Unprojected modeled data and opaque
files are preserved from the baseline.

See [the format specification](../docs/fhm10/save-sqlite-format.md) for the
schema boundary and validation contract.

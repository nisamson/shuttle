# Shuttle.Fhm.Serde.Sqlite

`Shuttle.Fhm.Serde.Sqlite` is a local SQLite adapter for an FHM 10
save-folder `FhmSave`. It uses EF Core SQLite and is intentionally separate
from the binary codec library and the production analytics database.

```csharp
var save = new FhmSaveReader().Read(@"C:\saves\Example.lg");
await new FhmSaveSqliteWriter().WriteAsync(save, @"C:\work\example.sqlite");

// Edit the projection tables through FhmSaveSqliteContext or SQLite tooling.
var exported = await new FhmSaveSqliteReader().ReadAsync(@"C:\work\example.sqlite");
new FhmSaveWriter().Write(exported, @"C:\saves\Example-copy.lg");
```

The writer refuses to replace a database by default. To explicitly replace
one, pass `new FhmSaveSqliteWriteOptions { Overwrite = true }`.
`FhmSaveSqliteContext.OpenAsync(...)` applies pending schema migrations before
returning a context; the reader and writer do the same.

Every source file is retained in `SaveFiles` as an exact baseline blob. An
unedited import/export returns raw documented content as well as opaque files,
so a folder round trip is byte-equivalent. Export materializes a documented
file only when an editable projection changed.

Editable projections cover `names.dat`, player profiles/rating vectors,
team top-level fields and embedded tactical settings, fixed-order game
settings, stored-line slots, `team_tactics.dat`, `tactics.dat`,
`tactic_templates.dat`, `set_play_*.dat`, `shot_type_mod.dat`,
`tactical_settings_mod.dat`, and `zone_event_mod.dat`.

This is an **update-only** format. Export rejects inserted, deleted, reordered,
or identity-changing projected rows; invalid dates, position ratings, setting
type tags, references, vector sizes, and malformed tactical payload lengths
also fail before a save is returned. Serialized player backing is retained for
validation and is not an editable field. Unprojected modeled data and opaque
files are preserved from the baseline.

See [the format specification](../docs/fhm10/save-sqlite-format.md) for the
schema boundary and validation contract.

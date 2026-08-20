# Shuttle.Fhm.Entities

This project contains domain models/entities mapped to/from the raw entries parsed in
`Shuttle.Fhm.Serde.Domain`.

Known values are inlined where possible (e.g. Player Entities have their names inlined instead of in a separate file).
Opaque data is stored in a separate entity (e.g. `FhmPlayer -> FhmOpaquePlayer`) keyed on whatever the unique key for the main entity is
(e.g. `FhmPlayer.Id -> FhmOpaquePlayer.PlayerId`).

Things like position are mapped to models from `Shuttle.Shl.Api.Models`.

`FhmPlayer.FromSave` maps a `FhmPlayerRecord` using its internal identity, resolves
name ids from `names.dat`, converts `FhmDate` to `DateOnly`, and maps all six FHM
position ratings. The `-1` name-id sentinel is treated as an absent name; any other
missing name id, invalid date, empty player identity, or position rating outside
`0..20` is rejected. Primary-position ties use a fixed FHM position order.

`FhmPlayerAttributes.FromSave` maps every field in the save-file rating vector,
including separate skater and goalie values where the public SHL rating interfaces
use overlapping names. `FhmPlayerOpaqueData` stores a serialized deep snapshot of
the complete source record, so `ToSaveRecord()` reconstructs all known and unknown
fields for a lossless record round-trip.

The entity configurations intentionally use only EF Core metadata and are
RDBMS-agnostic. Registration in an application-specific `DbContext`, provider
choices, and migrations are deliberately deferred to `Shuttle.EFCore`.
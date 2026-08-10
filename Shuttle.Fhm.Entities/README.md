# Shuttle.Fhm.Entities

This project contains domain models/entities mapped to/from the raw entries parsed in
`Shuttle.Fhm.SaveData`.

Known values are inlined where possible (e.g. Player Entities have their names inlined instead of in a separate file).
Opaque data is stored in a separate entity (e.g. `FhmPlayer -> FhmOpaquePlayer`) keyed on whatever the unique key for the main entity is
(e.g. `FhmPlayer.Id -> FhmOpaquePlayer.PlayerId`).

Things like position are mapped to models from `Shuttle.Shl.Api.Models`.
# Shuttle.Fhm.Serde

`Shuttle.Fhm.Serde` is the FHM 10 binary save-folder library. It deliberately
separates editable save data from BinarySerializer/Qt wire contracts:

- `Shuttle.Fhm.Serde.Domain.*` is the public editable model: `FhmSave`, folder
  reader/writer, modeled files, domain primitives, opaque-file preservation,
  validation, and mappings to wire data.
- `Shuttle.Fhm.Serde.Wire.*` contains BinarySerializer-facing Qt contracts
  only: field ordering, `QString`/`QDate`/`QList` values, framing, and wire
  serializer helpers. Wire never depends on Domain.

Use `Domain` for save-folder editing and round trips. Use `Wire` only when
working directly with a documented binary contract. Domain maps to Wire and
uses the generic `Shuttle.BinarySerde.Common` Qt support library.

`Shuttle.Fhm.Serde.Domain` is a lossless FHM 10 save-folder API. It models one
save directory through `FhmSaveReader` and `FhmSaveWriter`. Each
`IFhmSaveFile` writes directly to a `Stream` through its corresponding
serializer contract.

```csharp
var save = new FhmSaveReader().Read(@"C:\saves\Example.lg");
new FhmSaveWriter().Write(save, @"C:\saves\Example-copy.lg");
```

The supported codecs model `info`, `names`, `players`, `player_roles`, `teams`, `team_tactics`,
`stored_lines`, `trade`, `trade_history`, `leagues`, tactic-template,
set-play, and modifier catalogues. They preserve their serialization order,
nullable string distinction, raw enum values, reference sentinels (`-1` and
`9999`), and every documented opaque segment.

`players.dat` is fully modeled and BinarySerializer-backed for FHM 10
version 58. Player records expose both the zero-based internal identity used
by in-game cross-file references and the distinct player, team, and franchise
ids emitted by FHM's CSV exports. `teams.dat` is fully modeled through
BinarySerializer contracts, including its count-delimited participation and
roster chains, fixed opaque segments, tactics region, and tail records.
`leagues.dat`, `tactics.dat`, and `zone_event_mod.dat` use BinarySerializer
contracts for their documented prefixes and length-prefixed grids while
retaining their unresolved payload regions exactly. The fixed-order
`game_settings.dat` does likewise. Any unsupported file is retained
as a normalized relative path plus its original bytes in `FhmSave.OpaqueFiles`.

No save data is bundled with the project. A user may generate their own saves with a copy of FHM10. Only the English
version of the game was tested.
`Shuttle.Tests/Fhm` constructs minimal synthetic byte fixtures and byte-compares a complete folder
round-trip.

For a local, relational, update-only editing boundary, see
[`Shuttle.Fhm.Serde.Sqlite`](../Shuttle.Fhm.Serde.Sqlite/README.md) and
[the FHM SQLite format](../docs/fhm10/save-sqlite-format.md). The adapter keeps
an exact in-memory baseline through the Domain codec factory; it does not use
temporary folders to rehydrate documented file blobs.

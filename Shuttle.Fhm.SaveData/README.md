# Shuttle.Fhm.SaveData

`Shuttle.Fhm.SaveData` is a lossless FHM 10 save-folder library. It
models one save directory through `FhmSaveReader` and `FhmSaveWriter`, migrating
documented codecs to BinarySerializer-backed Qt `QDataStream` wire primitives (big-endian values, nullable `QString`,
`QDate`, count-prefixed lists, and fixed opaque blocks).
Each `IFhmSaveFile` writes directly to a `Stream` through its corresponding serializer contract.

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

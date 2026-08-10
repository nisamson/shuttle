# Shuttle.Fhm.SaveData

`Shuttle.Fhm.SaveData` is a dependency-free, lossless FHM 10 save-folder library. It
models one save directory through `FhmSaveReader` and `FhmSaveWriter`, using
Qt `QDataStream` wire primitives (big-endian values, nullable `QString`,
`QDate`, count-prefixed lists, and fixed opaque blocks).

```csharp
var save = new FhmSaveReader().Read(@"C:\saves\Example.lg");
new FhmSaveWriter().Write(save, @"C:\saves\Example-copy.lg");
```

The supported codecs model `info`, `names`, `player_roles`, `team_tactics`,
`stored_lines`, `trade`, `trade_history`, `leagues`, tactic-template,
set-play, and modifier catalogues. They preserve their serialization order,
nullable string distinction, raw enum values, reference sentinels (`-1` and
`9999`), and every documented opaque segment.

`players.dat` is validated as FHM 10 version 58 and `teams.dat` exposes their
version/count containers. Their currently unresolved record regions are kept
as exact concatenated bytes, so reading and writing an unmodified folder is
lossless while the remaining record-level serializer work is completed.
`tactics.dat` and the fixed-order `game_settings.dat` likewise retain their
documented unresolved payload regions exactly. Any unsupported file is retained
as a normalized relative path plus its original bytes in `FhmSave.OpaqueFiles`.

No save data is bundled with the project. `Shuttle.Tests/Fhm` constructs
minimal synthetic byte fixtures and byte-compares a complete folder
round-trip.

# FHM 10 save format - Kaitai Struct specs

[Kaitai Struct](https://kaitai.io) (`.ksy`) descriptions of Franchise Hockey
Manager 10 save-folder `*.dat` files. The schemas describe the binary wire
format and can generate parsers for any Kaitai-supported language.

## Format basics

- **Byte order:** big-endian throughout.
- **Primitives:** `qint32` = `s4`, `quint32` = `u4`, `quint16` = `u2`,
  `quint8` = `u1`, `bool` = one byte, and `double` = `f8`.
- **`QString`:** `s4` byte-length prefix followed by UTF-16BE data. A length
  of `-1` denotes a null string.
- **`QList<T>`:** `s4` count followed by `count` elements.
- **`QDate`:** three `s4` fields: year, month, and day. Some structures use a
  Julian `s8` date instead.
- **Typical container:** `s4 version_tag`, `s4 count`, then `count` records.
  Exceptions include `game_settings.dat`, `info.dat`, `names.dat`, and
  `stored_lines.dat`.

Shared Qt primitives and enums are defined in `fhm_common.ksy`.

The current player schema targets `players.dat` format version 58. Other
schemas use their file-specific version fields where present.

## Current schema status

| Spec | File(s) | Current coverage |
|------|---------|------------------|
| `fhm_common.ksy` | Shared module | Qt strings and dates plus shared enums. |
| `players.ksy` | `players.dat` | Complete version-58 structural parse. Exposes name references, internal identity, birth date, position ratings, 58 attributes, contracts, selected roles, special abilities, aggregate statistics, and detailed game statistics. Unknown administrative fields retain neutral names. |
| `player_roles.ksy` | `player_roles.dat` | Complete role-catalogue parse, including names, applicability flags, descriptions, requirement vectors, tuning values, and index lists. |
| `teams.ksy` | `teams.dat` | Complete self-delimiting structural parse. Exposes identity, franchise and season history, active lines, leadership, rosters, appearance, rivals, fan history, team and unit tactics, managed-team data, and the complete record tail. Unknown fields retain neutral names or fixed-size structural blocks. |
| `team_tactics.ksy` | `team_tactics.dat` | Complete catalogue parse for selectable systems in twelve tactical zones. |
| `stored_lines.ksy` | `stored_lines.dat` | Complete named-preset parse with thirteen situational player lists and thirteen parallel lock-state lists. |
| `names.ksy` | `names.dat` | Complete master-name table, per-nation name-id lists, and scalar arrays. |
| `leagues.ksy` | `leagues.dat` | Container and early fields parsed; the remaining league body is structurally opaque and the current schema supports a single league record. |
| `trade.ksy` | `trade.dat` | Trade container and reusable trade record structures. |
| `trade_history.ksy` | `trade_history.dat` | Trade-history container using the shared trade entry structure. |
| `game_settings.ksy` | `game_settings.dat` | Complete flat settings structure. |
| `info.ksy` | `info.dat` | Two-string structure; compile-validated but not runtime-validated. |
| `set_play.ksy` | `set_play_*.dat` | Shared structural parser for all eight set-play catalogues. |
| `shot_type_mod.ksy` | `shot_type_mod.dat` | Complete structural parse. |
| `tactical_settings_mod.ksy` | `tactical_settings_mod.dat` | Complete structural parse. |
| `zone_event_mod.ksy` | `zone_event_mod.dat` | Complete structural parse. |
| `tactics.ksy` | `tactics.dat` | Container skeleton with an opaque record payload. |
| `tactic_templates.ksy` | `tactic_templates.dat` | Named-template structure; compile-validated but not runtime-validated. |

A **complete structural parse** consumes the file to end-of-file and preserves
the boundaries and encodings of every record. It does not imply that every
field has a final semantic name.

## Cross-file relationships

### Player identities and names

- `players.dat` stores first, surname, and common-name ids that reference
  `names.dat`.
- In version 58, `player_record.internal_identity` equals the zero-based
  `players.dat` record ordinal.
- Team line slots and stored-line presets use this internal identity.
- A player reference of `-1` denotes an empty slot.

### Roles and role fitness

- Player records can contain primary and supplementary selected role
  instances.
- `role_id` references `player_roles.dat`.
- A role instance stores nine tendency flag/value pairs. The first eight are
  Attacking, Aggressiveness, Backchecking, Pressure, Hitting, Tempo, Passing,
  and Shooting; the ninth is reserved.
- Role fitness is derived from the player's attributes and the role
  definition. It is not stored as a standalone player field.

### Team lines

`teams.dat` and `stored_lines.dat` use thirteen situational lists in this
order:

1. Even-strength forwards: four `LW, C, RW` lines.
2. Even-strength defence: four `LD, RD` pairs.
3. Power play 5-on-4: two units.
4. Power play 5-on-3: two units.
5. Penalty kill 4-on-5: three units.
6. Penalty kill 3-on-5: two units.
7. Four-on-four: two units.
8. Three-on-three: two units.
9. Power play 4-on-3: two units.
10. Penalty kill 3-on-4: two units.
11. Extra attackers.
12. Shootout order.
13. Starter and backup goalies.

### Team tactics

- Each team stores 22 selector blocks and 22 tendency blocks.
- Selector block 0 is the global team setting.
- Blocks 1-4 are even-strength forward lines.
- Blocks 5-6, 7-8, and 9-10 are 5-on-4, 5-on-3, and 4-on-3 power-play units.
- Blocks 11-13, 14-15, and 16-17 are 4-on-5, 3-on-5, and 3-on-4 penalty-kill
  units.
- Blocks 18-19 are four-on-four units.
- Blocks 20-21 are three-on-three units.
- A disabled unit `use settings` flag selects the global fallback.
- Every selector block contains one `team_tactics.dat` system id for each of
  the twelve tactical zones.
- Team tendency values are ordered Aggressiveness, Attacking, Backchecking,
  Hitting, Passing, Pressure, Shooting, and Tempo.

## Remaining semantic work

The principal structurally parsed but incompletely named areas are:

- player contracts, status, development, transaction, and administrative
  fields;
- individual fields within aggregate and detailed statistic records;
- the 59 general team-tactics settings preceding the system selectors;
- several team history, managed-team, finance, and tail fields;
- portions of franchise season-stat blocks;
- the main `leagues.dat` body;
- payload semantics in the generic tactic and modifier catalogues.

## Worked examples

Decoded-data walkthroughs are available in [`../examples`](../examples):

- [`impex-teams-decoded.md`](../examples/impex-teams-decoded.md)
- [`real-league-history-decoded.md`](../examples/real-league-history-decoded.md)

## Compiling and validating

Compile a spec with the Kaitai Struct compiler, with this directory on the
import path:

```text
kaitai-struct-compiler --target <language> --import-path . <spec>.ksy
```

For runtime validation, generate a parser, parse the matching `.dat` file, and
confirm that the parser consumes the complete stream.

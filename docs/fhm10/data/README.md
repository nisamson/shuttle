# FHM 10 save format — Kaitai Struct specs

[Kaitai Struct](https://kaitai.io) (`.ksy`) descriptions of the
Franchise Hockey Manager 10 save-folder `*.dat` binary formats. These specs
describe the on-disk wire format so the files can be parsed by any
Kaitai-supported language. The structure of the save files was determined through
byte-comparison of save files after changes in-game.

## Format basics

- **Byte order:** big-endian throughout (Qt `QDataStream` default).
- **Primitives:** `qint32`=`s4`, `quint32`=`u4`, `quint16`=`u2`, `quint8`=`u1`,
  `bool`=1 byte, `double`=`f8`.
- **`QString`:** `s4` byte-length prefix, then UTF-16BE code units. A length of
  `-1` (`0xFFFFFFFF`) denotes a null string.
- **`QList<T>`:** `s4` count, then `count` elements of `T`.
- **`QDate`:** three `s4` fields (year, month, day); some records use a Julian
  `s8` instead.
- **Typical container:** `s4 version_tag` + `s4 count` + `count` records.
  Exceptions: `game_settings` (flat, no header), `info` (two strings),
  `names` (leading zero + sections), `stored_lines` (count only, no version tag).

The shared primitives and enums live in **`fhm_common.ksy`**, which every other
spec imports (`meta: imports: [fhm_common]`) and references as
`fhm_common::qstring`, `fhm_common::qdate`, `fhm_common::playing_role`, etc.

These specs target the **single save version** observed in a real save
(`players.dat` `format_version` 58; per-file version tags noted in each spec).
They are not version-gated across multiple save revisions.

## Specs

| Spec | File(s) | Status against the reference save |
|------|---------|-----------------------------------|
| `fhm_common.ksy` | *(shared module)* | Qt primitives (`qstring`, `qdate`, `qdate_julian`) + enums (`playing_role`, `squad_status`). |
| `players.ksy` | `players.dat` | Container (`format_version`, `player_count`) parsed to EOF; player records kept opaque (no per-record length). Records are splittable on the fixed pre-name marker `[65535,-65536,65536,0,0]` (one per player); names are `name_id` references into `names.dat`, not inline (`name_block`). Confirmed sub-structures documented as reference types (`rating_attributes`, `player_role_instance`, `special_ability_list`, `player_leading_fields`, `name_block`). |
| `player_roles.ksy` | `player_roles.dat` | Full parse — 32 role records. |
| `teams.ksy` | `teams.dat` | Full parse of the reference save (embedded tactic + line-unit sub-types); trailing per-record block stays opaque and is sized by hard-coded reference-save offsets, though its sub-structures are now identified (see below). Line assignments are fully decoded: `line_unit` holds 13 situational `QList<s4>` slot arrays (fixed counts `[12,8,10,10,12,6,8,6,8,6,5,5,2]`, one per game situation — ES F/D, PP 5v4/5v3/4v3, PK 4v5/3v5/3v4, 4-on-4, 3-on-3 OT, EN, shootout, goalies) where each slot = a player's 1-based `players.dat` record position (`-1` = empty). Record boundaries are portable via the record-start signature (see `../tools/parse_teams.py`), independent of the hard-coded offsets: `record_index` is the record's dense 0-based position (distinct from `team_id`, which is a separate, non-unique id), and `internal_code`/`internal_code_2` are separate internal-code fields that can diverge from each other and from the displayed abbreviation. The 32-byte tail closing most records is a finance-defaults artifact, not a terminator (edited-finance teams lack it). Affiliates link to their parent org by `affiliate_parent_id` = the parent's `record_index` (with `affiliate_parent_id_2` = the intermediate AHL parent for a 3-tier NHL→AHL→ECHL chain). |
| `team_tactics.ksy` | `team_tactics.dat` | Full parse — 72 built-in per-zone tactic-system options across 12 confirmed tactical-zone groups (`zone_group_id` enum). Static built-in catalogue (identical across `rs_one`/`rs_two` backups); supplies the per-zone system display names. |
| `leagues.ksy` | `leagues.dat` | Full parse — 1 league (early fields decoded; league body opaque to EOF; single-league only, see the spec's LIMITATION note). |
| `trade.ksy` | `trade.dat` | Full parse — container + reusable trade record types (0 records in the save). |
| `trade_history.ksy` | `trade_history.dat` | Full parse — reuses `trade`'s history-entry type (0 records in the save). |
| `game_settings.ksy` | `game_settings.dat` | Full parse — flat field dump, no header. |
| `stored_lines.ksy` | `stored_lines.dat` | Full parse — count + `stored_line` records (0 in the save). |
| `names.ksy` | `names.dat` | Full parse — master name entries + per-nation id lists + scalar arrays. |
| `info.ksy` | `info.dat` | Compile-only — two `QString`s; file absent from the reference save. |
| `set_play.ksy` | `set_play_*.dat` (×8) | Full parse — one shared spec parses all eight set-play files. |
| `shot_type_mod.ksy` | `shot_type_mod.dat` | Full parse. |
| `tactical_settings_mod.ksy` | `tactical_settings_mod.dat` | Full parse. |
| `zone_event_mod.ksy` | `zone_event_mod.dat` | Full parse. |
| `tactics.ksy` | `tactics.dat` | Compile-only — file absent from the reference save. |
| `tactic_templates.ksy` | `tactic_templates.dat` | Compile-only — file absent from the reference save. |

"Full parse" means the spec consumes the real file to end-of-file with no
trailing bytes.

## Worked examples

Decoded-data walkthroughs applying these specs to concrete saves live in
[`../examples`](../examples):

- [`impex-teams-decoded.md`](../examples/impex-teams-decoded.md) — every
  `teams.ksy` header field decoded for a small, fully fictional 9-team
  single-league save (clean ground truth: contiguous records, no real-world
  franchises, `record_index` and `team_id` deliberately divergent).

## Opaque regions

Where the byte layout of a nested structure is not fully determined, the spec
keeps that region as a named raw byte field (documented `opaque`) so the
surrounding structure still parses. The main opaque areas are:

- `players.dat` — the concatenated player records (`players_payload`).
- `leagues.dat` — the per-league configuration/embedded-list body.
- `teams.dat` — some per-slider / per-slot tactic and line-unit blocks.
- the tactic-catalogue payloads (set-play formations, modifier grids/rows,
  tactic template/settings blobs).

## Compiling and validating

Compile a spec with the Kaitai Struct compiler (`fhm_common.ksy` must be on the
import path):

```
kaitai-struct-compiler --target <lang> --import-path . <spec>.ksy
```

To validate against a real save, compile to Python and parse the matching
`.dat` file with the `kaitaistruct` runtime, checking that the stream is fully
consumed (`_io.pos() == _io.size()`).

> Toolchain note: kaitai-struct-compiler 0.10's **Python** target does not emit
> the cross-module `import` for an imported spec, so a validation harness must
> make the generated `FhmCommon` class available to the importing module (e.g.
> `import fhm_common; module.FhmCommon = fhm_common.FhmCommon`). The `.ksy`
> `imports:` declarations are correct; other language targets emit the import.

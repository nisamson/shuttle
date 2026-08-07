# IMPEX save — decoded `teams.dat`

A worked example of the [`teams.ksy`](../data/teams.ksy) decoding applied to the
**IMPEX** save: a small, fully fictional 9-team single-league save used as clean
ground truth (no real-world franchises, contiguous ids, minimal history). Every
value below is extracted directly from the record header fields the spec
decodes; the variable line-up / tactic / season-history blocks are summarised at
the end.

## Container

| Field | Value |
| --- | --- |
| `version_tag` | 51 |
| `count` (declared records) | 9 |
| Records recovered | 9 (all) |

The whole file splits cleanly into 9 team records: `count` matches, and every
record's inline `record_index` is the dense sequence 0..8.

## Teams

Identity and structure. `code` is the frozen internal short-code
(`internal_code` == `internal_code_2` for every IMPEX team); `abbrev` is the
separate user-facing editable abbreviation stored deeper in the record.

| idx | team_id | code | abbrev | City | Nickname | location_id |
| --: | --: | --- | --- | --- | --- | --: |
| 0 | 13 | FRE | FRE | Fresno | Caribou | 72948 |
| 1 | 11 | ATL | **ZZZ** | Atlanta | Fire Fighters | 104611 |
| 2 | 12 | OKL | OKL | Oklahoma City | Sunrays | 96392 |
| 3 | 15 | OTT | OTT | Ottawa | White Wings | 65132 |
| 4 | 14 | CAL | CAL | Calgary | Toreros | 65856 |
| 5 | 10 | SAN | SAN | San Antonio | Fighting Lions | 78809 |
| 6 | 16 | LOS | LOS | Los Angeles | Silvertips | 73093 |
| 7 | 17 | WAR | WAR | San Francisco | Warlords | 73396 |
| 8 | 18 | MIL | MIL | Milwaukee | Loons | 93579 |

> **`record_index` vs `team_id` are distinct.** The dense `record_index` (0..8)
> is the record's position; `team_id` is a separate id (10..18 here, unsorted).
> This save is exactly why the two fields must not be conflated — in a stock
> real-league save they happen to coincide, but here they clearly diverge.

> **Editable abbreviation is not `internal_code`/`internal_code_2`.** Team 1's
> frozen internal code is `ATL`, but its user-facing abbreviation is `ZZZ` (a
> deliberate in-game edit). The edit rewrote only the deeper repeated
> abbreviation copies, leaving `internal_code`/`internal_code_2` untouched —
> confirming they are frozen internal codes.

## League structure

Every team shares the same placement — a single league, conference and division —
consistent with a one-division fictional league. No affiliates exist.

| Field | Value (all 9 teams) |
| --- | --- |
| `league` | 0 |
| `conference` | 0 |
| `division` | 0 |
| `affiliate_parent_id` | −1 (all top-level) |
| `affiliate_parent_id_2` | −1 (none) |

## Finances

`finance_2` (operating budget) is a flat 9,000,000 for every team; `finance_1`
(franchise cash / value) varies by market. `finance_3`, `finance_4` and
`unknown_14` are 0 across the board.

| idx | code | finance_1 | finance_2 |
| --: | --- | --: | --: |
| 0 | FRE | 82,170,000 | 9,000,000 |
| 1 | ATL | 76,790,000 | 9,000,000 |
| 2 | OKL | 82,710,000 | 9,000,000 |
| 3 | OTT | 83,660,000 | 9,000,000 |
| 4 | CAL | 80,090,000 | 9,000,000 |
| 5 | SAN | 77,530,000 | 9,000,000 |
| 6 | LOS | 78,320,000 | 9,000,000 |
| 7 | WAR | 84,420,000 | 9,000,000 |
| 8 | MIL | 79,780,000 | 9,000,000 |

## Other decoded scalars

| idx | code | market_size | fan_loyalty | unknown_13 | record size (bytes) |
| --: | --- | --: | --: | --: | --: |
| 0 | FRE | 2 | 4 | 0 | 4,874 |
| 1 | ATL | 2 | 1 | 1 | 5,733 |
| 2 | OKL | 4 | 2 | 2 | 4,896 |
| 3 | OTT | 5 | 2 | 3 | 4,894 |
| 4 | CAL | 3 | 1 | 4 | 4,871 |
| 5 | SAN | 3 | 2 | 5 | 4,881 |
| 6 | LOS | 4 | 1 | 6 | 4,861 |
| 7 | WAR | 4 | 2 | 7 | 4,869 |
| 8 | MIL | 2 | 3 | 8 | 4,835 |

Notes:

- `market_size` (was `unknown_8`) and `fan_loyalty` (was `unknown_9`) are the
  editable team settings of those names, CONFIRMED by controlled in-game
  byte-diffs on this very save: setting OKL's market size to maximum moved
  `market_size` 4 -> 5, and setting OTT's fan loyalty to maximum moved
  `fan_loyalty` 2 -> 4, and a second round set both to minimum, moving them to
  0. The scales are 0..5 (six levels) and 0..4 (five levels).
- `unknown_13` equals the **`record_index`** here (0..8), not `team_id`. (In a
  real-league save it usually tracks `team_id`; the two are equal there but not
  in IMPEX, so this is a useful counter-example.)
- `flag_1` = 1 for every team, and `nickname_placement` (was `flag_2`) = 0 for
  every team; flipping WAR's nickname-usage setting in the editor moved it to 1.

## Variable / not-yet-fully-decoded blocks

- **Tactics:** the record's 12 per-zone tactic selectors ARE populated — e.g.
  team 1 (`ZZZ`) runs Flexible Reaction / Dump In / Cycle / 1-2-2 / 1-2-2 Wide /
  2-3 at even strength, Drop Pass + Overload + Pursue Aggressively on the power
  play, and Tandem Forecheck + Press + Counterattack shorthanded, with system
  names resolved through `team_tactics.dat`.
  Each record holds **22** such 12-entry `u2` selector blocks (block 0 =
  team-wide, blocks 1..21 = the per-line units), at a stride of 28-32 bytes — the
  24 bytes of selectors plus a small per-block suffix of tendency/toggle bytes.

  > **CORRECTION.** An earlier version of this page reported "`tactics_count` = 0
  > for every team — the fictional league saved no custom tactic records". That
  > was a mislabelled field, not an absence of tactics: the `s4` in question is
  > the **season count**, and the array following it is the per-season franchise
  > history (see [`teams.ksy`](../data/teams.ksy) `season_count` /
  > `season_history`). It reads 0 here simply because IMPEX is a newly created
  > league with no seasons played yet.
- **Line-ups (`line_unit`):** structurally decoded (13 situational `QList<s4>`
  slot lists with fixed counts `[12,8,10,10,12,6,8,6,8,6,5,5,2]`), but this save
  is unmanaged, so the slots are empty (−1). There is no accompanying
  `players.dat` / `names.dat` for IMPEX to resolve slot ordinals to player names.
- **Per-season history / appearance / URL blocks:** present in each record's
  opaque tail but minimal for a newly created league; individual stat fields are
  not yet labelled (see the `opaque_tail` notes in `teams.ksy`).

---

*Generated from the IMPEX save as a reference example for the FHM10 `teams.dat`
format. All teams are fictional.*

# FHM 10 save — byte-diff / decode tools

Small, dependency-free Python helpers used to reverse-engineer the FHM 10
save-folder `*.dat` formats (see the `.ksy` specs in `../data`). All are
single-file scripts with PEP&nbsp;723 inline metadata, so they run with
[`uv`](https://docs.astral.sh/uv/):

```
uv run <script>.py --help
```

They read a save's `*.dat` files directly (big-endian Qt `QDataStream`) and make
no changes to them. Point them at copies of a save's files.

## How the line-assignment format was decoded

Determined by a controlled byte-diff: dress a lineup and set all lines for one
team, save (**S0**), swap exactly two players between two 5v5 line slots, save
(**S1**), then diff. A size-neutral swap produced two mirror-image changes,
revealing that a line slot is a big-endian `s4` **player reference = the
player's 1-based record position in `players.dat`** (`players.dat` 0-based
ordinal `= slot - 1`; `-1` = empty). Identity was confirmed by resolving every
slot to a name through `players.dat` → `names.dat`. See `teams.ksy` `line_unit`
and `players.ksy` `name_block`.

## Scripts

| Script | Purpose |
|--------|---------|
| `diff_teams.py` | Raw byte-diff of two `*.dat` captures (e.g. `teams.dat` S0 vs S1). Groups changed offsets into ranges and decodes each as big-endian `u2`/`s4`, flagging values that match a supplied id list (`--ids`). Use to locate what a single in-game edit changed. |
| `dump_lines.py` | Structured dump of a `line_unit` slot region: from a starting `s4` count offset, reads consecutive `QList<s4>` blocks (forwards 3/line, defense 2/pair, then special-teams units) so the layout can be read without hand-counting hex. |
| `dump_named_lines.py` | Locate a team's `line_unit` by abbreviation and dump all 13 situational `QList<s4>` lists (fixed counts `[12,8,10,10,12,6,8,6,8,6,5,5,2]`) resolved to player names, each labelled with its confirmed game situation (ES F/D, PP 5v4/5v3/4v3, PK 4v5/3v5/3v4, 4-on-4, 3-on-3 OT, EN, shootout, goalies). `--compare OTHER.dat` reports exactly which `(list, position)` slots changed between two snapshots — the primitive that disambiguated the auto-fill-identical pairs (lists 2/3 and 5/9) via a single controlled in-game edit. |
| `parse_names.py` | Parse the `names.dat` master name table and resolve `name_id` ⇄ text (`--name` / `--id`). |
| `enumerate_players.py` | Split `players.dat` into records using the fixed pre-name marker `[65535,-65536,65536,0,0]`, assign each a 0-based global ordinal, and resolve name ids via `names.dat`. Use `--surname-id` to report a player's ordinal (to map a line slot → player). |
| `find_player.py` | Locate a player by first+last name in `players.dat` (kept for reference; note names are stored as `name_id` references, so a direct string search does not match — use `parse_names.py` + `enumerate_players.py`). |
| `parse_teams.py` | Split `teams.dat` into team records from the data (no hard-coded offsets). Records carry no length prefix, so it anchors on the record-start signature — `record_index` = a dense sequential 0-based **record position** (0,1,2,…; distinct from `team_id`, a separate id that is not unique and may be 0), then `team_id s4`, then the abbreviation/city/nickname QStrings — and reports each record's start, `team_id`, abbreviation, and size. Portable across clean saves; replaces `teams.ksy`'s reference-save-only `record_end_pos` offsets. Note `name` and `name_2` are separate internal codes that can diverge (and differ from the displayed abbreviation); it does not require them equal. Its filter also gates on identity strings, so it skips affiliate/placeholder records (which lack them) in large real-league saves — a fully robust splitter should follow the dense `record_index` alone. Do not bound records on the fixed 32-byte finance-defaults tail: edited-finance teams lack it. |
| `scan_record.py` | Landmark scanner for one `teams.dat` team record: walks its bytes and reports recognizable anchors — QStrings, QDate year/month/day `s4` triples, and lone year `s4` values — to map the large undecoded trailing block (finance / history / appearance `opaque_tail`) without a full sequential parse. Offsets are printed absolute and record-relative. |
| `annotate_bytes.py` | Annotated byte dumper for a record-relative window (`--rel-start`/`--rel-end`) of a team record: prints, per offset, a compact multi-interpretation view (hex byte plus the `s4`/`u2`/`f8`/QString values starting there) so undecoded regions can be characterized by eye. A lens, not a parser. |

## Typical workflow

```sh
# 0. Split teams.dat into team records (data-driven, no magic offsets):
uv run parse_teams.py teams.dat

# 1. Find what an in-game line change altered (S0 vs S1 copies of teams.dat):
uv run diff_teams.py teams_s0.dat teams_s1.dat

# 2. Dump a team's line block from the s4 count offset the diff pointed at:
uv run dump_lines.py teams_s1.dat --start 0x1423 --lists 8

# 3. Resolve a slot value V to a player (ordinal = V - 1):
uv run enumerate_players.py players.dat names.dat --surname-id <id>
uv run parse_names.py names.dat --name <Surname>

# 4. Dump a team's full lineup as named, situation-labelled lines; or diff two
#    snapshots to see which situation an in-game edit moved:
uv run dump_named_lines.py teams.dat players.dat names.dat --abbrev ATL
uv run dump_named_lines.py before.dat players.dat names.dat --abbrev ATL --compare after.dat
```

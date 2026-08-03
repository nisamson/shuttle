# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Dump / diff an FHM10 team's line_unit as named players.

A line_unit in teams.dat is a run of 13 length-prefixed QList<s4> situational
slot lists (fixed counts [12,8,10,10,12,6,8,6,8,6,5,5,2]); each slot is a
player reference = that player's 1-based record position in players.dat (-1 =
empty). This tool locates a team's active line_unit, reads the 13 lists, and
resolves every slot to a player name via players.dat -> names.dat.

With --compare OTHER_TEAMS_DAT it reports exactly which (list, position) slots
changed between two snapshots -- the primitive used to disambiguate which list
index corresponds to which game situation via a controlled in-game edit.

Situation labels (list index -> situation), all confirmed by controlled
in-game edits + byte-diff:
  0 ES forwards | 1 ES defense | 2 PP 5v4 | 3 PP 5v3 | 4 PK 4v5 |
  5 PK 3v5 | 6 4-on-4 | 7 3-on-3 OT | 8 PP 4v3 |
  9 PK 3v4 | 10 extra attackers (EN) | 11 shootout | 12 goalies
Lists 2==3 and 5==9 are byte-identical in a default auto-generated lineup (the
5v3 PP auto-fills like the 5v4 PP, and the 3v4 PK like the 3v5 PK); editing one
situation's unit in-game and running --compare shows which single list index
moved, which pinned each pair (list 2 = 5v4 PP screen, list 5 = 3v5 PK screen).
"""
from __future__ import annotations
import argparse
import struct
from pathlib import Path

LINE_COUNTS = [12, 8, 10, 10, 12, 6, 8, 6, 8, 6, 5, 5, 2]
LABELS = {
    0: "ES forwards (LW,C,RW x4)",
    1: "ES defense (LD,RD x4)",
    2: "PP 5v4 (4F+1D x2)",
    3: "PP 5v3 (4F+1D x2)  [=list2 by default]",
    4: "PK 4v5 (2F+2D x3)",
    5: "PK 3v5 (1F+2D x2)",
    6: "4-on-4 (2F+2D x2)",
    7: "3-on-3 OT (2F+1D x2)",
    8: "PP 4v3 (3F+1D x2)",
    9: "PK 3v4 (1F+2D x2)  [=list5 by default]",
    10: "extra attackers / EN (5)",
    11: "shootout order (5)",
    12: "goalies (2)",
}


def s4(d: bytes, o: int) -> int:
    return struct.unpack_from(">i", d, o)[0]


def load_name_map(names: bytes) -> dict[int, str]:
    off = 4
    count = s4(names, off); off += 4
    by: dict[int, str] = {}
    for _ in range(count):
        blen = s4(names, off); off += 4
        text = names[off:off + blen].decode("utf-16-be") if blen > 0 else ""
        off += max(blen, 0)
        nid = s4(names, off); off += 4
        off += 4 + 2 + 3  # group_id + weight + 3 flags
        if text:
            by[nid] = text
    return by


def load_players(players: bytes, max_name_id: int) -> list[tuple[int, int]]:
    marker = struct.pack(">iiiii", 65535, -65536, 65536, 0, 0)
    recs: list[tuple[int, int]] = []
    start = 0
    while True:
        m = players.find(marker, start)
        if m < 0:
            break
        no = m + len(marker)
        start = m + 1
        if no + 24 > len(players):
            continue
        fn, ln, cn, by, bm, bd = struct.unpack_from(">iiiiii", players, no)
        if not (0 <= fn < max_name_id and 0 <= ln < max_name_id):
            continue
        if not (cn == -1 or 0 <= cn < max_name_id):
            continue
        if not (1900 <= by <= 2016 and 1 <= bm <= 12 and 1 <= bd <= 31):
            continue
        recs.append((fn, ln))
    return recs


def find_line_unit(teams: bytes, rec_start: int, rec_end: int) -> int | None:
    """Locate the first list (count==12 forwards, then count==8 defense)."""
    def plausible(v: int) -> bool:
        return v == -1 or 1 <= v <= 100000
    for o in range(rec_start, rec_end - 4):
        if s4(teams, o) == 12 and s4(teams, o + 52) == 8:
            vals = [s4(teams, o + 4 + 4 * j) for j in range(12)]
            d8 = [s4(teams, o + 56 + 4 * j) for j in range(8)]
            if all(plausible(v) for v in vals + d8):
                return o
    return None


def read_lists(teams: bytes, start: int) -> list[list[int]]:
    o = start
    out = []
    for _ in range(len(LINE_COUNTS)):
        c = s4(teams, o)
        vals = [s4(teams, o + 4 + 4 * j) for j in range(c)]
        o = o + 4 + 4 * c
        out.append(vals)
    return out


def find_team_records(teams: bytes) -> list[tuple[int, int, int, str]]:
    """Return (index, team_id, start, abbrev) using the sequential-index sig."""
    def read_qstr(o):
        if o + 4 > len(teams):
            return None
        ln = s4(teams, o)
        if ln <= 0 or ln % 2 or o + 4 + ln > len(teams):
            return None
        try:
            return teams[o + 4:o + 4 + ln].decode("utf-16-be"), o + 4 + ln
        except UnicodeDecodeError:
            return None
    recs = []
    o = 8
    expect = 0
    while o < len(teams) - 24:
        if s4(teams, o) == expect and 0 < s4(teams, o + 4) < 100000:
            r1 = read_qstr(o + 8)
            if r1 and 2 <= len(r1[0]) <= 4 and r1[0].isupper() and r1[0].isalpha():
                r2 = read_qstr(r1[1])
                if r2 and teams[r2[1]] in (0, 1):
                    r3 = read_qstr(r2[1] + 1)
                    if r3 and r3[0].replace(" ", "").isalpha():
                        r4 = read_qstr(r3[1])
                        if r4:
                            recs.append((expect, s4(teams, o + 4), o, r1[0]))
                            expect += 1
                            o = r4[1]
                            continue
        o += 1
    return recs


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("teams", type=Path)
    ap.add_argument("players", type=Path)
    ap.add_argument("names", type=Path)
    ap.add_argument("--abbrev", default="ATL",
                    help="team abbreviation to dump (default ATL)")
    ap.add_argument("--compare", type=Path, default=None,
                    help="second teams.dat; report changed (list,pos) slots")
    args = ap.parse_args()

    teams = args.teams.read_bytes()
    by = load_name_map(args.names.read_bytes())
    max_name_id = max(by) + 1
    recs = load_players(args.players.read_bytes(), max_name_id)

    def name(slot: int) -> str:
        if slot == -1:
            return "(empty)"
        o = slot - 1
        if 0 <= o < len(recs):
            fn, ln = recs[o]
            return f"{by.get(fn, '?')} {by.get(ln, '?')}"
        return f"?slot{slot}"

    trecs = find_team_records(teams)
    match = next((t for t in trecs if t[3] == args.abbrev), None)
    if not match:
        print(f"team {args.abbrev} not found; teams present: "
              f"{[t[3] for t in trecs]}")
        return 1
    idx, team_id, start, abbrev = match
    rec_end = trecs[idx + 1][2] if idx + 1 < len(trecs) else len(teams)
    lu = find_line_unit(teams, start, rec_end)
    if lu is None:
        print(f"{abbrev}: line_unit not found")
        return 1
    lists = read_lists(teams, lu)

    if args.compare is None:
        print(f"{abbrev} (team_id={team_id}) line_unit @0x{lu:06x}\n")
        for i, vals in enumerate(lists):
            print(f"list {i:2}  {LABELS.get(i, '?')}")
            print(f"    {[name(v) for v in vals]}")
        return 0

    # compare mode
    teams2 = args.compare.read_bytes()
    trecs2 = find_team_records(teams2)
    match2 = next((t for t in trecs2 if t[3] == args.abbrev), None)
    if not match2:
        print(f"team {args.abbrev} not found in compare file")
        return 1
    idx2 = match2[0]
    rec_end2 = trecs2[idx2 + 1][2] if idx2 + 1 < len(trecs2) else len(teams2)
    lu2 = find_line_unit(teams2, match2[2], rec_end2)
    lists2 = read_lists(teams2, lu2)

    print(f"{abbrev}: comparing line_units\n"
          f"  A = {args.teams.name}\n  B = {args.compare.name}\n")
    any_change = False
    for i in range(len(LINE_COUNTS)):
        a, b = lists[i], lists2[i]
        diffs = [(p, a[p], b[p]) for p in range(min(len(a), len(b)))
                 if a[p] != b[p]]
        if diffs:
            any_change = True
            print(f"list {i:2}  {LABELS.get(i, '?')}")
            for p, av, bv in diffs:
                print(f"    pos {p:2}: {name(av)}  ->  {name(bv)}")
    if not any_change:
        print("no slot changes between the two line_units")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

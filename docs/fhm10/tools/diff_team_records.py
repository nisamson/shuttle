# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Byte-diff ONE team's record between two teams.dat exports (before/after).

Given two teams.dat files and a team abbreviation, this splits each file into
records (via the dense record_index + identity-QString start signature, the same
method as parse_teams.py), extracts the named team's record from each, and prints
difflib opcodes (replace/insert/delete) with hex. Intended for controlled in-game
byte-diffs: make one change in the game, re-save, and see exactly which bytes moved.

This is how the human-managed-team preset block was decoded: taking human control
of a previously-AI club made a fixed 908-byte block appear in that club's record
(and flipped a u1 human-managed flag 0 -> 1), with all other clubs unchanged.

Usage: diff_team_records.py <before.dat> <after.dat> <TEAM_ABBREV>
"""
from __future__ import annotations

import difflib
import struct
import sys
from pathlib import Path


def read_qstring(d: bytes, o: int):
    if o + 4 > len(d):
        return None
    ln = struct.unpack_from(">i", d, o)[0]
    if ln <= 0 or ln % 2 or ln > 400 or o + 4 + ln > len(d):
        return None
    try:
        return (d[o + 4:o + 4 + ln].decode("utf-16-be"), o + 4 + ln)
    except UnicodeDecodeError:
        return None


def _is_abbrev(s: str) -> bool:
    return 2 <= len(s) <= 4 and s.isascii() and s.isupper() and s.isalpha()


def find_starts(d: bytes):
    """Return [(offset, abbrev), ...] record starts via dense index + identity."""
    starts = []
    o, exp = 8, 0
    while o < len(d) - 24:
        if struct.unpack_from(">i", d, o)[0] == exp and 0 <= struct.unpack_from(">i", d, o + 4)[0] < 100000:
            r1 = read_qstring(d, o + 8)
            if r1 and _is_abbrev(r1[0]):
                r2 = read_qstring(d, r1[1])
                if r2 and d[r2[1]] in (0, 1):
                    r3 = read_qstring(d, r2[1] + 1)
                    if r3 and r3[0].replace(" ", "").isalpha():
                        r4 = read_qstring(d, r3[1])
                        if r4:
                            starts.append((o, r1[0]))
                            exp += 1
                            o = r4[1]
                            continue
        o += 1
    return starts


def record(path: str, team: str) -> bytes:
    d = Path(path).read_bytes()
    st = find_starts(d)
    order = [o for o, _ in st] + [len(d)]
    idx = {ab: i for i, (_, ab) in enumerate(st)}
    if team not in idx:
        raise SystemExit(f"team {team!r} not found in {path} (have: {sorted(idx)})")
    i = idx[team]
    return d[st[i][0]:order[i + 1]]


def main() -> None:
    if len(sys.argv) != 4:
        raise SystemExit(__doc__)
    before, after, team = sys.argv[1], sys.argv[2], sys.argv[3]
    a, b = record(before, team), record(after, team)
    print(f"{team}: before={len(a)}  after={len(b)}  delta {len(b) - len(a):+d}")
    sm = difflib.SequenceMatcher(a=a, b=b, autojunk=False)
    for tag, i1, i2, j1, j2 in sm.get_opcodes():
        if tag == "equal":
            continue
        print(f"  {tag:<7} A[{i1}:{i2}](len {i2 - i1})  B[{j1}:{j2}](len {j2 - j1})")
        if tag in ("replace", "insert"):
            print(f"          after : {b[j1:min(j2, j1 + 48)].hex(' ')}")
        if tag in ("replace", "delete"):
            print(f"          before: {a[i1:min(i2, i1 + 48)].hex(' ')}")


if __name__ == "__main__":
    main()

# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Detect the conditional human-managed-team preset block in FHM10 teams.dat.

Every club record ends with mostly-opaque trailing data. The ONLY large per-team
length variation is a preset block that is present iff the club is controlled by a
human GM. When present it is a FIXED 908-byte structure: a run of 17 count-prefixed
arrays [s4 count][count * s4] with the constant count sequence
[20,20,20,20,20,5,5,10,10,10,10,10,10,10,10,10,10] (line / depth-chart preset
slots, all -1 (0xFFFFFFFF) in an empty template; a GM's saved custom lines would
store player ordinals here). AI clubs do not carry the block at all.

Correlated 1:1 with a u1 "human-managed" flag located a fixed offset past the
roster array (see managed_flag.py). For each record this prints whether the block
is present, its offset/length, and the count sequence, so a run over a whole file
shows exactly which clubs are human-managed.

Usage: managed_block.py <teams.dat>
"""
from __future__ import annotations

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


def find_block(d: bytes, o: int, end: int):
    """Locate the preset block: first [s4=20][20 * s4=-1] array, then walk
    consecutive [s4 count][count * s4] arrays. Returns (start, stop, counts)."""
    p = o + 3500
    start = None
    while p < end - 84:
        if struct.unpack_from(">i", d, p)[0] == 20 and d[p + 4:p + 84] == b"\xff" * 80:
            start = p
            break
        p += 1
    if start is None:
        return None
    counts, q = [], start
    while q < end - 4:
        c = struct.unpack_from(">i", d, q)[0]
        if not (1 <= c <= 40) or q + 4 + c * 4 > end:
            break
        counts.append(c)
        q += 4 + c * 4
        if len(counts) > 40:
            break
    return (start, q, counts)


def main() -> None:
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)
    d = Path(sys.argv[1]).read_bytes()
    st = find_starts(d)
    order = [o for o, _ in st] + [len(d)]
    for i, (o, ab) in enumerate(st):
        end = order[i + 1]
        blk = find_block(d, o, end)
        if blk:
            start, stop, counts = blk
            seq = ",".join(map(str, counts))
            print(f"{ab:<5} size {end - o:<5} MANAGED  block @rel {start - o}..{stop - o} "
                  f"len {stop - start} narr {len(counts)} counts=[{seq}]")
        else:
            print(f"{ab:<5} size {end - o:<5} ai       (no block)")


if __name__ == "__main__":
    main()

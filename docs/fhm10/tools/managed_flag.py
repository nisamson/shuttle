# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Read the FHM10 teams.dat u1 "human-managed" flag for every club.

A club record carries a single-byte flag a fixed offset (23 bytes) past the end of
its item-4 roster array: 1 when the club is controlled by a human GM, 0 for an AI
club. It correlates 1:1 with the presence of the 908-byte managed preset block (see
managed_block.py) -- verified across every record of before/after saves in a
controlled experiment where one AI club was handed to a human GM.

For each record this prints the flag byte plus the surrounding window, so the flag
and its correlation with block presence can be eyeballed across a whole file.

Usage: managed_flag.py <teams.dat>
"""
from __future__ import annotations

import struct
import sys
from pathlib import Path

FLAG_OFFSET_PAST_ROSTER = 23


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


def roster_end(d: bytes, o: int, end: int):
    """Return the offset just past the item-4 roster array [s4 0][s4 count][count s4]."""
    p = o + 1000
    while p < end - 200:
        if struct.unpack_from(">i", d, p)[0] == 0:
            cnt = struct.unpack_from(">i", d, p + 4)[0]
            if 15 <= cnt <= 60 and all(
                0 <= struct.unpack_from(">i", d, p + 8 + k * 4)[0] <= 3000 for k in range(cnt)
            ):
                return p + 8 + cnt * 4
        p += 1
    return None


def main() -> None:
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)
    d = Path(sys.argv[1]).read_bytes()
    st = find_starts(d)
    order = [o for o, _ in st] + [len(d)]
    print(f"{'team':<5}{'flag':>6}   window (roster_end .. +28)")
    for i, (o, ab) in enumerate(st):
        end = order[i + 1]
        re = roster_end(d, o, end)
        if re is None:
            print(f"{ab:<5}{'?':>6}")
            continue
        flag = d[re + FLAG_OFFSET_PAST_ROSTER]
        print(f"{ab:<5}{flag:>6}   {d[re:re + 28].hex(' ')}")


if __name__ == "__main__":
    main()

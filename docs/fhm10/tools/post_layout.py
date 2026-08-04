# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Map the FHM10 teams.dat item-4 POST interior (roster array -> item 5).

The team record's opaque_tail item 4 is the roster array followed by a large
"POST" region that this repo has been decoding toward making the record
self-delimiting (so teams.ksy's file-specific record_end_pos offset table can be
dropped). Cross-team analysis of a clean multi-club save shows the POST is:

  * a FIXED 156-byte scaffold immediately after the roster array (byte-identical
    across AI clubs),
  * a run of per-team f4 rating/finance floats (fixed size, differing values),
  * the main length variable: count-prefixed big-endian s4 arrays `[s4 N][N* s4]`
    of globally-allocated SEQUENTIAL club IDs (self-delimiting); the larger array
    is followed by a FIXED anchor `s4 -1, s4 0x270F0000, s4 0, s4 999`,
  * a FIXED 66-byte tail (item 5's 32-byte finance trailer + 34 constant bytes).

So the variable content is bounded to `roster_end+156 .. record_end-66`. A small
residual per-player field inside that window is not yet enumerated (clubs with
equal total ID-array element counts can still differ by a few bytes), which is
why the record is not yet byte-exact walkable.

This tool reports, per club: the human-managed flag, roster_end, POST length, the
fixed-head/fixed-tail bounds, and every count-prefixed consecutive-ID array it
finds in the variable window (count + first id). Use it to watch how the POST
layout shifts between two saves after a controlled in-game edit.

Usage: post_layout.py <teams.dat>
"""
from __future__ import annotations

import struct
import sys
from pathlib import Path

FIXED_HEAD = 156
FIXED_TAIL = 66
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


def id_arrays(d: bytes, lo: int, hi: int):
    """Find count-prefixed big-endian s4 arrays of consecutive (+1) ids in [lo, hi)."""
    out = []
    p = lo
    while p < hi:
        n = struct.unpack_from(">i", d, p)[0]
        if 1 <= n <= 150 and p + 4 + 4 * n <= hi:
            e0 = struct.unpack_from(">i", d, p + 4)[0]
            if 0 <= e0 < 200000 and all(
                struct.unpack_from(">i", d, p + 4 + 4 * k)[0] == e0 + k for k in range(n)
            ):
                out.append((p - lo, n, e0))
                p += 4 + 4 * n
                continue
        p += 1
    return out


def main() -> None:
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)
    d = Path(sys.argv[1]).read_bytes()
    st = find_starts(d)
    order = [o for o, _ in st] + [len(d)]
    print(f"{'club':<5}{'flag':>5}{'roster_end':>12}{'post_len':>10}{'var_window':>12}   id_arrays [(rel,count,first)]")
    for i, (o, ab) in enumerate(st):
        end = order[i + 1]
        re = roster_end(d, o, end)
        if re is None:
            print(f"{ab:<5}   ?")
            continue
        flag = d[re + FLAG_OFFSET_PAST_ROSTER]
        post_len = end - re
        lo, hi = re + FIXED_HEAD, end - FIXED_TAIL
        arrs = id_arrays(d, lo, hi)
        big = [(rel, n, e0) for (rel, n, e0) in arrs if n >= 10]
        print(f"{ab:<5}{flag:>5}{re:>12}{post_len:>10}{hi - lo:>12}   {big}")


if __name__ == "__main__":
    main()

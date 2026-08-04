# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Walk each FHM10 teams.dat record by a self-delimiting field model (no offsets).

This is the executable proof that a team record's length is COMPUTABLE from the
data alone, so teams.ksy's file-specific `record_end_pos` absolute-offset table is
not fundamentally required. For every record it computes the end offset purely
from parsed field lengths and checks it against the independent record-START
signature boundary (the same one parse_teams.py uses).

Decoded model of item-4's POST (the region after the roster array), established by
cross-team analysis + controlled byte-diffs on the reference save:

    roster_end
      + 219 bytes            fixed scaffold
      + array1 [s4 n1][n1 * s4]   consecutive-id array (self-delimiting)
      + array2 [s4 n2][n2 * s4]   consecutive-id array, immediately adjacent
      + G bytes             fixed, where G = 3607 if human-managed else 2767
                            (a human GM adds a net +840 preset block inside G)
      + five [s4 c5][c5 * (s4 + u1=0x03)]   5-byte-record array
      + 65 bytes            fixed tail (includes item 5's 32-byte finance trailer)
    = record_end

So record_end = roster_end + 219 + (4+4*n1) + (4+4*n2) + G + (4+5*c5) + 65.
All three arrays are count-prefixed / self-delimiting and the inter-array gaps are
constant, so no absolute offsets are needed.

Usage: walk_record.py <teams.dat>
Exit status is non-zero if any record fails to land on its boundary.
"""
from __future__ import annotations

import struct
import sys
from pathlib import Path

FIXED_HEAD = 219          # roster_end -> array1 start
GAP_AI = 2767             # array2 end -> five-array start (AI club)
GAP_MANAGED = 3607        # ... for a human-managed club (+840 preset block)
FIXED_TAIL = 65           # five-array end -> record end
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


def _consec_count(d: bytes, p: int, limit: int):
    """If p starts a [s4 N][N* s4] array of consecutive (+1) ids, return (N, end)."""
    n = struct.unpack_from(">i", d, p)[0]
    if not (10 <= n <= 150) or p + 4 + 4 * n > limit:
        return None
    e0 = struct.unpack_from(">i", d, p + 4)[0]
    if not (0 <= e0 < 300000):
        return None
    if all(struct.unpack_from(">i", d, p + 4 + 4 * k)[0] == e0 + k for k in range(n)):
        return n, p + 4 + 4 * n
    return None


def walk(d: bytes, start: int, end: int):
    """Compute record end from the field model. Returns (computed_end, detail)."""
    re = roster_end(d, start, end)
    if re is None:
        return None, "no roster_end"
    managed = d[re + FLAG_OFFSET_PAST_ROSTER] == 1
    p = re + FIXED_HEAD
    a1 = _consec_count(d, p, end)
    if not a1:
        return None, f"array1 not found at rel {p - re}"
    n1, p = a1
    a2 = _consec_count(d, p, end)
    if not a2:
        return None, f"array2 not found at rel {p - re}"
    n2, p = a2
    p += GAP_MANAGED if managed else GAP_AI
    c5 = struct.unpack_from(">i", d, p)[0]
    if not (0 <= c5 <= 20):
        return None, f"bad five-array count {c5} at rel {p - re}"
    p += 4 + 5 * c5
    p += FIXED_TAIL
    return p, f"managed={int(managed)} n1={n1} n2={n2} c5={c5}"


def main() -> None:
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)
    d = Path(sys.argv[1]).read_bytes()
    st = find_starts(d)
    order = [o for o, _ in st] + [len(d)]
    ok = True
    for i, (o, ab) in enumerate(st):
        end = order[i + 1]
        computed, detail = walk(d, o, end)
        good = computed == end
        ok = ok and good
        mark = "OK " if good else "BAD"
        print(f"  {mark} {ab:<5} start=0x{o:06x} boundary=0x{end:06x} computed="
              f"{('0x%06x' % computed) if computed else 'None':>8}  {detail}")
    print("ALL RECORDS SELF-DELIMITING" if ok else "MISMATCH: model incomplete")
    raise SystemExit(0 if ok else 1)


if __name__ == "__main__":
    main()

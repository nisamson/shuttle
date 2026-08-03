# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Raw byte-diff helper for FHM 10 save `*.dat` files (big-endian Qt QDataStream).

Purpose
-------
Locate and interpret the byte changes between two save captures produced by a
single, controlled in-game edit (e.g. swapping two players between line slots).
It makes no assumptions about the (still partly unconfirmed) record layout: it
diffs the raw bytes, groups adjacent changed offsets into ranges, and prints
each change decoded as big-endian u2 / s4 so line-slot player references are easy
to spot. Optionally cross-references changed u2 values against a list of known
player ids/indices you pass in.

Usage
-----
    uv run diff_teams.py BEFORE.dat AFTER.dat
    uv run diff_teams.py BEFORE.dat AFTER.dat --ids ids.txt
    uv run diff_teams.py BEFORE.dat AFTER.dat --context 8 --max-gap 3

`--ids` is an optional text file of known player ids/indices (one integer per
line, decimal or 0x-hex). Any changed 2-byte value matching one of them is
flagged, which quickly reveals how a slot references a player.
"""

from __future__ import annotations

import argparse
import struct
import sys
from pathlib import Path


def load_ids(path: Path | None) -> set[int]:
    if path is None:
        return set()
    ids: set[int] = set()
    for raw in path.read_text(encoding="utf-8").splitlines():
        token = raw.strip()
        if not token or token.startswith("#"):
            continue
        ids.add(int(token, 0))
    return ids


def group_changes(a: bytes, b: bytes, max_gap: int) -> list[tuple[int, int]]:
    """Return [start, end) ranges where a and b differ, merging gaps <= max_gap."""
    n = max(len(a), len(b))
    ranges: list[list[int]] = []
    for i in range(n):
        av = a[i] if i < len(a) else None
        bv = b[i] if i < len(b) else None
        if av != bv:
            if ranges and i - ranges[-1][1] <= max_gap:
                ranges[-1][1] = i + 1
            else:
                ranges.append([i, i + 1])
    return [(s, e) for s, e in ranges]


def as_u2(data: bytes, off: int) -> int | None:
    if off + 2 <= len(data):
        return struct.unpack_from(">H", data, off)[0]
    return None


def as_s4(data: bytes, off: int) -> int | None:
    if off + 4 <= len(data):
        return struct.unpack_from(">i", data, off)[0]
    return None


def hexdump(data: bytes, start: int, length: int) -> str:
    chunk = data[start : start + length]
    return " ".join(f"{byte:02x}" for byte in chunk)


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("before", type=Path)
    parser.add_argument("after", type=Path)
    parser.add_argument("--ids", type=Path, default=None,
                        help="optional file of known player ids/indices")
    parser.add_argument("--context", type=int, default=6,
                        help="context bytes shown around each change (default 6)")
    parser.add_argument("--max-gap", type=int, default=3,
                        help="merge changed ranges separated by <= N equal bytes")
    args = parser.parse_args(argv)

    a = args.before.read_bytes()
    b = args.after.read_bytes()
    ids = load_ids(args.ids)

    print(f"before: {args.before}  ({len(a)} bytes)")
    print(f"after : {args.after}  ({len(b)} bytes)")
    if len(a) != len(b):
        print(f"!! sizes differ by {len(b) - len(a)} bytes "
              f"(a swap should be size-neutral; a move/add/remove is not)")
    print()

    ranges = group_changes(a, b, args.max_gap)
    if not ranges:
        print("No byte differences.")
        return 0

    print(f"{len(ranges)} changed region(s):\n")
    for idx, (start, end) in enumerate(ranges, 1):
        length = end - start
        ctx = args.context
        lo = max(0, start - ctx)
        print(f"[{idx}] offset 0x{start:06x} ({start})  len {length}")
        print(f"    before ctx: {hexdump(a, lo, (start - lo) + length + ctx)}")
        print(f"    after  ctx: {hexdump(b, lo, (start - lo) + length + ctx)}")
        # The changed byte may be the low byte of a big-endian field, so scan a
        # small window of candidate field starts (u2: start-1..start;
        # s4: start-3..start) and report decodings whose bytes actually changed,
        # flagging any that match a known player id/index.
        for label, fn, span in (("u2", as_u2, 2), ("s4", as_s4, 4)):
            for cand in range(start - (span - 1), start + 1):
                if cand < 0:
                    continue
                av = fn(a, cand)
                bv = fn(b, cand)
                if av is None or bv is None or av == bv:
                    continue
                flag_a = "  <-known-id" if av in ids else ""
                flag_b = "  <-known-id" if bv in ids else ""
                print(f"    {label}@0x{cand:06x}: {av}{flag_a}  ->  {bv}{flag_b}")
        print()

    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Structured dump of an FHM10 teams.dat line-slot region (big-endian).

Given a teams.dat and a starting offset that points at an s4 *count* prefix,
reads a QList<s4> and prints the values, then keeps reading following
QList<s4> blocks so the forwards / defense / goalie / special-teams layout of a
line_unit can be mapped without hand-counting hex.
"""
from __future__ import annotations
import argparse
import struct
from pathlib import Path


def read_s4(data: bytes, off: int) -> int:
    return struct.unpack_from(">i", data, off)[0]


def read_list_s4(data: bytes, off: int) -> tuple[int, list[int], int]:
    count = read_s4(data, off)
    off += 4
    vals = []
    for _ in range(count):
        vals.append(read_s4(data, off))
        off += 4
    return count, vals, off


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("file", type=Path)
    ap.add_argument("--start", type=lambda s: int(s, 0), required=True,
                    help="offset of the first s4 count prefix (e.g. 0x1423)")
    ap.add_argument("--lists", type=int, default=6,
                    help="how many consecutive QList<s4> blocks to read")
    args = ap.parse_args()
    data = args.file.read_bytes()
    off = args.start
    for i in range(args.lists):
        list_off = off
        try:
            count, vals, off = read_list_s4(data, off)
        except struct.error:
            print(f"list {i}: read past EOF at 0x{list_off:06x}")
            break
        print(f"list {i} @0x{list_off:06x}  count={count}")
        # print values with running index
        for j, v in enumerate(vals):
            print(f"    [{j:2}] {v}")
        # peek next 4 bytes
        nxt = read_s4(data, off) if off + 4 <= len(data) else None
        print(f"    next s4 @0x{off:06x} = {nxt}\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Locate a player by first+last name in FHM10 players.dat and report the
player_id that precedes the record, to test what a line-slot index references.

Per players.ksy `player_leading_fields`, first_name (a QString) begins exactly
62 bytes after the record's leading s4 player_id:
  player_id(4) nation_id_1(4) nation_id_2(4) birth_date(12) bio_u16(6)
  club_refs(24) bio_i32(8) = 62 bytes, then first_name.
So player_id lives at (first_name_length_prefix_offset - 62).
"""
from __future__ import annotations
import argparse
import struct
from pathlib import Path


def qstring_bytes(s: str) -> bytes:
    """QString wire form: s4 big-endian length (code units) + UTF-16BE."""
    return struct.pack(">i", len(s)) + s.encode("utf-16-be")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("file", type=Path)
    ap.add_argument("--first", required=True)
    ap.add_argument("--last", required=True)
    args = ap.parse_args()
    data = args.file.read_bytes()

    # Search for adjacent first_name + last_name QStrings.
    needle = qstring_bytes(args.first) + qstring_bytes(args.last)
    idx = data.find(needle)
    if idx < 0:
        print(f"pattern for '{args.first} {args.last}' not found")
        # fall back: just the last name
        ln = qstring_bytes(args.last)
        p = data.find(ln)
        print(f"last-name-only match at: {p if p >= 0 else 'none'}")
        return 1

    first_name_start = idx  # points at first_name length prefix
    pid_off = first_name_start - 62
    if pid_off < 0:
        print("record starts before file origin?")
        return 1
    player_id = struct.unpack_from(">i", data, pid_off)[0]
    nation1 = struct.unpack_from(">i", data, pid_off + 4)[0]
    nation2 = struct.unpack_from(">i", data, pid_off + 8)[0]
    print(f"{args.first} {args.last}:")
    print(f"  first_name QString @0x{first_name_start:06x}")
    print(f"  player_id  @0x{pid_off:06x} = {player_id}")
    print(f"  nation_id_1 = {nation1}   nation_id_2 = {nation2}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

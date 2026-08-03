# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Landmark scanner for one FHM10 teams.dat team record.

Given a teams.dat and a record index (via parse_teams-style signature split),
walks the record's bytes and reports recognizable landmarks so the large
undecoded trailing block ("opaque_tail": finance / history / appearance) can be
mapped without a full sequential parse:

  * QStrings   -- s4 byte-length (>0, even) + valid UTF-16BE text
  * QDates     -- three consecutive plausible s4 (year 1900..2100, month 1..12,
                  day 1..31); FHM stores QDate as year/month/day s4 triples
  * year s4    -- a lone s4 in 1900..2100 (season-history anchors)

Offsets are printed both absolute and relative to the record start. Use to find
the structure of the tail and cross-reference between records/saves.
"""
from __future__ import annotations
import argparse
import struct
from pathlib import Path


def read_qstring(d: bytes, o: int):
    if o + 4 > len(d):
        return None
    ln = struct.unpack_from(">i", d, o)[0]
    if ln <= 0 or ln % 2 != 0 or o + 4 + ln > len(d) or ln > 200:
        return None
    try:
        s = d[o + 4:o + 4 + ln].decode("utf-16-be")
    except UnicodeDecodeError:
        return None
    if not all(c.isprintable() for c in s):
        return None
    return s, o + 4 + ln


def is_abbrev(s: str) -> bool:
    return 2 <= len(s) <= 4 and s.isascii() and s.isupper() and s.isalpha()


def find_record_bounds(d: bytes) -> list[int]:
    starts: list[int] = []
    o = 8
    end = len(d)
    exp = 0
    while o < end - 24:
        idx = struct.unpack_from(">i", d, o)[0]
        tid = struct.unpack_from(">i", d, o + 4)[0]
        if idx == exp and 0 < tid < 100000:
            r1 = read_qstring(d, o + 8)
            if r1 and is_abbrev(r1[0]):
                r2 = read_qstring(d, r1[1])
                if r2 and d[r2[1]] in (0, 1):
                    r3 = read_qstring(d, r2[1] + 1)
                    if r3 and r3[0].replace(" ", "").isalpha():
                        r4 = read_qstring(d, r3[1])
                        if r4:
                            starts.append(o)
                            exp += 1
                            o = r4[1]
                            continue
        o += 1
    return starts + [end]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("file", type=Path)
    ap.add_argument("--rec", type=int, required=True, help="record index")
    ap.add_argument("--from-rel", type=lambda x: int(x, 0), default=0,
                    help="start scan at this record-relative offset (default 0)")
    args = ap.parse_args()
    d = args.file.read_bytes()
    bounds = find_record_bounds(d)
    start = bounds[args.rec]
    end = bounds[args.rec + 1]
    print(f"record {args.rec}: [0x{start:06x}, 0x{end:06x})  size={end - start}")

    o = start + args.from_rel
    while o < end:
        qs = read_qstring(d, o)
        if qs:
            txt = qs[0].encode("ascii", "backslashreplace").decode()
            print(f"  0x{o:06x} (+{o - start:5d})  QStr[{len(qs[0])}] '{txt}'")
            o = qs[1]
            continue
        # QDate triple?
        if o + 12 <= end:
            y, m, day = struct.unpack_from(">iii", d, o)
            if 1900 <= y <= 2100 and 1 <= m <= 12 and 1 <= day <= 31:
                print(f"  0x{o:06x} (+{o - start:5d})  QDate {y:04d}-{m:02d}-{day:02d}")
                o += 12
                continue
        # lone year s4?
        if o + 4 <= end:
            v = struct.unpack_from(">i", d, o)[0]
            if 1900 <= v <= 2100:
                print(f"  0x{o:06x} (+{o - start:5d})  year? s4={v}")
        o += 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

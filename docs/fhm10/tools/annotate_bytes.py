# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Annotated byte dumper for a record-relative window of an FHM10 .dat file.

Prints, for each offset in [--rel-start, --rel-end) of a chosen team record,
a compact multi-interpretation view (hex byte, and where they start: s4, u2,
f8, and a QString probe) so undecoded regions (the finance / history /
appearance "opaque_tail") can be characterized by eye. Not a parser -- a lens.
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
    o, end, exp = 8, len(d), 0
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
                            starts.append(o); exp += 1; o = r4[1]; continue
        o += 1
    return starts + [end]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("file", type=Path)
    ap.add_argument("--rec", type=int, required=True)
    ap.add_argument("--rel-start", type=lambda x: int(x, 0), default=0)
    ap.add_argument("--rel-end", type=lambda x: int(x, 0), default=0x80)
    ap.add_argument("--stride", type=int, default=4, help="row stride in bytes")
    args = ap.parse_args()
    d = args.file.read_bytes()
    b = find_record_bounds(d)
    start = b[args.rec]
    o = start + args.rel_start
    stop = min(start + args.rel_end, b[args.rec + 1])
    print(f"record {args.rec}: [0x{start:06x}, 0x{b[args.rec + 1]:06x})  "
          f"window +{args.rel_start}..+{args.rel_end}")
    while o < stop:
        row = d[o:o + args.stride]
        hexs = row.hex(" ")
        parts = []
        if o + 4 <= len(d):
            s4 = struct.unpack_from(">i", d, o)[0]
            u2a, u2b = struct.unpack_from(">HH", d, o)
            parts.append(f"s4={s4:<11} u2=({u2a},{u2b})")
        if o + 8 <= len(d):
            f8 = struct.unpack_from(">d", d, o)[0]
            if f8 == f8 and abs(f8) < 1e12 and (f8 == 0 or abs(f8) > 1e-6):
                parts.append(f"f8={f8:.4g}")
        qs = read_qstring(d, o)
        if qs and len(qs[0]) >= 2:
            txt = qs[0].encode("ascii", "backslashreplace").decode()
            parts.append(f"QStr[{len(qs[0])}]='{txt}'")
        print(f"  +{o - start:5d} 0x{o:06x}  {hexs:<12}  {'  '.join(parts)}")
        o += args.stride
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

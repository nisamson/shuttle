# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Robust record splitter for a REAL-LEAGUE FHM10 teams.dat.

parse_teams.py splits on the record-START identity signature (dense record_index
+ abbreviation/city/nickname QStrings). That works on a fresh fictional league
where every team carries display strings, but UNDER-ENUMERATES a real-league save:
most records are minor-league affiliates or placeholder / expansion slots that
carry no identity strings, so an identity-gated walk skips them. Merely relaxing
the identity requirement to allow EMPTY QStrings does not help -- the header then
degenerates to a run of zero words and false-matches on the zero padding inside a
large record (verified: it split Montreal at an interior zero-run instead of at
its true end).

This tool splits on the END-OF-RECORD marker instead, which is reliable on real
saves: every record ends with item 5's fixed finance trailer
`00*8 27 0F 00*5 05 F5 E1 00 00*7 01 FF FF FF FF`, whose `27 0F` sits 24 bytes
before the record end (record_end = position_of(27 0F) + 24). On the reference
real-league save (5220 records) this marker occurs exactly once per record except
a single club whose budget was edited away from the default (its trailer differs),
so trailer-splitting alone yields count-1 records with two of them merged.

The splitter therefore:
  1. cuts the file at every finance-trailer end (record_end = 27 0F pos + 24), and
  2. VERIFIES the cut against the dense record_index (each record begins with an
     s4 equal to its 0-based position), and where a segment is found to contain
     two records (the next segment's index skips by 2, or the file ends one record
     short), it REPAIRS the split by locating the missing record_index header
     inside the oversized segment.
The result is the full set of `count` records with a strictly dense record_index,
on a real-league save, with no hard-coded offsets.

Usage: split_records.py <teams.dat> [--limit N]
"""
from __future__ import annotations

import argparse
import re
import struct
from pathlib import Path

TRAILER = re.compile(re.escape(b"\x27\x0f\x00\x00\x00\x00\x00\x05\xf5\xe1\x00"))
TRAILER_TO_END = 24  # bytes from the `27 0F` marker to the record end
FIRST_RECORD = 8     # after version_tag (s4) + count (s4)


def read_qstring_end(d: bytes, o: int):
    """Return the offset just past a valid QString at o (empty allowed), else None."""
    if o + 4 > len(d):
        return None
    ln = struct.unpack_from(">i", d, o)[0]
    if ln < 0 or ln % 2 or ln > 400 or o + 4 + ln > len(d):
        return None
    try:
        d[o + 4:o + 4 + ln].decode("utf-16-be")
    except UnicodeDecodeError:
        return None
    return o + 4 + ln


def header_end(d: bytes, o: int):
    """If o starts a plausible record header, return the offset past it, else None.

    Header = record_index s4, team_id s4, internal_code QString, internal_code_2
    QString, flag u1(0/1), city QString, nickname QString, flag u1(0/1). QStrings
    may be empty (affiliate / placeholder records), so this is a weak check meant
    only to confirm a candidate whose record_index already matches the expected
    dense value.
    """
    p = read_qstring_end(d, o + 8)
    if p is None:
        return None
    p = read_qstring_end(d, p)
    if p is None or p >= len(d) or d[p] not in (0, 1):
        return None
    p = read_qstring_end(d, p + 1)
    if p is None:
        return None
    p = read_qstring_end(d, p)
    if p is None or p >= len(d) or d[p] not in (0, 1):
        return None
    return p + 1


def _find_index_header(d: bytes, lo: int, hi: int, want: int):
    """Find a start in (lo, hi) whose record_index == want and header is valid."""
    for p in range(lo + 1, hi - 8):
        if struct.unpack_from(">i", d, p)[0] == want:
            tid = struct.unpack_from(">i", d, p + 4)[0]
            if 0 <= tid < 100000 and header_end(d, p) is not None:
                return p
    return None


def split_records(d: bytes):
    """Return a list of (start, end) record extents covering the whole container."""
    n = len(d)
    count = struct.unpack_from(">i", d, 4)[0]
    ends = [m.start() + TRAILER_TO_END for m in TRAILER.finditer(d)]
    if not ends or ends[-1] != n:
        ends.append(n)  # tolerate a final record whose trailer differs / is absent
    # Primary segments: record i spans [prev_end, ends[i]).
    segs = []
    prev = FIRST_RECORD
    for e in ends:
        segs.append((prev, e))
        prev = e

    records = []
    expected = 0
    for s, e in segs:
        if s + 4 > n:
            break
        idx = struct.unpack_from(">i", d, s)[0]
        if idx == expected:
            records.append((s, e))
            expected += 1
            continue
        # Segment start index does not match: the PREVIOUS record had no trailer
        # and swallowed this record. Repair by splitting the previous record at
        # the header of the record whose index == expected.
        if records:
            ps, pe = records[-1]
            cut = _find_index_header(d, ps, pe, expected)
            if cut is not None:
                records[-1] = (ps, cut)
                records.append((cut, pe))
                expected += 1
                # Re-evaluate the current segment against the (now advanced) index.
                idx2 = struct.unpack_from(">i", d, s)[0]
                if idx2 == expected:
                    records.append((s, e))
                    expected += 1
                    continue
        # Could not repair cleanly; keep the segment as-is under its index.
        records.append((s, e))
        expected = idx + 1

    # If we ended one or more records short (final record(s) lacked a trailer and
    # merged into the last segment), split the tail segment on the missing indices.
    while len(records) < count and records:
        ls, le = records[-1]
        cut = _find_index_header(d, ls, le, len(records))
        if cut is None:
            break
        records[-1] = (ls, cut)
        records.append((cut, le))
    return records, count


def _abbrev(d: bytes, o: int) -> str:
    end = read_qstring_end(d, o + 8)
    if end is None:
        return ""
    ln = struct.unpack_from(">i", d, o + 8)[0]
    try:
        return d[o + 12:o + 12 + ln].decode("utf-16-be")
    except UnicodeDecodeError:
        return "?"


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("file", type=Path)
    ap.add_argument("--limit", type=int, default=0, help="print only the first N records")
    args = ap.parse_args()
    d = args.file.read_bytes()
    version_tag = struct.unpack_from(">i", d, 0)[0]
    records, count = split_records(d)
    print(f"{args.file.name}: {len(d)} bytes  version_tag={version_tag} count={count}")
    print(f"records recovered: {len(records)} (expected {count})")

    dense = all(struct.unpack_from(">i", d, s)[0] == i for i, (s, _) in enumerate(records))
    print(f"record_index dense 0..{len(records) - 1}: {dense}")

    shown = records if args.limit <= 0 else records[:args.limit]
    for i, (s, e) in enumerate(shown):
        idx = struct.unpack_from(">i", d, s)[0]
        tid = struct.unpack_from(">i", d, s + 4)[0]
        ab = _abbrev(d, s).encode("ascii", "backslashreplace").decode("ascii")
        print(f"  rec {i}: idx={idx:<5} start=0x{s:07x} team_id={tid:<6} "
              f"abbrev={ab!r:<8} size={e - s}")
    ok = len(records) == count and dense
    print("OK" if ok else "!! incomplete or non-dense")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())

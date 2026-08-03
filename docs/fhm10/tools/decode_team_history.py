# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Decode the per-season franchise-history array from an FHM10 teams.dat record.

teams.dat is a big-endian Qt QDataStream container. Deep inside each team
record's undecoded trailing block (see teams.ksy `opaque_tail`) is a per-season
history array: one fixed-stride record per season from the franchise's founding
year to the present. Each season record is

    s4 year
    QString city  QString nickname  QString abbreviation   (that season's identity)
    ~134-byte big-endian numeric stat block

The stat block's confirmed fields, at offsets relative to the start of the stat
block (i.e. immediately after the three identity QStrings), big-endian u2 unless
noted:

    +20 u1 made-playoffs flag      +21 u1 championship-won flag
    +23 finish/standing
    +25 regulation wins            +27 losses (regulation, the "L" column)
    +29 ties                       +31 overtime wins
    +33 overtime losses            +35 shootout wins        +37 shootout losses
    +39 points (== 2*(reg_W + OT_W + SO_W) + T + (OT_L + SO_L))
    +57 average home attendance
    +65 goals-for                  +67 goals-against
    +69 penalty minutes
    +71 power-play goals for       +73 power-play goals against
    +75 short-handed goals for     +77 short-handed goals against
    +79 power-play opportunities   +81 times short-handed
        (power-play % = +71/+79,  penalty-kill % = 1 - +73/+81)

The displayed record is W = +25+31+35, L = +27, OTL = +33+37. Older/seed seasons
collapse the record into +25 (all wins) and +33 (all OTL) with the OT/shootout
split fields (+31/+35/+37) zero; those split fields only populate for the recent
seasons stored in full detail (and for the save's own simulated seasons). A
lockout/cancelled season stores an all-zero stat block; the special-teams fields
(+69..+81) are also zero for pre-NHL NHA seasons (before 1917-18).

This tool does not hard-code offsets: it re-reads each season's identity
QStrings to re-anchor the stat block per record, and auto-detects the array by
finding the first run of consecutive, year-incrementing season records whose
identity fields are valid QStrings, measuring the (constant) stride from the
first two seasons. Re-anchoring per record means it also handles a franchise
whose identity string lengths change (a relocation/rename). Pass --start to
anchor manually if auto-detection misfires. It reads the file read-only; point
it at a copy of a save's teams.dat.
"""
from __future__ import annotations

import argparse
import struct
from pathlib import Path


def read_qstring(d: bytes, o: int):
    """Return (text, next_offset) for a big-endian Qt QString, or None."""
    if o + 4 > len(d):
        return None
    ln = struct.unpack_from(">i", d, o)[0]
    if ln <= 0 or ln % 2 != 0 or ln > 400 or o + 4 + ln > len(d):
        return None
    try:
        s = d[o + 4:o + 4 + ln].decode("utf-16-be")
    except UnicodeDecodeError:
        return None
    return (s, o + 4 + ln) if s.isprintable() else None


def read_identity(d: bytes, year_off: int):
    """From a season record's `s4 year` offset, read year + 3 identity QStrings.

    Return (year, [city, nickname, abbrev], offset_after_strings) or None.
    """
    if year_off + 4 > len(d):
        return None
    year = struct.unpack_from(">i", d, year_off)[0]
    if not (1900 <= year <= 2100):
        return None
    p = year_off + 4
    names: list[str] = []
    for _ in range(3):
        qs = read_qstring(d, p)
        if qs is None:
            return None
        names.append(qs[0])
        p = qs[1]
    return year, names, p


def find_history_start(d: bytes):
    """Auto-detect the (start_offset, stride) of the season-history array.

    Scans for the first offset O such that a season record parses at O and the
    next season (year+1, valid identity) sits a constant stride later, holding
    for several consecutive seasons. Returns (start_offset, stride) or None.
    """
    n = len(d)
    for o in range(0, n - 8):
        # cheap prefilter: a plausible founding year as big-endian s4
        year = struct.unpack_from(">i", d, o)[0]
        if not (1900 <= year <= 1990):
            continue
        first = read_identity(d, o)
        if first is None:
            continue
        stride = _measure_stride(d, o, first)
        if stride is not None:
            return o, stride
    return None


def _measure_stride(d: bytes, o: int, first):
    """Confirm o begins a season run; return the constant stride, else None."""
    year, _, after = first
    # The next year's `s4 year` sits somewhere shortly after the stat block.
    for stride in range(after - o + 100, after - o + 260):
        nxt = read_identity(d, o + stride)
        if nxt is None or nxt[0] != year + 1:
            continue
        # require the stride to hold for a few more consecutive seasons
        good = True
        for k in range(2, 6):
            rec = read_identity(d, o + stride * k)
            if rec is None or rec[0] != year + k:
                good = False
                break
        if good:
            return stride
    return None


def be16(num: bytes, off: int) -> int:
    return struct.unpack_from(">H", num, off)[0] if off + 2 <= len(num) else -1


def decode_seasons(d: bytes, start: int, stride: int, limit: int):
    rows = []
    k = 0
    while k < limit:
        rec = read_identity(d, start + stride * k)
        if rec is None:
            break
        year, names, stat_off = rec
        num = d[stat_off:stat_off + 134]
        rows.append({
            "year": year,
            "city": names[0], "nickname": names[1], "abbrev": names[2],
            "playoffs": num[20] if len(num) > 20 else -1,
            "champ": num[21] if len(num) > 21 else -1,
            "finish": be16(num, 23),
            "regw": be16(num, 25), "l": be16(num, 27), "t": be16(num, 29),
            "otw": be16(num, 31), "otl": be16(num, 33),
            "sow": be16(num, 35), "sol": be16(num, 37), "pts": be16(num, 39),
            "att": be16(num, 57), "gf": be16(num, 65), "ga": be16(num, 67),
            "pim": be16(num, 69),
            "ppgf": be16(num, 71), "ppga": be16(num, 73),
            "shgf": be16(num, 75), "shga": be16(num, 77),
            "ppof": be16(num, 79), "tsh": be16(num, 81),
        })
        k += 1
    return rows


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("file", type=Path, help="teams.dat (a copy)")
    ap.add_argument("--start", type=lambda x: int(x, 0), default=None,
                    help="offset of the first season's s4 year (else auto-detect)")
    ap.add_argument("--stride", type=int, default=None,
                    help="season stride in bytes (auto-measured if omitted)")
    ap.add_argument("--seasons", type=int, default=200,
                    help="max seasons to decode")
    args = ap.parse_args()
    d = args.file.read_bytes()

    if args.start is None:
        found = find_history_start(d)
        if found is None:
            raise SystemExit("could not auto-detect a season-history array; "
                             "pass --start (see scan_record.py for year landmarks)")
        start, stride = found
    else:
        start = args.start
        stride = args.stride
        if stride is None:
            first = read_identity(d, start)
            if first is None:
                raise SystemExit(f"no season record at --start {start:#x}")
            stride = _measure_stride(d, start, first)
            if stride is None:
                raise SystemExit("could not measure stride; pass --stride")

    rows = decode_seasons(d, start, stride, args.seasons)
    if not rows:
        raise SystemExit("no season records decoded")

    ident = rows[0]
    print(f"# {ident['city']} {ident['nickname']} ({ident['abbrev']}) "
          f"-- {len(rows)} seasons from {rows[0]['year']} "
          f"(start {start:#x}, stride {stride})\n")
    hdr = (f"{'year':>4} {'fin':>3} {'W':>3} {'L':>3} {'T':>3} {'OTL':>3} "
           f"{'pts':>4} {'GF':>4} {'GA':>4} {'PIM':>4} {'PP%':>5} {'PK%':>5} "
           f"{'att':>6} {'PO':>2} {'CH':>2}  team")
    print(hdr)
    for r in rows:
        team = f"{r['abbrev']} {r['city']} {r['nickname']}"
        w = r['regw'] + r['otw'] + r['sow']
        otl = r['otl'] + r['sol']
        pp = f"{100 * r['ppgf'] / r['ppof']:.1f}" if r['ppof'] else "-"
        pk = f"{100 * (1 - r['ppga'] / r['tsh']):.1f}" if r['tsh'] else "-"
        print(f"{r['year']:>4} {r['finish']:>3} {w:>3} {r['l']:>3} "
              f"{r['t']:>3} {otl:>3} {r['pts']:>4} {r['gf']:>4} "
              f"{r['ga']:>4} {r['pim']:>4} {pp:>5} {pk:>5} "
              f"{r['att']:>6} {r['playoffs']:>2} {r['champ']:>2}  "
              f"{team.encode('ascii', 'backslashreplace').decode()}")


if __name__ == "__main__":
    main()

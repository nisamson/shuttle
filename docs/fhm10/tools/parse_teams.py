# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Data-driven team-record boundary finder for FHM10 teams.dat.

teams.dat is a Qt QDataStream container: s4 version_tag, s4 count, then `count`
variable-length team records with no per-record length prefix. The original
teams.ksy skipped each record's undecoded trailing block by hard-coding absolute
end offsets measured from one specific save, so it cannot parse any other save.

This finds record boundaries from the data instead: every record begins with
  record_index (s4)  -- a DENSE 0-based index equal to the record's position
                        (0, 1, 2, ... count-1) in every save inspected
  team_id      (s4)  -- canonical team id, independent of record_index and NOT
                        unique (a user-created club can reuse an id, e.g. 0)
  name (QString), name_2 (QString), flag (u1), name_3 (QString), name_4 (QString)
This tool anchors on the dense index AND on the identity strings (abbreviation /
city / nickname). That extra identity requirement is why it only fully enumerates
a save whose teams all carry those strings (e.g. a fresh fictional league): in a
real-league save most records are minor-league affiliates or placeholder /
expansion slots that have no identity strings, so the identity-gated walk
desyncs and skips them -- NOT because the index is sparse (it is dense). A fully
robust splitter should key on the dense record_index alone.

Do NOT bound records on the fixed 32-byte tail that closes most records: it is
merely the finance section's DEFAULT values (a 9999 cap + 100,000,000 budget)
and is absent on any team whose finances were edited, so splitting on it merges
the following record into the edited one. name and name_2 are two SEPARATE inline
fields that coincide for most teams but diverge for relocated/renamed franchises
(and can differ entirely from the displayed abbreviation, which lives deeper in
the record), so the split does not assume they are equal.
"""
from __future__ import annotations
import argparse
import struct
from pathlib import Path


def read_qstring(d: bytes, o: int):
    """Return (text, next_offset) or None if not a valid non-empty QString."""
    if o + 4 > len(d):
        return None
    ln = struct.unpack_from(">i", d, o)[0]
    if ln <= 0 or o + 4 + ln > len(d) or ln % 2 != 0:
        return None
    try:
        s = d[o + 4:o + 4 + ln].decode("utf-16-be")
    except UnicodeDecodeError:
        return None
    return s, o + 4 + ln


def is_abbrev(s: str) -> bool:
    return 2 <= len(s) <= 4 and s.isascii() and s.isupper() and s.isalpha()


def find_record_starts(d: bytes) -> list[tuple[int, str]]:
    """Locate team-record starts from the data.

    A record begins with a strong multi-field signature:
      record_index (s4)  -- a dense 0-based index equal to the record's position
      team_id      (s4)  -- canonical id (>= 0; not unique across edited saves)
      name      (QString)  -- short upper-case abbreviation, e.g. "ATL"
      name_2    (QString)  -- second short QString (NOT required to equal name)
      flag      (u1)       -- 0/1
      name_3    (QString)  -- city
      name_4    (QString)  -- nickname
    This keys on the index being the next expected value 0,1,2,... AND on the
    identity strings being present. The index itself is dense in every save, so
    the desync in a real-league save comes from the identity requirement, not the
    index: affiliate / placeholder / expansion records carry no abbreviation,
    city or nickname yet still occupy a full record, so the identity-gated walk
    skips them and the expected-index check then fails at the next real team.
    This tool therefore fully enumerates only saves where every team has identity
    strings (fictional leagues); a robust full-league splitter should follow the
    dense record_index alone (each record's [start, next_start) extent) rather
    than gating on identity.
    """
    starts: list[tuple[int, str]] = []
    o = 8  # after version_tag + count
    end = len(d)
    expected_index = 0
    while o < end - 24:
        idx = struct.unpack_from(">i", d, o)[0]
        team_id = struct.unpack_from(">i", d, o + 4)[0]
        if idx == expected_index and 0 <= team_id < 100000:
            r1 = read_qstring(d, o + 8)
            if r1 and is_abbrev(r1[0]):
                r2 = read_qstring(d, r1[1])
                if r2 and d[r2[1]] in (0, 1):
                    r3 = read_qstring(d, r2[1] + 1)  # city
                    if r3 and r3[0].replace(" ", "").isalpha():
                        r4 = read_qstring(d, r3[1])  # nickname
                        if r4:
                            starts.append((o, r1[0]))
                            expected_index += 1
                            o = r4[1]
                            continue
        o += 1
    return starts


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("file", type=Path)
    args = ap.parse_args()
    d = args.file.read_bytes()
    version_tag = struct.unpack_from(">i", d, 0)[0]
    count = struct.unpack_from(">i", d, 4)[0]
    print(f"{args.file.name}: {len(d)} bytes  version_tag={version_tag} count={count}")

    starts = find_record_starts(d)
    print(f"record-start signatures found: {len(starts)} (expected {count})")
    bounds = [o for o, _ in starts] + [len(d)]
    for i, (o, abbrev) in enumerate(starts):
        team_id = struct.unpack_from(">i", d, o + 4)[0]
        size = bounds[i + 1] - o
        print(f"  rec {i}: start=0x{o:06x} team_id={team_id:<5} "
              f"abbrev={abbrev:<4} size={size}")
    ok = len(starts) == count
    print("OK" if ok else "!! count mismatch")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())

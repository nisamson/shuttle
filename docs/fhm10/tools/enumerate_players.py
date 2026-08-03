# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Enumerate FHM10 players.dat records via the per-record name-block signature.

Each player record contains a fixed marker immediately before its name ids:
  s4 sequence [65535, -65536, 65536, 0, 0] then first_name_id, surname_id,
  common_name_id, birth_year, birth_month, birth_day.
We scan for that marker (validating the trailing name/date fields), which lets
us split the variable-length records without a full field map, assign each a
0-based global ordinal (file order == players.dat record order), and resolve
name ids against names.dat.

Purpose: determine whether a teams.dat line-slot value is a player's global
record ordinal or its raw player_id, by reporting both keyed by surname.
"""
from __future__ import annotations
import argparse
import struct
from pathlib import Path

MARKER = struct.pack(">iiiii", 65535, -65536, 65536, 0, 0)


def load_name_map(names_path: Path) -> dict[int, str]:
    data = names_path.read_bytes()
    off = 4  # skip reserved_zero
    count = struct.unpack_from(">i", data, off)[0]; off += 4
    by_id: dict[int, str] = {}
    for _ in range(count):
        blen = struct.unpack_from(">i", data, off)[0]; off += 4
        if blen > 0:
            text = data[off:off + blen].decode("utf-16-be"); off += blen
        else:
            text = ""
        name_id = struct.unpack_from(">i", data, off)[0]; off += 4
        off += 4 + 2 + 3  # group_id + category_weight + 3 flags
        if text:
            by_id[name_id] = text
    return by_id


def enumerate_records(data: bytes, max_name_id: int):
    records = []
    start = 0
    while True:
        m = data.find(MARKER, start)
        if m < 0:
            break
        name_off = m + len(MARKER)
        try:
            fn, ln, cn, by, bm, bd = struct.unpack_from(">iiiiii", data, name_off)
        except struct.error:
            break
        start = m + 1
        # validate
        if not (0 <= fn < max_name_id and 0 <= ln < max_name_id):
            continue
        if not (cn == -1 or 0 <= cn < max_name_id):
            continue
        if not (1900 <= by <= 2016 and 1 <= bm <= 12 and 1 <= bd <= 31):
            continue
        records.append({"marker_off": m, "name_off": name_off,
                        "first_id": fn, "surname_id": ln, "common_id": cn,
                        "birth": (by, bm, bd)})
    return records


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("players", type=Path)
    ap.add_argument("names", type=Path)
    ap.add_argument("--surname-id", action="append", type=int, default=[],
                    help="surname_id(s) to locate and report ordinal for")
    args = ap.parse_args()
    data = args.players.read_bytes()
    fmt = struct.unpack_from(">i", data, 0)[0]
    player_count = struct.unpack_from(">i", data, 4)[0]
    by_id = load_name_map(args.names)
    max_name_id = max(by_id) + 1

    recs = enumerate_records(data, max_name_id)
    print(f"players.dat format_version={fmt} header player_count={player_count}")
    print(f"name-block records found: {len(recs)}")
    if recs:
        deltas = [recs[i+1]["marker_off"] - recs[i]["marker_off"]
                  for i in range(len(recs) - 1)]
        print(f"first marker @0x{recs[0]['marker_off']:06x}  "
              f"record-size deltas: min={min(deltas)} max={max(deltas)} "
              f"distinct={sorted(set(deltas))[:8]}{'...' if len(set(deltas))>8 else ''}")

    targets = set(args.surname_id)
    for ordinal, r in enumerate(recs):
        if r["surname_id"] in targets:
            fn = by_id.get(r["first_id"], "?")
            ln = by_id.get(r["surname_id"], "?")
            print(f"  ordinal={ordinal}  {fn} {ln}  "
                  f"first_id={r['first_id']} surname_id={r['surname_id']} "
                  f"birth={r['birth']}  marker@0x{r['marker_off']:06x}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

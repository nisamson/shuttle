# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Parse FHM10 names.dat master name table and resolve names <-> name_id.

Structure (per names.ksy):
  reserved_zero: s4 (==0)
  num_master_names: s4
  master_names: num_master_names x name_entry
    name_entry = qstring text + s4 name_id + s4 group_id + s2 category_weight
                 + u1 flag_a + u1 flag_b + u1 flag_c
Only the master table is parsed here (enough to resolve name_id -> text).
Outputs matches for the requested --name values and can dump a name_id lookup.
"""
from __future__ import annotations
import argparse
import struct
from pathlib import Path


def parse_master(data: bytes):
    off = 0
    reserved = struct.unpack_from(">i", data, off)[0]; off += 4
    count = struct.unpack_from(">i", data, off)[0]; off += 4
    by_id: dict[int, str] = {}
    by_text: dict[str, list[tuple[int, int]]] = {}  # text -> [(ordinal, name_id)]
    for ordinal in range(count):
        blen = struct.unpack_from(">i", data, off)[0]; off += 4
        if blen > 0:
            text = data[off:off + blen].decode("utf-16-be"); off += blen
        elif blen == 0:
            text = ""
        else:
            text = None  # null
        name_id = struct.unpack_from(">i", data, off)[0]; off += 4
        _group_id = struct.unpack_from(">i", data, off)[0]; off += 4
        off += 2  # category_weight s2
        off += 3  # three u1 flags
        if text:
            by_id[name_id] = text
            by_text.setdefault(text, []).append((ordinal, name_id))
    return reserved, count, off, by_id, by_text


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("file", type=Path)
    ap.add_argument("--name", action="append", default=[],
                    help="name text(s) to resolve to name_id")
    ap.add_argument("--id", action="append", type=int, default=[],
                    help="name_id(s) to resolve to text")
    args = ap.parse_args()
    data = args.file.read_bytes()
    reserved, count, end, by_id, by_text = parse_master(data)
    print(f"reserved={reserved} num_master_names={count} "
          f"master table ends @0x{end:06x} (file {len(data)} bytes)")
    for nm in args.name:
        hits = by_text.get(nm)
        print(f"  text '{nm}': {hits}")
    for i in args.id:
        print(f"  name_id {i}: {by_id.get(i)!r}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

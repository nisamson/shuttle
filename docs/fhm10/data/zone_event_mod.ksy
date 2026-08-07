meta:
  id: zone_event_mod
  title: FHM 10 zone-event modifier catalogue
  endian: be
  ks-version: '0.10'
  imports:
    - fhm_common
doc: |
  Parser for zone_event_mod.dat. The leading count is read from the
  stream; the remaining length-prefixed blobs are opaque numeric modifier grids.
seq:
  - id: version
    type: s4
    doc: file format version tag
  - id: zone_count
    type: s4
    doc: catalogue group count
  - id: modifier_grids
    type: length_prefixed_bytes
    repeat: eos
    doc: opaque zone-event modifier grids
types:
  length_prefixed_bytes:
    seq:
      - id: len_data
        type: s4
      - id: data
        size: len_data
        doc: opaque


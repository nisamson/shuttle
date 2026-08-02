meta:
  id: set_play
  title: FHM 10 tactic set-play catalogue
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Shared parser for the eight FHM 10 set_play_*.dat tactic formation
  catalogues. The first list contains selectable formation records; the trailing
  length-prefixed records are numeric catalogue data not yet interpreted.
seq:
  - id: version
    type: s4
    doc: file format version tag
  - id: num_formations
    type: s4
    doc: number of selectable formations
  - id: formations
    type: length_prefixed_bytes
    repeat: expr
    repeat-expr: num_formations
    doc: selectable formation records
  - id: extra_records
    type: length_prefixed_bytes
    repeat: eos
    doc: opaque trailing numeric catalogue records
types:
  length_prefixed_bytes:
    seq:
      - id: len_data
        type: s4
      - id: data
        size: len_data
        doc: opaque


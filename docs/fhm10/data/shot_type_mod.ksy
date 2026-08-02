meta:
  id: shot_type_mod
  title: FHM 10 shot-type modifier catalogue
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Parser for shot_type_mod.dat. The file stores length-prefixed
  numeric modifier grids used by the simulation engine.
seq:
  - id: version
    type: s4
    doc: file format version tag
  - id: modifier_grids
    type: length_prefixed_bytes
    repeat: eos
    doc: opaque numeric shot-type modifier grids
types:
  length_prefixed_bytes:
    seq:
      - id: len_data
        type: s4
      - id: data
        size: len_data
        doc: opaque


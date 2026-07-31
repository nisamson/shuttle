meta:
  id: tactical_settings_mod
  title: FHM 10 tactical-settings modifier catalogue
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Parser for tactical_settings_mod.dat. The file stores
  length-prefixed numeric rows for tactical slider modifier curves.
seq:
  - id: version
    type: s4
    doc: file format version tag
  - id: modifier_rows
    type: length_prefixed_bytes
    repeat: eos
    doc: opaque per-setting modifier vectors
types:
  length_prefixed_bytes:
    seq:
      - id: len_data
        type: s4
      - id: data
        size: len_data
        doc: opaque


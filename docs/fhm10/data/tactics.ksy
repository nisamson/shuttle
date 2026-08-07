meta:
  id: tactics
  title: FHM 10 tactics catalogue
  endian: be
  ks-version: '0.10'
  imports:
    - fhm_common
doc: |
  Compile-only skeleton for tactics.dat, a tactic catalogue. The IMPEX.lg
  validation save does not include the file, so the record payload remains
  opaque pending validation data.
seq:
  - id: version
    type: s4
    doc: file format version tag
  - id: tactic_count
    type: s4
    doc: number of tactic records
  - id: records_blob
    size-eos: true
    doc: opaque

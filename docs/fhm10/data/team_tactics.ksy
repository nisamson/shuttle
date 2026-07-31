meta:
  id: team_tactics
  title: FHM 10 team tactic preset index
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Franchise Hockey Manager 10 team tactic preset index.
seq:
  - id: version_tag
    type: s4
  - id: num_records
    type: s4
  - id: records
    type: tactic_preset
    repeat: expr
    repeat-expr: num_records

types:
  tactic_preset:
    doc: Named tactic preset.
    seq:
      - id: preset_id
        type: s4
      - id: preset_type
        type: s4
      - id: name
        type: fhm_common::qstring
      - id: tuning_value_a
        type: s4
      - id: tuning_value_b
        type: s4

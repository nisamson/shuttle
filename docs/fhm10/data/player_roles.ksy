meta:
  id: player_roles
  title: FHM 10 player role catalogue
  endian: be
  ks-version: '0.10'
  imports:
    - fhm_common
doc: |
  Player-role catalogue. Player records store selected role ids from this
  catalogue. Role fitness is derived from player attributes and the role's
  requirement data rather than stored as a standalone player value.
seq:
  - id: version_tag
    type: s4
  - id: num_records
    type: s4
  - id: records
    type: role_record
    repeat: expr
    repeat-expr: num_records

types:
  role_record:
    doc: |
      Player role definition, applicability metadata, descriptive text, and
      requirement/tuning vectors used to evaluate role suitability.
    seq:
      - id: role_id
        type: s4
      - id: name
        type: fhm_common::qstring
      - id: weight_group_a
        type: s4
        repeat: expr
        repeat-expr: 8
      - id: weight_group_b
        type: s4
        repeat: expr
        repeat-expr: 13
      - id: weight_group_c
        type: s4
        repeat: expr
        repeat-expr: 17
      - id: weight_group_d
        type: s4
        repeat: expr
        repeat-expr: 4
      - id: applies_to_forwards
        type: u1
        doc: Boolean applicability flag.
      - id: applies_to_defencemen
        type: u1
        doc: Boolean applicability flag.
      - id: applies_to_goalies
        type: u1
        doc: Boolean applicability flag.
      - id: role_flags
        type: u1
      - id: position_category
        type: u2
      - id: short_name
        type: fhm_common::qstring
      - id: tuning_value_a
        type: u2
      - id: tuning_value_b
        type: u2
      - id: description
        type: fhm_common::qstring
      - id: tuning_value_c
        type: u2
      - id: weight_group_e
        type: s4
        repeat: expr
        repeat-expr: 19
      - id: weight_group_f
        type: s4
        repeat: expr
        repeat-expr: 9
      - id: index_list_a
        type: u1_list
      - id: index_list_b
        type: u1_list
      - id: index_list_c
        type: u1_list
      - id: index_list_d
        type: u1_list

  u1_list:
    doc: QList<quint8>.
    seq:
      - id: num_entries
        type: s4
      - id: entries
        type: u1
        repeat: expr
        repeat-expr: num_entries

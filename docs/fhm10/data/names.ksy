meta:
  id: names
  title: FHM 10 save — names.dat
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Name-generation reference database. The file starts with a reserved zero,
  then a master name table, per-nation name id lists, and two per-nation
  scalar arrays. Nation-indexed sections contain 102 entries.
seq:
  - id: reserved_zero
    type: s4
    valid: 0
    -doc: reserved zero
  - id: num_master_names
    type: s4
    -doc: master name entry count
  - id: master_names
    type: name_entry
    repeat: expr
    repeat-expr: num_master_names
    -doc: master name entries
  - id: first_name_lists
    type: name_id_list
    repeat: expr
    repeat-expr: 102
    -doc: per-nation name id lists
  - id: surname_lists
    type: name_id_list
    repeat: expr
    repeat-expr: 102
    -doc: per-nation surname id lists
  - id: scalar_array_a
    type: s4
    repeat: expr
    repeat-expr: 102
    -doc: per-nation scalar values
  - id: scalar_array_b
    type: s4
    repeat: expr
    repeat-expr: 102
    -doc: per-nation scalar values
types:
  name_entry:
    seq:
      - id: text
        type: fhm_common::qstring
        -doc: name text
      - id: name_id
        type: s4
        -doc: referenced name id
      - id: group_id
        type: s4
        -doc: empirical group id
      - id: category_weight
        type: s2
        -doc: empirical category or weight
      - id: flag_a
        type: u1
        -doc: empirical flag
      - id: flag_b
        type: u1
        -doc: empirical flag
      - id: flag_c
        type: u1
        -doc: empirical flag

  name_id_list:
    seq:
      - id: num_ids
        type: s4
        -doc: id count
      - id: ids
        type: s4
        repeat: expr
        repeat-expr: num_ids
        -doc: name entry ids

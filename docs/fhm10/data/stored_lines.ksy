meta:
  id: stored_lines
  title: FHM 10 save — stored_lines.dat
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Saved lineup preset list. The file has no version tag; it starts with a
  record count followed by that many StoredLine records.
seq:
  - id: num_stored_lines
    type: s4
    -doc: stored line count
  - id: stored_lines
    type: stored_line
    repeat: expr
    repeat-expr: num_stored_lines
    -doc: saved lineup presets
types:
  stored_line:
    seq:
      - id: name
        type: fhm_common::qstring
        -doc: preset name
      - id: units
        type: player_index_list
        repeat: expr
        repeat-expr: 13
        -doc: player index lists
      - id: flag_lists
        type: bool_list
        repeat: expr
        repeat-expr: 13
        -doc: parallel flag lists

  player_index_list:
    seq:
      - id: num_player_indices
        type: s4
        -doc: player index count
      - id: player_indices
        type: s4
        repeat: expr
        repeat-expr: num_player_indices
        -doc: player list indices

  bool_list:
    seq:
      - id: num_flags
        type: s4
        -doc: flag count
      - id: flags
        type: u1
        repeat: expr
        repeat-expr: num_flags
        -doc: flags

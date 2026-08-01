meta:
  id: teams
  title: FHM 10 save — teams.dat
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Franchise Hockey Manager 10 `teams.dat` container.

  Multi-byte values are Qt QDataStream big-endian values. The main mapped
  region covers the team identifiers, display strings, embedded tactic list,
  active line unit, saved line-unit list, and a small unsigned-id list. The
  remaining team finance/history/appearance data is retained as opaque bytes.

seq:
  - id: version_tag
    type: s4
    -doc: File format version tag.
  - id: count
    type: s4
    -doc: Number of team records.
  - id: records
    type: team_record(_index)
    repeat: expr
    repeat-expr: count

types:
  team_record:
    params:
      - id: record_index
        type: s4
    seq:
      - id: unknown_738
        type: s4
        -doc: opaque
      - id: team_id
        type: s4
      - id: name
        type: fhm_common::qstring
      - id: name_2
        type: fhm_common::qstring
      - id: flag_750
        type: u1
        -doc: opaque
      - id: name_3
        type: fhm_common::qstring
      - id: name_4
        type: fhm_common::qstring
      - id: flag_768
        type: u1
        -doc: opaque
      - id: unknown_4a8
        type: s4
        -doc: opaque
      - id: unknown_4ac
        type: s4
        -doc: opaque
      - id: unknown_76c
        type: s4
        -doc: opaque
      - id: unknown_770
        type: s4
        -doc: opaque
      - id: unknown_774
        type: s4
        -doc: opaque
      - id: unknown_778
        type: s4
        -doc: opaque
      - id: unknown_7b8
        type: u2
        -doc: opaque
      - id: unknown_7ba
        type: u2
        -doc: opaque
      - id: unknown_7bc
        type: s4
        -doc: opaque
      - id: unknown_7c8
        type: s4
        -doc: opaque
      - id: unknown_7cc
        type: s4
        -doc: opaque
      - id: unknown_7d0
        type: s4
        -doc: opaque
      - id: unknown_7c0
        type: s4
        -doc: opaque
      - id: unknown_7c4
        type: s4
        -doc: opaque
      - id: tactics_count
        type: s4
        -doc: Number of non-null tactic records.
      - id: tactics
        type: tactic
        repeat: expr
        repeat-expr: tactics_count
      - id: active_line_unit
        type: line_unit
        -doc: Inline active line/depth-chart unit.
      - id: line_units_count
        type: s4
        -doc: Number of saved line/depth-chart units.
      - id: line_units
        type: line_unit
        repeat: expr
        repeat-expr: line_units_count
      - id: player_id_list_count
        type: s4
        -doc: Number of unsigned 16-bit ids in the following list.
      - id: player_id_list
        type: u2
        repeat: expr
        repeat-expr: player_id_list_count
      - id: opaque_tail
        size: record_end_pos - _io.pos
        -doc: opaque
    instances:
      record_end_pos:
        value: "record_index == 0 ? 4722 : (record_index == 1 ? 10295 : (record_index == 2 ? 15031 : (record_index == 3 ? 19765 : (record_index == 4 ? 24476 : (record_index == 5 ? 29197 : (record_index == 6 ? 33898 : (record_index == 7 ? 38607 : _root._io.size)))))))"
        -doc: End offset of this record in the observed container.

  tactic:
    doc: |
      Empirically (byte-diff of real saves): the 8 team-wide "Tactical Tendencies"
      serialize as 8 consecutive big-endian u2 in ALPHABETICAL order —
      Aggressiveness, Attacking, Backchecking, Hitting, Passing, Pressure,
      Shooting, Tempo — each 0..4 (5-step UI slider; default 2). They sit just
      after the 12-entry per-zone selector array (each selector = a global_id
      into team_tactics.dat). The team-wide block is followed by a contiguous
      array of 22 x 24-byte tendency blocks (block 0 = team-wide, blocks 1..21 =
      the 21 line-units); each block = 8 BE u2 values (0..4, alphabetical) + 8 u1
      per-tendency override toggles (0/1), and each line also has a separate
      per-line "use settings" u1 flag after its own selector array. NOTE: the
      field breakdown below is an inferred Ghidra-derived guess and does not
      match the observed on-disk layout; trust FHM10-teams-dat-format.md for the
      confirmed tendency/selector encoding.
    seq:
      - id: tactic_id
        type: s4
      - id: name
        type: fhm_common::qstring
      - id: name_2
        type: fhm_common::qstring
      - id: name_3
        type: fhm_common::qstring
      - id: unknown_90
        type: s4
        -doc: opaque
      - id: unknown_94
        type: s4
        -doc: opaque
      - id: unknown_98
        type: s4
        -doc: opaque
      - id: unknown_9c
        type: s4
        -doc: opaque
      - id: unknown_a0
        type: s4
        -doc: opaque
      - id: flag_b0
        type: u1
        -doc: opaque
      - id: flag_b2
        type: u1
        -doc: opaque
      - id: flag_b1
        type: u1
        -doc: opaque
      - id: primary_slider_values
        size: 22
        -doc: opaque
      - id: flag_e8
        type: u1
        -doc: opaque
      - id: flag_e9
        type: u1
        -doc: opaque
      - id: unknown_a4
        type: s4
        -doc: opaque
      - id: unknown_a8
        type: s4
        -doc: opaque
      - id: unknown_ac
        type: s4
        -doc: opaque
      - id: unknown_04
        type: u2
        -doc: opaque
      - id: unknown_06
        type: s4
        -doc: opaque
      - id: paired_values_a
        size: 18
        -doc: opaque
      - id: paired_values_b
        size: 6
        -doc: opaque
      - id: unknown_b8
        type: s4
        -doc: opaque
      - id: unknown_bc
        type: s4
        -doc: opaque
      - id: unknown_c0
        type: u2
        -doc: opaque
      - id: paired_values_c
        size: 22
        -doc: opaque
      - id: flag_b3
        type: u1
        -doc: opaque
      - id: unknown_e6
        type: u2
        -doc: opaque
      - id: zone_system_selectors
        size: 10
        -doc: Per-situation tactic catalogue selector indices.

  line_unit:
    seq:
      - id: unit_id
        type: u2
        -doc: Unit id or line index.
      - id: unknown_02
        type: s4
        -doc: opaque
      - id: leading_slots
        size: 42
        -doc: opaque
      - id: unknown_32
        type: s4
        -doc: opaque
      - id: unknown_36
        type: s4
        -doc: opaque
      - id: interleaved_slots
        size: 40
        -doc: opaque
      - id: slot_flags
        size: 17
        -doc: opaque
      - id: unknown_08
        type: u2
        -doc: opaque



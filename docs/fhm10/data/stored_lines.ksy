meta:
  id: stored_lines
  title: FHM 10 save — stored_lines.dat
  endian: be
  ks-version: '0.10'
  imports:
    - fhm_common
doc: |
  Saved lineup presets. The file has no version tag.

  Each preset stores the same thirteen situational player-reference lists as
  a team record's active line unit, followed by thirteen parallel lock-state
  lists.
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
      - id: even_strength_forwards
        type: player_index_list
        -doc: Four lines of LW, C, RW slots.
      - id: even_strength_defence
        type: player_index_list
        -doc: Four LD, RD pairs.
      - id: power_play_5_on_4
        type: player_index_list
        -doc: Two units of four forwards and one defenceman.
      - id: power_play_5_on_3
        type: player_index_list
        -doc: Two units of four forwards and one defenceman.
      - id: penalty_kill_4_on_5
        type: player_index_list
        -doc: Three units of two forwards and two defencemen.
      - id: penalty_kill_3_on_5
        type: player_index_list
        -doc: Two units of one forward and two defencemen.
      - id: four_on_four
        type: player_index_list
        -doc: Two units of two forwards and two defencemen.
      - id: three_on_three
        type: player_index_list
        -doc: Two units of two forwards and one defenceman.
      - id: power_play_4_on_3
        type: player_index_list
        -doc: Two units of three forwards and one defenceman.
      - id: penalty_kill_3_on_4
        type: player_index_list
        -doc: Two units of one forward and two defencemen.
      - id: extra_attackers
        type: player_index_list
      - id: shootout_order
        type: player_index_list
      - id: goalies
        type: player_index_list
        -doc: Starter followed by backup.
      - id: unit_locks
        type: bool_list
        repeat: expr
        repeat-expr: 13
        -doc: Lock-state lists parallel to the thirteen player lists.

  player_index_list:
    seq:
      - id: num_player_indices
        type: s4
        -doc: player index count
      - id: player_indices
        type: s4
        repeat: expr
        repeat-expr: num_player_indices
        -doc: |
          Internal player identities; `-1` denotes an empty slot. In
          version-58 saves these identities equal zero-based `players.dat`
          record ordinals.

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

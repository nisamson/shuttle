meta:
  id: players
  title: FHM 10 save — players.dat
  endian: be
  ks-version: '0.10'
  imports:
    - fhm_common
doc: |
  Franchise Hockey Manager 10 `players.dat`, version 58. The file contains a
  count followed by that many consecutive, self-delimiting player records.

  Names are referenced through `names.dat`. The `internal_identity` field is
  the cross-file identity used by `teams.dat` lineup slots. In version 58 it
  equals the player's zero-based record ordinal.

  The record model exposes the 58 rating attributes, position-rating vector,
  selected tactical roles, aggregate skater/goalie statistics, and detailed
  skater/goalie game statistics. Role fitness is derived from the attributes
  and `player_roles.dat`; it is not stored as a standalone field. Fields
  without confirmed semantics retain neutral names while preserving their
  exact encodings and list framing.
seq:
  - id: format_version
    type: s4
    valid: 58
  - id: player_count
    type: s4
  - id: players
    type: player_record
    repeat: expr
    repeat-expr: player_count

types:
  player_record:
    seq:
      - id: first_name_id
        type: s4
        doc: Name id in `names.dat`; -1 denotes no value.
      - id: surname_id
        type: s4
        doc: Name id in `names.dat`; -1 denotes no value.
      - id: common_name_id
        type: s4
        doc: Common/display-name id in `names.dat`; -1 denotes no value.
      - id: birth_date
        type: fhm_common::qdate
      - id: unknown_u2_values_01
        type: u2
        repeat: expr
        repeat-expr: 3
      - id: unknown_s4_values_01
        type: s4
        repeat: expr
        repeat-expr: 6
      - id: unknown_s4_01
        type: s4
      - id: internal_identity
        type: s4
        doc: |
          Cross-file player identity used by `teams.dat` lineup slots. In
          version 58 this equals this record's zero-based ordinal.
      - id: unknown_string_01
        type: fhm_common::qstring
      - id: unknown_string_02
        type: fhm_common::qstring
      - id: unknown_string_03
        type: fhm_common::qstring
      - id: unknown_u2_values_02
        type: u2
        repeat: expr
        repeat-expr: 3
      - id: position_ratings
        type: position_rating_vector
      - id: unknown_u2_values_03
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: unknown_u1_01
        type: u1
      - id: unknown_s4_02
        type: s4
      - id: unknown_u1_02
        type: u1
      - id: unknown_record_list_01
        type: fixed_24_record_list
      - id: unknown_u2_values_04
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: unknown_s4_03
        type: s4
      - id: unknown_u2_01
        type: u2
      - id: unknown_f8_01
        type: f8
      - id: unknown_u1_values_01
        type: u1
        repeat: expr
        repeat-expr: 2
      - id: contracts
        type: contract_list
      - id: unknown_u2_02
        type: u2
      - id: unknown_s4_values_02
        type: s4
        repeat: expr
        repeat-expr: 2
      - id: unknown_u1_values_02
        type: u1
        repeat: expr
        repeat-expr: 2
      - id: unknown_u2_03
        type: u2
      - id: rating_attributes
        type: rating_attributes
      - id: unknown_s4_values_03
        type: s4
        repeat: expr
        repeat-expr: 3
      - id: unknown_u2_values_05
        type: u2
        repeat: expr
        repeat-expr: 15
      - id: unknown_s4_list_01
        type: s4_list
      - id: unknown_u2_values_06
        type: u2
        repeat: expr
        repeat-expr: 3
      - id: unknown_pair_list_01
        type: fixed_8_record_list
      - id: unknown_date_01
        type: fhm_common::qdate
      - id: unknown_s4_04
        type: s4
      - id: aggregate_skater_stats_01
        type: aggregate_skater_stats_list
      - id: aggregate_goalie_stats_01
        type: aggregate_goalie_stats_list
      - id: aggregate_skater_stats_02
        type: aggregate_skater_stats_list
        doc: Second aggregate-skater list family; empty in validated saves.
      - id: aggregate_goalie_stats_02
        type: aggregate_goalie_stats_list
        doc: Second aggregate-goalie list family; empty in validated saves.
      - id: detailed_skater_game_stats
        type: detailed_skater_game_stats_list
      - id: detailed_goalie_game_stats
        type: detailed_goalie_game_stats_list
      - id: unknown_s4_05
        type: s4
      - id: unknown_u2_04
        type: u2
      - id: unknown_u1_values_03
        type: u1
        repeat: expr
        repeat-expr: 2
      - id: unknown_s4_06
        type: s4
      - id: unknown_f8_values_01
        type: f8
        repeat: expr
        repeat-expr: 2
      - id: unknown_s4_values_04
        type: s4
        repeat: expr
        repeat-expr: 3
      - id: unknown_u2_values_07
        type: u2
        repeat: expr
        repeat-expr: 5
      - id: unknown_s4_list_02
        type: s4_list
      - id: unknown_u1_values_04
        type: u1
        repeat: expr
        repeat-expr: 3
      - id: unknown_s4_list_03
        type: s4_list
      - id: unknown_s4_07
        type: s4
      - id: unknown_f8_02
        type: f8
      - id: unknown_f8_values_02
        type: f8
        repeat: expr
        repeat-expr: 3
      - id: unknown_s4_values_05
        type: s4
        repeat: expr
        repeat-expr: 2
      - id: unknown_f8_03
        type: f8
      - id: unknown_u1_03
        type: u1
      - id: unknown_s4_08
        type: s4
      - id: unknown_u1_04
        type: u1
      - id: unknown_u2_values_08
        type: u2
        repeat: expr
        repeat-expr: 3
      - id: unknown_s4_09
        type: s4
      - id: unknown_f8_values_03
        type: f8
        repeat: expr
        repeat-expr: 2
      - id: unknown_s4_10
        type: s4
      - id: unknown_u1_values_05
        type: u1
        repeat: expr
        repeat-expr: 4
      - id: unknown_u2_05
        type: u2
      - id: unknown_s4_values_06
        type: s4
        repeat: expr
        repeat-expr: 2
      - id: unknown_u1_values_06
        type: u1
        repeat: expr
        repeat-expr: 5
      - id: unknown_u2_values_09
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: unknown_u1_values_07
        type: u1
        repeat: expr
        repeat-expr: 5
      - id: unknown_u2_values_10
        type: u2
        repeat: expr
        repeat-expr: 6
      - id: unknown_u1_values_08
        type: u1
        repeat: expr
        repeat-expr: 4
      - id: unknown_u2_values_11
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: unknown_f8_values_04
        type: f8
        repeat: expr
        repeat-expr: 5
      - id: unknown_u2_06
        type: u2
      - id: unknown_u2_list_01
        type: u2_list
      - id: unknown_f8_values_05
        type: f8
        repeat: expr
        repeat-expr: 5
      - id: unknown_u2_values_12
        type: u2
        repeat: expr
        repeat-expr: 4
      - id: dated_string_records
        type: dated_string_record_list
      - id: unknown_u2_values_13
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: unknown_u1_05
        type: u1
      - id: unknown_u2_values_14
        type: u2
        repeat: expr
        repeat-expr: 3
      - id: unknown_u1_06
        type: u1
      - id: unknown_s4_11
        type: s4
      - id: unknown_f8_values_06
        type: f8
        repeat: expr
        repeat-expr: 5
      - id: unknown_u2_07
        type: u2
      - id: unknown_f8_04
        type: f8
      - id: unknown_record_list_02
        type: fixed_8_record_list
      - id: unknown_s4_12
        type: s4
      - id: unknown_record_list_03
        type: fixed_23_record_list
      - id: unknown_u2_values_15
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: unknown_u1_07
        type: u1
      - id: unknown_u2_values_16
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: unknown_string_04
        type: fhm_common::qstring
      - id: unknown_u2_values_17
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: unknown_s4_values_07
        type: s4
        repeat: expr
        repeat-expr: 4
      - id: primary_role
        type: optional_role_instance
      - id: supplementary_role
        type: optional_role_instance
      - id: unknown_u2_values_18
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: unknown_u1_values_09
        type: u1
        repeat: expr
        repeat-expr: 2
      - id: unknown_u1_pair_list
        type: fixed_2_record_list
      - id: unknown_s4_values_08
        type: s4
        repeat: expr
        repeat-expr: 3
      - id: unknown_dated_u2_records
        type: dated_u2_record_list
      - id: unknown_f8_values_07
        type: f8
        repeat: expr
        repeat-expr: 3
      - id: unknown_u2_08
        type: u2
      - id: unknown_u1_values_10
        type: u1
        repeat: expr
        repeat-expr: 2
      - id: unknown_s4_list_04
        type: s4_list
      - id: unknown_u1_values_11
        type: u1
        repeat: expr
        repeat-expr: 3
      - id: unknown_record_list_04
        type: fixed_16_record_list
      - id: unknown_u1_08
        type: u1
      - id: unknown_u2_09
        type: u2
      - id: special_abilities
        type: u1_list
      - id: unknown_u1_09
        type: u1
      - id: unknown_s4_13
        type: s4
      - id: unknown_s4_list_05
        type: s4_list
      - id: unknown_s4_14
        type: s4
      - id: unknown_u1_values_12
        type: u1
        repeat: expr
        repeat-expr: 5
      - id: unknown_s4_list_06
        type: s4_list
      - id: unknown_u1_s4_pair_list
        type: u1_s4_pair_list
      - id: unknown_u1_10
        type: u1

  position_rating_vector:
    doc: |
      Count-prefixed position values. Validated version-58 records contain 12
      values. Even indices are ratings for G, LD, RD, LW, C, and RW,
      respectively; odd-indexed values have no confirmed semantics.
    seq:
      - id: count
        type: s4
      - id: values
        type: u2
        repeat: expr
        repeat-expr: count

  rating_attributes:
    doc: |
      The complete 58-byte hidden and visible rating vector. Ratings normally
      use the 0..20 range, with the format permitting values through 50.
    seq:
      - id: big_games
        type: u1
      - id: consistency
        type: u1
      - id: greed
        type: u1
      - id: adaptability
        type: u1
      - id: loyalty
        type: u1
      - id: coachability
        type: u1
      - id: aging
        type: u1
      - id: sportsmanship
        type: u1
      - id: pass_shoot_tendency
        type: u1
      - id: controversy
        type: u1
      - id: handle_critics
        type: u1
      - id: handle_failure
        type: u1
      - id: handle_success
        type: u1
      - id: intelligence
        type: u1
      - id: mood
        type: u1
      - id: dev_rate
        type: u1
      - id: aggression
        type: u1
      - id: bravery
        type: u1
      - id: determination
        type: u1
      - id: teamplayer
        type: u1
      - id: leadership
        type: u1
      - id: temperament
        type: u1
      - id: professionalism
        type: u1
      - id: ambition
        type: u1
      - id: acceleration
        type: u1
      - id: agility
        type: u1
      - id: balance
        type: u1
      - id: speed
        type: u1
      - id: stamina
        type: u1
      - id: strength
        type: u1
      - id: fighting
        type: u1
      - id: goalie_reflexes
        type: u1
      - id: goalie_stamina
        type: u1
      - id: screening
        type: u1
      - id: getting_open
        type: u1
      - id: passing
        type: u1
      - id: puck_handling
        type: u1
      - id: shooting_accuracy
        type: u1
      - id: shooting_range
        type: u1
      - id: offensive_read
        type: u1
      - id: checking
        type: u1
      - id: faceoffs
        type: u1
      - id: hitting
        type: u1
      - id: positioning
        type: u1
      - id: shot_blocking
        type: u1
      - id: stickchecking
        type: u1
      - id: defensive_read
        type: u1
      - id: goalie_positioning
        type: u1
      - id: goalie_passing
        type: u1
      - id: goalie_pokecheck
        type: u1
      - id: goalie_blocker
        type: u1
      - id: goalie_glove
        type: u1
      - id: goalie_rebound
        type: u1
      - id: goalie_recovery
        type: u1
      - id: goalie_puckhandling
        type: u1
      - id: goalie_low_shots
        type: u1
      - id: mental_toughness
        type: u1
      - id: goalie_skating
        type: u1

  optional_role_instance:
    doc: |
      Optional selected tactical role. A presence value of -1 means absent;
      0 means the role instance follows.
    seq:
      - id: presence
        type: s4
        valid:
          any-of:
            - -1
            - 0
      - id: value
        type: player_role_instance
        if: presence == 0

  player_role_instance:
    doc: |
      Selected tactical role and nine tendency overrides. Indices 0..7 are
      Attacking, Aggressiveness, Backchecking, Pressure, Hitting, Tempo,
      Passing, and Shooting; index 8 is reserved. A disabled override normally
      has flag 0 and value 2. An enabled low or high override has flag 1 and
      value 1 or 3, respectively. Tactical role instances apply to skaters;
      consumers should not interpret a role-shaped value as a goaltender role.
    seq:
      - id: role_id
        type: s4
        doc: Role id in `player_roles.dat`.
      - id: use_override
        type: u1
        repeat: expr
        repeat-expr: 9
      - id: tendency_value
        type: u2
        repeat: expr
        repeat-expr: 9

  aggregate_skater_stats_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: aggregate_skater_stats
        repeat: expr
        repeat-expr: count

  aggregate_goalie_stats_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: aggregate_goalie_stats
        repeat: expr
        repeat-expr: count

  detailed_skater_game_stats_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: detailed_skater_game_stats
        repeat: expr
        repeat-expr: count

  detailed_goalie_game_stats_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: detailed_goalie_game_stats
        repeat: expr
        repeat-expr: count

  aggregate_skater_stats:
    doc: |
      133-byte aggregate skater statistics record. The fields retain neutral
      names where their individual statistic has not been validated.
    seq:
      - id: header_u2
        type: u2
        repeat: expr
        repeat-expr: 5
      - id: value_s4_01
        type: s4
      - id: counters_u2_01
        type: u2
        repeat: expr
        repeat-expr: 18
      - id: values_s4_01
        type: s4
        repeat: expr
        repeat-expr: 3
      - id: counters_u2_02
        type: u2
        repeat: expr
        repeat-expr: 7
      - id: values_f8_01
        type: f8
        repeat: expr
        repeat-expr: 3
      - id: value_u2_01
        type: u2
      - id: values_f8_02
        type: f8
        repeat: expr
        repeat-expr: 3
      - id: values_u2_01
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: flags_u1
        type: u1
        repeat: expr
        repeat-expr: 3

  aggregate_goalie_stats:
    doc: |
      53-byte aggregate goalie statistics record. The fields retain neutral
      names where their individual statistic has not been validated.
    seq:
      - id: header_u2
        type: u2
        repeat: expr
        repeat-expr: 5
      - id: value_s4_01
        type: s4
      - id: values_u2_01
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: value_s4_02
        type: s4
      - id: counters_u2
        type: u2
        repeat: expr
        repeat-expr: 10
      - id: value_f8
        type: f8
      - id: value_u2_01
        type: u2
      - id: flag_u1
        type: u1

  detailed_skater_game_stats:
    doc: |
      164-byte detailed skater game-statistics record, including standard and
      advanced event totals. Unassigned fields retain neutral names.
    seq:
      - id: header_u2
        type: u2
        repeat: expr
        repeat-expr: 5
      - id: value_s4_01
        type: s4
      - id: counters_u2_01
        type: u2
        repeat: expr
        repeat-expr: 18
      - id: values_s4_01
        type: s4
        repeat: expr
        repeat-expr: 3
      - id: counters_u2_02
        type: u2
        repeat: expr
        repeat-expr: 26
      - id: value_s4_02
        type: s4
      - id: values_f8
        type: f8
        repeat: expr
        repeat-expr: 3
      - id: values_u2_01
        type: u2
        repeat: expr
        repeat-expr: 6
      - id: trailing_u1
        type: u1
        repeat: expr
        repeat-expr: 10

  detailed_goalie_game_stats:
    doc: |
      67-byte detailed goalie game-statistics record. Unassigned fields retain
      neutral names.
    seq:
      - id: header_u2
        type: u2
        repeat: expr
        repeat-expr: 5
      - id: value_s4_01
        type: s4
      - id: values_u2_01
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: value_s4_02
        type: s4
      - id: counters_u2_01
        type: u2
        repeat: expr
        repeat-expr: 10
      - id: counters_u2_02
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: value_f8
        type: f8
      - id: values_u2_02
        type: u2
        repeat: expr
        repeat-expr: 6
      - id: trailing_u1
        type: u1

  contract_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: contract_record
        repeat: expr
        repeat-expr: count

  contract_record:
    seq:
      - id: unknown_s4_values_01
        type: s4
        repeat: expr
        repeat-expr: 2
      - id: unknown_u2_values_01
        type: u2
        repeat: expr
        repeat-expr: 2
      - id: unknown_s4_01
        type: s4
      - id: unknown_date_01
        type: fhm_common::qdate
      - id: unknown_f8_01
        type: f8
      - id: unknown_s4_list
        type: s4_list
      - id: unknown_u2_values_02
        type: u2
        repeat: expr
        repeat-expr: 3
      - id: unknown_s4_02
        type: s4
      - id: unknown_u2_01
        type: u2
      - id: unknown_u1_01
        type: u1
      - id: unknown_s4_values_02
        type: s4
        repeat: expr
        repeat-expr: 2
      - id: unknown_u2_02
        type: u2
      - id: unknown_u1_values_01
        type: u1
        repeat: expr
        repeat-expr: 2
      - id: unknown_s4_values_03
        type: s4
        repeat: expr
        repeat-expr: 4
      - id: unknown_u1_values_02
        type: u1
        repeat: expr
        repeat-expr: 4
      - id: unknown_s4_03
        type: s4
      - id: unknown_u1_values_03
        type: u1
        repeat: expr
        repeat-expr: 2
      - id: unknown_s4_04
        type: s4
      - id: unknown_u1_values_04
        type: u1
        repeat: expr
        repeat-expr: 3
      - id: unknown_s4_05
        type: s4
      - id: unknown_u2_03
        type: u2
      - id: unknown_s4_06
        type: s4
      - id: unknown_u1_values_05
        type: u1
        repeat: expr
        repeat-expr: 3
      - id: unknown_u2_values_03
        type: u2
        repeat: expr
        repeat-expr: 3
      - id: unknown_u1_values_06
        type: u1
        repeat: expr
        repeat-expr: 4
      - id: unknown_u1_list_01
        type: u1_list
      - id: unknown_u1_list_02
        type: u1_list
      - id: unknown_u1_values_07
        type: u1
        repeat: expr
        repeat-expr: 3

  dated_string_record_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: dated_string_record
        repeat: expr
        repeat-expr: count

  dated_string_record:
    seq:
      - id: date
        type: fhm_common::qdate_julian
      - id: unknown_u1_01
        type: u1
      - id: unknown_s4_values_01
        type: s4
        repeat: expr
        repeat-expr: 3
      - id: text
        type: fhm_common::qstring
      - id: unknown_s4_values_02
        type: s4
        repeat: expr
        repeat-expr: 2

  dated_u2_record_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: dated_u2_record
        repeat: expr
        repeat-expr: count

  dated_u2_record:
    seq:
      - id: date
        type: fhm_common::qdate
      - id: unknown_u2_values
        type: u2
        repeat: expr
        repeat-expr: 2

  s4_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: s4
        repeat: expr
        repeat-expr: count

  u2_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: u2
        repeat: expr
        repeat-expr: count

  u1_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: u1
        repeat: expr
        repeat-expr: count

  fixed_2_record_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: fixed_2_record
        repeat: expr
        repeat-expr: count

  fixed_2_record:
    seq:
      - id: values
        type: u1
        repeat: expr
        repeat-expr: 2

  fixed_8_record_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: fixed_8_record
        repeat: expr
        repeat-expr: count

  fixed_8_record:
    seq:
      - id: data
        size: 8

  fixed_16_record_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: fixed_16_record
        repeat: expr
        repeat-expr: count

  fixed_16_record:
    seq:
      - id: data
        size: 16

  fixed_23_record_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: fixed_23_record
        repeat: expr
        repeat-expr: count

  fixed_23_record:
    seq:
      - id: data
        size: 23

  fixed_24_record_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: fixed_24_record
        repeat: expr
        repeat-expr: count

  fixed_24_record:
    seq:
      - id: data
        size: 24

  u1_s4_pair_list:
    seq:
      - id: count
        type: s4
      - id: entries
        type: u1_s4_pair
        repeat: expr
        repeat-expr: count

  u1_s4_pair:
    seq:
      - id: unknown_u1
        type: u1
      - id: unknown_s4
        type: s4

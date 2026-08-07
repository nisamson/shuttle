meta:
  id: teams
  title: FHM 10 save — teams.dat
  endian: be
  ks-version: '0.10'
  imports:
    - fhm_common
doc: |
  FHM 10 `teams.dat` format. Multi-byte numeric values are big-endian.
  The file contains a version tag, a team count, and that many team records.

enums:
  offensive_orientation:
    0: offensive
    1: balanced
    2: defensive

  physical_orientation:
    0: physical
    1: balanced
    2: non_physical

  fan_happiness_event:
    0: starting_happiness
    1: won_game
    2: won_playoff_round
    3: won_league_championship
    4: won_competition
    5: picked_player_in_first_round
    6: ticket_price_change
    7: signed_extremely_talented_player
    8: signed_very_talented_player
    9: signed_living_legend
    10: signed_extremely_popular_player
    11: signed_popular_player
    12: acquired_extremely_talented_player
    13: acquired_very_talented_player
    14: acquired_living_legend
    15: acquired_extremely_popular_player
    16: acquired_popular_player
    17: fired_staff_member
    18: hired_living_legend
    19: promoted
    20: new_season_optimism
    21: lost_game
    22: lost_playoff_round
    23: missed_playoffs
    24: increased_ticket_prices
    25: extremely_talented_player_became_free_agent
    26: very_talented_player_became_free_agent
    27: living_legend_became_free_agent
    28: extremely_popular_player_became_free_agent
    29: popular_player_became_free_agent
    30: lost_extremely_talented_player
    31: lost_very_talented_player
    32: lost_living_legend
    33: lost_extremely_popular_player
    34: lost_popular_player
    35: traded_longtime_player
    36: fired_living_legend
    37: relegated
    38: beat_main_rival
    39: beat_rival
    40: lost_to_main_rival
    41: lost_to_rival
    42: drafted_first_overall
    43: traded_recent_draft_pick
    44: traded_first_round_pick
    45: reacquired_longtime_player
    46: resigned_longtime_player
    47: upset_in_playoffs
    48: five_game_losing_streak
    49: ten_game_losing_streak
    50: five_game_winning_streak
    51: ten_game_winning_streak
    52: ticket_price_change_alternate
    53: off_ice_incident
    54: midseason_performance_evaluation
    55: season_ending_performance_evaluation
    56: hired_head_coach
    57: hired_general_manager

seq:
  - id: version_tag
    type: s4
  - id: count
    type: s4
  - id: records
    type: team_record(_index)
    repeat: expr
    repeat-expr: count

types:
  team_record:
    params:
      - id: array_index
        type: s4
    seq:
      - id: record_index
        type: s4
      - id: team_id
        type: s4
      - id: internal_code
        type: fhm_common::qstring
      - id: internal_code_2
        type: fhm_common::qstring
      - id: flag_1
        type: u1
      - id: city
        type: fhm_common::qstring
      - id: nickname
        type: fhm_common::qstring
      - id: nickname_placement
        type: u1
      - id: affiliate_parent_id
        type: s4
      - id: affiliate_parent_id_2
        type: s4
      - id: league
        type: s4
      - id: conference
        type: s4
      - id: division
        type: s4
      - id: location_id
        type: s4
      - id: market_size
        type: u2
      - id: fan_loyalty
        type: u2
      - id: finance_1
        type: s4
      - id: finance_2
        type: s4
      - id: finance_3
        type: s4
      - id: unknown_13
        type: s4
      - id: unknown_14
        type: s4
      - id: finance_4
        type: s4
      - id: season_count
        type: s4
      - id: season_history
        type: season_record
        repeat: expr
        repeat-expr: season_count
      - id: franchise_history_block
        type: franchise_history_block
      - id: active_line_unit
        type: line_unit
      - id: leadership_reserve
        type: leadership_reserve
      - id: season_participation
        type: season_participation_chain
      - id: roster
        type: roster_chain
      - id: post_head
        type: post_head
      - id: post_body
        type: post_body
      - id: tail
        type: team_tail

  leadership_reserve:
    seq:
      - id: captain
        type: s4
      - id: alternate_captain_1
        type: s4
      - id: alternate_captain_2
        type: s4
      - id: reserve_count
        type: s4
      - id: reserve_slots
        type: s4
        repeat: expr
        repeat-expr: reserve_count

  season_participation_record:
    seq:
      - id: seq_no
        type: u2
      - id: year
        type: u2
      - id: participation_id
        type: s4
      - id: flag
        type: u1

  season_participation_block:
    seq:
      - id: count
        type: s4
      - id: records
        type: season_participation_record
        repeat: expr
        repeat-expr: count
    instances:
      next_count_peek:
        pos: _io.pos
        type: s4

  season_participation_chain:
    seq:
      - id: blocks
        type: season_participation_block
        repeat: until
        repeat-until: _.next_count_peek == 0

  roster_chain:
    seq:
      - id: lists
        type: roster_id_list
        repeat: until
        repeat-until: _.post_sig_peek == [0, 100, 1]

  post_head:
    seq:
      - id: pre
        size: 7
      - id: list_a
        type: s4_list
      - id: gap
        size: 196
      - id: goalies
        type: s4_list
      - id: defensemen
        type: s4_list
      - id: forwards
        type: s4_list
      - id: region_id
        type: s4
      - id: nation_index
        type: u2
      - id: position_requirement_count
        type: u2
      - id: position_requirement_words
        type: s4_list

  team_tail:
    seq:
      - id: pre
        size: 32
      - id: m_count
        type: u2
      - id: m_words
        type: s4
        valid:
          expr: _ == 3 * m_count
      - id: pre_2_prefix
        size: 139
      - id: pre_2_value
        type: u2
      - id: pre_2_suffix
        size: 21
      - id: m_records
        size: 12
        repeat: expr
        repeat-expr: m_count
      - id: retired_count
        type: s4
      - id: retired_numbers
        type: retired_number
        repeat: expr
        repeat-expr: retired_count
      - id: wiki_url
        type: fhm_common::qstring
      - id: website_url
        type: fhm_common::qstring
      - id: tactics
        type: team_tactics
      - id: rest
        type: team_tail_rest
    instances:
      fan_happiness:
        value: pre_2_value
        if: m_count == 0
        -doc: Current fan happiness on the 1..100 scale.

  team_tail_rest:
    seq:
      - id: major_junior_history
        type: junior_history_list
      - id: main_rival_record_index
        type: s4
      - id: potential_rival_record_index
        type: s4
      - id: potential_rival_progress
        type: u2
      - id: fan_happiness_history
        type: fan_happiness_history_list
      - id: active_line_slot_locks
        type: bool_list
        repeat: expr
        repeat-expr: 13
      - id: unknown_managed_list_01
        type: s4_list
        -doc: |
          First of seventeen managed-team lists. The individual meanings of
          all seventeen lists remain unknown.
      - id: unknown_managed_list_02
        type: s4_list
      - id: unknown_managed_list_03
        type: s4_list
      - id: unknown_managed_list_04
        type: s4_list
      - id: unknown_managed_list_05
        type: s4_list
      - id: unknown_managed_list_06
        type: s4_list
      - id: unknown_managed_list_07
        type: s4_list
      - id: unknown_managed_list_08
        type: s4_list
      - id: unknown_managed_list_09
        type: s4_list
      - id: unknown_managed_list_10
        type: s4_list
      - id: unknown_managed_list_11
        type: s4_list
      - id: unknown_managed_list_12
        type: s4_list
      - id: unknown_managed_list_13
        type: s4_list
      - id: unknown_managed_list_14
        type: s4_list
      - id: unknown_managed_list_15
        type: s4_list
      - id: unknown_managed_list_16
        type: s4_list
      - id: unknown_managed_list_17
        type: s4_list
      - id: unknown_s4_1
        type: s4
      - id: unknown_s4_2
        type: s4
      - id: unsigned_short_lists
        type: u2_list
        repeat: expr
        repeat-expr: 7
      - id: additional_player_ids
        type: s4_list
      - id: flag_1
        type: u1
      - id: flag_2
        type: u1
      - id: unknown_s4_3
        type: s4
      - id: flag_3
        type: u1
      - id: unknown_f8_1
        type: f8
      - id: unknown_s4_4
        type: s4
      - id: unknown_u1_1
        type: u1
      - id: unknown_u1_2
        type: u1
      - id: unknown_u1_3
        type: u1
      - id: unknown_u2_1
        type: u2
      - id: unknown_u2_2
        type: u2
      - id: unknown_u1_4
        type: u1
      - id: unknown_u1_5
        type: u1
      - id: flags_4_to_9
        type: u1
        repeat: expr
        repeat-expr: 6
      - id: unknown_u1_6_to_9
        type: u1
        repeat: expr
        repeat-expr: 4
      - id: unknown_u2_3
        type: u2
      - id: unknown_f8_2
        type: f8
      - id: unknown_s4_5
        type: s4
      - id: flag_10
        type: u1
      - id: unknown_u2_4
        type: u2
      - id: unknown_u1_10
        type: u1
      - id: finance_curve_records
        type: fixed_89_list
      - id: unknown_s4_6
        type: s4
      - id: unknown_f8_3
        type: f8
      - id: flag_11
        type: u1
      - id: unknown_u1_11_to_13
        type: u1
        repeat: expr
        repeat-expr: 3
      - id: flag_12
        type: u1
      - id: nested_player_id_lists
        type: nested_s4_lists
      - id: flag_13
        type: u1
      - id: flag_14
        type: u1
      - id: unknown_u1_14_to_16
        type: u1
        repeat: expr
        repeat-expr: 3
      - id: tagged_player_ids
        type: fixed_5_list
      - id: closing_u2_1
        type: u2
      - id: closing_u2_2
        type: u2
      - id: closing_flag_1
        type: u1
      - id: closing_f8_1
        type: f8
      - id: closing_f8_2
        type: f8
      - id: closing_flag_2
        type: u1
      - id: closing_f8_3
        type: f8
      - id: closing_f8_4
        type: f8
      - id: closing_flag_3
        type: u1
      - id: closing_u2_3
        type: u2
      - id: closing_u2_4
        type: u2
      - id: closing_flag_4
        type: u1
      - id: closing_s4_1
        type: s4
      - id: closing_s4_2
        type: s4
      - id: closing_flags
        type: u1
        repeat: expr
        repeat-expr: 9
      - id: closing_s4_3
        type: s4

  junior_history_list:
    seq:
      - id: count
        type: s4
      - id: records
        type: junior_history_record
        repeat: expr
        repeat-expr: count

  junior_history_record:
    seq:
      - id: flag
        type: u2
      - id: year
        type: u2
      - id: team_id
        type: s4
      - id: pad
        type: u1

  fan_happiness_history_list:
    seq:
      - id: count
        type: s4
      - id: records
        type: fan_happiness_history_record
        repeat: expr
        repeat-expr: count

  fan_happiness_history_record:
    -doc: |
      Fan-happiness adjustment with the resulting 1..100 value and optional
      related identities.
    seq:
      - id: event_type
        type: u2
        enum: fan_happiness_event
      - id: resulting_happiness
        type: u2
      - id: player_id
        type: s4
      - id: staff_id
        type: s4
      - id: related_team_record_index
        type: s4
      - id: competition_id
        type: s4
      - id: league_id
        type: s4

  bool_list:
    seq:
      - id: count
        type: s4
      - id: values
        type: u1
        repeat: expr
        repeat-expr: count

  u2_list:
    seq:
      - id: count
        type: s4
      - id: values
        type: u2
        repeat: expr
        repeat-expr: count

  fixed_89_list:
    seq:
      - id: count
        type: s4
      - id: records
        size: 89
        repeat: expr
        repeat-expr: count

  nested_s4_lists:
    seq:
      - id: count
        type: s4
      - id: lists
        type: s4_list
        repeat: expr
        repeat-expr: count

  fixed_5_list:
    seq:
      - id: count
        type: s4
      - id: records
        type: tagged_player_id
        repeat: expr
        repeat-expr: count

  tagged_player_id:
    seq:
      - id: player_id
        type: s4
      - id: tag
        type: u1

  retired_number:
    seq:
      - id: year
        type: u2
      - id: number
        type: u2
      - id: flag
        type: u2
      - id: player_ref
        type: s4

  team_tactics:
    seq:
      - id: team_value_1
        type: u2
      - id: team_flag
        type: u1
      - id: team_rating
        type: u1
      - id: team_value_2
        type: s4
      - id: team_value_3
        type: s4
      - id: team_value_4
        type: s4
      - id: tactics_object_version
        type: s4
      - id: base_settings
        type: u2
        repeat: expr
        repeat-expr: 59
      - id: selectors
        type: zone_selector_block(_index)
        repeat: expr
        repeat-expr: 22
        -doc: |
          Selector mapping: block 0 global; 1..4 even-strength forward lines;
          5..6 power play 5-on-4; 7..8 power play 5-on-3; 9..10 power play
          4-on-3; 11..13 penalty kill 4-on-5; 14..15 penalty kill 3-on-5;
          16..17 penalty kill 3-on-4; 18..19 4-on-4; 20..21 3-on-3.
      - id: final_offensive_orientation
        type: u2
        enum: offensive_orientation
      - id: final_physical_orientation
        type: u2
        enum: physical_orientation
      - id: final_use_own_settings_flags
        type: u1
        repeat: expr
        repeat-expr: 2
        -doc: |
          Use-settings flags for selector blocks 20 and 21. False selects the
          global fallback.
      - id: tendencies
        type: tendency_block
        repeat: expr
        repeat-expr: 22

  zone_selector_block:
    params:
      - id: block_index
        type: s4
    seq:
      - id: systems
        type: u2
        repeat: expr
        repeat-expr: 12
      - id: offensive_orientation
        type: u2
        enum: offensive_orientation
        if: block_index != 21
      - id: physical_orientation
        type: u2
        enum: physical_orientation
        if: block_index != 21
      - id: delayed_use_own_settings_flags
        type: u1
        repeat: expr
        repeat-expr: 'block_index == 4 ? 4 : (block_index == 13 ? 3 : ((block_index == 6 or block_index == 8 or block_index == 10 or block_index == 15 or block_index == 17 or block_index == 19) ? 2 : 0))'
        -doc: |
          Use-settings flags for blocks 1..19, grouped after blocks 4, 6, 8,
          10, 13, 15, 17, and 19. False selects the global fallback.

  tendency_block:
    -doc: |
      Values are ordered Aggressiveness, Attacking, Backchecking, Hitting,
      Passing, Pressure, Shooting, Tempo. The `overrides` array uses the same
      order. In unit blocks, a false override inherits the corresponding value
      from block 0.
    seq:
      - id: values
        type: u2
        repeat: expr
        repeat-expr: 8
      - id: overrides
        type: u1
        repeat: expr
        repeat-expr: 8

  post_body:
    seq:
      - id: open_roster_slots
        type: s4
      - id: all_time_players
        type: s4_list
      - id: pre_colour_value
        type: u2
      - id: pre_colour_list
        type: s4_list
      - id: pre_colour_flag_1
        type: u1
      - id: pre_colour_value_1
        type: s4
      - id: pre_colour_value_2
        type: s4
      - id: pre_colour_value_3
        type: s4
      - id: pre_colour_value_4
        type: u2
      - id: pre_colour_value_5
        type: u2
      - id: pre_colour_flag_2
        type: u1
      - id: pre_colour_flag_3
        type: u1
      - id: colours
        type: qcolor
        repeat: expr
        repeat-expr: 13
      - id: gap_2
        size: 41
      - id: abbreviation
        type: fhm_common::qstring
      - id: pad
        size: 1
      - id: units
        type: post_unit
        repeat: expr
        repeat-expr: 4

  qcolor:
    seq:
      - id: spec
        type: u1
      - id: alpha
        type: u2
      - id: red
        type: u2
      - id: green
        type: u2
      - id: blue
        type: u2
      - id: reserved
        contents: [0, 0]

  post_unit:
    seq:
      - id: magic
        contents: [0, 0x0a]
      - id: values
        type: s4_list

  s4_list:
    seq:
      - id: count
        type: s4
      - id: values
        type: s4
        repeat: expr
        repeat-expr: count

  roster_id_list:
    seq:
      - id: count
        type: s4
      - id: refs
        type: s4
        repeat: expr
        repeat-expr: count
    instances:
      post_sig_peek:
        pos: _io.pos + 4
        size: 3

  franchise_history_block:
    seq:
      - id: head
        size: 115
      - id: season_stat_count
        type: s4
      - id: head_tail
        size: 8
      - id: season_stats
        type: season_stat_record
        repeat: expr
        repeat-expr: season_stat_count
      - id: name_history_count
        type: s4
      - id: name_history_string_count
        type: s4
      - id: name_history
        type: fhm_common::qstring
        repeat: expr
        repeat-expr: name_history_string_count
      - id: abbreviation_history_count
        type: s4
      - id: abbreviation_history
        type: fhm_common::qstring
        repeat: expr
        repeat-expr: abbreviation_history_count

  season_stat_record:
    seq:
      - id: body
        size: 108
      - id: flag
        type: u1
      - id: value
        type: s4
      - id: year
        type: u2

  season_record:
    seq:
      - id: year
        type: s4
      - id: city
        type: fhm_common::qstring
      - id: nickname
        type: fhm_common::qstring
      - id: abbreviation
        type: fhm_common::qstring
      - id: stats
        size: 134

  tactic_selection:
    -doc: Tactics are represented by `team_tactics`.

  line_unit:
    -doc: |
      Thirteen situational slot lists in this order: even-strength forwards,
      even-strength defense, power play 5-on-4, power play 5-on-3, penalty kill
      4-on-5, penalty kill 3-on-5, 4-on-4, 3-on-3, power play 4-on-3,
      penalty kill 3-on-4, extra attackers, shootout order, and goalies.
    seq:
      - id: lists
        type: line_slot_list
        repeat: expr
        repeat-expr: 13

  line_slot_list:
    seq:
      - id: num_slots
        type: s4
      - id: slots
        type: s4
        repeat: expr
        repeat-expr: num_slots
        -doc: |
          Each active slot stores the player's internal serialized identity.
          In version 58 this equals the zero-based record ordinal in
          `players.dat`. A value of -1 means empty.

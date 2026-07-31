meta:
  id: players
  title: FHM 10 save — players.dat
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Franchise Hockey Manager 10 `players.dat`: a Qt QDataStream (big-endian)
  container of `player_count` variable-length player records.

  Only the container framing (`format_version`, `player_count`) is modeled as a
  live parse; the concatenated player records are captured as one opaque
  `players_payload` field. A player record carries no length prefix, so the
  record sequence cannot be split without a byte-exact record layout, and the
  exact on-disk field order of a full record (bio, contract, status, six season
  stat-line lists, dates and doubles, all under save-version gates) is not fully
  pinned for this observed `format_version`.

  The confirmed record sub-structures are provided below as reference `types`
  (`rating_attributes`, `player_role_instance`, `special_ability_list`) for
  documentation; they are not wired into the live parse. See the folder README.
seq:
  - id: format_version
    type: s4
    doc: Save format version; gates optional fields inside each record.
  - id: player_count
    type: s4
  - id: players_payload
    size-eos: true
    doc: |
      `player_count` concatenated player records (opaque). Each record begins
      with the confirmed leading fields documented in `player_leading_fields`
      and contains, among other data, a `rating_attributes` block, up to two
      `player_role_instance`s (each guarded by an `s4` presence marker, `-1` =
      absent), and a `special_ability_list`.

types:
  # ---- Confirmed record sub-structures (reference / documentation only) ----

  # The 58 one-byte rating attributes (values 0..50, almost always 0..20).
  # Listed here in their exact on-disk read order, which is a fixed scramble of
  # the in-memory offsets 0x2c0..0x2f9 (hidden attributes first, then visible
  # mental/physical, then on-ice skill and goalie attributes). A skater row
  # fills only skater attributes and a goaltender row only goalie attributes;
  # the unused subset reads 0.
  rating_attributes:
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

  # One inline System-3 (detailed skating role) instance. In the record each of
  # the two instances (primary, secondary) is preceded by an s4 presence marker
  # (-1 = null/absent; otherwise the instance follows). role_id indexes the
  # player_roles.dat catalogue (-1 = derive from position on load).
  player_role_instance:
    seq:
      - id: role_id
        type: s4
      - id: flags
        type: u1
        repeat: expr
        repeat-expr: 9
      - id: sub_ratings
        type: u2
        repeat: expr
        repeat-expr: 9

  # Special abilities (traits): a QList<quint8> of ability ids.
  special_ability_list:
    seq:
      - id: num_abilities
        type: s4
      - id: ability_ids
        type: u1
        repeat: expr
        repeat-expr: num_abilities

  # Confirmed leading fields at the start of each player record, in on-disk
  # read order (documentation only; the record continues beyond these fields).
  player_leading_fields:
    seq:
      - id: player_id
        type: s4
      - id: nation_id_1
        type: s4
      - id: nation_id_2
        type: s4
      - id: birth_date
        type: fhm_common::qdate
        doc: Read as three int32 year/month/day.
      - id: bio_u16
        type: u2
        repeat: expr
        repeat-expr: 3
      - id: club_refs
        type: s4
        repeat: expr
        repeat-expr: 6
        doc: Club index references; last entry is the parent-club index.
      - id: bio_i32
        type: s4
        repeat: expr
        repeat-expr: 2
      - id: first_name
        type: fhm_common::qstring
      - id: last_name
        type: fhm_common::qstring
      - id: common_name
        type: fhm_common::qstring
      - id: bio_u16_after_name
        type: u2
      - id: position_ratings
        type: position_list
        doc: Per-position ratings (G, LD, RD, LW, C, RW at even indices).

  position_list:
    seq:
      - id: num_values
        type: s4
      - id: values
        type: u2
        repeat: expr
        repeat-expr: num_values

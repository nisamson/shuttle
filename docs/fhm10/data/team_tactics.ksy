meta:
  id: team_tactics
  title: FHM 10 built-in per-zone tactic-system catalogue
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Franchise Hockey Manager 10 built-in tactic-system catalogue.

  Despite the file name, this is NOT a per-team or user-preset store. It is the
  game's static master list of the selectable tactic *systems*, one record per
  option, that populate the per-zone dropdowns on the tactics screen. In the
  reference save it holds 72 records across 12 tactical-zone groups, and it is
  byte-identical across the save's rolling backups (`rs_one`, `rs_two`) — i.e.
  built-in data that does not change between saves.

  Each record carries a globally-unique id, the id of the tactical zone/slot it
  belongs to, the system's display name, and two small 1..5 descriptor ratings.
  This is the human-readable name source a renderer/validator needs to turn a
  Team's per-zone selector bytes (see teams.dat `Tactic`) into meaning; the
  selector for a zone indexes that zone's members here.
seq:
  - id: version_tag
    type: s4
    doc: always 0 in the reference save
  - id: num_records
    type: s4
    doc: number of tactic-system records (72 in the reference save)
  - id: records
    type: tactic_system
    repeat: expr
    repeat-expr: num_records

types:
  tactic_system:
    doc: |
      One selectable tactic system for a given tactical zone. Serialized by
      `TacticPreset_writeToStream` (@140662320) in field order p[1], p[0],
      name(p+2), p[4], p[5].
    seq:
      - id: global_id
        type: s4
        doc: |
          Globally-unique sequential id (0..num_records-1 in the save;
          in-memory p[1] @0x04). This is the value a team's per-zone tactic
          selector stores in teams.dat (confirmed by byte-diff: an in-game
          Cycle->Triangle OZ-attack change flipped the selector 12 -> 16, the
          global_ids of those two systems, not their within-group ordinals
          0 -> 4). teams.dat holds 12 such selectors, one per zone_group_id.
      - id: zone_group_id
        type: s4
        enum: tactic_zone
        doc: |
          Tactical zone this system belongs to (0..11), confirmed against the
          in-game tactics screen (in-memory p[0] @0x00). Records are grouped
          contiguously by this value; a zone's selector byte in teams.dat
          indexes the members that share this id. The file is ordered by
          strength state then phase of play. Member counts per group in the
          reference save: 0:6, 1:6, 2:9, 3:5, 4:7, 5:7, 6:6, 7:8, 8:3, 9:5,
          10:7, 11:3.
      - id: name
        type: fhm_common::qstring
        doc: |
          System display name (e.g. "Cycle", "1-3-1 Trap", "Umbrella"). This is
          the per-zone system name; the earlier catalogue note that these names
          existed only in tactic_templates.dat is superseded by this file.
      - id: rating_a
        type: s4
        doc: |
          Aggressiveness / risk descriptor, 1..5 (in-memory p[4] @0x10).
          Strongly evidenced by self-describing groups, e.g. group 8:
          "Pursue Aggressively"=5, "Stand your Ground"=3, "Back Up"=1; group 11:
          "Counterattack"=5, "Puck Possession"=3, "Dump and Retreat"=1.
      - id: rating_b
        type: s4
        doc: |
          Second 1..5 descriptor (in-memory p[5] @0x14): a
          spread-vs-compactness axis. High (5) = the system spreads players
          across the ice (perimeter / passive-deep / wide coverage); low (1) =
          it collapses toward the puck or net (tight / direct / net-front).
          Orthogonal to rating_a (hence not its inverse). Inferred from the
          full catalogue and validated by the save author's hockey knowledge
          (the UI shows no numeric ratings; the file is static so it cannot be
          byte-diffed).

enums:
  tactic_zone:
    # Confirmed against the in-game tactics screen; ordered by strength state
    # then phase of play.
    0: breakout               # ES: DZ possession -> transition to offense
    1: nz_offense             # ES: carrying through the NZ toward the OZ
    2: oz_attack              # ES: offensive-zone attack
    3: forecheck              # ES: defending vs the opponent's breakout
    4: nz_coverage            # ES: transitioning through the NZ into defense
    5: dz_coverage            # ES: defensive-zone coverage
    6: pp_breakout            # PP: power-play breakout
    7: pp_oz_attack           # PP: power-play offensive-zone attack
    8: pp_defense             # PP: defending while on the power play
    9: pk_forecheck           # PK: penalty-kill forecheck
    10: pk_dz_coverage        # PK: penalty-kill defensive-zone coverage
    11: pk_attack             # PK: penalty-kill attack

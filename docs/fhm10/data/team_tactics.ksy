meta:
  id: team_tactics
  title: FHM 10 built-in per-zone tactic-system catalogue
  endian: be
  ks-version: '0.10'
  imports:
    - fhm_common
doc: |
  Built-in catalogue of selectable tactic systems.

  Team tactic selectors store a `global_id` from this catalogue for each of
  the twelve tactical zones. Records provide the display name and two
  descriptor ratings for each system.
seq:
  - id: version_tag
    type: s4
    doc: Catalogue format version.
  - id: num_records
    type: s4
    doc: Number of tactic-system records.
  - id: records
    type: tactic_system
    repeat: expr
    repeat-expr: num_records

types:
  tactic_system:
    doc: |
      One selectable tactic system for a given tactical zone.
    seq:
      - id: global_id
        type: s4
        doc: |
          Globally unique system id. This is the value stored by each
          `teams.dat` zone selector.
      - id: zone_group_id
        type: s4
        enum: tactic_zone
        doc: |
          Tactical zone to which this system belongs. Records are grouped by
          zone and ordered by strength state and phase of play.
      - id: name
        type: fhm_common::qstring
        doc: |
          System display name, such as `Cycle`, `1-3-1 Trap`, or `Umbrella`.
      - id: rating_a
        type: s4
        doc: |
          Aggressiveness or risk descriptor on a 1..5 scale.
      - id: rating_b
        type: s4
        doc: |
          Spread-versus-compactness descriptor on a 1..5 scale. Higher values
          spread players across the ice; lower values concentrate play toward
          the puck or net.

enums:
  tactic_zone:
    # Ordered by strength state and phase of play.
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

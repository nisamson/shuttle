meta:
  id: fhm_common
  title: FHM 10 save — shared Qt QDataStream primitives and enums
  endian: be
  ks-version: 0.10
doc: |
  Shared building blocks for the Franchise Hockey Manager 10 save-folder `*.dat`
  files. Every file uses Qt's QDataStream in its default big-endian byte order.

  This module is imported by the per-file specs and provides:
    * `qstring` / `qbytes_string` — length-prefixed strings.
    * `qdate` / `qdate_julian` — the two date encodings seen in records.
    * enums shared across files (playing role, squad status).

  QList<T> has no generic Kaitai representation: it is a `s4 count` followed by
  `count` elements of T. Each per-file spec expresses a QList inline with
  `repeat: expr` / `repeat-expr: <count>` over the appropriate element type.

types:
  # QString: s4 byte-length prefix, then that many bytes of UTF-16BE code units.
  # A byte-length of -1 (0xFFFFFFFF) denotes a null string; 0 denotes empty.
  qstring:
    seq:
      - id: byte_length
        type: s4
      - id: value
        type: str
        size: byte_length
        encoding: UTF-16BE
        if: byte_length > 0
    instances:
      is_null:
        value: byte_length < 0

  # QDate serialized as three separate int32 fields (year, month, day).
  qdate:
    seq:
      - id: year
        type: s4
      - id: month
        type: s4
      - id: day
        type: s4

  # QDate serialized as a single Julian-day int64 (used by some records).
  qdate_julian:
    seq:
      - id: julian_day
        type: s8

enums:
  # System 1 — playing role (broad AI archetype), stored as quint16.
  # The "none" sentinel is written as -1, i.e. 0xFFFF when read as unsigned.
  playing_role:
    0: agitator
    1: defensive_defenceman
    2: checking_forward
    3: enforcer
    4: goalscorer
    5: grinder
    6: offensive_defenceman
    7: offensive_forward
    8: playmaker
    9: power_forward
    10: screener
    11: two_way_defenceman
    12: two_way_forward
    13: standard_goalie
    14: puckhandling_goalie
    65535: none

  # System 2 — squad status / identity, stored as quint16.
  squad_status:
    0: none
    1: franchise_player
    2: star_player
    3: leader
    4: blue_chip_prospect
    5: prospect
    6: fringe_prospect
    7: policeman
    8: depth
    9: starting_goalie
    10: backup_goalie

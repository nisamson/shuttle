meta:
  id: leagues
  title: FHM 10 save — leagues.dat
  endian: be
  ks-version: '0.10'
  imports:
    - fhm_common
doc: |
  Kaitai Struct description of the FHM 10 leagues.dat container.
  The record starts with confirmed identity, name, and early scalar/date anchors;
  the remaining league configuration and embedded container area is preserved as
  opaque bytes because the available format notes do not define element sizes for
  every nested league sub-record.

  LIMITATION: the opaque league body extends to end-of-file, so this spec
  faithfully parses a single-league container (`count == 1`, as written by the
  observed save). A file with multiple league records would require the
  per-record league size to be resolved first.
seq:
  - id: version_tag
    type: s4
    doc: Container version tag read from the stream.
  - id: count
    type: s4
    doc: Number of league records.
  - id: records
    type: league
    repeat: expr
    repeat-expr: count

types:
  league:
    doc: League definition record.
    seq:
      - id: league_id
        type: s4
        doc: League id.
      - id: flag_0
        type: u1
        doc: Boolean flag.
      - id: flag_1
        type: u1
        doc: Boolean flag.
      - id: flag_2
        type: u1
        doc: Boolean flag.
      - id: name
        type: fhm_common::qstring
        doc: Full league name.
      - id: short_name
        type: fhm_common::qstring
        doc: Short league name.
      - id: abbreviation
        type: fhm_common::qstring
        doc: League abbreviation or cup name.
      - id: nickname
        type: fhm_common::qstring
        doc: League nickname.
      - id: type_parent_id
        type: u2
        doc: Type or parent id.
      - id: type_level_id
        type: u2
        doc: Type or level id.
      - id: config_double_0
        type: f8
        doc: Opaque league configuration double.
      - id: early_int_0
        type: s4
        doc: Early scalar.
      - id: early_int_1
        type: s4
        doc: Early scalar.
      - id: config_doubles_primary
        type: f8
        repeat: expr
        repeat-expr: 17
        doc: Opaque league configuration doubles.
      - id: config_int_0
        type: s4
        doc: Opaque league configuration scalar.
      - id: config_int_1
        type: s4
        doc: Opaque league configuration scalar.
      - id: config_u16_0
        type: u2
        doc: Opaque league configuration scalar.
      - id: config_int_2
        type: s4
        doc: Opaque league configuration scalar.
      - id: config_u16_1
        type: u2
        doc: Opaque league configuration scalar.
      - id: founding_date
        type: fhm_common::qdate
        doc: Founding or start date.
      - id: opaque_league_body
        size-eos: true
        doc: "Opaque bytes: remaining league config, embedded lists, dates, u16 blocks, and trailing scalars."

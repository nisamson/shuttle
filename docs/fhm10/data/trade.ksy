meta:
  id: trade
  title: FHM 10 save — trade.dat
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Kaitai Struct description of the FHM 10 trade.dat container and
  reusable trade-related record types. Values are Qt QDataStream big-endian.
seq:
  - id: version_tag
    type: s4
    doc: Container version tag read from the stream.
  - id: count
    type: s4
    doc: Number of active trade records.
  - id: records
    type: trade_record
    repeat: expr
    repeat-expr: count

types:
  int_list:
    doc: QList<qint32>.
    seq:
      - id: count
        type: s4
      - id: items
        type: s4
        repeat: expr
        repeat-expr: count

  int_u16_pair:
    doc: QPair<qint32, quint16>.
    seq:
      - id: id
        type: s4
      - id: value
        type: u2

  pair_list:
    doc: QList<QPair<qint32, quint16>>.
    seq:
      - id: count
        type: s4
      - id: items
        type: int_u16_pair
        repeat: expr
        repeat-expr: count

  draft_pick_descriptor:
    doc: Historical trade draft-pick descriptor.
    seq:
      - id: field_u16
        type: u2
      - id: field_int_0
        type: s4
      - id: field_int_1
        type: s4
      - id: field_int_2
        type: s4
      - id: field_u16_0
        type: u2
      - id: field_u16_1
        type: u2

  draft_pick_list:
    doc: QList<historical draft-pick descriptor>.
    seq:
      - id: count
        type: s4
      - id: items
        type: draft_pick_descriptor
        repeat: expr
        repeat-expr: count

  trade_record:
    doc: Active trade proposal record.
    seq:
      - id: field0
        type: s4
      - id: date
        type: fhm_common::qdate
      - id: field1
        type: s4
      - id: field2
        type: s4
      - id: field3
        type: s4
      - id: field4
        type: u2
      - id: field5
        type: s4
      - id: list_a
        type: int_list
      - id: list_b
        type: int_list
      - id: list_c
        type: int_list
      - id: list_d
        type: int_list
      - id: list_e
        type: int_list
      - id: field6
        type: u2
      - id: list_f
        type: int_list
      - id: field7
        type: u2
      - id: list_g
        type: int_list
      - id: field8
        type: u2
      - id: list_h
        type: int_list
      - id: field9
        type: u2
      - id: field10
        type: u2
      - id: field11
        type: u2
      - id: field12
        type: s4
      - id: field13
        type: s4
      - id: flag
        type: u1
      - id: field14
        type: u2
      - id: field15
        type: s4
      - id: field16
        type: s4
      - id: pairs_a
        type: pair_list
      - id: pairs_b
        type: pair_list

  trade_history_entry:
    doc: Completed trade history record.
    seq:
      - id: id
        type: s4
      - id: date
        type: fhm_common::qdate
      - id: team_a
        type: s4
      - id: team_b
        type: s4
      - id: field2
        type: s4
      - id: field3
        type: s4
      - id: list_a
        type: int_list
      - id: list_b
        type: int_list
      - id: list_c
        type: int_list
      - id: list_d
        type: int_list
      - id: list_e
        type: int_list
      - id: list_f
        type: int_list
      - id: list_g
        type: int_list
      - id: list_h
        type: int_list
      - id: list_i
        type: int_list
      - id: list_j
        type: int_list
      - id: picks_a
        type: draft_pick_list
      - id: picks_b
        type: draft_pick_list
      - id: flag
        type: u1

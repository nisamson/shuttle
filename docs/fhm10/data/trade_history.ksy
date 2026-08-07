meta:
  id: trade_history
  title: FHM 10 save — trade_history.dat
  endian: be
  ks-version: '0.10'
  imports:
    - fhm_common
    - trade
doc: |
  Kaitai Struct description of the FHM 10 trade_history.dat
  container. The completed-trade record is reused from trade.ksy.
seq:
  - id: version_tag
    type: s4
    doc: Container version tag read from the stream.
  - id: count
    type: s4
    doc: Header record count.
  - id: records
    type: trade::trade_history_entry
    repeat: expr
    repeat-expr: count

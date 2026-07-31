meta:
  id: info
  title: FHM 10 save — info.dat
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Save summary sidecar. The file has no version tag, count, or record list;
  it contains two Qt QString values.
seq:
  - id: description
    type: fhm_common::qstring
    -doc: load dialog description
  - id: name_id
    type: fhm_common::qstring
    -doc: save or world name

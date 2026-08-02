meta:
  id: game_settings
  title: FHM 10 save — game_settings.dat
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Flat game-options payload. The file has no leading version tag and no count;
  fields are written in this fixed order as Qt QDataStream values.
seq:
  - id: setting_001
    type: u1
    -doc: option flag
  - id: setting_002
    type: u1
    -doc: option flag
  - id: setting_003
    type: u1
    -doc: option flag
  - id: setting_004
    type: u1
    -doc: option flag
  - id: setting_005
    type: u1
    -doc: option flag
  - id: setting_006
    type: u1
    -doc: option flag
  - id: setting_007
    type: u2
    -doc: option value
  - id: setting_008
    type: u1
    -doc: option flag
  - id: setting_009
    type: u2
    -doc: option value
  - id: setting_010
    type: u2
    -doc: option value
  - id: setting_011
    type: u1
    -doc: option flag
  - id: setting_012
    type: u1
    -doc: option flag
  - id: deprecated_flag_013
    type: u1
    valid: 0
    -doc: deprecated false flag
  - id: setting_014
    type: f8
    -doc: option value
  - id: setting_015
    type: f8
    -doc: option value
  - id: setting_016
    type: u1
    -doc: option flag
  - id: setting_017
    type: u2
    -doc: option value
  - id: setting_018
    type: u2
    -doc: option value
  - id: setting_019
    type: u2
    -doc: option value
  - id: setting_020
    type: u1
    -doc: option flag
  - id: setting_021
    type: u2
    -doc: option value
  - id: setting_022
    type: u1
    -doc: option flag
  - id: setting_023
    type: u1
    -doc: option flag
  - id: setting_024
    type: u1
    -doc: option flag
  - id: setting_025
    type: u1
    -doc: option flag
  - id: setting_026
    type: u1
    -doc: option flag
  - id: setting_027
    type: u2
    -doc: option value
  - id: setting_028
    type: u1
    -doc: option flag
  - id: setting_029
    type: u1
    -doc: option flag
  - id: referenced_object_id
    type: s4
    -doc: referenced object id or -1
  - id: setting_031
    type: u1
    -doc: option flag
  - id: setting_032
    type: u1
    -doc: option flag
  - id: setting_033
    type: u1
    -doc: option flag
  - id: setting_034
    type: u1
    -doc: option flag
  - id: setting_035
    type: u1
    -doc: option flag
  - id: setting_036
    type: s4
    -doc: option value
  - id: setting_037
    type: u1
    -doc: option flag
  - id: setting_038
    type: u1
    -doc: option flag
  - id: setting_039
    type: fhm_common::qstring
    -doc: option string
  - id: setting_040
    type: u1
    -doc: option flag
  - id: setting_041
    type: u1
    -doc: option flag
  - id: setting_042
    type: u1
    -doc: option flag
  - id: setting_043
    type: f8
    -doc: option value
  - id: setting_044
    type: fhm_common::qstring
    -doc: option string
  - id: setting_045
    type: u2
    -doc: option value
  - id: setting_046
    type: u2
    -doc: option value
  - id: setting_047
    type: u2
    -doc: option value
  - id: setting_048
    type: u2
    -doc: option value
  - id: setting_049
    type: s4
    -doc: option value
  - id: setting_050
    type: u1
    -doc: option flag
  - id: setting_051
    type: u1
    -doc: option flag
  - id: setting_052
    type: u1
    -doc: option flag
  - id: setting_053
    type: u1
    -doc: option flag
  - id: setting_054
    type: u1
    -doc: option flag
  - id: setting_055
    type: u1
    -doc: option flag
  - id: setting_056
    type: u1
    -doc: option flag
  - id: setting_057
    type: u1
    -doc: option flag
  - id: setting_058
    type: u1
    -doc: option flag
  - id: setting_059
    type: u1
    -doc: option flag
  - id: setting_060
    type: u1
    -doc: option flag
  - id: setting_061
    type: u1
    -doc: option flag
  - id: setting_062
    type: u1
    -doc: option flag
  - id: setting_063
    type: u1
    valid: 0
  - id: setting_064
    type: u1
    -doc: option flag
  - id: setting_065
    type: u1
    -doc: option flag
  - id: setting_066
    type: u1
    -doc: option flag
  - id: setting_067
    type: u1
    -doc: option flag
  - id: setting_068
    type: u1
    -doc: option value
  - id: setting_069
    type: u1
    -doc: option flag
  - id: setting_070
    type: u1
    -doc: option flag
  - id: setting_071
    type: u1
    -doc: option flag
  - id: setting_072
    type: u2
    -doc: option value
  - id: setting_073
    type: u2
    -doc: option value
  - id: setting_074
    type: u1
    -doc: option flag
  - id: setting_075
    type: u1
    -doc: option flag
  - id: setting_076
    type: u1
    -doc: option value
  - id: setting_077
    type: u1
    -doc: option flag
  - id: setting_078
    type: u1
    -doc: option flag
  - id: setting_079
    type: u1
    -doc: option flag
  - id: setting_080
    type: u1
    -doc: option flag

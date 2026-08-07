meta:
  id: tactic_templates
  title: FHM 10 tactic-template catalogue
  endian: be
  ks-version: '0.10'
  imports:
    - fhm_common
doc: |
  Parser for tactic_templates.dat named preset records. This spec is
  compile-only for the IMPEX.lg validation set because that save does not include
  tactic_templates.dat.
seq:
  - id: version
    type: s4
    doc: file format version tag
  - id: num_templates
    type: s4
    doc: number of template records
  - id: templates
    type: template
    repeat: expr
    repeat-expr: num_templates
    doc: named tactic templates
types:
  template:
    seq:
      - id: internal_key
        type: fhm_common::qstring
        doc: internal template key
      - id: template_index
        type: s4
        doc: template index or variant id
      - id: display_name
        type: fhm_common::qstring
        doc: user-facing preset name
      - id: settings_blob
        size: 4856
        doc: opaque


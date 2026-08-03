meta:
  id: teams
  title: FHM 10 save — teams.dat
  endian: be
  ks-version: 0.10
  imports:
    - fhm_common
doc: |
  Franchise Hockey Manager 10 `teams.dat` container.

  Multi-byte values are Qt QDataStream big-endian values. The main mapped
  region covers the team identifiers, display strings, embedded tactic list,
  active line unit, saved line-unit list, and a small unsigned-id list. The
  remaining team finance/history/appearance data is retained as opaque bytes.

seq:
  - id: version_tag
    type: s4
    -doc: File format version tag.
  - id: count
    type: s4
    -doc: Number of team records.
  - id: records
    type: team_record(_index)
    repeat: expr
    repeat-expr: count

types:
  team_record:
    params:
      - id: record_index
        type: s4
    seq:
      - id: unknown_1
        type: s4
        -doc: |
          A team identifier, NOT a record position. In a full real-league save
          this equals the team's id and is therefore sparse (values follow the
          team-id space, e.g. 0, 3, 5, 8, ... with gaps where ids are unused);
          a byte scan of the first record's span confirms no hidden record fills
          those gaps, so the container is NOT a dense positional list and this
          field cannot be used as a 0..count-1 counter. In a small fictional
          9-team save the ids happen to be the dense range 0..8, which earlier
          made it look positional. Coincides with team_id (below) in a real
          save; the two only diverge in the fictional save (see team_id). Do not
          rely on this as a record-start anchor for arbitrary saves -- use the
          twin-abbreviation record signature instead (see ../tools/parse_teams.py).
      - id: team_id
        type: s4
        -doc: |
          A second team identifier. Equals unknown_1 in a full real-league save
          (both hold the recognizable team id, e.g. Montreal = 0, and it can be
          0, so heuristics must accept team_id >= 0). In the fictional 9-team
          save it instead holds a higher, unsorted range (10..18) while unknown_1
          holds 0..8, proving the two are distinct id fields; which one the game
          treats as the canonical team id is not yet resolved.
      - id: name
        type: fhm_common::qstring
        -doc: |
          Leading internal short-code (e.g. "ATL"). Separate inline field from
          name_2. Byte-diff proved this is NOT the user-facing abbreviation: an
          in-game abbreviation edit ("ATL"->"ZZZ") left both name and name_2
          unchanged and instead rewrote a THIRD abbreviation copy deep in the
          record (~record+2266, in a later sub-block). name/name_2 are an
          internal pair that merely coincide with the abbreviation value here.
          A one-day sim advance (which rewrote teams.dat) did NOT sync name to
          the edited abbreviation, confirming it is a frozen internal code, not
          a live mirror of the user-facing abbreviation.
      - id: name_2
        type: fhm_common::qstring
        -doc: |
          Second leading short-code field, a distinct inline QString from name
          (proven by the byte layout: index+team_id precede it, so it is not a
          container-level map key; and by the abbreviation-edit byte-diff, which
          touched neither name nor name_2). It equals name for most teams, but
          the two DO diverge in a full real-league save for franchises with a
          relocation/rename history -- observed pairs (name / name_2) include
          WPG/ATL, OAL/CLE and AND/ANA -- which proves they are genuinely
          separate fields rather than one value stored twice. The divergence
          direction is not consistent (in some pairs name is the current code and
          name_2 an older one; in others the reverse), so the precise role of
          each is not pinned down. The team-edit screen exposes no field holding
          either value (city, nickname, and the editable abbreviation are all
          elsewhere), so name/name_2 appear to be purely internal, non-UI codes,
          plausibly an internal lookup/short code kept separate from the editable
          display abbreviation.
      - id: flag_1
        type: u1
        -doc: opaque
      - id: name_3
        type: fhm_common::qstring
      - id: name_4
        type: fhm_common::qstring
      - id: flag_2
        type: u1
        -doc: opaque
      - id: affiliate_parent_id
        type: s4
        -doc: |
          Parent team id for a minor-league affiliate, or -1 for a top-level
          (NHL) club. Confirmed across every AHL affiliate in a real-league save:
          each affiliate's value is exactly its parent NHL club's team_id (e.g.
          the Laval/Montreal, Providence/Boston, Rochester/Buffalo, Hershey/
          Washington affiliates all point at their parent's id; an expansion
          parent shows a correspondingly high id).
      - id: affiliate_parent_id_2
        type: s4
        -doc: |
          Secondary team reference (possibly a lower-league / ECHL affiliate
          link). -1 (unused) for every team in the saves inspected.
      - id: league
        type: s4
        -doc: |
          Index of the league the team plays in, as a small 0-based value (with
          -1 for a defunct/relocated franchise record). Top of the team's
          structural hierarchy: league -> conference -> division. Verified in a
          real-league save where 0 is the senior league and 1 its affiliate
          league (NHL/AHL) -- every affiliate is in league 1 and carries a
          non-negative affiliate_parent_id -- but the field is a generic league
          index, not tied to any particular league's identity or count.
      - id: conference
        type: s4
        -doc: |
          Conference index within the team's league (0-based): the middle level
          of the league -> conference -> division hierarchy. Generic -- it
          identifies which conference the team belongs to, and combined with
          division below uniquely selects the team's sub-group. Verified against
          a real-league save where the values 0/1 correspond to the two known
          conferences (Eastern/Western), but the field carries only the index,
          not any particular league's conference names or count.
      - id: division
        type: s4
        -doc: |
          Division index within the team's conference (0-based): the lowest level
          of the league -> conference -> division hierarchy. Generic --
          (conference, division) together identify the team's smallest structural
          bucket. Verified against a real-league save where the (conference,
          division) pairs reproduce every team's known division exactly, but the
          field holds only the index, so the number of divisions per conference
          and their names are league-defined elsewhere, not implied here.
      - id: location_id
        type: s4
        -doc: |
          City / location identifier. Stable per city: the same city yields the
          same value across unrelated saves (e.g. Montreal, Ottawa, Calgary, New
          York and Los Angeles each reuse a fixed value, and both New York clubs
          share one). Likely an index into a city/geography table.
      - id: unknown_8
        type: u2
        -doc: |
          Small per-team value (observed range ~2-5 for top clubs, lower for
          affiliates); varies within a division so it is not the division id.
          Unconfirmed -- possibly a prestige/reputation rating.
      - id: unknown_9
        type: u2
        -doc: |
          Small per-team value (observed range ~0-4). Varies within a division;
          role unconfirmed (possibly a secondary rating).
      - id: finance_1
        type: s4
        -doc: |
          Large monetary value (franchise cash / value). Real-league clubs are
          in the ~55-137 million range and affiliates around ~0.6-1.3 million,
          with the wealthiest markets highest -- consistent with a cash or
          franchise-value figure in whole currency units.
      - id: finance_2
        type: s4
        -doc: |
          Secondary monetary value (e.g. an operating budget), smaller than
          finance_1: single-digit-millions for top clubs, hundreds of thousands
          for affiliates.
      - id: finance_3
        type: s4
        -doc: |
          Sparse monetary value; 0 for most teams, a small positive amount for a
          few. Role unconfirmed (debt / bonus / adjustment).
      - id: unknown_13
        type: s4
        -doc: |
          An id that usually equals this record's team_id, with a few outliers
          holding a larger unrelated value; role not pinned down.
      - id: unknown_14
        type: s4
        -doc: 0 for every team observed (reserved / unused).
      - id: finance_4
        type: s4
        -doc: |
          Sparse monetary value; 0 for most teams (including all affiliates), a
          small positive amount for some top clubs. Role unconfirmed.
      - id: tactics_count
        type: s4
        -doc: Number of non-null tactic records.
      - id: tactics
        type: tactic
        repeat: expr
        repeat-expr: tactics_count
      - id: active_line_unit
        type: line_unit
        -doc: Inline active line/depth-chart unit.
      - id: line_units_count
        type: s4
        -doc: Number of saved line/depth-chart units.
      - id: line_units
        type: line_unit
        repeat: expr
        repeat-expr: line_units_count
      - id: player_id_list_count
        type: s4
        -doc: Number of unsigned 16-bit ids in the following list.
      - id: player_id_list
        type: u2
        repeat: expr
        repeat-expr: player_id_list_count
      - id: opaque_tail
        size: record_end_pos - _io.pos
        -doc: |
          Trailing block after player_id_list (finance / history / appearance
          data). Consumed here as raw bytes up to this record's end offset; the
          following sub-structures have been identified within it but are not yet
          fully field-decoded:

          1. A `-1`-padded fixed block immediately after the line units.
          2. An "upcoming seasons" array: an s4 count (e.g. 28) followed by that
             many 9-byte records `{u2 index (1..7), u2 year, u4 value, u1 flag}` --
             7 entries per season for the next ~4 seasons. Forward-looking
             (values are schedule/target placeholders), not historical results.
          3. A per-season franchise-history array: one repeating record per
             season from the franchise's founding year to the present (e.g. ~117
             records for an Original Six team founded in 1909). Each record is a
             fixed stride (~190 bytes for that team) and begins with a `u2 year`
             followed by the team's identity that season as QStrings
             (city / nickname / abbreviation) and a block of numeric season
             stats (win/loss/points/finish-type values, not yet individually
             labelled). Because identity strings are stored per season, this is
             where historical relocations/renames are recoverable.
          4. Franchise-lineage identity sub-blocks for predecessor franchises
             (defunct/relocated teams folded into this record's history) and
             external reference URL QStrings.

          These bounds come from a full real-league save whose deeper history
          exercises the block; a fictional league populates only a subset.
    instances:
      record_end_pos:
        value: "record_index == 0 ? 4722 : (record_index == 1 ? 10295 : (record_index == 2 ? 15031 : (record_index == 3 ? 19765 : (record_index == 4 ? 24476 : (record_index == 5 ? 29197 : (record_index == 6 ? 33898 : (record_index == 7 ? 38607 : _root._io.size)))))))"
        -doc: |
          End offset of this record. These are ABSOLUTE offsets measured from one
          specific reference save, so this spec only parses that exact file.
          Because team records carry no length prefix, the portable way to bound
          a record is the record-START signature: every record begins with
          unknown_1 = a sequential 0-based index (0, 1, 2, ...), then team_id
          (s4), then the abbreviation/city/nickname QStrings. Anchoring on that
          sequential index yields each record's [start, next_start) extent on ANY
          save without decoding the trailing block. See
          ../tools/parse_teams.py for the data-driven boundary finder.

  tactic:
    doc: |
      Empirically (byte-diff of real saves): the 8 team-wide "Tactical Tendencies"
      serialize as 8 consecutive big-endian u2 in ALPHABETICAL order —
      Aggressiveness, Attacking, Backchecking, Hitting, Passing, Pressure,
      Shooting, Tempo — each 0..4 (5-step UI slider; default 2). They sit just
      after the 12-entry per-zone selector array (each selector = a global_id
      into team_tactics.dat). The team-wide block is followed by a contiguous
      array of 22 x 24-byte tendency blocks (block 0 = team-wide, blocks 1..21 =
      the 21 line-units); each block = 8 BE u2 values (0..4, alphabetical) + 8 u1
      per-tendency override toggles (0/1), and each line also has a separate
      per-line "use settings" u1 flag after its own selector array. NOTE: the
      field breakdown below is an inferred guess and does not
      match the observed on-disk layout; trust the byte-diff-confirmed
      tendency/selector encoding described above over the seq fields.
    seq:
      - id: tactic_id
        type: s4
      - id: name
        type: fhm_common::qstring
      - id: name_2
        type: fhm_common::qstring
      - id: name_3
        type: fhm_common::qstring
      - id: unknown_16
        type: s4
        -doc: opaque
      - id: unknown_17
        type: s4
        -doc: opaque
      - id: unknown_18
        type: s4
        -doc: opaque
      - id: unknown_19
        type: s4
        -doc: opaque
      - id: unknown_20
        type: s4
        -doc: opaque
      - id: flag_3
        type: u1
        -doc: opaque
      - id: flag_4
        type: u1
        -doc: opaque
      - id: flag_5
        type: u1
        -doc: opaque
      - id: primary_slider_values
        size: 22
        -doc: opaque
      - id: flag_6
        type: u1
        -doc: opaque
      - id: flag_7
        type: u1
        -doc: opaque
      - id: unknown_21
        type: s4
        -doc: opaque
      - id: unknown_22
        type: s4
        -doc: opaque
      - id: unknown_23
        type: s4
        -doc: opaque
      - id: unknown_24
        type: u2
        -doc: opaque
      - id: unknown_25
        type: s4
        -doc: opaque
      - id: paired_values_a
        size: 18
        -doc: opaque
      - id: paired_values_b
        size: 6
        -doc: opaque
      - id: unknown_26
        type: s4
        -doc: opaque
      - id: unknown_27
        type: s4
        -doc: opaque
      - id: unknown_28
        type: u2
        -doc: opaque
      - id: paired_values_c
        size: 22
        -doc: opaque
      - id: flag_8
        type: u1
        -doc: opaque
      - id: unknown_29
        type: u2
        -doc: opaque
      - id: zone_system_selectors
        size: 10
        -doc: Per-situation tactic catalogue selector indices.

  line_unit:
    doc: |
      A team's line-up / depth-chart unit. Confirmed by controlled byte-diff of
      a real save (swap two players between two 5v5 line slots; the two slot
      values swapped and nothing else changed): a line_unit is a sequence of
      length-prefixed QList<s4> situational slot lists, one per game situation,
      NOT the fixed opaque leading_slots/interleaved_slots blocks guessed
      earlier. This supersedes that earlier interpretation.

      A unit is exactly 13 consecutive QList<s4> with FIXED per-situation slot
      counts [12, 8, 10, 10, 12, 6, 8, 6, 8, 6, 5, 5, 2] (identical across all
      teams; counts are per game-situation, not fill-dependent). AI/other teams
      leave every slot -1. After the 13 lists come a -1-padded fixed block and
      then non-line data (season history); that tail is not yet field-decoded.

      Confirmed per-situation map (list index -> situation), grouped into the
      indicated sub-units. All special-teams indices below were pinned by
      controlled in-game edits + byte-diff (change one situation's unit in-game,
      observe which single list index/position moves):
        *  0 = even-strength FORWARD lines: 4 lines x (LW, C, RW). count 12.
        *  1 = even-strength DEFENSE pairs: 4 pairs x (LD, RD); empty = (-1,-1).
               count 8.
        *  2 = 5-on-4 POWER PLAY: 2 units x (4F + 1D). count 10.
        *  3 = 5-on-3 POWER PLAY: 2 units x (4F + 1D). count 10. Auto-fills
               identically to list 2 by default, so the two are byte-identical
               until edited; list 2 is the one the 5-on-4 PP screen edits.
        *  4 = 4-on-5 PENALTY KILL: 3 units x (2F + 2D). count 12.
        *  5 = 3-on-5 PENALTY KILL: 2 units x (1F + 2D). count 6.
        *  6 = 4-on-4: 2 units x (2F + 2D). count 8.
        *  7 = 3-on-3 overtime: 2 units x (2F + 1D). count 6.
        *  8 = 4-on-3 POWER PLAY: 2 units x (3F + 1D). count 8.
        *  9 = 3-on-4 PENALTY KILL: 2 units x (1F + 2D). count 6. Auto-fills
               identically to list 5 by default (byte-identical until edited);
               list 5 is 3-on-5, list 9 is 3-on-4 (confirmed by editing each
               PK screen and observing which list moved).
        * 10 = extra attackers (empty-net offense): 5 forwards. count 5.
        * 11 = shootout order: 5 forwards. count 5.
        * 12 = goalies: (starter, backup). count 2.
      Within-unit slot order matches the on-screen unit order (unit 1 first),
      confirmed for list 2: on-screen unit 1 = positions 0..4, unit 2 = 5..9,
      first on-screen player = position 0.

      SLOT ENCODING (confirmed): each slot is a big-endian s4 that references a
      player by that player's 1-based record position in players.dat, i.e.
      players.dat 0-based record ordinal = slot_value - 1. It is NOT the
      player_id (players.dat player_id values do not match slot values) and NOT
      a roster index (values exceed the per-team roster size). A slot value of
      -1 means the slot is empty.

      Because the surrounding record scaffolding (see team_record.record_end_pos)
      was derived from a different reference save, the fields below are retained
      as the raw byte view of that reference save; treat the QList<s4> model and
      the slot encoding above as the authoritative structure. See the
      documentation-only line_slot_list type.
    seq:
      - id: unit_id
        type: u2
        -doc: Unit id or line index.
      - id: unknown_30
        type: s4
        -doc: opaque
      - id: leading_slots
        size: 42
        -doc: |
          Raw view (reference save) of the leading even-strength slot lists.
          Confirmed content is QList<s4> forwards (LW,C,RW per line) followed by
          QList<s4> defense (LD,RD per pair); see line_slot_list.
      - id: unknown_31
        type: s4
        -doc: opaque
      - id: unknown_32
        type: s4
        -doc: opaque
      - id: interleaved_slots
        size: 40
        -doc: |
          Raw view (reference save) of the special-teams (PP/PK) slot lists,
          same s4 slot encoding as the even-strength lists.
      - id: slot_flags
        size: 17
        -doc: opaque
      - id: unknown_33
        type: u2
        -doc: opaque

  # Documentation-only reference type (not wired into the live parse): the
  # confirmed encoding of one situational slot list inside a line_unit. A
  # line_unit is a sequence of these. Each entry is a player reference =
  # (players.dat 1-based record position); -1 = empty slot. Forwards lists group
  # entries 3 per line (LW, C, RW); defense lists group entries 2 per pair
  # (LD, RD).
  line_slot_list:
    seq:
      - id: num_slots
        type: s4
        -doc: slot count (12 = four forward lines; 8 = four defense pairs)
      - id: slots
        type: s4
        repeat: expr
        repeat-expr: num_slots
        -doc: |
          Player references. slot = players.dat 1-based record position
          (0-based ordinal + 1); -1 = empty slot.



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
      - id: array_index
        type: s4
    seq:
      - id: record_index
        type: s4
        -doc: |
          Dense 0-based record index: this record's own position in the array
          (0, 1, 2, ... count-1), stored inline at the start of every record.
          Verified to equal the array position for 100% of records in BOTH a
          fictional save and a full real-league save once records are split on
          their true boundaries (see the record-boundary note on
          ../tools/parse_teams.py). The value never has gaps.

          An earlier analysis mistook this for a "sparse team id" because a
          record splitter that keyed on the display-name/abbreviation signature
          silently skipped the many records that carry no such identity strings
          (minor-league affiliates, placeholder/expansion slots). With those
          records omitted the surviving indices looked like 0, 3, 5, 8, ...;
          re-splitting on the real per-record boundary restores the dense
          sequence and exposes the skipped teams (e.g. an NHL club's AHL/ECHL
          affiliates sit at the indices in the "gaps"). This is the reliable
          record-start anchor; it is a distinct value from team_id below.
      - id: team_id
        type: s4
        -doc: |
          Canonical team id, a value independent of record_index above. It is
          NOT guaranteed unique and NOT a record counter: a user-created team can
          reuse an id already in use (observed: a created NHL club "Sacramento
          Express" carries team_id 0, the same id as Montreal, while its
          record_index is a distinct large value). In a default real-league save
          the ids happen to be assigned in step with the record order, so
          team_id == record_index for stock teams, which previously made the two
          fields look identical; a fictional save (ids 10..18 against indices
          0..8) and the reused id 0 above both prove they are separate fields.
          Use record_index, not team_id, as the unique per-record key.
      - id: internal_code
        type: fhm_common::qstring
        -doc: |
          Leading internal short-code (e.g. "ATL"). Separate inline field from
          internal_code_2. Byte-diff proved this is NOT the user-facing
          abbreviation: an in-game abbreviation edit ("ATL"->"ZZZ") left both
          internal_code and internal_code_2 unchanged and instead rewrote a THIRD
          abbreviation copy deep in the record (~record+2266, in a later
          sub-block). internal_code/internal_code_2 are an internal pair that
          merely coincide with the abbreviation value here. A one-day sim advance
          (which rewrote teams.dat) did NOT sync internal_code to the edited
          abbreviation, confirming it is a frozen internal code, not a live mirror
          of the user-facing abbreviation. A second, independent confirmation: a
          user-created club shown in-game as "SAC" stores the frozen internal code
          "BKW" in internal_code/internal_code_2, while the editable "SAC" appears
          only as the deeper repeated copies (first at ~record+0xa8), so
          internal_code/internal_code_2 can differ completely from the displayed
          abbreviation.
      - id: internal_code_2
        type: fhm_common::qstring
        -doc: |
          Second leading short-code field, a distinct inline QString from
          internal_code (proven by the byte layout: index+team_id precede it, so
          it is not a container-level map key; and by the abbreviation-edit
          byte-diff, which touched neither internal_code nor internal_code_2). It
          equals internal_code for most teams, but the two DO diverge in a full
          real-league save for franchises with a relocation/rename history --
          observed pairs (internal_code / internal_code_2) include WPG/ATL,
          OAL/CLE and AND/ANA -- which proves they are genuinely separate fields
          rather than one value stored twice. The divergence direction is not
          consistent (in some pairs internal_code is the current code and
          internal_code_2 an older one; in others the reverse), so the precise
          role of each is not pinned down. The team-edit screen exposes no field
          holding either value (city, nickname, and the editable abbreviation are
          all elsewhere), so internal_code/internal_code_2 appear to be purely
          internal, non-UI codes, plausibly an internal lookup/short code kept
          separate from the editable display abbreviation.
      - id: flag_1
        type: u1
        -doc: |
          Single-byte flag, observed = 1 for every team in the fictional save;
          role unconfirmed.
      - id: city
        type: fhm_common::qstring
        -doc: |
          Team city / location name (the editable "city" field, e.g. "Atlanta",
          "Los Angeles"). Editable in-game and distinct from the
          internal_code/internal_code_2 codes and the location_id index below.
      - id: nickname
        type: fhm_common::qstring
        -doc: |
          Team nickname / mascot name (the editable "nickname" field, e.g.
          "Fire Fighters", "Silvertips"). Editable in-game.
      - id: flag_2
        type: u1
        -doc: |
          Single-byte flag, observed = 0 for every team in the fictional save;
          role unconfirmed.
      - id: affiliate_parent_id
        type: s4
        -doc: |
          Parent-organization link for a minor-league affiliate, or -1 for a
          top-level (NHL) club. This holds the parent's RECORD_INDEX (the
          record_index field above), NOT its team_id. It always points at the
          top of the affiliation chain -- the senior (NHL) club -- even from the
          lowest (ECHL) tier. Confirmed on default real-league affiliates (each
          AHL club points at its NHL parent) and decisively on a user-built
          three-tier farm system where both the AHL and the ECHL affiliate carry
          the NHL club's record_index while that club's team_id is a different
          (and non-unique) value. Because it references record_index, resolve it
          against record_index -- resolving against team_id is ambiguous (ids can
          repeat, e.g. a created club reusing id 0).
      - id: affiliate_parent_id_2
        type: s4
        -doc: |
          Intermediate-parent link: the RECORD_INDEX of the affiliate one tier
          up, populated on the lowest tier of a multi-level farm system and -1
          otherwise. Observed on an ECHL club whose affiliate_parent_id is the
          NHL org and whose affiliate_parent_id_2 is the AHL club sitting between
          them (chain: NHL -> AHL -> ECHL). It is -1 for top-level clubs and for
          affiliates with no team below them, which is why saves without a
          three-tier user chain show it unused everywhere.
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
          The team's editable CASH, in whole currency units. Confirmed by an
          in-game byte-diff: editing one club's cash (123,456,789 -> 98,765,432)
          changed exactly these 4 bytes and nothing else in the file. Real-league
          clubs sit in the ~55-137 million range and affiliates around ~0.6-1.3
          million, with the wealthiest markets highest.
      - id: finance_2
        type: s4
        -doc: |
          The team's STAFF BUDGET, in whole currency units. User-confirmed: the
          value here (e.g. 8,140,000 for Montreal) matches the club's staff
          budget exactly. Smaller than finance_1 (cash): single-digit-millions
          for top clubs, hundreds of thousands for affiliates.
      - id: finance_3
        type: s4
        -doc: |
          Sparse monetary value; 0 for most teams, a small positive amount for a
          few. Role unconfirmed (debt / bonus / adjustment).
      - id: unknown_13
        type: s4
        -doc: |
          An id that tracks this record's record_index, not its team_id. In a
          stock real-league save (where team_id == record_index) it equals both,
          which previously made it look like a team_id copy; a fictional save
          that separates the two fields resolves the ambiguity -- there its
          values are the dense 0..count-1 sequence, matching record_index and NOT
          the (10..18) team_ids. A few outliers in the real-league save hold a
          larger unrelated value, so the exact role is still not pinned down, but
          for the common case it mirrors record_index.
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

          1. A leadership group + a `-1`-padded reserve list, immediately after
             the 13 line-unit lists. This whole region is now field-decoded and
             computable (verified across all 9 records of a fresh fictional
             save):
               a. `3 x s4` player-ordinal references (1-based players.dat record
                  positions, the same slot encoding line_unit uses) -- the team
                  captain and its two alternate captains. For an active club they
                  resolve to real skaters (e.g. Atlanta = 138, 173, 154 =
                  its RD1 / C1 / LW1); an empty placeholder club still stores
                  three values here.
               b. `s4 count` (70 in every record observed) followed by that many
                  `s4` reserve/roster slots, all `-1` in a fresh save -- a
                  fixed-capacity reserve list. On-disk length is `4 + count*4`
                  (computable), so this block is self-delimiting and item 2
                  (below) begins immediately after it.
          2. An "upcoming seasons" array: an `s4` count followed by that many
             9-byte records `{u2 index (1..7), u2 year, u4 value, u1 flag}` --
             7 entries per season for the next 4 seasons (e.g. count 28 = 7 x
             2024..2027). Its on-disk length is therefore `4 + count*9`
             (computable). Forward-looking (values are schedule/target
             placeholders, all zero in a fresh save), not historical results.
          3. A per-season franchise-history array: an `s4` season-count followed
             by that many season records, one per season from the franchise's
             founding year to the present (e.g. count 118 for an Original Six team
             founded in 1909). Each season record is `s4 year`, the team's
             identity that season as QStrings (city / nickname / abbreviation),
             and a FIXED 134-byte numeric stat block. The 134-byte stat-block size
             is constant across every season and every team (verified over 1300+
             arrays in a real-league save), so each season record is
             self-delimiting and the whole array's on-disk length is computable:
             `4 + sum over seasons of (4 + len(3 identity QStrings) + 134)`.
             Because identity strings are stored per season, this is also where
             historical relocations/renames are recoverable.

             The numeric stat block is big-endian (like the rest of the Qt
             file). Confirmed fields, at offsets relative to the start of the
             stat block (i.e. after the year + 3 QStrings), big-endian `u2`
             unless noted:

               +20  u1  made-playoffs flag (1 = qualified for the postseason)
               +21  u1  championship-won flag (1 = won a championship that
                        season -- the league title and/or the cup; in the early
                        NHA/NHL era a league title without the cup also sets it)
               +23  finish / final standing (1 = first)
               +25  regulation wins
               +27  losses (regulation -- the displayed "L" column)
               +29  ties (0 in the modern OTL era)
               +31  overtime wins   (0 in older seed seasons; see below)
               +33  overtime losses (0 before the OTL era)
               +35  shootout wins   (0 in older seed seasons)
               +37  shootout losses (0 in older seed seasons)
               +39  points
                        (== 2*(reg_wins + ot_wins + so_wins) + ties
                            + (ot_losses + so_losses))
               +57  average home attendance (pegs at arena capacity for a
                        sold-out season; 0 for a no-crowd season)
               +65  goals for
               +67  goals against
               +69  penalty minutes (0 in the pre-NHL NHA era, i.e. before
                        1917-18, when the stat was not tracked)
               +71  power-play goals for
               +73  power-play goals against
               +75  short-handed goals for
               +77  short-handed goals against
               +79  power-play opportunities for   (power-play % = +71 / +79)
               +81  times short-handed             (penalty-kill % = 1 - +73 / +81)

             The displayed standings line is
               W = +25 + +31 + +35   (reg + OT + shootout wins)
               L = +27               (regulation losses)
               OTL = +33 + +37       (OT + shootout losses)
             and total games = W + L + OTL.

             Older/seed seasons collapse the record into aggregates: +25 holds
             ALL wins and +33 holds ALL overtime/shootout losses, with the
             overtime/shootout split fields (+31, +35, +37) left at zero -- so
             the simpler `points == 2*(+25) + ties + (+33)` also holds for them.
             The split fields only populate for the recent seasons the save
             stores in full detail (2019 onward in the reference save; the save's
             own simulation, whose results diverge from real history, begins a
             few seasons later). The two real COVID seasons (2019-20 paused,
             2020-21 shortened) carry irregular breakdowns that do not reconcile
             with the points formula.

             A lockout/cancelled season (e.g. 2004-05) stores an all-zero stat
             block. The special-teams fields (+69..+81) are also zero for
             pre-NHL NHA seasons. Offsets between the confirmed fields (e.g. +41,
             +43..+56, +59..+64, +83+) hold further per-season figures that are
             not yet individually labelled. See ../tools/decode_team_history.py
             for a decoder and ../examples/real-league-history-decoded.md for a
             validated Montreal example.

             Parsing note: because the stat block is a fixed 134 bytes, each
             season record is self-delimiting and a reader should walk the array
             record-to-record (read year + 3 identity QStrings, then skip 134
             bytes) for `season_count` records rather than assume a constant
             stride. That handles a franchise whose identity string lengths change
             across seasons (a relocation/rename), where a fixed stride would
             desync. For a team that never relocates the stride happens to be a
             constant ~190 bytes, but the count + fixed-block walk does not rely
             on that.
          4. Immediately after the history array (item 3): the roster block plus
             a large mostly-padding tail. It opens with `s4 = 0`, then `s4
             roster_count` (~30-41 for a fresh fictional club), then
             `roster_count` big-endian `s4` player-ordinal references (1-based
             players.dat record positions, the same slot encoding line_unit
             uses) -- the active roster. That opening array is decoded and its
             length is `8 + roster_count*4` (computable). Shortly after the
             roster array (at a fixed offset past its trailing fields) is a `u1`
             HUMAN-MANAGED FLAG: `1` when the club is controlled by a human GM,
             `0` for an AI club. After it comes a POST region (~3.4 KB) that is
             mostly binary padding with fixed-offset landmarks: a 3-character code
             QString at a constant offset (~828 bytes into POST -- the team's
             affiliation/relocation code, e.g. "OKL" for a stock club, "ZZZ" as a
             placeholder) and a "#$%" placeholder QString near its end. Every AI
             club's POST is the SAME fixed size; the ONLY per-team length
             variation is a conditional MANAGED-TEAM PRESET BLOCK inserted iff the
             human-managed flag is `1`. That block is itself fixed: exactly 908
             bytes, a run of 17 count-prefixed arrays `[s4 count][count* s4]` with
             the constant count sequence [20,20,20,20,20,5,5,10,10,10,10,10,10,10,
             10,10,10] (5x 20-slot + 2x 5-slot + 10x 10-slot line/depth-chart
             PRESET slots). In an empty template every slot is `-1` (0xFFFFFFFF);
             a GM who saved custom preset lines would store player ordinals here.
             The block was proven present-iff-managed and byte-identical in
             structure by a controlled in-game byte-diff: taking human control of
             a previously-AI club made exactly this 908-byte block appear (and
             flipped the flag `0`->`1`), with all other clubs unchanged. This
             conditional block is the LARGEST per-team length variation and is now
             fully accounted for: `managed ? 908 : 0`. The POST interior (between
             the roster array and item 5) has since been mapped further by
             cross-team byte analysis of the 9-club reference save.
             IMPORTANT SCOPE: the field-by-field POST model below was derived and
             validated ONLY on FRESH FICTIONAL 9-club saves; a later check against
             a real-league save REFUTES its generality (see the REAL-SAVE CAVEAT at
             the end of this item). On those fresh fictional saves the whole region
             roster_end -> record_end walks as a sequence of self-delimiting fields
             and a Python walker (../tools/walk_record.py) computes record_end from
             this model and lands byte-EXACT on all 9 record boundaries of both the
             AI-only and the human-managed fictional reference save:
               * gap1: a FIXED 219-byte region immediately after the roster array
                 (byte-identical across all AI clubs; a 156-byte scaffold plus a
                 run of per-team f4 rating/finance floats, fixed size, values
                 differ).
               * array1: a count-prefixed big-endian s4 array `[s4 N1][N1* s4]` of
                 globally-allocated SEQUENTIAL club IDs (~26-29 elements).
               * array2: a second such array `[s4 N2][N2* s4]` (~64-67 elements)
                 immediately adjacent to array1 (gap2 = 0), followed by a FIXED
                 anchor `s4 -1, s4 0x270F0000, s4 0, s4 999`.
               * gap3: a FIXED region to the next array -- 2767 bytes for an AI
                 club, 3607 for a human-managed club (the +840 is exactly where
                 the managed-team preset block lands, so the `managed ? +840 : 0`
                 length delta lives entirely inside gap3).
               * five_array: a count-prefixed array of 5-byte records
                 `[s4 C][C* (s4 id + u1 tag=0x03)]`, preceded by the byte pattern
                 `01 05 03 0X` and terminated by the fixed word `s4 99`
                 (0x00000063). C observed 3-5. This ODD-width (5-byte) record array
                 was the last unmodelled field: two clubs with equal total ID-array
                 element counts differed by a multiple of 5 bytes (e.g. FRE C=4 vs
                 SAN C=3 -> a 5-byte delta), which pinpointed a 5-byte-record array
                 rather than an s4 (x4) array or a QString (always even).
               * gap4: a FIXED 65-byte tail to record_end (includes item 5's
                 32-byte finance trailer).
             All four gaps are CONSTANT across every AI club, so the record length
             is fully computable from the three parsed counts and the managed flag:

               record_end = roster_end + 219
                          + (4 + 4*N1) + (4 + 4*N2)
                          + (managed ? 3607 : 2767)
                          + (4 + 5*C) + 65

             Equivalently `post_len = 3063 + 4*(N1+N2) + 5*C (+840 if managed)`,
             where 3063 = 219 + 2767 + 65 + the three 4-byte count prefixes. This
             formula matched EXACTLY for all 8 AI clubs in the reference save and
             both managed clubs. A controlled in-game byte-diff established that the
             two ID arrays are FRANCHISE-level and roster-INDEPENDENT: releasing a
             player from a club left that club's teams.dat record byte-identical
             (active roster membership lives in players.dat, not here), so the IDs
             are franchise-owned entity handles allocated at league creation, not
             per-active-roster-player records.

             REAL-SAVE CAVEAT (this POST model does NOT generalize): running the
             walker against a full real-league save (an Original Six league,
             version_tag 51, 5220 records) REFUTES the field model above. A
             whole-file scan of that 45 MB save found ZERO consecutive-ID arrays
             (array1/array2 as modelled), no `0x270F0000` array anchor in a normal
             club record, and no `01 05 03` five_array signature in a clean club
             record. Those "sequential club ID" arrays were an ARTIFACT of a fresh
             league that allocates entity ids 1,2,3,... in order (so a plain id
             list looks like a `+1` consecutive run); in a real league with
             creation/relocation history the same lists hold NON-consecutive ids
             and do not match the consecutive-run detector, and the gap constants
             (219 / 2767 / 3607 / 65) and the `3063 + 4*IDs + 5*C` formula do not
             hold. What DID generalize is item 5's fixed finance/settings tail.
             Aligning every record on its end reveals a byte-identical 65-byte tail
             (this item's "gap4") on default-finance clubs, confirmed 65/65 constant
             across three unrelated leagues (a 5220-record Original Six save, an
             unrelated 5224-record league, and the 9-record fictional save) and
             byte-for-byte equal between arbitrary records within a save:

               00 00 00 63                s4 = 99      (a default setting)
               00*9 40 20 00 00           f4 = 2.5     (a default price/ratio)
               00*9 40 20 00 00           f4 = 2.5     (a default price/ratio)
               00*8 27 0F 00*5 05 F5 E1 00 00*7 01 FF FF FF FF   (32-byte trailer:
                          9999 cap, 100,000,000 budget, ... -1)

             So item 5 is a FIXED-LAYOUT 65-byte finance/settings block at record
             end whose all-default byte pattern is what ../tools/split_records.py
             keys on; an edited-finance club has different VALUES in the same layout
             (e.g. a non-default budget), not a different length, which is why the
             trailing 32-byte default pattern is merely absent (not relocated) on
             such a club. It found that pattern 5219 times in the 5220-record save
             -- once per record except a single edited-finance club -- so it remains
             a near-universal end-of-record ANCHOR for default-finance teams but is
             not a length model. The roster_end detector below is also
             fictional-tuned (it caps slot values at 3000; real players.dat
             ordinals exceed that) and fails to locate the roster on the real save.
             NET: the roster_end..record_end walk is proven only for FRESH FICTIONAL
             saves; on real saves the POST beyond item 5's trailer is NOT yet
             decoded, so this remains open, and the record-START signature (dense
             record_index) stays the portable boundary.
             (Creating the GM also bumped every club's item-1 reserve-list count
             70 -> 90, i.e. +20 `-1` entries / +80 bytes -- already covered by the
             item-1 `4 + count*4` model, not a separate field.)
          5. A finance/settings block: a FIXED 32-byte trailer that ends every
             record, `00*8 27 0F 00*5 05 F5 E1 00 00*7 01 FF FF FF FF` (a 9999
             cap and a 100,000,000 budget), byte-identical across all 9 records
             (including the club whose CASH was edited -- editable team cash lives
             in the header finance_1, not here, so this trailer is unaffected).
             The `27 0F` marker sits 24 bytes before the record end; the 8 bytes
             before it are zero. NOTE: this is the DEFAULT budget/cap block -- a
             team whose budget or cap was edited would differ here -- so it is a
             reliable end-of-record delimiter only among default-budget teams; do
             not use it as a general record terminator (use the record-start
             signature; see record_end_pos).

          These bounds come from a full real-league save whose deeper history
          exercises the block; a fictional league populates only a subset.

          Record count: the container's `count` header slightly exceeds the
          number of records recoverable by identity signature, because most
          records are minor-league affiliates or placeholder/expansion slots that
          carry no display strings yet still occupy a full record (with the dense
          record_index). Splitting on record_index -- not on identity strings --
          accounts for them.
    instances:
      record_end_pos:
        value: "array_index == 0 ? 4722 : (array_index == 1 ? 10295 : (array_index == 2 ? 15031 : (array_index == 3 ? 19765 : (array_index == 4 ? 24476 : (array_index == 5 ? 29197 : (array_index == 6 ? 33898 : (array_index == 7 ? 38607 : _root._io.size)))))))"
        -doc: |
          End offset of this record. These are ABSOLUTE offsets measured from one
          specific reference save, so this spec only parses that exact file.
          Because team records carry no length prefix, the portable way to bound
          a record is the record-START signature: every record begins with
          record_index = a dense sequential 0-based index (0, 1, 2, ...), then
          team_id (s4), then the abbreviation/city/nickname QStrings. Anchoring on
          that sequential index yields each record's [start, next_start) extent on
          ANY save without decoding the trailing block. See ../tools/parse_teams.py
          for the data-driven boundary finder.

          COMPUTABILITY STATUS: items 1, 2, 3 and 5 are count-prefixed /
          self-delimiting / fixed and hold on both fictional and real saves. Item 4
          is fully modelled END-TO-END ONLY ON FRESH FICTIONAL SAVES: there its
          roster array is count-prefixed; the conditional managed-team preset block
          (gated by the item-4 human-managed `u1` flag) is a `managed ? +840 : 0`
          delta inside a fixed gap; and its POST interior resolves to
          gap1(219) + array1`[s4 N1][N1*s4]` + array2`[s4 N2][N2*s4]`+anchor +
          gap3(managed ? 3607 : 2767) + five_array`[s4 C][C*5]` + gap4(65). On those
          fictional saves a Python walker (../tools/walk_record.py) computes
          record_end purely from this model (record_end = roster_end + 219 +
          (4+4*N1) + (4+4*N2) + (managed?3607:2767) + (4+5*C) + 65) and lands
          byte-EXACT on all 9 records of BOTH fictional reference saves.

          HOWEVER, this item-4 model does NOT generalize: checked against a real
          Original Six league save (5220 records) it FAILS -- a whole-file scan
          found ZERO of the consecutive-ID arrays it depends on (they were an
          artifact of a fresh league allocating ids 1,2,3,...; real ids are
          non-consecutive), and the gap/formula constants do not hold. Only item 5's
          finance trailer generalized (present once per record). See the REAL-SAVE
          CAVEAT under item 4. So a data-driven record_end is proven only for fresh
          fictional saves; on real saves the item-4 POST beyond the roster array and
          item 5's trailer is NOT yet decoded.

          Two things therefore still block DROPPING this offset table: (a) the
          real-save item-4 POST is undecoded (above), and (b) even the record HEADER
          is not self-delimiting -- the `line_unit` type below is only a fixed-size
          "raw byte view of the reference save" and does NOT match the true
          structure (a leading block plus 13 count-prefixed QList<s4>), so a forward
          parse of the active_line_unit + line_units array does not reach
          opaque_tail's start byte-exactly on an arbitrary save. (The fictional
          walker sidesteps the header by LOCATING roster_end via a data signature
          rather than forward-parsing it.) Until both are resolved this
          ABSOLUTE-offset ternary is retained. It stays file-specific and is NOT a
          portable record delimiter; the portable boundary is the record-START
          signature (dense record_index) -- note that on a real-league save the
          identity-gated variant in ../tools/parse_teams.py under-enumerates
          (affiliate / placeholder records carry no identity strings), so a robust
          real-save splitter must key on the dense record_index alone.
          (There is also no Kaitai compiler in-repo to validate a full sequential
          rewrite; the decode so far is documented in prose above and validated by
          ../tools/walk_record.py.)

          Do NOT try to terminate a record on the fixed 32-byte tail that closes
          most records (bytes `00*8 27 0F 00*5 05 F5 E1 00 00*7 01 FF FF FF FF`):
          that block is just this finance section's DEFAULT values (a 9999 cap and
          100,000,000 budget), present only on teams left at those defaults. A team
          whose finances were edited (observed: a user-created club with a
          10,000,000 budget) has a different tail and no such marker, so splitting
          on it silently merges the following record into the edited one. The dense
          record_index start signature is the only robust boundary.

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
      leave every slot -1. After the 13 lists come three s4 leadership refs
      (captain + 2 alternate captains) and a count-prefixed `-1`-padded reserve
      list, then the upcoming-seasons and season-history arrays (see
      team_record.opaque_tail items 1-3).

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



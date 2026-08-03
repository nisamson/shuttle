# Real-league save — decoded per-season franchise history

A worked example of decoding the **per-season franchise-history array** buried in
each team record's `opaque_tail` (see [`teams.ksy`](../data/teams.ksy)). Unlike
the fictional [IMPEX example](./impex-teams-decoded.md) — which has no history —
this uses a **real-league save** whose Original Six clubs carry a full
century-plus of seasons, so the block is exercised and can be validated against
the in-game season-history screen and against real-world records.

The example team is the first record in that save, the **Montreal Canadiens
(MTL)**: 118 season records, from the franchise's founding year **1909** to the
present, at a constant **190-byte stride** (constant because the club never
relocates, so its three per-season identity QStrings keep a fixed length).

## Season-record layout

Each season record is:

```
s4 year
QString city   QString nickname   QString abbreviation      (identity that season)
~134-byte big-endian numeric stat block
```

Confirmed stat-block fields, at offsets **relative to the start of the stat
block** (immediately after the three identity QStrings), big-endian `u2` unless
noted:

| Offset | Type | Field | Notes |
| --: | --- | --- | --- |
| +20 | u1 | made-playoffs flag | 1 = qualified for the postseason |
| +21 | u1 | championship-won flag | 1 = won a championship (league title and/or cup) |
| +23 | u2 | finish / final standing | 1 = first |
| +25 | u2 | wins | |
| +27 | u2 | losses | |
| +29 | u2 | ties | 0 in the modern OTL era |
| +33 | u2 | overtime losses | 0 before the OTL era |
| +39 | u2 | points | `== 2*wins + ties + overtime_losses` |
| +57 | u2 | average home attendance | pegs at arena capacity for a sellout; 0 for a no-crowd season |
| +65 | u2 | goals for | |
| +67 | u2 | goals against | |
| +69 | u2 | penalty minutes | 0 in the pre-NHL NHA era (before 1917-18) |
| +71 | u2 | power-play goals for | |
| +73 | u2 | power-play goals against | |
| +75 | u2 | short-handed goals for | |
| +77 | u2 | short-handed goals against | |
| +79 | u2 | power-play opportunities | power-play % = +71 / +79 |
| +81 | u2 | times short-handed | penalty-kill % = 1 − +73 / +81 |

Offsets between the confirmed fields (e.g. +31, +35..+38, +41..+56, +59..+64,
+83+) hold further per-season figures not yet individually labelled.

**Stride caveat:** the fixed ~190-byte stride is validated from the founding
year through ~2018; the most recent few seasons appear to store a longer
per-season record, so a fixed stride desyncs there. The decoder re-reads each
season's identity QStrings to re-anchor the stat block per record.

Decode any team's history with
[`../tools/decode_team_history.py`](../tools/decode_team_history.py), which
auto-detects the array (no hard-coded offsets):

```sh
uv run decode_team_history.py teams.dat
```

## Representative seasons

A slice of the 118 decoded MTL seasons (`PO` = made-playoffs flag, `CH` =
championship-won flag):

| year | fin | W | L | T | OTL | pts | GF | GA | att | PO | CH |
| --: | --: | --: | --: | --: | --: | --: | --: | --: | --: | --: | --: |
| 1909 | 7 | 2 | 10 | 0 | 0 | 4 | 59 | 100 | 6000 | 0 | 0 |
| 1915 | 1 | 16 | 7 | 0 | 0 | 32 | 104 | 76 | 6000 | 1 | 1 |
| 1943 | 1 | 38 | 5 | 7 | 0 | 83 | 234 | 109 | 8449 | 1 | 1 |
| 1955 | 1 | 45 | 15 | 10 | 0 | 100 | 222 | 131 | 13590 | 1 | 1 |
| 1976 | 1 | 60 | 8 | 12 | 0 | 132 | 387 | 171 | 16702 | 1 | 1 |
| 1992 | 3 | 48 | 30 | 6 | 0 | 102 | 326 | 280 | 17018 | 1 | 1 |
| 2004 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 21273 | 0 | 0 |
| 2005 | 3 | 42 | 31 | 0 | 9 | 93 | 243 | 247 | 21273 | 1 | 0 |
| 2007 | 1 | 47 | 25 | 0 | 10 | 104 | 262 | 222 | 21273 | 1 | 0 |

(The table stops before the most recent seasons, which fall in the stride-desync
zone noted above; run the decoder for the full, per-record-anchored series.)

## Validation

Every decoded value cross-checks against ground truth:

- **In-game season-history screen (2005-06):** 42-31-9, **243 GF**, **247 GA**,
  **21273 average attendance**, made playoffs, no cup — all match exactly.
- **Special teams (2005-06):** **1336 PIM**, power play **89 goals on 463
  opportunities (19.2%)**, penalty kill **91 PP goals allowed on 481 times
  short-handed (81.1%)**, and **10 / 6 short-handed goals for/against** — all
  match the in-game detail. Power-play % = +71/+79 and penalty-kill % =
  1 − +73/+81 land in realistic ranges every season (e.g. the dominant 1975-77
  dynasty shows elite special teams; PIM peaks in the mid-80s enforcer era).
- **Points formula** holds every season: `points == 2*wins + ties + overtime_losses`.
- **Ties → OTL transition:** ties are populated through the pre-shootout era and
  drop to 0 once overtime-losses appear (1996 onward), matching the rule change.
- **Lockout/cancelled season (2004-05):** stored as an all-zero stat block.
- **Attendance = arena capacity when sold out:** MTL's attendance pegs at
  **21273** across 2004–2013 — exactly the Bell Centre's listed capacity — then
  varies slightly afterward, drops to **0** for the no-crowd 2020-21 season, and
  returns to a partial **15495** in 2021-22.
- **Goals over 255** decode correctly (e.g. 2007-08 = 262 GF), confirming the
  fields are 16-bit big-endian, not bytes.
- **Championship flag** matches the club's real title history: the 5 straight
  cups of 1955-59, the 60-win 1976-77 season, etc. In the early NHA/NHL era the
  flag also fires for a season won as a **league title without the cup** (1916-17,
  1924-25), consistent with "won a championship" rather than "won the cup".

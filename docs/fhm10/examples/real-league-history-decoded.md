# Real-league save — decoded per-season franchise history

A worked example of decoding the **per-season franchise-history array** buried in
each team record's `opaque_tail` (see [`teams.ksy`](../data/teams.ksy)). Unlike
the fictional [IMPEX example](./impex-teams-decoded.md) — which has no history —
this uses a **real-league save** whose Original Six clubs carry a full
century-plus of seasons, so the block is exercised and can be validated against
the in-game season-history screen and against real-world records.

The example team is the first record in that save, the **Montreal Canadiens
(MTL)**: 118 season records (matching the array's `s4` season-count prefix),
from the franchise's founding year **1909** to the present. Each season's stat
block is a fixed **134 bytes**, so the records are self-delimiting; for a club
that never relocates like MTL the resulting stride is a constant **190 bytes**.

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
| +25 | u2 | regulation wins | all wins in older/seed seasons (see below) |
| +27 | u2 | losses | regulation losses — the displayed "L" column |
| +29 | u2 | ties | 0 in the modern OTL era |
| +31 | u2 | overtime wins | 0 in older/seed seasons |
| +33 | u2 | overtime losses | all OTL in older/seed seasons |
| +35 | u2 | shootout wins | 0 in older/seed seasons |
| +37 | u2 | shootout losses | 0 in older/seed seasons |
| +39 | u2 | points | `== 2*(regW+OTW+SOW) + ties + (OTL+SOL)` |
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

The displayed standings line is **W** = +25 + +31 + +35 (reg + OT + shootout
wins), **L** = +27, **OTL** = +33 + +37. Offsets between the confirmed fields
(e.g. +41..+56, +59..+64, +83+) hold further per-season figures not yet
individually labelled.

**Seed vs. simulated seasons:** older/seed seasons collapse the record into
aggregates — +25 holds *all* wins and +33 holds *all* overtime/shootout losses,
with the split fields (+31/+35/+37) left at zero — so the simpler
`points == 2*(+25) + ties + (+33)` also holds for them. The OT/shootout split
only populates for the recent seasons the save stores in full detail (2019
onward in this save's seed) and for the save's own **simulated** seasons, whose
results diverge from real history and begin a few seasons later (2023-24 here).
The presence of nonzero +31/+35/+37 is therefore a practical discriminator
between a collapsed seed season and a fully-detailed/simulated one.

**Season count & self-delimiting records:** the array is preceded by an `s4`
season-count (118 for MTL), and each season's stat block is a **fixed 134 bytes**
(verified constant across every season and every team — 1300+ arrays in the
save). So each season record — `s4 year` + 3 identity QStrings + 134 bytes — is
self-delimiting, and the whole array's length is
`4 + Σ(4 + 3 QStrings + 134)`. A reader walks it record-to-record for
`season_count` seasons; it never needs a fixed stride. That also handles a
franchise whose identity string lengths change (a relocation/rename shifts the
stat block within its record). For a club that never relocates, like MTL, the
stride happens to be a constant **190 bytes**, but nothing relies on that.

The season-history array is self-delimiting and can be decoded without
hard-coded record offsets.

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
| 2023 | 6 | 35 | 40 | 0 | 7 | 77 | 230 | 264 | 20998 | 0 | 0 |
| 2024 | 8 | 33 | 40 | 0 | 9 | 75 | 241 | 286 | 20869 | 0 | 0 |

(The `W`/`OTL` columns already fold in the OT/shootout split — e.g. 2024-25's
33 wins = 27 reg + 3 OT + 3 SO and 9 OTL = 2 OT + 7 SO losses. Run the decoder
for the full, per-record-anchored series.)

## Validation

Every decoded value cross-checks against ground truth:

- **In-game season-history screen (2005-06):** 42-31-9, **243 GF**, **247 GA**,
  **21273 average attendance**, made playoffs, no cup — all match exactly.
- **Modern standings split (2024-25):** the in-game record **33-40-9, 75 pts,
  241 GF, 286 GA, 50 PPG** reconciles exactly — 33 W = 27 reg + 3 OT + 3 SO
  (+25/+31/+35), 40 L (+27), 9 OTL = 2 OT + 7 SO losses (+33/+37), and
  `points = 2*(27+3+3) + 0 + (2+7) = 75`.
- **Special teams (2005-06):** **1336 PIM**, power play **89 goals on 463
  opportunities (19.2%)**, penalty kill **91 PP goals allowed on 481 times
  short-handed (81.1%)**, and **10 / 6 short-handed goals for/against** — all
  match the in-game detail. Power-play % = +71/+79 and penalty-kill % =
  1 − +73/+81 land in realistic ranges every season (e.g. the dominant 1975-77
  dynasty shows elite special teams; PIM peaks in the mid-80s enforcer era).
- **Points formula** holds every season: `points == 2*(regW+OTW+SOW) + ties + (OTL+SOL)`
  (the two real COVID seasons, 2019-20 paused and 2020-21 shortened, carry
  irregular truncated records that are the only exceptions).
- **Seed vs. simulated:** the OT/shootout split fields (+31/+35/+37) are zero for
  older seed seasons (which collapse into +25/+33) and populate only for the
  recent fully-detailed seasons and the save's own simulated seasons (2023-24
  onward here), whose results diverge from real NHL history.
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

# `personal.dat`

`personal.dat` begins with two big-endian signed 32-bit values: format version and the
next/high-water personnel ID. Historical saves can contain deleted identity gaps, so the
high-water ID is not necessarily the serialized record count. FHM 10 personnel records are variable-length and retain
unknown content losslessly. The current model locates records by their ordered stable personnel
identities and exposes only fields verified through controlled editor diffs.

## Verified personnel fields

| Record offset | Type | Meaning |
| --- | --- | --- |
| `0x00` | `s4` | First-name ID in `names.dat` |
| `0x04` | `s4` | Surname ID in `names.dat` |
| `0x08` | `s4` | Nickname ID in `names.dat`; `-1` means none |
| `0x0C`–`0x14` | `3 × s4` | Birth year, month, and day |
| `0x18` | `u2` | Nationality ID |
| `0x1C` | `s4` | Birth-city ID |
| `0x28` | `s4` | Team record index; `-1` means unassigned |
| `0x30` | `s4` | Stable personnel ID |
| `0x42` | `u1` | Job |
| `0x4C` | `u2` | Negotiating |
| `0x4E` | `u2` | Offensive preference |
| `0x50` | `u2` | Player management |
| `0x52` | `u2` | Physical preference |
| `0x54`–`0x5E` | `u2` | Defense, forwards, goalies, prospects, ability, and potential ratings |
| `0x6A` | `u2` | Reputation |
| `0x6E` | `s4` | Salary |
| `0x99` | `u1` | Contract length; zero means no contract |
| `0xE5` | `u1` | Retired flag |
| tail `0x17E` | `u2` | Line-matching tendency |
| tail `0x180` | `u2` | Goalie-handling tendency |
| tail `0x182` | `u2` | Veteran preference |
| tail `0x184` | `u2` | Innovation tendency |
| tail `0x186` | `u2` | Loyalty tendency |

The remaining verified values occur in a stable tail block after a variable-length middle
section and are addressed relative to the record end. This includes offensive skills, defensive
skills, the mixed geographic-location ID shown by the editor as **Based In**, physical training,
tactics, discipline, self-preservation, motivation, in-game tactics, and trainer skill. Values
outside `0..20` at rating locations are retained as unknown raw data and represented as null in
the relational model.

Some human-controlled personnel records contain additional variable payload. It remains opaque
and is preserved byte-for-byte; sensitive fields in that payload are intentionally not exposed.

The editor's **Confidence in GM** value produced no controlled save-file change for either a GM
or an assistant coach and is not currently modeled.

## Personality tendencies

The five personality fields are consecutive in the native `QDataStream` serializer and occur
after the variable-length record payload, so their offsets are relative to the reference
`0x1B6`-byte record end. Their serialized order matches the game's `staff_ratings.csv` export.

| Value | Line matching | Goalie handling | Veteran preference | Innovation | Loyalty |
| ---: | --- | --- | --- | --- | --- |
| `0` | Very Passive | Very Conservative | Loves Prospects | Very Conservative | Very Ruthless |
| `1` | Passive | Conservative | Prefers Prospects | Conservative | Ruthless |
| `2` | Balanced | Balanced | Balanced | Balanced | Balanced |
| `3` | Aggressive | Aggressive | Prefers Veterans | Innovative | Loyal |
| `4` | Very Aggressive | Very Aggressive | Loves Veterans | Very Innovative | Very Loyal |

## Jobs

`0` GM/Head Coach, `1` General Manager, `2` Head Coach, `3` Owner, `4` Scout,
`5` Assistant Coach, and `7` Trainer. Value `6` was not observed.

`teams.dat` independently stores the Head Coach personnel ID at `FhmTeamTail.Pre2Prefix + 0x0B`
and General Manager personnel ID at `+ 0x0F`. Export synchronizes those slots from each team's
personnel and job assignments.

# Shuttle.Fhm.Vision

Windows-only tool that captures **Franchise Hockey Manager (FHM)** player-info screens and extracts
their on-screen data — name, jersey number, position/handedness, attribute ratings, and derived
**role ratings** — into a local **SQLite** database, keeping each source screenshot for review.

FHM's save format is heavily obfuscated, so this tool reads the data straight off the rendered game
UI via **region-based OCR**. The collected dataset is intended to later train a model correlating a
player's raw attribute ratings with their per-role ratings (model training is out of scope here —
this project is the **data-collection pipeline**).

> This project targets `net10.0-windows` and uses Win32/WinForms plus the built-in Windows OCR
> engine; it only builds and runs on Windows.

## How it works

1. **Capture** — finds the FHM window (auto-detected by process name, or by explicit `--pid`) and
   grabs its pixels via GDI `PrintWindow` (works for most GPU-rendered windows).
2. **Detect unique screens** — a perceptual frame hash skips transient/unchanged frames and waits
   for the screen to settle before doing any OCR.
3. **Extract** — each field is read from a fixed region defined in a **layout profile** (rectangles
   stored as ratios of the window size, so they are resolution/DPI independent) and parsed by kind
   (text vs integer).
4. **Store** — a content hash over the normalized identity + rating vectors de-duplicates repeat
   captures of the same player state; unique captures are written to SQLite and the screenshot is
   saved to an `images/` subfolder.

## Commands

Build once, then run from the project folder with `dotnet run --`, or run the built exe.

```pwsh
# 1. Find FHM's window / PID
dotnet run --project Shuttle.Fhm.Vision -- list-windows --process FHM

# 2. Author a layout profile interactively (draw + label field regions on a live capture)
dotnet run --project Shuttle.Fhm.Vision -- calibrate --pid 12345 --profile profiles/fhm.json
#   or calibrate against a saved screenshot instead of a live window:
dotnet run --project Shuttle.Fhm.Vision -- calibrate --image shot.png --profile profiles/fhm.json

# 3. Monitor the FHM window and collect unique player screens
#    (repeat --profile to match each frame against several profiles, in order)
dotnet run --project Shuttle.Fhm.Vision -- monitor --pid 12345 \
    --profile fhm10-forward-profile.json --profile fhm10-defense-profile.json --db fhm-captures.db

# 4. Parse a single saved screenshot offline (handy for testing profiles; repeat --profile)
dotnet run --project Shuttle.Fhm.Vision -- ingest-image --image shot.png \
    --profile fhm10-forward-profile.json --profile fhm10-defense-profile.json --db fhm-captures.db

# 5. Diagnose a profile: dump each anchor/region crop + its OCR text (why is a region empty?)
dotnet run --project Shuttle.Fhm.Vision -- inspect --image shot.png \
    --profile fhm10-forward-profile.json --out inspect

# 6. Train the custom digit recognizer for FHM's rating font (interactive labelling)
dotnet run --project Shuttle.Fhm.Vision -- train-digits --image shot.png \
    --profile fhm10-forward-profile.json --templates digits.json --out glyphs

# 6b. Same, but in a GUI: original + normalized glyph previews, live confidence, multi-image
dotnet run --project Shuttle.Fhm.Vision -- train-digits-gui --templates digits.json \
    --profile fhm10-forward-profile.json --image shot.png --image shot2.png

# 7. Score the trained template set (leave-one-out accuracy, confusions, margins)
dotnet run --project Shuttle.Fhm.Vision -- eval-digits --templates digits.json
```

Common options: `--pid` (explicit process id) or `--process` (name fragment, default `FHM`);
`--profile` (layout JSON; `monitor` accepts it repeatedly to match each frame against several
profiles in order — the first whose anchors match wins); `--db` (SQLite path, default
`fhm-captures.db`); `--images` (screenshot folder, default `images/` beside the database);
`--interval` (monitor poll ms, default 750); `--templates` (digit-template JSON produced by
`train-digits`; when supplied to `monitor`/`ingest-image`, numeric cells use the trained
recognizer and fall back to OCR only when it is not confident); `--verbose`/`-v` (on
`monitor`/`ingest-image`, log Debug-level detail: every scanned frame, per-region
recognizer/OCR reads, profile-match results and duplicate skips).

## Calibration workflow

`calibrate` opens an editor showing the captured screenshot:

- Set the **field key** (e.g. `name`, `skating`, `playmaker`), **group** (`Identity`, `Attribute`,
  `Role`, `Other`), and **kind** (`Text` / `Integer` / `Float` / `Bio`), then **drag a rectangle**
  over that field to add it.
- Tick **Anchor** and give an **expected text** to mark a region used only to confirm the screen is
  a player-info screen (all anchors must match before a frame is parsed).
- Reserved identity keys: `name`, `number`, `position`, `handedness`. Any other `Identity` key is
  kept as a text field.
- **Kind = `Float`** parses a decimal value (currency symbols, thousands separators and units are
  stripped) into the `Numbers` vector.
- **Kind = `Bio`** parses the fixed FHM10 "bio" line, e.g.
  `LD/RD | SACRAMENTO EXPRESS | SHOOTS: LEFT | AGE: 23 | 6'5" - 243 LBS | SALARY: $775,000 (1)`.
  Its key/group are ignored; it fills `position` (identity), `height` (raw text) plus `heightInches`
  and `weight` (numbers). Draw the rectangle over the whole bio line.
- **Save (keep open)** writes the profile JSON without closing; **Save & close** does both
  (rectangles are converted to ratios of the screenshot size).

## Data model

SQLite (created on first use via EF Core `EnsureCreated`):

- `Captures` — one row per unique capture: `CapturedAtUtc`, `Name`, `JerseyNumber`, `Position`,
  `Handedness`, `ContentHash` (unique), `ImageFileName`.
- `Attributes` / `RoleRatings` — `(CaptureRecordId, Key, Value:int)` child rows (the rating vectors).
- `Numbers` — `(CaptureRecordId, Key, Value:double)` for decimal fields and numeric metadata
  (e.g. `weight`, `heightInches`, a `Float` rating).
- `TextFields` — `(CaptureRecordId, Key, Value:string)` for any extra text fields (e.g. `height`).

Screenshots are saved as `images/{yyyyMMdd-HHmmss}-{shorthash}.png`.

## OCR engine

OCR sits behind `IOcrEngine`. The default is **`WindowsMediaOcrEngine`** (built-in
`Windows.Media.Ocr`) — offline and dependency-free, using the installed Windows OCR language packs.

Region crops are **upscaled** before OCR (integer factor, shorter side lifted to at least
`RegionImaging.OcrMinDimension` = 64px) because `Windows.Media.Ocr` returns nothing on very small
crops such as a tight box around a two-digit rating.

Because FHM renders its numeric data as **white text** over a coloured/dark background, numeric and
bio regions (`Integer`, `Float`, `Bio`) are also **binarised to black-on-white** before OCR: pixels
whose R, G and B all reach `RegionImaging.WhiteTextThreshold` (170) become black text, everything
else becomes white. This strips the busy background and markedly improves recognition. Disable it by
constructing `RegionExtractor(..., isolateWhiteText: false)`.

If a region still reads empty, use `inspect` (above): it dumps the exact upscaled crop fed to OCR
plus, for numeric/bio regions, a `.bw.png` showing the black-on-white isolation, and prints the OCR
text **before (raw)** and **after (b/w text)** so you can compare.

Two further robustness measures target FHM's small numeric cells:

- **White quiet-zone padding.** Isolated numeric crops are padded with a white border before OCR,
  because Windows OCR routinely drops a lone digit that touches the image edge (single-digit cells).
- **Confusable-digit recovery.** `FieldTextParser.ParseInteger` maps the common OCR letter/symbol
  substitutions back to digits for strictly-numeric cells (`l`/`I`/`|` → `1`, `O`/`Q` → `0`,
  `S` → `5`, `B` → `8`, `Z` → `2`, `G` → `6`, `T` → `7`), so e.g. a `14` read as `l4` is not
  silently truncated to `4`.

## Custom digit recognizer (FHM rating font)

General-purpose OCR is unreliable on the short, isolated numeric cells FHM renders. Because those
ratings use a single fixed font, a tiny **template / nearest-neighbour recognizer** is far more
accurate. It lives in `Recognition/` and is *opt-in* via `--templates`:

1. **Train** with `train-digits`: for each numeric region of each matching profile it segments the
   cell into individual glyphs (`DigitSegmenter`, vertical projection over white "ink" pixels),
   optionally dumps a preview PNG per glyph (`--out`), and prompts you to label each glyph. When the
   template set already has samples it shows the **current best guess** (label + confidence) for the
   glyph, so you can press **Enter to accept it**, type a character to label/override, `s` to skip,
   or `q` to save and quit. The recognizer is rebuilt as you go, so guesses improve within a run.
   Labels are appended to the `--templates` JSON (`DigitTemplateStore`), so you can build the set up
   across several screenshots. Because the font is fixed, near-identical repeat captures of a glyph
   are **de-duplicated on add** (`DigitTemplateSet.TryAdd`) — the trainer reports and skips them, so
   the set stays small and its confidence margins stay meaningful.

   Prefer a GUI? **`train-digits-gui`** (Windows-only WinForms) does the same labelling visually: for
   each glyph it shows the **original crop** next to the **normalized (matched) bitmap** and the
   current model's **confidence** (guess label, distance `d`, runner-up margin `m`, confident/low).
   Press **Enter** to accept the guess (or type `0`–`9`/`.`/`-` then Enter to label), **Skip** to
   move on, and **Save**/**Save && close** to write the JSON. An **Add images…** button pulls in more
   screenshots and segments them into the queue mid-session. It takes the same `--templates` and
   (repeatable) `--profile`; `--image` is optional since you can add images from the UI.
2. **Use** it by passing the same `--templates` file to `monitor` or `ingest-image`. For
   `Integer`/`Float` regions, `RegionExtractor` normalizes each segmented glyph to a fixed grid
   (default 12×20), classifies it by nearest template (Hamming distance), and uses the result when
   every glyph matches within tolerance; otherwise it **falls back to the Windows OCR pipeline**
   above. `Bio` regions (which contain letters) always use OCR.

Two properties keep the classifier robust against the confusable pairs a fixed font still produces:

- **Aspect-preserving normalization** — a segmented glyph is scaled by a *single* factor to fit the
  normalized box and then centred (background padding), rather than stretched independently on each
  axis. Stretching widened a narrow `1` toward `2`/wider digits; preserving aspect ratio removes that
  whole class of confusion.
- **Confidence margin** — a glyph is only accepted when its nearest template both clears the distance
  tolerance (`maxNormalizedDistance`, default 0.18) **and** beats the nearest *different-label*
  template by a minimum gap (`minConfidenceMargin`, default 0.02). A glyph whose winning label barely
  edges out a rival is treated as unrecognized and **falls back to OCR** instead of guessing wrong.

> **Normalization change:** template sets are normalization-specific. Sets trained before
> aspect-preserving normalization was introduced must be **re-generated** (re-run `train-digits` over
> the source screenshots) — old stored bitstrings will not match freshly segmented glyphs.

The template set is plain JSON (glyph size + `'1'`/`'0'` bit strings), so it is easy to inspect,
edit, and commit alongside the layout profiles.

### How much training data, and knowing when it's solid

Because this is nearest-neighbour matching of a **single fixed font** (not a learned model), extra
samples only buy coverage of rendering variance (anti-aliasing, segmentation jitter). You only need
the 10 digits (`0`–`9`), plus `.`/`-` if any region shows decimals or negatives — segmentation is
per-glyph, so `14` is just `1` then `4`; you do not need every 1–99 value. A good target is roughly
**8–15 clean samples per digit (~100–150 total)**; **3–5 per digit** is a workable minimum. Bias
collection toward the confusable pairs (`1`/`7`, `3`/`8`, `5`/`6`, `6`/`8`, `0`/`8`) and capture each
digit both alone and as part of a multi-digit cell.

Rather than guess a count, measure it with **`eval-digits`**, which runs leave-one-out
cross-validation over the template set (each glyph classified against all the others) and reports:

- **Leave-one-out accuracy**, overall and per label.
- **Confusion pairs** — which actual label gets misread as which, so you know exactly what to collect.
- **Confidence margin** per label: the mean distance to the nearest *other-label* template minus the
  mean distance to the nearest *same-label* one. A comfortably positive margin (well above the
  recognition `--threshold`, default 0.18) means robust identification regardless of raw count.
- Flags for labels with **errors**, **few samples** (< 5), or a **slim margin**, plus a
  SOLID / GOOD / NEEDS MORE DATA verdict.

Collect until accuracy plateaus at 100% with healthy margins and no thin/slim-margin flags. Note that
leave-one-out cannot validate a label that has only one sample (removing it leaves no same-label
neighbour), so `eval-digits` calls those out separately.

Alternatives can be dropped in behind the same interface. As evaluated in the plan:

- **Windows.Media.Ocr** (default): no native deps, good general-text accuracy.
- **Tesseract**: needs the native engine + trained data; another general-purpose option.
- **ML.NET / ONNX**: ML.NET has no built-in OCR, but it can run an ONNX text/digit-recognition model
  via ONNX Runtime. The clean, fixed-font numeric attribute grid is the strongest fit for a small,
  highly-accurate custom digit classifier — a promising path for the rating cells specifically.

## Tests

```pwsh
dotnet test Shuttle.Fhm.Vision.Tests/Shuttle.Fhm.Vision.Tests.csproj
```

The tests cover the platform-agnostic logic (profile serialization, ratio↔pixel mapping, field/
numeric parsing, content-hash dedup, and end-to-end region extraction with a deterministic fake OCR
engine). Live capture, the Windows OCR engine, and the WinForms calibrator are thin Windows-specific
shells around this tested core.

# SHL game-file archive

`Shuttle.Api` mirrors files discoverable through Apache directory listings under
`https://simulationhockey.com/games/` into the **separate**
`shuttle-shl/shl-games-archive` Git repository, retaining its root `README.md`.
It preserves source-relative paths (e.g. `shl/S85/csv/...`). It is not an
archive stored in the SHLAnalytics repository.

The `GameArchiveJob` runs every six hours in production via the persistent Quartz
store. It inventories the eligible directory tree and downloads all eligible
files before modifying a temporary clone. `manifest.json` identifies paths it
owns and skipped directories; on a
successful scan the job removes only managed paths absent from the new inventory,
including files beneath intentionally skipped directories. It
skips commits on no change and never force-pushes. A failed traversal or
download does not publish a partial snapshot. Earlier Git commits retain
files later removed upstream.
The API job and local runner share the `Shuttle.GameArchive` library.
Clone, commit and push use LibGit2Sharp in-process; the production API does
not invoke an external Git executable or write credentials to a helper file.
Temporary checkouts use a shallow clone at depth 2. Publishing adds a normal
commit without truncating or rewriting the remote repository's history.
Git remotes must use HTTP(S) for shallow cloning; LibGit2Sharp's local-file
transport does not support shallow fetches.

## Archive repository structure

Representative layout after a successful sync (season/file contents are
abbreviated, not a statement that the repository has already been populated):

```text
shl-games-archive/
|-- README.md       # Preserved repository introduction
|-- manifest.json   # Versioned managed-file inventory and skipped directories
|-- iihf/
|   `-- ...
|-- prospects/
|   `-- ...
|-- shl/
|   |-- S85/
|   |   `-- csv/
|   |       `-- ...
|   `-- ...
|-- smjhl/
|   `-- ...
|-- team-files/
|   `-- 83/
|       `-- csv/
|           `-- ...
`-- wjc/
    `-- ...
```

The source's `/games/` directory maps directly to the repository root; there
is no extra `games/` wrapper. File paths, names, and season numbering are
preserved rather than normalized. The manifest's `files` array covers source
files only, not the README or `manifest.json` itself. Root `manifest.json` is
reserved for archive metadata; a nested source file such as `shl/manifest.json`
is permitted.

Directories appear only when they contain archived files; Git does not track
empty directories. Skipped subtrees such as `iihf/S37/` are documented in the
manifest, not represented by placeholder folders. Previously managed files
removed from the current tree remain accessible through earlier Git commits.

## File-type coverage

Source files ending in **`.html`, `.xml`, or `.txt` are intentionally not
archived**, using case-insensitive extension matching. Their existence is
documented here rather than represented by per-file manifest entries.
Apache HTML directory listings are still fetched to discover eligible files;
directories with names ending in those extensions are still traversed.
Other extensions remain eligible, including unknown extensions. Files inside
ZIPs or other binary containers are not inspected, filtered, or rewritten.

These exclusions acknowledge useful historical data without attempting to
preserve it in the current snapshot:

| Excluded type | Observed contents | Files observed on 2026-10-08 |
|---|---|---:|
| `.html` | Older STHS game reports, detailed play-by-play, line usage, rosters/ratings, and season statistics | 33,498 |
| `.xml` | Structured STHS player/goalie ratings, contracts, statistics, schedules, league configuration, and transaction/event exports | 1,790 |
| `.txt` | Later-season narrated, timestamped play-by-play logs | 20,231 |

Their coverage is not uniform. SHL HTML was observed in S3-S52 except S36;
SHL XML in S28, S29, S32, and S34; SHL text logs in S53-S68. SMJHL HTML was
observed in S15-S52, XML in S31-S45, and text logs in S53-S65. Older IIHF
and WJC custom-index exclusions limit what this inventory can establish.
Special tournaments and prospects have additional, irregular coverage.

Previously managed excluded files are removed from the current snapshot
only after all eligible downloads succeed; earlier commits retain their
contents. Unmanaged files remain untouched, even if their extensions are
excluded. Ignored file-content changes do not trigger commits. Unsafe and
case-colliding listing entries remain fatal, including excluded entries.

### Remaining formats and observed season coverage

The 2026-10-08 Apache-listing inventory contains **7,808 remaining files**,
approximately **1.390 GiB** uncompressed. Sizes are rounded listing estimates,
not measured Git storage. The 40 skipped custom-index subtrees are outside
these counts. A season listed below has at least one file of that type;
this is not proof of a complete season or a complete save.

| Type | Files | Contents | Observed distribution |
|---|---:|---|---|
| `.csv` | 5,649 | Structured simulator exports, including players/ratings, teams, schedules, statistics, and modern box-score summaries | SHL/SMJHL S53-S90; IIHF/WJC S53-S89; sporadic older exports and prospects S45-S49; 41 additional files under `team-files/83/` |
| `.sth` | 387 | STHS native league saves, including timestamped backup copies | IIHF S47-S52; SMJHL S30-S36 and S47-S52; prospects S45-S49; WJC S44-S46, S48, S51-S52 and special tournaments; none inventoried under SHL |
| `.stc` | 233 | STHS client league files | SHL S28-S35 and S37-S52; SMJHL S30-S52; IIHF S43-S52; WJC S44-S52 and special tournaments; prospects S45-S49 |
| `.js` | 237 | Legacy HTML-page interaction helpers and bundled libraries such as jQuery | SHL S28-S35 and S37-S52; SMJHL S30-S52; IIHF S43-S52; WJC S44-S52 and special tournaments; prospects S45-S49 and S53 |
| `.css` | 437 | Legacy HTML-page and table styles | Same broad season distribution as `.js` |
| `.png` | 289 | Mostly navigation/toggle icons; also two `test1.png` files | Patchy legacy coverage: SHL S28-S52; SMJHL S30-S52; IIHF S43-S52; WJC S44-S52 and special tournaments; prospects S45-S49 and S53 |
| `.gif` | 188 | Footer-background images | Patchy legacy coverage over the same broad ranges as `.png` |
| `.jpg` | 188 | Page-background images | Same broad distribution as `.gif` |
| `.dat` | 154 | `STHSLegacy.dat` text navigation maps listing HTML filenames and labels, not FHM save records | SHL S40-S52; SMJHL S32-S52; IIHF S43-S52; WJC S44-S52 and special tournaments; prospects S53 |
| `.ini` | 37 | Windows `desktop.ini` folder/icon metadata | SHL S28-S35, S37-S38, and S43 |
| `.str` | 3 | STHS rating packs | SMJHL S50-S51 |
| `.log` | 3 | Chromium-style crash/debug messages, not hockey play-by-play | WJC S48 |
| `.stcareer` | 1 | STHS cumulative career-statistics file | IIHF S46 round robin |
| `.fhm` | 1 | Binary `team_0.fhm` file; its detailed contents have not been decoded | WJC S61 |
| `.zip` | 1 | FHM save-data bundle: 28 `.dat` entries, including `players.dat`, `teams.dat`, `leagues.dat`, `names.dat`, and `personal.dat`, plus one other entry | SHL S88 |

The roles of STHS native extensions are described in the
[official STHS manual](https://sths.simont.info/ManualV3_En.php); their sampled
binary contents were not decoded. CSV exports dominate newer-season coverage.
JS, CSS, images, and navigation maps primarily support the HTML files now
excluded, but remain eligible under the current policy.

Older CSV coverage is sporadic: SHL S28, S29, S32, and S34; SMJHL S32, S35,
S37, S38, and S40; WJC S45 and S51. The `team-files/83/` path is reported
literally, without assuming that its numeric folder is a season identifier.
Sampled newer CSVs are semicolon-delimited and contain named FHM attributes,
ability/potential, line assignments, and box-score fields. Sampled older
STHS CSVs are comma-delimited and include ratings, contracts, and statistics.

## Manifest schema and stable ordering

Every published snapshot contains one `manifest.json` matching the versioned
[JSON Schema](game-archive-manifest.schema.json) (Draft 2020-12):

```json
{
  "$schema": "https://raw.githubusercontent.com/nisamson/shuttle/main/docs/game-archive-manifest.schema.json",
  "schemaVersion": 1,
  "files": [
    "shl/S85/csv/example.csv"
  ],
  "skippedDirectories": [
    {
      "path": "iihf/S37/",
      "url": "https://simulationhockey.com/games/iihf/S37/",
      "title": "SHL Hockey => IIHF Indexes",
      "reason": "non-apache-directory-listing"
    }
  ]
}
```

`files` contains owned source-relative file paths. `skippedDirectories` contains
each excluded boundary's relative `path` (trailing slash; `.` means the source
root), absolute `url`, nullable page `title`, and `reason`. Empty inventories or
exclusion lists are represented by empty arrays, not omitted fields.

For useful diffs, file paths and skipped-directory entries are sorted using
ordinal, case-sensitive path order, independent of traversal order or host
culture. Root properties always appear as `$schema`, `schemaVersion`, `files`,
`skippedDirectories`; skipped entries use `path`, `url`, `title`, `reason`.
Output uses two-space indentation, LF line endings, and a final newline, with
no timestamps. Unchanged content therefore produces an identical manifest and
no extra commit.

The reader requires all schema fields with exact names and types, rejects
duplicate/unknown properties and unsupported schema versions, and additionally
checks portable path safety, case-insensitive
uniqueness, safe directory URLs, known skip reasons, and that no owned file lies
beneath a skipped directory. Invalid existing metadata aborts the sync without
publishing. The schema document lives in this repository, not as a second
metadata file in the archive.

There is no legacy metadata migration: the archive has not been deployed.
Development repositories using the previous format must be reset before use.

## Skipped source directories

A successful HTML directory response that is not an Apache listing is
automatically recorded and skipped, along with its entire subtree. For example,
`games/iihf/S37/` serves a hand-written IIHF index. Its navigation links are not
followed because they cannot establish a complete file inventory.

Warning logs identify skipped directories and make the reduced coverage
explicit. The manifest's `skippedDirectories` lists only discovered boundaries,
not unseen directories beneath them. When nothing is skipped that array is `[]`.
If a directory resumes serving an Apache listing, it is crawled normally and removed
from the exclusion list.

**Previously managed files beneath skipped directories are removed from the
current snapshot**, while earlier Git commits retain them. Unmanaged files,
including the root README, remain untouched. If the source root itself is
non-Apache HTML, all managed source files are removed from the current snapshot
and the root is recorded as skipped.

HTTP failures, non-HTML directory responses, unsafe entries, and failed or
incomplete downloads remain fatal. They publish neither file changes nor a new
manifest. Skipping is an intentional coverage exclusion, not a fallback for
network/download errors. Supporting custom indexes remains future work; this
policy enables archiving the remaining Apache-listed source now.

### HTTP 404 and upstream removals

HTTP 404 responses are not retried. A 404 while fetching a directory listing
or downloading an eligible file advertised by a listing aborts the sync, leaving the
remote archive and manifest unchanged. A 404 is not a skipped-directory
entry; that requires a successful HTML response.

A previously managed file absent from a successfully fetched listing is
treated as removed upstream and deleted from the current snapshot, with its
content retained in Git history. In contrast, an eligible listed file returning 404
makes the inventory inconsistent, so no snapshot is published.

If upstream updates are shown to cause transient listed-file 404s, a small,
separately bounded retry budget could be added for those downloads. This is
not implemented; persistent 404s must still fail the snapshot.

GitHub rejects normal Git files over 100 MiB. The job fails with the offending
file's URL rather than silently omitting it; Git LFS is intentionally not used.
For a large initial backfill, inspect available disk space and the projected
repository size (GitHub recommends staying under 5 GiB) before activation.
Every sync holds downloaded files and the full current Git checkout separately
until the snapshot is pushed. Budget for roughly two uncompressed snapshots,
plus Git objects, packing overhead, and free-space headroom. Depth 2 limits
temporary history, not the current checkout's file size.

## Development

### Standalone archive-only run

With the .NET 10 SDK and Docker available, run from the repository root:

```powershell
dotnet run --project Shuttle.Backend.Aspire\Shuttle.Backend.Aspire.csproj --launch-profile ArchiveOnly
```

This profile starts only Gitea, its bootstrap, the Key Vault Emulator, and
`Shuttle.GameArchive.Runner`, alongside the Aspire dashboard. It requires no
Azure login, SQL database, API, Quartz, WebClient, or production PAT.
The runner waits for both the vault and Gitea bootstrap, reads its credential
through the emulator, and synchronizes **the real SHL source** once. Selecting
this profile is the explicit request to sync; `Archive:Enabled` does not gate it.
Check disk capacity before starting: every run downloads the entire eligible source
and shallow-clones the archive at depth 2. The 100 MiB file limit and
5 GiB repository-size warning also apply locally.

The runner exits `0` on success or no change, `1` on failure, and `130` on
cancellation. Inspect its logs/status in Aspire. Gitea and the dashboard stay
running after it exits. Browse the archive at
`http://127.0.0.1:3000/shuttle-bot/shl-games-archive`.
For another sync, restart `archive-runner` in the dashboard or use:

```powershell
aspire resource archive-runner restart --apphost Shuttle.Backend.Aspire\Shuttle.Backend.Aspire.csproj
```

Alternatively, stop and relaunch the AppHost. The repository persists in
`shuttle-games-archive-gitea-data`; no automatic reset occurs. The normal local
stack shares this container/volume and the dashboard ports, so do not run both
profiles simultaneously.

The runner only accepts the Development environment and loopback HTTP(S)
Git/vault destinations. Configuration overrides cannot point it at GitHub
or a production vault. `ArchiveOnly` is rejected during publish/deploy.
This exercises the production synchronization code locally, not Azure
managed-identity/RBAC or GitHub PAT permissions.

The real source contains custom index pages, so inspect the manifest's
`skippedDirectories` as well as the runner's warnings to understand which subtrees are not
archived. Use the fixture to verify local credential lookup and publishing
independently of the size and availability of the real source.

For a small, deterministic smoke test, start the committed fixture in another
terminal:

```powershell
uv run --no-project --python 3.13 python -m http.server 8000 --bind 127.0.0.1 --directory docs\game-archive-fixture
```

Then override the source in the terminal starting Aspire:

```powershell
$env:Archive__SourceUrl = "http://127.0.0.1:8000/games/"
dotnet run --project Shuttle.Backend.Aspire\Shuttle.Backend.Aspire.csproj --launch-profile ArchiveOnly
Remove-Item Env:Archive__SourceUrl
```

Check `shl/S01/summary.csv`, the preserved README, and
`manifest.json` in Gitea. Restarting the runner against an unchanged
fixture should create no new commit. Switching sources replaces the current
managed snapshot, not the Git history; use the fixture before a large backfill.

### Full-stack local development

Run the ordinary `Shuttle.Backend.Aspire` AppHost with Docker available. In run mode
Aspire starts local Gitea on loopback with persistent `/data` volume, and the
Azure Key Vault Emulator. The Gitea bootstrap creates `shuttle-bot` and the
`shuttle-bot/shl-games-archive` repository with a root README if missing; it
must not overwrite an existing repository. The local Gitea instance disables
public account registration; the AppHost's development-only bootstrap creates
the bot inside the container. The emulator supplies a Git
credential secret containing `testtest`. It may prompt to trust a local
certificate on first use; trust only the generated development certificate.
Neither local Git credentials nor the emulator are used in production.

The API's archive sync remains disabled by default in local development to avoid an
unintended full crawl. For a small smoke test, start the
committed Apache-style fixture with:

```powershell
uv run --no-project --python 3.13 python -m http.server 8000 --bind 127.0.0.1 --directory docs\game-archive-fixture
```

Set `Archive__SourceUrl=http://127.0.0.1:8000/games/` and
`Archive__Enabled=true` for the API process before starting the AppHost.
If running the API inside a container, host networking must make that
loopback fixture reachable; in Aspire local run mode the API is a host
project. Once Gitea and the vault emulator are ready, trigger the job
through the protected Quartz dashboard, check `shl/S01/summary.csv` and
the preserved README in Gitea, then restart the AppHost and confirm the
commit persists. The local Git service's named volume persists between
runs; do not remove that volume unless intentionally discarding the test archive.

## Production setup and rollout

1. Bootstrap the updated Aspire AppHost with `FIRST_RUN=true` from an identity
   authorized for Azure role assignments. It provisions Azure Key Vault and
   grants the API managed identity **Key Vault Secrets User**. The ordinary
   release principal cannot create role assignments, so bootstrap is required
   before CI deploys; see [deployment prerequisites](deployment-ci.md).
2. Create a fine-grained GitHub PAT for `shuttle-bot` with **Contents: read/write**
   on `shuttle-shl/shl-games-archive` only. Insert it into the provisioned
   Azure Key Vault under `shl-games-archive-git-password` using an authorized
   operator identity. Do **not** put it in appsettings, GitHub Actions secrets,
   remote URLs, or the repository. Rotate it by replacing the vault secret;
   subsequent runs read the latest version.
3. Production defaults to `Archive:Enabled=true`; set it to `false` explicitly
   to pause syncing. Until the vault secret is readable, the job fails on
   credential lookup without crawling or pushing any files. `Archive:SourceUrl`,
   `Archive:RemoteUrl`, `Archive:SecretName`, and `Archive:GitUsername` can be
   configured if needed; production permits only the `shuttle-shl/shl-games-archive`
   HTTPS GitHub remote. The vault URI comes from the Aspire
   `archivevault` connection reference (or `Archive:VaultUri`); the legacy
   `archive-vault` connection name remains accepted. The dash-free connection
   name is required by Azure App Service.
4. Monitor initial backfill, GitHub repository growth and job failures in
   Quartz/Application Insights. A missing secret, oversize file, source error,
   credential failure or non-fast-forward push must fail visibly rather than
   delete or skip files. Enabling the schedule does not provision the PAT.
   Review `manifest.json`'s `skippedDirectories` and warning logs for
   intentionally excluded non-Apache directories; they do not fail the job.

The archive is public; its access credential is not. Do not paste the PAT into
logs, ticket comments, or a Git remote URL.

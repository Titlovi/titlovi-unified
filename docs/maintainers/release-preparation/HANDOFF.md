# Official Titlovi.com Unified release handoff (preparation only)

**Status:** Source migration and historical Git/PR audit completed on
2026-10-19, including recovery of the one confirmed missing functional change
([official PR #3](https://github.com/Titlovi/titlovi-unified/pull/3)).
Official binary release **2.0.0.0 has not been approved or published**.
See [migration completeness audit](../MIGRATION-AUDIT.md) for exact source
provenance, branch review and remaining verification limitations. The canonical source is now
`Titlovi/titlovi-unified`, imported from the previously private development
repository `mihvel/Titlovi.com.Unified` with its original Git ancestry.
The source import commit is `8161e0686cd22e6e71f5c88f38712bd14bb0df18`,
whose parent is `1621749f5901dd4c056a16b9590f35659ac98166`.
Both commits reference the **same source tree**
`8fd979085639ab8e21d8a4ab85893f9d26280407`.

The agreed architecture remains: this repository is the sole canonical source;
`Titlovi/titlovi-jellyfin` is Jellyfin-only distribution with its unchanged
historic catalogue; and `Titlovi/titlovi-emby` is the future manual Emby DLL
download location. All three repositories exist. The official 2.0 release,
publication automation and catalogue update have **not** been performed.

## Release boundaries

- Official target: **Jellyfin 12 / .NET 10** and **Emby Server 4.10.x**.
- One source tree: `src/Common`, `src/Jellyfin`, `src/Emby`.
  Each host DLL compiles Common sources directly. Never distribute a separate
  Common/Core DLL.
- Preserve the Jellyfin plugin GUID
  `6e25df50-638e-4109-a50b-03c14fc93fdd` and Emby GUID
  `1dbf2e1e-3187-4f63-9a60-4ca7ace4232e`.
- First official Unified candidate: **2.0.0.0**, requiring maintainer approval
  at release time. Source stays at 1.1.9 until release preparation is approved.
- Historical Matko Jellyfin 1.x releases stay intact in GitHub Releases, tags,
  Git history and the **same existing catalogue**:
  `https://titlovi.github.io/titlovi-jellyfin/repository.json`.
- No general migration from the old unofficial temporary Jellyfin GUID; its
  only tester can remove it manually.

## What has been proven

- Historical pre-migration acceptance: **164/164** regressions on Windows
  and Ubuntu (migration fix branch). **Current official main baseline: 166/166**
  on both Windows and Ubuntu, with both host builds, after recovering the
  host-specific Search `X-App` regression in [PR #3](https://github.com/Titlovi/titlovi-unified/pull/3)
  ([CI](https://github.com/Titlovi/titlovi-unified/actions/runs/39325495070)).
- Jellyfin 12 runtime smoke with original Matko credentials XML and a
  *pre-existing blank new configuration*; Emby 4.10 runtime load.
  Official PR #3's [Linux runtime smoke](https://github.com/Titlovi/titlovi-unified/actions/runs/39325495191) passed.
- Real Windows Jellyfin 12: successful upgrade to Unified preview **1.99.1.0**,
  automatic import of the original Matko login and successful download of a
  subtitle after server restart.
- Cross-repo test catalogue publishing permission: authenticated dry-run push
  passed. The *actual publish operation* has not been exercised.
- A full Jellyfin 10.11 **server database** migration to 12 did not finish in a
  separate CI rehearsal. This is not a failure of plugin migration, but neither
  is it a passed end-to-end host upgrade.

## Agreed official repository split (2026-10-18)

- **`Titlovi/titlovi-unified` (created, populated):** the sole canonical source
  and developer repository with `src/Common`, `src/Emby`, `src/Jellyfin`,
  regression tests, release orchestration and common source tags. One build
  produces two self-contained host plugins, without a Common/Core runtime DLL.
- **`Titlovi/titlovi-jellyfin` (existing):** Jellyfin-only release ZIP,
  existing immutable 1.x release assets/history and unchanged Jellyfin
  catalogue at `https://titlovi.github.io/titlovi-jellyfin/repository.json`.
- **`Titlovi/titlovi-emby` (created, currently no release):** only the
  `Emby.Titlovi.dll` release downloads and manual Emby 4.10.x install/upgrade
  documentation. Do **not** prepare an Emby third-party plugin feed or promise
  automatic installation/updating via a repository.

The initial candidate build scripts and read-only Jellyfin catalogue validator
remain useful, but they are not a complete implementation of the new
cross-repository release workflow. Do not distribute Emby artifacts via the
Jellyfin repo. Host distribution repos link to the matching canonical
Unified source tag/commit. GitHub Actions cross-repo publication needs
separately authorized, narrowly scoped credentials and must be verified
before use.

## Existing upstream state — do not overwrite silently

At preparation time `Titlovi/titlovi-jellyfin` has:

- `main` for Jellyfin 10.11 / net9.0 at `1.2.5.0`.
- `jellyfin-12`, the historical **draft upstream PR #5**
  (https://github.com/Titlovi/titlovi-jellyfin/pull/5), which attempted a
  separate net10.0/Jellyfin 12 adaptation of Matko's plugin. **Decision
  confirmed with Nikola on 2026-10-19: this approach is superseded and will
  not be incorporated into Unified.** The PR was closed *without merging*;
  its branch, commits and discussion were retained as history. This item is
  **resolved** and is not a release blocker.
- `gh-pages/repository.json` with **nine** original Jellyfin releases; the
  older versions include both 10.10 and 10.11 ABIs.
- The historic reusable Jellyfin publish workflow builds the *old*
  Titlovi.Plugin and its multiple dependencies. It is **not** correct for
  self-contained `Jellyfin.Titlovi.dll`. Replace/disable that workflow before
  any official 2.x release/tag.

## Safe local release-candidate preview (Windows and Linux)

From the isolated migration preparation branch:

```bat
REM Windows, from the repository root:
scripts\windows\build-official-candidate.cmd
```

```bash
# Linux, from the repository root:
python3 scripts/migration/build-official-candidate.py
```

Requirements: Python 3, stable .NET 10 SDK and NuGet access. Windows uses
the existing PowerShell build machinery, which can download the appropriate
Emby reference archives if needed; Linux uses Bash and Docker if Emby 4.10
reference assemblies are not already installed. The script:

1. Copies source to an ephemeral directory (ignoring `.git`, build and dist).
2. Patches **only the ephemeral copy** to version 2.0.0.0 and official naming
   in both plugins, configuration pages, Jellyfin packaging and Windows
   packaging scripts (the latter for the eventual Windows release process).
3. Uses the current Linux build scripts to build **both** host assemblies.
4. Validates a three-file Jellyfin 12 ZIP (`Jellyfin.Titlovi.dll`,
   `meta.json`, `thumb.png`) with the original Jellyfin GUID and correct ABI.
5. Collects `dist/official-candidate/Emby.Titlovi.dll` and
   `dist/official-candidate/titlovi-com_2.0.0.0.zip`, plus MD5/SHA256 files.
6. Does not change source version, create a Git tag, push a release, change the
   official catalogue or publish downloadable assets.

**Candidate output is NOT a release.** Every build must be reviewed against
the final official source and approved by the official maintainers.

## Preparing the existing Jellyfin catalogue (offline)

First obtain the **current** official
`gh-pages/repository.json` from `Titlovi/titlovi-jellyfin` (never the
working user's temporary test catalogue). For example:

```bash
curl --fail --location --silent --show-error \
  https://raw.githubusercontent.com/Titlovi/titlovi-jellyfin/gh-pages/repository.json \
  --output dist/official-candidate/original-repository.json

python3 scripts/migration/prepare-official-catalogue.py \
  dist/official-candidate/original-repository.json \
  dist/official-candidate/titlovi-com_2.0.0.0.zip
```

This generates `dist/official-candidate/repository.review-only.json` with
the new version first and all previously published version **objects unchanged**.
It fails closed for wrong GUID, unexpected version/ABI, missing older versions,
invalid ZIP or an already-published 2.0.0.0 entry.

Under the agreed three-repository split, this source URL correctly points
to a **future Jellyfin-only** `Titlovi/titlovi-jellyfin` release asset
`v2.0.0/titlovi-com_2.0.0.0.zip`. The catalog is **not publishable** until
that exact release and asset exist and the published ZIP checksum is verified
against the new catalogue entry. An asset upload and catalogue update must
follow that order.

## Candidate build verification (2026-10-18)

Both isolated 2.0.0.0 build paths have passed without modifying tracked
versions or publishing any release:

- **Ubuntu/Linux:** [GitHub Actions #39127480109](https://github.com/mihvel/Titlovi.com.Unified/actions/runs/39127480109). **164/164** regression cases, both host builds, validated Jellyfin ABI/ZIP, MD5/SHA256, historical official catalogue with **nine** entries preserved. Review-only artifact expires 2026-10-23.
- **Windows:** [GitHub Actions #39127793618](https://github.com/mihvel/Titlovi.com.Unified/actions/runs/39127793618). Both host builds passed; Jellyfin ZIP name, layout, GUID, ABI and SHA256 sidecars verified. Review-only artifact expires 2026-10-23.

These are candidate packaging tests. The final source version and official
GitHub Release have **not** been updated.

## Existing publication template — superseded, DO NOT deploy

`scripts/migration/templates/official-publish.yml` is deliberately
**non-executable** in this repository. It assumed the *old* architecture:
source code and both host assets would live in
`Titlovi/titlovi-jellyfin`. It is now **superseded** by the agreed
three-repository structure. Keep it only as a reference for version,
checksum and historical-catalogue safety checks. **Do not copy it as-is
into any official repository or enable it.**

A replacement pipeline must build/test once from a tagged
`Titlovi/titlovi-unified` source commit; publish only the Jellyfin ZIP
to `titlovi-jellyfin` and only the Emby DLL to `titlovi-emby`; verify
published checksums, and update the old Jellyfin catalogue only after the
new ZIP is actually accessible. Publishing rights should be limited to
the necessary destination repos and gated by explicit maintainer approval.

## Canonical source handoff — complete

On 2026-10-19, the reviewed `migration/official-release-preparation`
source branch was imported to the previously empty official `main`, with
its ancestry intact and a migration-only `[skip ci]` commit. The tree
was verified identical and **no GitHub Actions runs** were triggered.
No version change, source rebuild or publication occurred.

The official repository now contains both host projects, shared Common code,
regression tests, platform-specific build scripts and English documentation.
Generated `.build` and `dist` output, local configuration, secrets and tokens
were not added.

Remaining coordination and release-preparation tasks:
- Preserve required GPLv3 notices and Matko's attribution in historical
  source/release references. Explicitly link the new central source to
  the legacy Jellyfin project/history.
- Keep the existing `titlovi-jellyfin` Git history, tags, nine
  released versions, original GUID, `gh-pages` catalogue and user links.
  Its distribution workflow should handle **only Jellyfin** assets.
- Historical upstream Jellyfin PR #5: **resolved** in agreement with Nikola;
  archived by closing without merging, retaining its original branch/history.
- Historical host-specific Search `X-App` PR #12 in the former development
  repository: **recovered and merged** via official Unified PR #3. Jellyfin
  sends `titlovi-jellyfin`; Emby sends `emby-titlovi` on Search requests only.
- Use `titlovi-emby` as an Emby 4.10 **manual-download** location.
  Distribute `Emby.Titlovi.dll` with documentation and checksums, not
  a Jellyfin ZIP or third-party Emby plugin-repository manifest.
- Prepare host-specific READMEs/INSTALLs linking back to the same
  central Unified tagged source. The Emby installation instructions
  should cover stopping the server, copying the DLL into the actual
  plugins folder, restarting and checking subtitles.
- Review cross-repository build/publish permissions and final freeze
  gates with Nikola before enabling any official release workflow.

## Draft publication documentation

The following English documents remain **drafts** in the canonical source
repository. They do not authorize a release or imply that 2.0 binaries are
available:

- `docs/maintainers/release-preparation/RELEASE-NOTES-DRAFT.md` — release notes for proposed 2.0,
  compatibility, migrated credentials, features, limitations and attribution.
- `docs/maintainers/release-preparation/INSTALL-DRAFT.md` — Jellyfin 12 fresh install and original-plugin
  upgrade, Jellyfin 10.11 server-upgrade boundaries, Emby 4.10 manual install,
  credential troubleshooting and rollback.

At actual release preparation, integrate the reviewed content into the
official `README.md`, `docs/en/INSTALL.md`, `CHANGELOG.md`, `NOTICE.txt` and
release notes. Do not replace current stable development docs prematurely.


## Historical migration-plan decision reconciliation (2026-10-19)

The old living `MIGRATION-PLAN.md` (on
`mihvel/Titlovi.com.Unified:planning/official-migration`) was compared with
this handoff and the official installation/release drafts. Its applicable
technical and release decisions are represented here; the historical plan is
preserved in the old Git repository and is **not** a second release authority.

| Original decision / risk | Current disposition |
| --- | --- |
| One source, two self-contained host DLLs | Implemented in `src/Common`, `src/Jellyfin`, `src/Emby`; independent Emby/Jellyfin builds verified. |
| Jellyfin original GUID and legacy credentials, no old-token reuse | Implemented with one-time migration; real preview 1.99.1.0 credential import and subtitle download passed. Still check the final 2.0 package. |
| Jellyfin 10.11 to 12 host upgrade is distinct from plugin update | Documented in the official install draft. Full server/database upgrade rehearsal **not passed**; it remains a separate validation/host-support matter, not evidence against plugin credentials import. |
| Historical catalogue and original 1.x compatibility | Nine existing entries preserved in read-only catalogue checks. Real 2.0 catalogue update, selection by host ABI and 1.x historical download check are **not yet performed**. |
| Host-specific `X-App` on GET Search | **Recovered** from original unmerged PR #12 and merged into official source via PR #3; both new regression cases passed (166/166 total). |
| Original Matko Jellyfin 12 draft PR #5 | **Resolved with Nikola:** closed without merging, retained for historical reference; do not revive or port changes automatically. |
| Release permissions, responsible maintainers and rollback | **Still require explicit release authorization and a restricted cross-repo pipeline**. Preserve catalogue backup, original asset URLs and a hold/rollback path; do not republish old release assets. |
| Publication sequence | Tag approved Unified source; build/test once; publish Jellyfin ZIP and Emby DLL to their **separate** distribution repos; verify downloaded binaries/checksums; **only then** update the existing Jellyfin catalogue and announce. No Emby feed. |
| Developer temporary unofficial plugin | No public migration route. Remove/isolate the temporary developer installation during real upgrade acceptance; preserve the official original Jellyfin GUID. |
| Branding, source version and public documentation | Official display name `Titlovi.com` and first version `2.0.0.0` are **release-time changes**, not yet applied. The English docs are authoritative and Croatian and Serbian Cyrillic translations are maintained; this later documentation decision supersedes the old English-only note. |

The former planning document also described **opt-in experimental** Jellyfin
test-catalogue publishing and a Jellyfin server-upgrade CI harness. Those are
historical test tools, not currently authorized production workflows. The
superseded `scripts/migration/templates/official-publish.yml` must never be
activated as-is.

### Outstanding decisions for release readiness

- Agree with Nikola who approves the source tag, both distribution releases,
  the Jellyfin catalogue change and any rollback.
- Confirm restricted, cross-repository publishing credentials and a staging
  / dry-run that cannot affect historical releases.
- Record final Jellyfin 12 fresh-install, original-plugin upgrade and Emby
  4.10 live validation against the **actual release candidate**.
- Verify Jellyfin 10.11 catalogue compatibility for historical 1.x releases
  and correct Jellyfin 12-only ABI targeting for 2.0.

## Remaining release gates

The historical [official migration audit](../MIGRATION-AUDIT.md) and the
reconciled decision register below are source-completeness evidence, **not**
a substitute for final 2.0.0.0 install/upgrade and distribution acceptance.

- Confirm the remaining official release and publication approvals with Nikola.
  Upstream Jellyfin draft PR #5 was already resolved/archived on 2026-10-19.
- Review **final** official docs, licensing, historical attribution and API
  client request headers for each host.
- Verify a fresh Jellyfin 12 installation and Emby 4.10 package installation.
- Verify original 1.x Jellyfin catalogue entries and downloads remain intact
  after the proposed 2.0 addition; confirm the real published release ZIP's MD5.
- Confirm the official 2.0 release publish workflow does not produce a
  dependency bundle or unexpected ZIP layout.
- Final source release/version bump **only after** approval; create final tags,
  official release and catalogue through the official maintainers' process.

**Do not interpret a test candidate or a successful preview as production
approval.**

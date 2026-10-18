# Official Titlovi.com Unified release handoff (preparation only)

**Status:** Candidate preparation; **NOT approval to publish**. Working source of
truth: `mihvel/Titlovi.com.Unified`. Future public source and releases:
`Titlovi/titlovi-jellyfin`. No official repository writes occur in this work.

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

- Windows and Ubuntu offline regression suite: **164/164** on the tested
  migration fix branch, with both Windows host builds.
- Jellyfin 12 runtime smoke with original Matko credentials XML and a
  *pre-existing blank new configuration*; Emby 4.10 runtime load.
- Real Windows Jellyfin 12: successful upgrade to Unified preview **1.99.1.0**,
  automatic import of the original Matko login and successful download of a
  subtitle after server restart.
- Cross-repo test catalogue publishing permission: authenticated dry-run push
  passed. The *actual publish operation* has not been exercised.
- A full Jellyfin 10.11 **server database** migration to 12 did not finish in a
  separate CI rehearsal. This is not a failure of plugin migration, but neither
  is it a passed end-to-end host upgrade.

## Existing upstream state — do not overwrite silently

At preparation time `Titlovi/titlovi-jellyfin` has:

- `main` for Jellyfin 10.11 / net9.0 at `1.2.5.0`.
- `jellyfin-12`, a distinct **draft upstream PR #5**
  (https://github.com/Titlovi/titlovi-jellyfin/pull/5), preparing net10.0,
  Jellyfin 12 and version **1.3.0.0** with upstream fixes to authorization and
  episode ZIP selection. Coordinate its disposition with Nikola/maintainers.
  Do not delete that branch/PR or claim its fixes are automatically present in
  Unified without comparing behavior.
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

The proposed source URL deliberately points to a **future**
`Titlovi/titlovi-jellyfin` release asset
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

## Prepared upstream publication workflow (disabled template)

`scripts/migration/templates/official-publish.yml` is a **non-executable
template in this repository**, not a deployed GitHub Actions workflow. It is
specifically restricted to the initial `v2.0.0` official release in
`Titlovi/titlovi-jellyfin`, and requires explicit upstream review.

After the official source branch has been merged and checked, upstream
maintainers can adapt and enable the template **instead of** the current
Matko build/publish workflows. It runs the suite, builds both host packages,
validates the official identity and ZIP contents, creates Matko-compatible
release checksum sidecars, uploads only allowlisted assets, downloads the
actual release ZIP again and checks its content, then generates a new
`gh-pages` catalogue from the existing historical manifest with old release
objects untouched.

It should be verified on a controlled draft release/isolated fork before
being trusted to change the actual official catalogue. **Do not copy it
unchanged into production without testing and without coordination with
Nikola.** Verify the permissions, release event semantics, and the live
GitHub Pages branch protection first. Official packaging scripts also need
the final tracked official version/name update before the template can run.

## Preparing the upstream source PR

After explicit upstream agreement, work in a dedicated **official repository
branch** created from its actual latest `main` (not force-push or reset).

- Preserve Git history and 1.x releases/tags. Replacing tracked source paths
  with a focused commit is fine; rewriting prior commits is not.
- Remove obsolete Matko-specific build source/dependencies only as part of
  the approved source replacement; keep all legally required notices.
- Transfer the reviewed Unified source, both host project definitions, tests,
  platform-specific build scripts and **English** documentation. Do not transfer
  `.build`, `dist`, caches, local settings, tokens or secrets.
- Adapt `README.md`, `INSTALL.md`, `README-BUILD.txt`, `ARCHITECTURE.md`,
  `CHANGELOG.md`, `NOTICE.txt` and release notes for the official status.
  `INSTALL.md` must provide supported server versions, in-place migration
  instructions, a backup/rollback warning and the retained Jellyfin catalogue.
  Preserve Matko's authorship and distinguish GPLv3 copyright acknowledgments
  from Titlovi.com branding and trademark permission.
- Replace upstream build/release actions with tested **Unified** actions
  respecting Jellyfin 12/.NET10 and Emby 4.10. Limit pushes and automatic CI
  triggers to conserve GitHub Actions minutes. Publish only on an authorized
  release event with correct versions, asset allowlist and checksums.
- Get maintainers' review on the source PR, compare upstream PR #5's unique
  fixes, run a one-time full regression + two host builds and final runtime
  acceptance. Do not merge with an automatic release trigger still active.

## Remaining release gates

- Confirm current official repository ownership, commit privileges and explicit
  acceptance with Nikola; resolve upstream draft PR #5.
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

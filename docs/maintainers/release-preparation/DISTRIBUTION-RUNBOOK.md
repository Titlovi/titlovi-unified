# First official Unified 2.0.0.0 distribution runbook — DRAFT

> **Historical release-preparation snapshot (archived in place).** Emby and Jellyfin Unified 2.0.0.0 were both published on 2026-10-20. This document records pre-release planning and may contain obsolete status statements, pending checklist items, and provisional instructions. It is **not** the current installation or release status. For current guidance, see [the Unified README](../../../README.md), [installation guide](../../en/INSTALL.md), and [official changelog](../../../CHANGELOG.md). Do not use this historical document to publish another release.

**PRE-RELEASE ONLY.** This is an engineering/release checklist, not permission
to publish. No tag, GitHub Release, Jellyfin catalogue entry, Emby feed, or
public binary is authorized by this document. The first official release will
be explicitly approved by Titlovi.com maintainers.

## Repositories and roles

| Repository | Purpose | Allowed first-release files |
| --- | --- | --- |
| [Titlovi/titlovi-unified](https://github.com/Titlovi/titlovi-unified) | **Only canonical source**, regression tests, source tag and one coordinated build | Source tag `v2.0.0` when authorized; reviewed CI build output before redistribution |
| [Titlovi/titlovi-jellyfin](https://github.com/Titlovi/titlovi-jellyfin) | Preserve Matko's Git history, original GUID, nine 1.x releases, `gh-pages/repository.json`; distribute **only Jellyfin** | Release `v2.0.0`: `titlovi-com_2.0.0.0.zip` with exactly `Jellyfin.Titlovi.dll`, `meta.json`, `thumb.png`; corresponding checksum sidecars |
| [Titlovi/titlovi-emby](https://github.com/Titlovi/titlovi-emby) | Manual Emby DLL downloads and installation documentation, **no** Emby catalogue | Release `v2.0.0`: `Emby.Titlovi.dll` and checksum sidecars |

Both binaries must be built from **exactly the same reviewed Unified tag**.
Do not copy Unified application source
or Emby binaries into the historical Jellyfin distribution repository.

## Provenance and release gates

1. Verify current canonical `main`, migrate/close any unresolved accepted
   change, and review [source migration audit](../MIGRATION-AUDIT.md).
   Do not use an older ZIP, local build output, or historical migration branch
   in preference to the canonical source.
2. Confirm maintainer approval and accountability for the source tag,
   both distribution Releases, catalogue update, and possible rollback.
   Clarify narrowly scoped cross-repository write credentials; do not reuse
   personal tokens casually or expose them in logs.
3. Keep development version `1.1.9` until **approved release preparation**.
   After approval, use a focused version/name change PR on the canonical
   source and run all 166+ current regressions, Windows/Linux builds and
   runtime smoke checks. Keep the actual regression baseline updated.
4. Verify the final official GUID `6e25df50-638e-4109-a50b-03c14fc93fdd`
   throughout Jellyfin assembly/configuration/metadata/catalogue, preserve the
   separate Emby GUID `1dbf2e1e-3187-4f63-9a60-4ca7ace4232e`.
   Verify host-specific **GET Search** `X-App` headers:
   Jellyfin `titlovi-jellyfin`, Emby `emby-titlovi`.
5. **Phase-specific acceptance:** before publishing Emby, verify real Emby
   4.10 DLL installation/update, load, account settings, search, download and
   restart. Before publishing Jellyfin, additionally verify Jellyfin 12 fresh
   installation, original Matko plugin upgrade with credentials migration,
   search/download and restart. Do not conflate Jellyfin plugin migration
   with the separately blocked Jellyfin 10.11-to-12 **server database**
   upgrade rehearsal. Both hosts' build and offline tests must pass before
   the **first** (Emby) publication.
6. Preserve the original nine 1.x catalogue entries, URLs, checksums and
   historic tags/assets. Test that older compatible Jellyfin releases remain
   selectable for older hosts and that 2.0 targets **only Jellyfin 12**.
7. Review final English/Croatian/Serbian Cyrillic documentation, license
   and Matko attribution. Confirm packaging hashes, metadata, installation
   notes, backup and rollback instructions.

## Non-publishing review path — available now

Run the manually invoked, **read-only**
[`review-official-distribution-candidate.yml`](../../../.github/workflows/review-official-distribution-candidate.yml)
workflow on the reviewed source ref. It:

- tests offline and compiles independent Emby/Jellyfin packages on Windows
  and Linux;
- uses the existing `build-official-candidate.py` to patch official
  2.0 branding/version in a **temporary source copy only**;
- retrieves the existing public Jellyfin `gh-pages/repository.json`
  **read-only** and runs `prepare-official-catalogue.py` to generate a
  separate `repository.review-only.json`;
- validates the 3-file Jellyfin ZIP, Emby DLL, hashes, and preservation of
  all nine published Jellyfin release entries;
- stores artifacts as **NOT-FOR-RELEASE** CI artifacts with short retention.

Its token permissions are `contents: read`; it has no GitHub Releases
upload, push, catalogue write, third-party token, or production secret. It
is **not** the production publisher. Successful offline output alone never
authorizes distributing preview artifacts to users.

## Agreed sequence: Emby first, Jellyfin last (2026-10-19)

The maintainers have agreed to **stage the two host releases** rather than
publish them simultaneously. Emby 4.10's manually installed DLL is the first
distribution. Jellyfin 12 comes only after Emby publication/acceptance and its
own additional compatibility and legacy-catalogue gates.

**Important distinction:** source/build consistency does *not* require both
distribution releases to appear on the same day. Freeze and tag **one**
reviewed Unified source commit for the planned 2.0.0.0 binaries; build and
test **both** host candidates from this source before the Emby release. The
Jellyfin package can remain unpublished while its separate live and catalogue
checks are completed.

**Immutable source rule:** never move or rewrite the released Unified source
tag. If an Emby release is published and later Jellyfin work requires *any*
change to the original tagged source, do **not** silently reuse the same
2.0.0.0 version/tag for a different build. Evaluate a subsequent version and
new source tag, run full regressions/builds again, and record which original
source each published distribution asset actually came from. An Emby release
already published remains bound to its original verified source tag.

## Ordered production publication — not yet implemented or authorized

Only after the relevant approval and acceptance gates:

### Phase A — common source and candidate freeze (before any release)

1. Obtain explicit maintainer authorization for release preparation and
   coordinate the source tag/version/official naming in the canonical
   `Titlovi/titlovi-unified` repository.
2. Finish regression and build verification for **both** Emby and Jellyfin
   on Windows/Linux; keep the final count and traceable commit SHA. Prepare
   each package from the **same immutable approved source**. A historical
   candidate built from temporary version overlays is not itself a
   publishable official release.
3. Verify Emby 4.10 live acceptance independently: plugin load and settings,
   a real search/download, identity preservation on update, restart, and
   rollback to a compatible backed-up plugin. Verify the Emby `X-App`
   Search header. Review end-user installation/attribution and checksum
   documentation. See [Emby first-release checklist](EMBY-FIRST-ACCEPTANCE.md).

### Phase B — publish and verify **Emby 2.0.0.0 first**

4. Publish **only** `Emby.Titlovi.dll` and its verified checksum sidecars
   to `Titlovi/titlovi-emby` as an approved GitHub Release pointing to
   the matching `titlovi-unified` source tag. Emby has **no** repository
   feed, catalogue or automatic-update endpoint.
5. Re-download the **actual public DLL**, verify the SHA-256 checksum and
   provenance, install it on Emby 4.10 and confirm it loads and downloads a
   subtitle. Record the outcome, and **hold** the Jellyfin phase if a shared
   regression or release-provenance issue appears. Emby release notes must
   not claim that Jellyfin 2.0 is already available.

### Phase C — independently prepare **Jellyfin last**

6. Preserve a backup of the original Jellyfin
   `gh-pages/repository.json`, all **nine historical release entries**,
   URLs/checksums and old tags/assets. Confirm original plugin GUID,
   Jellyfin 12 ABI, Matko configuration import, fresh install, in-place
   update, restart and real subtitle search/download. Verify older hosts
   still have their original compatible 1.x catalogue entries.
7. **Before creating/publishing any Jellyfin 2.x Release**, replace/disable
   the old `Titlovi/titlovi-jellyfin/.github/workflows/publish.yaml`
   through an explicitly approved change. The original workflow builds
   Matko's net9 package, uploads files and regenerates the catalogue on
   release events. It must not run during Unified 2.x distribution.
8. Publish **only** the Jellyfin ZIP and checksum sidecars to
   `Titlovi/titlovi-jellyfin`, linked to its verified Unified source tag.
   Re-download and independently check SHA-256, Jellyfin ZIP MD5, all three
   ZIP entries, assembly allowlist, original plugin GUID and Jellyfin 12 ABI.
   Never overwrite or delete existing Matko 1.x releases.
9. **Only after** the published Jellyfin ZIP is reachable and checksummed,
   append the matching new entry to the original Jellyfin
   `gh-pages/repository.json` while preserving all older version objects
   exactly. Re-fetch the public catalogue and check historical downloads.
10. Verify final Jellyfin 12 install/upgrade from the actual catalogue and
    communicate the host-specific availability and compatibility
    requirements. Obtain maintainer sign-off for the Jellyfin announcement.

**No phase is authorized for execution just because this runbook exists.**
The [manual review workflow](../../../.github/workflows/review-official-distribution-candidate.yml)
can validate internal candidates, but it is not the production publisher.

**Failure/rollback:** Before catalogue publication, do not reference an
unavailable package. If distribution validation fails, hold the new
catalogue entry; if already updated, restore the backed-up original
catalogue without touching historical 1.x binaries/releases. Inform users
about any manual recovery steps. A Jellyfin 10.11 plugin is not a safe
rollback package for an already-upgraded Jellyfin 12 host.

## Present state

- Original Jellyfin 1.x releases and catalogue are unchanged.
- The original Jellyfin `publish.yaml` is still active and **must be
  addressed before** authorizing a 2.0 release.
- Emby distribution repository has documentation, **no release binaries**.
- **Emby-first / Jellyfin-last** sequencing is agreed; no distribution release
  has been authorized or published under this plan.
- Official branding/version `2.0.0.0` is **staged in draft PR #9 only**, not
  merged to `main` or tagged. The production cross-repo publisher has not
  been implemented and needs separate approval/review.
- A tester reported successful installation/upgrade, preserved settings,
  movie + episode search/download, and correct subtitle display on their
  live Emby 4.10.x server on 2026-10-20. This is **candidate acceptance
  evidence**, not publication approval or a verified public-release hash.
  See [Emby first-release acceptance](EMBY-FIRST-ACCEPTANCE.md) for remaining
  rollback, source provenance, fresh-install and approval gates.

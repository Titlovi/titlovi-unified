# First official Unified 2.0.0.0 distribution runbook — DRAFT

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
No runtime Common/Core DLL is packaged. Do not copy Unified application source
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
5. Verify Jellyfin 12 fresh install, in-place original Matko plugin upgrade
   (including credentials import), real search/download and restart; verify
   the Emby 4.10 DLL loads and searches/downloads. Do not conflate Jellyfin
   plugin migration with the separate, previously blocked Jellyfin 10.11 to
   12 **server database upgrade** rehearsal.
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

## Ordered production publication — not yet implemented or authorized

Only after all release gates pass:

1. Freeze and tag the reviewed, approved versioned source in
   `Titlovi/titlovi-unified`; record the immutable source commit SHA.
2. Build both host artifacts **once** from this tag with verified provenance.
   Do not build from separate distribution repository source histories.
3. Take a backup of the current Jellyfin `gh-pages/repository.json`, record
   all historical entries and checksums, and prepare (but do not yet push)
   the candidate 2.0 manifest.
4. **Before uploading a new Jellyfin 2.x Release**, replace or disable the
   old `Titlovi/titlovi-jellyfin/.github/workflows/publish.yaml`
   release-triggered workflow. It still compiles Matko's net9 `Titlovi.Plugin`
   with `jellyfin-meta-plugins`, auto-uploads assets and regenerates the
   catalogue. That workflow is incompatible with our distribution-only model.
   Make this a separately reviewed, explicit maintainer-authorized change;
   do not trigger an original `v2.0.0` release while it remains active.
5. Publish **only** the verified Jellyfin ZIP and checksum sidecars into a
   `titlovi-jellyfin` release; publish **only** the verified Emby DLL and
   checksum sidecars into a `titlovi-emby` release. Give both links back to
   the identical canonical Unified source tag. Do not overwrite or delete
   historical Jellyfin release assets.
6. Retrieve the actual published binaries and independently check SHA-256
   (and Jellyfin ZIP MD5 for the catalogue entry), source provenance,
   package content and compatibility metadata.
7. **Only after** the Jellyfin ZIP is publicly reachable and verified,
   update its original `gh-pages/repository.json` with the 2.0 entry, keeping
   every historical entry unchanged. Re-fetch and re-validate the published
   catalogue and original historical URLs. No Emby catalogue update exists.
8. Verify the catalogue install/update experience on supported Jellyfin 12
   and the actual Emby DLL install. Announce availability only after these
   checks pass and the maintainers sign off.

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
- Unified official 2.0 name/version bump and production cross-repo publisher
  have **not** been implemented. They require separate approval and review.

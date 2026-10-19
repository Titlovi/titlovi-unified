# First official Emby 2.0.0.0 release — acceptance checklist (DRAFT)

**Status: not approved, not published.** This checklist prepares the **first**
distribution step under the agreed Emby-first / Jellyfin-last release order.
Do not upload binaries, create tags or publish a release until the Titlovi.com
maintainers explicitly approve that action.

## Scope

- Target: **Emby Server 4.10.x** only. No claim of support for other Emby
  versions.
- Distribution: GitHub Releases of
  [Titlovi/titlovi-emby](https://github.com/Titlovi/titlovi-emby),
  **manual DLL installation**, no third-party Emby catalogue.
- Only source of code: [Titlovi/titlovi-unified](https://github.com/Titlovi/titlovi-unified).
  The shared `src/Common` C# sources are compiled into `Emby.Titlovi.dll`;
  **no separate Common/Core runtime DLL**.
- The **Jellyfin** 2.0 release and its catalogue update are separate, later
  milestones. Matko's original Jellyfin plugin and catalogue are untouched
  by publishing Emby.

## A. Before the first public Emby release

- [ ] Nikola/Titlovi.com maintainers explicitly approve the public Emby
      2.0.0.0 release, its source tag and who is authorized to publish.
- [ ] Finish official branding and `2.0.0.0` version preparation on a
      focused Unified PR **after approval**, not on the current development
      `1.1.9` source.
- [ ] Review and freeze **one source commit/tag** for Emby and later Jellyfin,
      record the full commit SHA and resulting Emby binary SHA-256.
- [ ] Full regression suite passes at the **current** baseline (166 at this
      checkpoint, updated as tests change) on Windows and Linux.
- [ ] Build **both** Emby 4.10 and Jellyfin 12 candidates from that same
      immutable source on Windows and Linux; check that the Emby output is
      one `Emby.Titlovi.dll` without a Common/Core runtime dependency.
- [ ] Review packaging metadata, source provenance, GPLv3 licensing,
      `NOTICE.txt`, and clear Emby-only README/INSTALL/release notes.
- [ ] Confirm the Emby plugin retains its existing GUID
      `1dbf2e1e-3187-4f63-9a60-4ca7ace4232e` and displays
      **Titlovi.com** after the official branding switch.
- [ ] Confirm `X-App: emby-titlovi` on Emby GET Search; don't send
      Jellyfin's `titlovi-jellyfin` identifier from Emby.

## B. Acceptance on an actual Emby Server 4.10.x

- [ ] Back up the installed DLL and plugin configuration; record the Emby
      server version and the actual active plugins directory.
- [ ] Verify fresh manual installation of the **candidate** DLL, restart,
      plugin discovery and configuration (without exposing credentials).
- [ ] Verify upgrade/replacement from the previously working Emby DLL
      preserves relevant configuration or clearly documents any manual step.
- [ ] Search for at least one real **film** and one real **series episode**;
      check that distinct media IDs remain distinct search results and
      release-name ranking does not hide otherwise valid subtitles.
- [ ] Download a subtitle from each search and verify the on-disk subtitle
      content/encoding and actual language returned by Titlovi.com.
- [ ] Confirm archive/subtitle handling against applicable existing
      regression cases and a realistic archive case when possible.
- [ ] Restart Emby and verify the plugin remains loaded/configured.
- [ ] Rehearse a safe rollback from backup to a compatible prior Emby DLL
      without touching Jellyfin or unrelated Emby plugins.
- [ ] Record results and sanitized log excerpts: no passwords, tokens,
      private configuration XML or unnecessary personal paths.

**Known Emby 4.10 host limitation:** alternative-language filename suffix
behavior is accepted and must **not** be worked around in shared subtitle
logic. The plugin must still retain the true language from Titlovi.com.

## C. Controlled Emby publication — only after approval

- [ ] Confirm the intended `Titlovi/titlovi-emby` release/tag convention,
      matching immutable Unified source commit, and reviewed release notes.
- [ ] Publish the **approved, previously verified** `Emby.Titlovi.dll`,
      with `SHA256` checksum sidecar and provenance, to **Emby only**.
      No Jellyfin ZIP, catalogue edit or source rebuild on the Emby repo.
- [ ] Download the public release DLL again and compare its SHA-256 to the
      approved build; check size and source tag links.
- [ ] Manually install/test this **downloaded public** DLL on Emby 4.10.x
      and confirm real subtitle search/download.
- [ ] Mark Emby 2.0 release accepted and document the source SHA/hash.
      Hold the Jellyfin phase if a shared-functionality issue appears.

## D. The later Jellyfin step

Do **not** publish Jellyfin 2.0 or modify its existing catalogue as part of
the Emby release. Before Jellyfin publication, separately validate the
original Matko plugin upgrade and credentials import, Jellyfin 12 fresh
installation, package layout and checksum, the replacement of the old
net9 release-triggered publishing workflow, and the preservation of all
nine historical 1.x entries.

If Jellyfin testing uncovers a problem that requires changing the original
**tagged source**, do not retag or silently distribute a different build as
the same source/version. Prepare a new version/tag and repeat the required
build/regression checks; the already-published Emby artifact remains tied
to its original source.

# Titlovi.com Unified 2.0.0.0 — DRAFT release notes

**Not released.** These notes are prepared for review by Titlovi.com
maintainers and must not be presented as a published official version.

## Overview

This proposed first official Unified release replaces the original
Titlovi.com Jellyfin plugin implementation with a **single, shared source
tree for Jellyfin and Emby**, while retaining the existing official Jellyfin
plugin identity, catalogue URL and historical versions.

Two independent host builds are included:

| Host | Supported server | Downloadable release asset |
| --- | --- | --- |
| Jellyfin | 12.x / .NET 10 | `titlovi-com_2.0.0.0.zip` |
| Emby | 4.10.x | `Emby.Titlovi.dll` |

The Jellyfin ZIP contains precisely `Jellyfin.Titlovi.dll`, `meta.json`,
and `thumb.png`. Both compiled plugins include their shared subtitle
logic; no separate runtime Common/Core DLL is installed.

## Upgrading from the original Jellyfin plugin

- The **official Jellyfin plugin GUID stays unchanged**
  (`6e25df50-638e-4109-a50b-03c14fc93fdd`).
- The existing Jellyfin repository URL remains:
  `https://titlovi.github.io/titlovi-jellyfin/repository.json`.
- For an existing original plugin on Jellyfin 12, the new version can be
  installed in place from the catalogue and then activated on server restart.
- The plugin imports the **original stored Username and Password** from
  `Titlovi.Plugin.xml` if the new configuration has no configured login.
  It does not copy the old cached token (the format is incompatible), and
  leaves the original XML untouched.
- Existing newer credentials take precedence. A persisted migration marker
  prevents old credentials from being silently restored after a user clears
  them.

**Validated:** A Windows Jellyfin 12 user upgraded from a configured original
Matko plugin to the corrected 1.99.1.0 preview, confirmed automatic
credential transfer and downloaded a subtitle successfully. The final
2.0.0.0 release package is still pending its own acceptance checks.

**Important host upgrade note:** The new plugin targets Jellyfin **12**.
Before upgrading Jellyfin **10.11 → 12**, follow Jellyfin's official server
upgrade and backup instructions. Old Jellyfin plugin binaries are not
compatible with the new server; keep the original XML backup and install
the 12-compatible plugin only after the host upgrade. This release is not
a substitute for full server data migration or backup.

Existing Titlovi.com releases for Jellyfin 10.10/10.11 remain available
through the same catalogue for their supported server versions.

## Subtitle features

- Movie and episode search using IMDb IDs and title-based fallbacks.
- Optional alternative Serbian, Croatian and Bosnian searches. The actual
  language returned by Titlovi.com is preserved.
- Separate results for different Titlovi media IDs, with duplicates only
  merged for identical media IDs.
- Release-aware result **ranking** (never strict filtering).
- Season/whole-series archives, nested ZIP files and legacy episode filename
  patterns, with selection of appropriate episode subtitle entries.
- SRT, SUB, TXT, SSA and ASS support.
- Optional downloaded subtitle UTF-8 conversion and optional debug logging.

## Known limitation

On Emby Server 4.10.x, the downloaded external subtitle filename may use the
language originally requested in search rather than the actual alternate
language selected. The search result still displays the actual Titlovi.com
language. Jellyfin 12 preserves the returned language when saving.

## Service access and attribution

Requires the user's own Titlovi.com account with API access. Provider access
terms and subscription requirements are determined by Titlovi.com.

This project is distributed under **GPL-3.0** and derives from/integrates
earlier open-source Titlovi.com plugin work. Retain original authorship and
licensing acknowledgements; see `NOTICE.txt` and the preserved Git history.

## Pre-publication review checklist

- [ ] Receive official Titlovi.com approval for branding, published assets
      and distribution ownership.
- [ ] Resolve/explain upstream draft PR #5 (Jellyfin 12 / 1.3.0.0).
- [ ] Final source/version bump and release notes review.
- [ ] Regression suite and both host builds pass on final source.
- [ ] Confirm Jellyfin 12 fresh installation and upgrade behavior against
      the actual official catalogue.
- [ ] Verify old catalogue entries remain bit-for-bit equivalent in metadata,
      download URLs remain valid, and the new published ZIP checksum matches.
- [ ] Publish 2.0.0.0 assets and update catalogue **only after** the release
      asset URLs exist.
- [ ] Update official end-user installation and rollback instructions.

These notes are **draft-only** and do not authorize a release.

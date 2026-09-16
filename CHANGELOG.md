# Changelog

Notable changes to **Titlovi.com.Unified** are documented here.

## Unreleased

### Added

- Added cross-platform shared filename and archive-entry parsing for Windows (`\`) and Unix (`/`) paths.
- Added regression testing on both Windows and Ubuntu through GitHub Actions.
- Added Linux runtime smoke validation using supported Emby 4.10 and Jellyfin 12 server containers.
- Added explicitly triggered live Linux end-to-end validation for both hosts, covering authenticated Titlovi.com search, download and host-side subtitle saving.

### Changed

- Changed B/S/H alternative-language priority so Serbian is the first alternative for Croatian and Bosnian searches, while Croatian is the first alternative for Serbian searches.
- Reorganized Windows and Linux build infrastructure under `scripts/windows` and `scripts/linux`.
- Centralized generated build, test, reference and log state under `.build/`.
- Centralized installable artifacts under `dist/Emby` and `dist/Jellyfin`, using the same host-oriented distribution layout on Windows and Linux.
- Improved project documentation for installation, development, architecture and supported host behavior.

### Fixed

- Improved title-result validation so clear fuzzy-title collisions returned by Titlovi.com are rejected without treating release matching as a strict filter.
- Added support for Titlovi.com `AKA` title segments when validating original, translated or international titles.
- Improved validation of sibling search results by using alternative titles confirmed by an exact matching result.
- Added cleanup and MSBuild exclusions for generated directories left behind by older repository layouts, preventing stale build state from causing duplicate assembly metadata errors.

## 1.1.9 — 2026-09-15

Initial **Titlovi.com.Unified** release.

- First packaged release under the **Titlovi.com.Unified** name.
- Emby and Jellyfin plugins are both presented to users as **Titlovi.com (Unofficial)** while retaining host-specific assembly names and GUIDs.
- One shared `src/Common` implementation is compiled directly into each host plugin; no separate Core runtime DLL is required.
- Supports movie and episode search, IMDb/title fallback, optional B/S/H alternative search, actual-language preservation, archive/subtitle selection and UTF-8 conversion.
- Includes synchronization-aware release ranking using source, release group, FPS and cut/edition as the strongest signals, with resolution/codec used only as weaker matching hints.
- Includes offline regression coverage for legacy episode naming, season/full-series/nested ZIPs, B/S/H behavior, encoding, token/search behavior and release ranking.
- Emby build output is written to `src/Emby/output`.
- Jellyfin uses a single `src/Jellyfin/output` directory containing both the ready-to-copy plugin folder and the distribution ZIP; no separate `release` staging directory is created.
- Build scripts prefer stable installed SDKs, avoid preview SDK selection and can use local portable SDK fallbacks when required.

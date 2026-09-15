# Changelog

## Unreleased

- Added cross-platform shared filename parsing so Windows (`\`) and Unix (`/`) media/archive paths are handled consistently without rewriting host-native filesystem paths.
- Added regression coverage for separator-independent episode/archive parsing, Linux/Docker-style media paths and release ranking; the current offline baseline is 110 tests.
- Added GitHub Actions regression runs on both Windows and Ubuntu, with Windows builds for both host plugins.
- Added Linux runtime smoke validation using the official Emby 4.10.0.40 and Jellyfin 12 server containers.
- Added explicitly triggered live Linux end-to-end validation for both hosts, covering authenticated Titlovi.com movie/episode search, download, host-side subtitle saving and UTF-8 output.
- Confirmed Emby 4.10 supplies parent-series IMDb metadata through `SeriesProviderIds` in the tested Linux flow, while preserving the existing `SeriesName` fallback behavior.
- Updated installation and developer documentation for Windows/Linux validation and the current manually installed Jellyfin repository-metadata limitation.
- No plugin version bump is made for these development changes; versions are bumped only when preparing a release.

## 1.1.9 — Initial Titlovi.com.Unified release

- First packaged release under the **Titlovi.com.Unified** name.
- Emby and Jellyfin plugins are both presented to users as **Titlovi.com (Unofficial)** while retaining host-specific assembly names and GUIDs.
- One shared `src/Common` implementation is compiled directly into each host plugin; no separate Core runtime DLL is required.
- Supports movie and episode search, IMDb/title fallback, optional B/S/H alternative search, actual-language preservation, archive/subtitle selection and UTF-8 conversion.
- Includes synchronization-aware release ranking using source, release group, FPS and cut/edition as the strongest signals, with resolution/codec used only as weaker matching hints.
- Includes offline regression coverage for legacy episode naming, season/full-series/nested ZIPs, B/S/H behavior, encoding, token/search behavior and release ranking.
- Emby build output is written to `src/Emby/output`.
- Jellyfin now uses a single `src/Jellyfin/output` directory containing both the ready-to-copy plugin folder and the distribution ZIP; no separate `release` staging directory is created.
- Build scripts prefer stable installed SDKs, avoid preview SDK selection and can use local portable SDK fallbacks when required.

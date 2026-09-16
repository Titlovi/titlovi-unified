# Changelog

## Unreleased

- Added cross-platform shared filename parsing so Windows (`\`) and Unix (`/`) media/archive paths are handled consistently without rewriting host-native filesystem paths.
- Added regression coverage for separator-independent episode/archive parsing, Linux/Docker-style media paths and release ranking; the current offline baseline is 146 tests.
- Added GitHub Actions regression runs on both Windows and Ubuntu.
- Added Linux runtime smoke validation using the official Emby 4.10.0.40 and Jellyfin 12 server containers.
- Added explicitly triggered live Linux end-to-end validation for both hosts, covering authenticated Titlovi.com movie/episode search, download, host-side subtitle saving and UTF-8 output.
- Confirmed Emby 4.10 supplies parent-series IMDb metadata through `SeriesProviderIds` in the tested Linux flow, while preserving the existing `SeriesName` fallback behavior.
- Reorganized build infrastructure into `scripts/windows` and `scripts/linux`, with equivalent build/test entry points for both operating systems.
- Centralized generated compiler/intermediate/reference/log state under root `.build/`, keeping `bin`, `obj` and host reference files out of `src/`.
- Centralized installable build artifacts under root `dist/Emby` and `dist/Jellyfin`; Windows and Linux builds use the same host-oriented distribution layout rather than separate OS products.
- Added migration cleanup and MSBuild exclusions so ignored pre-refactor `bin`/`obj`/`output`/`embylibs` folders left behind by `git pull` cannot cause duplicate generated assembly attributes.
- Changed B/S/H alternative-language priority so Serbian is the first alternative for Croatian and Bosnian searches, while Croatian is the first alternative for Serbian searches.
- Filtered clear fuzzy-title collisions returned by the Titlovi.com search API for both episodes and movies, so requests such as `The Wire` no longer accept `Wire in the Blood` and `Rebelové AKA Rebels` no longer accepts `Rebel Ridge`; unrelated localized or alternative titles are preserved when there is no definite title collision.
- Treat Titlovi.com `AKA` title segments as equivalent alternative titles during result validation, preserving matches by either the original or translated/international title without weakening fuzzy-match protection; a standalone title equal to `AKA` remains a normal title.
- Linux runtime smoke now builds through the canonical Linux packaging scripts and loads the resulting `dist/` artifacts into the official server containers.
- Updated installation/developer documentation for Windows/Linux validation, the centralized build layout and the current manually installed Jellyfin repository-metadata limitation.
- No plugin version bump is made for these development changes; versions are bumped only when preparing a release.

## 1.1.9 — Initial Titlovi.com.Unified release

- First packaged release under the **Titlovi.com.Unified** name.
- Emby and Jellyfin plugins are both presented to users as **Titlovi.com (Unofficial)** while retaining host-specific assembly names and GUIDs.
- One shared `src/Common` implementation is compiled directly into each host plugin; no separate Core runtime DLL is required.
- Supports movie and episode search, IMDb/title fallback, optional B/S/H alternative search, actual-language preservation, archive/subtitle selection and UTF-8 conversion.
- Includes synchronization-aware release ranking using source, release group, FPS and cut/edition as the strongest signals, with resolution/codec used only as weaker matching hints.
- Includes offline regression coverage for legacy episode naming, season/full-series/nested ZIPs, B/S/H behavior, encoding, token/search behavior and release ranking.
- Emby build output is written to `src/Emby/output`.
- Jellyfin uses a single `src/Jellyfin/output` directory containing both the ready-to-copy plugin folder and the distribution ZIP; no separate `release` staging directory is created.
- Build scripts prefer stable installed SDKs, avoid preview SDK selection and can use local portable SDK fallbacks when required.

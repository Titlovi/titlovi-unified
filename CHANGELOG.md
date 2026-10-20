# Changelog

This document records **official Titlovi.com Unified releases** from version 2.0.0.0 onward.

## 2.0.0.0 — first official Emby release (2026-10-20)

- **Emby Server 4.10.x — published:** [Titlovi.com 2.0.0.0 for Emby](https://github.com/Titlovi/titlovi-emby/releases/tag/v2.0.0), distributed as a manually installed `Emby.Titlovi.dll` with `SHA256SUMS.txt`.
- **Jellyfin 12 / .NET 10 — not yet published:** its 2.0.0.0 distribution and catalogue entry are a separate later phase. Existing historical Jellyfin catalogue releases are unchanged.
- **Canonical source:** [Unified tag `v2.0.0`](https://github.com/Titlovi/titlovi-unified/tree/v2.0.0), commit `1c4c6dfd04fc0bf51b88a1c8e276896e5f0cab78`.
- **Emby DLL SHA-256:** `5980d14869686be78c709ae19fc493f590859d06e7423a943dc6d4b04af0a852`.

Both host plugins are built from the same source tree. Shared logic is compiled directly into each host DLL. The first Emby release was verified using the 166-test regression suite and host builds on Windows and Linux; live Emby acceptance of the release candidate was also reported.

Earlier **pre-official development history** is preserved in [the maintainer archive](docs/maintainers/archive/CHANGELOG-PRE-OFFICIAL.md) and the Git commit history. Those experimental releases are not part of the official Unified 2.0 release series.

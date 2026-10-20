# Changelog

This document records **official Titlovi.com Unified releases** from version 2.0.0.0 onward.

## 2.0.0.0 — first official Unified release for Emby and Jellyfin (2026-10-20)

- **Emby Server 4.10.x — published:** [Titlovi.com 2.0.0.0 for Emby](https://github.com/Titlovi/titlovi-emby/releases/tag/v2.0.0), distributed as a manually installed `Emby.Titlovi.dll` with `SHA256SUMS.txt`.
- **Jellyfin 12 / .NET 10 — published:** [Titlovi.com 2.0.0.0 for Jellyfin](https://github.com/Titlovi/titlovi-jellyfin/releases/tag/v2.0.0), distributed via the existing [plugin catalogue](https://titlovi.github.io/titlovi-jellyfin/repository.json) and as `titlovi-com_2.0.0.0.zip` with `SHA256SUMS.txt`. The original plugin GUID and all nine historical 1.x catalogue versions are preserved.
- **Canonical source:** [Unified tag `v2.0.0`](https://github.com/Titlovi/titlovi-unified/tree/v2.0.0), commit `1c4c6dfd04fc0bf51b88a1c8e276896e5f0cab78`.
- **Jellyfin ZIP SHA-256:** `ddd4e5fd7d91765a3405bd9c68c35f51a913aeff249f59b6337c7be6d8307880`.
- **Jellyfin catalogue developer credit:** `mihvel`.
- **Emby DLL SHA-256:** `5980d14869686be78c709ae19fc493f590859d06e7423a943dc6d4b04af0a852`.

Both host plugins are built from the same source tree. Shared logic is compiled directly into each host DLL. Both distributions passed the 166-test regression suite on Windows and Linux and were built for both hosts. Practical acceptance was completed on Emby 4.10.x and Jellyfin 12, including Jellyfin catalogue-based migration from the original plugin.

Earlier **pre-official development history** is preserved in [the maintainer archive](docs/maintainers/archive/CHANGELOG-PRE-OFFICIAL.md) and the Git commit history. Those experimental releases are not part of the official Unified 2.0 release series.

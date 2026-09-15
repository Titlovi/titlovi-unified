# Changelog

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

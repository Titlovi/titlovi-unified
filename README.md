# Titlovi.com.Unified

**Titlovi.com.Unified** is a source/build package for the unofficial **Titlovi.com (Unofficial)** subtitle provider for **Emby** and **Jellyfin**.

The repository builds two independent, self-contained host plugins from one shared subtitle engine:

- `Emby.Titlovi.dll` for Emby Server 4.10.x
- `Jellyfin.Titlovi.dll` for Jellyfin 12 / .NET 10

There is no separate runtime `Titlovi.Core.dll`. Both host projects compile the same `src/Common` source directly into their own assembly.

> This is an unofficial community integration. It is not published or endorsed by Titlovi.com, Emby or Jellyfin. A Titlovi.com account/API access is required for subtitle searches and downloads.

## Supported hosts

- **Emby Server 4.10.x** — builds and regression coverage are validated on Windows and Ubuntu; runtime loading and live search/download have also been validated with the official Emby 4.10.0.40 Linux container.
- **Jellyfin 12 / .NET 10** — builds and regression coverage are validated on Windows and Ubuntu; runtime loading and live search/download have also been validated with the official Jellyfin 12 Linux container.

The same host plugin artifacts are used on Windows and Linux. The operating system changes the build tooling, not the plugin product.

## Main features

- Movies and TV episodes.
- IMDb-based search with title fallback.
- Optional combined Serbian/Croatian/Bosnian search while preserving the actual language returned by Titlovi.com.
- Stable result ordering with the requested B/S/H language first.
- Synchronization-aware release ranking based primarily on source, release group, FPS and cut/edition, with weaker bonuses for resolution, codec, HDR and audio markers.
- Season packs, whole-series packs, nested ZIP archives and legacy episode filename conventions.
- Safe handling of multi-episode subtitle bundles.
- SRT, SUB, TXT, SSA and ASS subtitle extraction.
- Optional legacy-encoding detection and UTF-8 conversion.
- Token caching and diagnostic logging.
- Separator-independent shared filename parsing for Windows (`\`) and Unix (`/`) media/archive paths.
- Offline regression suite using the exact same Common source as both plugins.

## Build and test quick start

Windows entry points are under `scripts/windows`:

```text
scripts\windows\run-tests.cmd
scripts\windows\build-emby.cmd
scripts\windows\build-jellyfin.cmd
scripts\windows\build-all.cmd
```

Linux entry points are under `scripts/linux`:

```text
./scripts/linux/run-tests.sh
./scripts/linux/build-emby.sh
./scripts/linux/build-jellyfin.sh
./scripts/linux/build-all.sh
```

The Linux Emby build obtains the required `MediaBrowser.*` compile-time references from an installed Emby server when available, from `EMBY_REFERENCE_DIR` when explicitly supplied, or from the official `emby/embyserver:4.10.0.40` Docker image as a fallback.

For developers building from source, see **README-BUILD.txt**. For installation of pre-built releases, see **[INSTALL.md](INSTALL.md)**.

## Build layout

Generated build state is kept outside the source tree:

```text
.build/
  Emby/
    bin/
    obj/
    refs/
  Jellyfin/
    bin/
    obj/
  Tests/
    bin/
    obj/
    results/
  logs/
  nuget/
  tooling/
```

`.build/` is ignored by Git and may be deleted at any time.

User-facing build artifacts are always collected under the root `dist/` directory, regardless of whether the build ran on Windows or Linux:

```text
dist/
  Emby/
    Emby.Titlovi.dll

  Jellyfin/
    Titlovi.com (Unofficial)_1.1.9.0/
      Jellyfin.Titlovi.dll
      meta.json
      thumb.png
    Titlovi.com.Unified-1.1.9-Jellyfin-12.zip
```

`dist/` is also ignored by Git. It is the only directory a developer needs to inspect after a successful packaging build.

## First configuration

After installation and server restart:

1. Open the **Titlovi.com (Unofficial)** plugin configuration page.
2. Enter the Titlovi.com username and password used for API access.
3. Optionally enable Serbian/Croatian/Bosnian alternative searches.
4. Select accepted subtitle formats.
5. Optionally enable conversion of downloaded subtitles to UTF-8.
6. Save the configuration.

The API token is obtained automatically when the first subtitle search is performed and is cached by the plugin.

## Language handling

The shared engine preserves the actual language reported by Titlovi.com and uses canonical two-letter host-boundary codes such as `sr`, `hr`, `bs`, `en`, `mk` and `sl`.

When B/S/H alternative searching is enabled, the selected language is searched first and the other two are added as alternatives. Duplicate API results with the same Titlovi media ID are merged; distinct media IDs remain separate results.

### Known Emby filename limitation

Emby 4.10.x can save a Croatian or Bosnian alternative with the suffix of the language that initiated the search. For example, a search started as Serbian may save a Croatian subtitle as `.sr.srt` even though the provider returned Croatian metadata. This occurs after the plugin returns the actual language and is therefore left unchanged in the shared engine.

Jellyfin uses the language returned in `SubtitleResponse.Language`, so Croatian/Bosnian/Serbian downloads are expected to be saved with `.hr.srt`, `.bs.srt` and `.sr.srt` respectively.

## Cross-platform validation

The current offline suite contains **110 regression cases** and runs on both Windows and Ubuntu in GitHub Actions.

Linux runtime smoke validation runs the canonical `scripts/linux/build-all.sh`, then loads the actual artifacts from `dist/` into the official Emby 4.10 and Jellyfin 12 Linux containers. This verifies both the Linux build/package path and host loading without contacting Titlovi.com.

Separately triggered live E2E workflows validate authenticated Titlovi.com movie/episode search, download, host-side subtitle saving and UTF-8 output. They are intentionally not run on ordinary pushes.

## Project layout

```text
src/Common/       shared platform-neutral Titlovi.com engine
src/Emby/         Emby adapter, plugin bootstrap and configuration UI
src/Jellyfin/     Jellyfin adapter, plugin bootstrap and configuration UI
scripts/windows/  Windows build/test entry points and PowerShell implementation
scripts/linux/    Linux build/test entry points and Bash implementation
tests/            regression suite plus Linux smoke/E2E validation
.build/           generated intermediate state (ignored)
dist/             generated installable artifacts (ignored)
```

For the source boundary and host-adapter design, see **ARCHITECTURE.md**.

## Diagnostics

Each host has normal and optional detailed plugin logs. Detailed logging can be enabled from the plugin configuration page. Passwords and API tokens are not written to the diagnostic logs.

Detailed logs may contain media titles, release names and archive/subtitle filenames, so treat them as diagnostic data when sharing them.

## License

GPL-3.0. See **LICENSE** and **NOTICE.txt**.

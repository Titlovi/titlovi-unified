# Titlovi.com.Unified

**Titlovi.com.Unified** is a source/build package for the unofficial **Titlovi.com (Unofficial)** subtitle provider for **Emby** and **Jellyfin**.

The package builds two independent host plugins from one shared subtitle engine:

- `Emby.Titlovi.dll` for Emby
- `Jellyfin.Titlovi.dll` for Jellyfin

The plugins share the same search, download, archive-selection, language, encoding and release-matching logic. There is no separate runtime `Titlovi.Core.dll`; each host plugin is self-contained.

> This is an unofficial community integration. It is not published or endorsed by Titlovi.com, Emby or Jellyfin. A Titlovi.com account/API access is required for subtitle searches and downloads.

## Supported hosts

- **Emby:** Emby Server 4.10.x. The Windows build script uses installed Emby reference assemblies when available and falls back to the official Emby 4.10.0.40 package for compilation references. Runtime loading and authenticated search/download have also been validated in the official Emby 4.10.0.40 Linux container.
- **Jellyfin:** Jellyfin 12.0 / .NET 10. Builds are validated on Windows and Ubuntu, and runtime loading plus authenticated search/download have been validated in the official Jellyfin 12 Linux container.

The same host plugin binaries are intended for the supported server version regardless of whether the server itself runs on Windows or Linux.

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
- Offline regression suite using the exact same Common source as both plugins.
- Separator-independent shared filename parsing for Windows (`\`) and Unix (`/`) media/archive paths.

## Quick start

The provided convenience build scripts are for Windows:

```text
RUN-TESTS.cmd
BUILD-EMBY.cmd
BUILD-JELLYFIN.cmd
```

Or build both host plugins in sequence:

```text
BUILD-ALL.cmd
```

The regression suite can also be run directly on any development host with a suitable .NET 10 SDK:

```text
dotnet test tests/Titlovi.Tests/Titlovi.Tests.csproj -c Release
```

The build scripts do not install or modify Emby or Jellyfin. They only compile and package the plugin files.

For installation of pre-built releases, see **[INSTALL.md](INSTALL.md)**.

For developers building from source, see **README-BUILD.txt**.

## Build output

### Emby

```text
src\Emby\output\
  Emby.Titlovi.dll
  build.log
```

### Jellyfin

```text
src\Jellyfin\output\
  Titlovi.com (Unofficial)_1.1.9.0\
    Jellyfin.Titlovi.dll
    meta.json
    thumb.png
  Titlovi.com.Unified-1.1.9-Jellyfin-12.zip
  build.log
```

The Jellyfin folder is ready to copy into Jellyfin's plugins directory. The ZIP contains the same three runtime files and is provided as a convenient distribution package.

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

The offline regression suite runs on both Windows and Ubuntu in GitHub Actions. Linux runtime smoke tests build and load both host plugins in the official Emby 4.10 and Jellyfin 12 server containers.

In addition, explicitly triggered live end-to-end workflows have validated authenticated Titlovi.com movie and episode searches, downloads, host-side subtitle saving and UTF-8 output on both supported Linux hosts. These live workflows are intentionally not run on ordinary pushes.

## Diagnostics

Each host has normal and optional detailed plugin logs. Detailed logging can be enabled from the plugin configuration page. Passwords and API tokens are not written to the diagnostic logs.

Detailed logs may contain media titles, release names and archive/subtitle filenames, so treat them as diagnostic data when sharing them.

## Project layout

```text
src/Common/      shared platform-neutral Titlovi.com engine
src/Emby/        Emby adapter, plugin bootstrap, UI and build script
src/Jellyfin/    Jellyfin adapter, plugin bootstrap, UI and build script
tests/            offline regression suite, smoke/E2E validation and corpus notes
```

For a technical explanation of the source boundary and host adapters, see **ARCHITECTURE.md**.

## Tests

`RUN-TESTS.cmd` runs the offline regression suite on Windows. The same suite can be run directly with `dotnet test` on Windows or Linux. It does not contact Titlovi.com. Search/download responses are supplied by a fake API client, while subtitle and ZIP fixtures are created as real byte streams in memory and passed through the same archive/selection/encoding code used by the plugins.

The suite currently contains **110 regression cases** covering B/S/H handling, episode filename parsing, Windows/Unix path handling, season/full-series/nested ZIP selection, encoding conversion, search fallback, token behavior and release ranking.

See `tests/README.md` and `tests/TEST-CORPUS.md` for details.

## License

GPL-3.0. See **LICENSE** and **NOTICE.txt**.

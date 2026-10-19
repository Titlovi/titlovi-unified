# Titlovi.com Unified — Emby and Jellyfin source

This is the **canonical development and source-code repository** for the Titlovi.com plugins for **Emby** and **Jellyfin**, hosted by the [Titlovi GitHub organization](https://github.com/Titlovi).

The checked-in development builds still use the display name **Titlovi.com (Unofficial)**. Their official branding and version will be finalized only when the first Unified release is approved. Importing this source code did **not** publish a new plugin version.

The project provides two independent host plugins built from the same shared subtitle engine:

- **Emby Server 4.10.x** — `Emby.Titlovi.dll`
- **Jellyfin 12** — `Jellyfin.Titlovi.dll`

A **Titlovi.com account** is required.

> **Source migration completed on 19 October 2026.** Official Unified version 2.0.0.0 has **not** been released. This repository is the source of truth for future development, not a download location for production plugins. Jellyfin and Emby are independent projects.

## Distribution and installation status

The project uses one source repository and two host-specific distribution repositories:

- **[Titlovi/titlovi-jellyfin](https://github.com/Titlovi/titlovi-jellyfin)** — existing Jellyfin releases and the [unchanged official catalogue](https://titlovi.github.io/titlovi-jellyfin/repository.json). All historical Matko 1.x releases remain available for their supported Jellyfin versions. The first Jellyfin 12 Unified release will be published here **after** final approval.
- **[Titlovi/titlovi-emby](https://github.com/Titlovi/titlovi-emby)** — destination for future Emby 4.10.x GitHub Releases containing the manually installed `Emby.Titlovi.dll`. There is **no** third-party Emby plugin catalogue or repository installation in the supported workflow.

**No official Unified 2.0.0.0 binaries are available yet.** Do not treat the candidate-build artifacts or temporary test catalogue as production releases.

For instructions on building and testing this source, see **[README-BUILD.txt](README-BUILD.txt)**. For manual installation of development builds, see **[INSTALL.md](INSTALL.md)**. Proposed end-user upgrade steps for the future official release are in **[OFFICIAL-INSTALL-DRAFT.md](OFFICIAL-INSTALL-DRAFT.md)**.

The same host-specific plugin build is used on supported Windows and Linux installations.

## Features

- Subtitle search and download for movies and TV episodes.
- IMDb-based search with title fallback when needed.
- Optional combined Serbian, Croatian and Bosnian search.
- Release-aware ranking to help place subtitles matching the local video release near the top of the results.
- Season packs, whole-series packs and nested ZIP archives.
- Support for legacy episode filename conventions.
- Safe handling of multi-episode subtitle bundles.
- SRT, SUB, TXT, SSA and ASS subtitles.
- Optional legacy character-encoding detection and conversion to UTF-8.
- Optional detailed diagnostic logging.

## Supported hosts

### Emby

**Emby Server 4.10.x**

The plugin is distributed as:

```text
Emby.Titlovi.dll
```

The same DLL is used on supported Windows and Linux installations.

### Jellyfin

**Jellyfin 12**

The Jellyfin distribution contains:

```text
Jellyfin.Titlovi.dll
meta.json
thumb.png
```

The same package is used on supported Windows and Linux installations.

## Configuration

After installing the plugin and restarting the server:

1. Open the **Titlovi.com (Unofficial)** plugin configuration page.
2. Enter your Titlovi.com username and password.
3. Select the subtitle formats you want to accept.
4. Optionally enable alternative Serbian/Croatian/Bosnian searches.
5. Optionally enable conversion of downloaded subtitles to UTF-8.
6. Save the configuration.

Authentication with Titlovi.com is handled automatically by the plugin.

## Serbian, Croatian and Bosnian searches

When alternative B/S/H searching is enabled, a search for Serbian, Croatian or Bosnian subtitles can also include results in the other two languages.

The search results identify the actual language reported by Titlovi.com, allowing users to distinguish Serbian, Croatian and Bosnian alternatives before downloading them.

Only results referring to the same Titlovi.com media ID are deduplicated. Different media IDs remain separate search results.

### Jellyfin

Jellyfin saves a downloaded subtitle using the actual language returned by the plugin.

For example, a Croatian subtitle found during a Serbian search is saved as Croatian.

### Emby 4.10.x

Emby uses the language originally selected for the search when naming a downloaded external subtitle.

For example, if a Serbian search also includes Croatian alternatives, a Croatian subtitle selected from those results may be saved with a `.sr.srt` suffix.

The actual language is still shown in the search results so that the user can identify the subtitle before downloading it.

## Release matching

Release information is used to rank subtitle results, not to exclude them.

The strongest matching signals include:

- source
- release group
- FPS
- cut or edition

Resolution, codec, HDR and audio markers may provide additional ranking signals.

A subtitle therefore remains available even when its release name does not exactly match the local video filename.

## Development

The repository uses one shared platform-neutral source tree for both host plugins:

```text
src/Common/       shared Titlovi.com engine
src/Emby/         Emby integration
src/Jellyfin/     Jellyfin integration
tests/            regression and runtime validation
scripts/windows/  Windows build/test tools
scripts/linux/    Linux build/test tools
```

There is no separate runtime Common/Core assembly. The shared `src/Common` source is compiled directly into each host plugin, so both plugins remain self-contained.

For build instructions, see **[README-BUILD.txt](README-BUILD.txt)**.

For design details and the Common/host boundary, see **[ARCHITECTURE.md](ARCHITECTURE.md)**.

## Testing

The shared subtitle engine is covered by an offline regression suite using the same `src/Common` source compiled into both plugins.

Automated validation includes:

- regression tests on Windows and Ubuntu;
- Linux runtime smoke testing with the generated Emby and Jellyfin packages;
- explicitly triggered authenticated end-to-end tests against Titlovi.com.

Live Titlovi.com tests are not run on normal pushes.

## Diagnostics

Optional detailed logging can be enabled from the plugin configuration page.

Passwords and API tokens are not written to diagnostic logs.

Detailed logs may contain media titles, release names and subtitle or archive filenames, so they should still be treated as diagnostic data when shared publicly.

## Documentation

- **[INSTALL.md](INSTALL.md)** — installing, upgrading and removing pre-built releases
- **[README-BUILD.txt](README-BUILD.txt)** — building and testing from source
- **[ARCHITECTURE.md](ARCHITECTURE.md)** — project architecture and host integration
- **[CHANGELOG.md](CHANGELOG.md)** — release history
- **[OFFICIAL-RELEASE-HANDOFF.md](OFFICIAL-RELEASE-HANDOFF.md)** — migration status and remaining official release gates

## License

Licensed under **GPL-3.0**.

See **[LICENSE](LICENSE)** and **[NOTICE.txt](NOTICE.txt)**.

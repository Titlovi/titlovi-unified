# Titlovi.com for Jellyfin and Emby

**Documentation:** **English** · [Hrvatski](docs/hr/README.md) · [Српски](docs/sr-Cyrl/README.md)

**Official Titlovi.com subtitle plugins for Jellyfin and Emby**, developed from a single source codebase. Search for and download subtitles from [Titlovi.com](https://titlovi.com/) directly in your media server.

> **Release status:** The first official Unified release, **2.0.0.0**, is being prepared. Installation packages for Jellyfin 12 and Emby 4.10.x have **not yet been published**. This repository contains the source code, not downloadable plugin releases.

## Supported platforms

| Media server | Supported version | Installation and distribution |
| --- | --- | --- |
| **Jellyfin** | **12.x** (.NET 10) | Official plugin catalogue and release packages at [Titlovi/titlovi-jellyfin](https://github.com/Titlovi/titlovi-jellyfin). The existing catalogue URL will be retained. |
| **Emby Server** | **4.10.x** | Manual installation of `Emby.Titlovi.dll` from future releases at [Titlovi/titlovi-emby](https://github.com/Titlovi/titlovi-emby). |

The above versions are the targets of the upcoming Unified release. **Do not attempt to install Unified 2.0 until its official packages are published.** Existing releases in the Jellyfin catalogue remain available for their respective supported server versions.

## Features

The Unified implementation provides:

- **Movies and television series:** subtitle search and download through the media server.
- **Flexible search:** IMDb-based matching and title fallback when needed.
- **Serbian, Croatian and Bosnian subtitles:** optional searches across all three languages, with each result identified by the language reported by Titlovi.com.
- **Release-aware ranking:** preferred results can be ranked using release details without hiding other valid results.
- **Subtitle archives:** season and complete-series packages, nested ZIP archives, multi-episode files and legacy episode filename patterns.
- **Subtitle formats:** SRT, SUB, TXT, SSA and ASS, with optional conversion to UTF-8.
- **Diagnostics:** optional detailed logging to help investigate search or download problems.

Distinct subtitles retain their separate identities: results with different Titlovi.com media IDs are not merged merely because their names or release details are similar.

### Known Emby limitation

Emby Server 4.10.x may save an alternative-language subtitle with a filename suffix corresponding to the language originally requested in the search. For example, a Croatian subtitle found while searching for Serbian may be saved with an `.sr.srt` suffix. The search results still identify the actual subtitle language reported by Titlovi.com.

## Requirements

- A compatible Jellyfin or Emby Server version.
- A **Titlovi.com account with API access**. Account eligibility and access conditions are determined by Titlovi.com.

[Installation](docs/en/INSTALL.md) explains the planned post-release procedure and [the user guide](docs/en/USER-GUIDE.md) describes the configuration and search features. **No official 2.0 package is available yet.**

## Source code and development

This is the **canonical source repository** for both plugins. Each plugin is built independently from shared logic; no additional Common/Core DLL is required.

```text
src/Common/       Shared subtitle search and download engine
src/Jellyfin/     Jellyfin-specific integration
src/Emby/         Emby-specific integration
tests/            Regression and integration tests
scripts/          Build, packaging and test scripts
```

- [Installation](docs/en/INSTALL.md)
- [User guide](docs/en/USER-GUIDE.md)
- [Build and test instructions](docs/en/BUILD.md)
- [Architecture and design](docs/development/ARCHITECTURE.md)
- [Development changelog](CHANGELOG.md)
- [Report an issue](https://github.com/Titlovi/titlovi-unified/issues)

Automated tests and builds run on Windows and Linux. Live end-to-end tests against Titlovi.com are opt-in and require separately configured GitHub Actions credentials; they are not run on routine pushes.

**Security:** Never share your Titlovi.com password, API tokens, configuration files containing credentials or unredacted sensitive logs in public issues.

## License and acknowledgements

This project is licensed under the **GNU General Public License v3.0**. See [LICENSE](LICENSE) and [NOTICE.txt](NOTICE.txt).

The original Titlovi.com Jellyfin plugin and its historical releases, developed by Matko, remain preserved in the [Jellyfin repository](https://github.com/Titlovi/titlovi-jellyfin).

Emby and Jellyfin are independent projects and are not responsible for maintaining this plugin.

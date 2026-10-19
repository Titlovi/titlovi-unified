# Titlovi.com — subtitles for Jellyfin and Emby

Subtitle-provider plugins that let Jellyfin and Emby search for and download subtitles from **[Titlovi.com](https://titlovi.com/)**. Both integrations are developed from one shared source tree and distributed as separate, self-contained host plugins.

This is the **official Titlovi GitHub organization's source repository** for the Unified implementation. It is not a plugin catalogue or a release download repository.

> **Release status — 19 October 2026:** The Unified source code has been migrated here, but **Unified 2.0.0.0 has not been released**. The checked-in development builds still identify themselves as **Titlovi.com (Unofficial)** and have version **1.1.9**. Do not mistake a development build or test candidate for an official 2.0 release.

## Downloads and supported servers

| Server | Current availability | Where to get it |
| --- | --- | --- |
| **Jellyfin 10.10 / 10.11** | Previously published **1.x** plugin releases; choose a release compatible with your Jellyfin server ABI. | [Titlovi Jellyfin releases](https://github.com/Titlovi/titlovi-jellyfin/releases) and [official plugin catalogue](https://titlovi.github.io/titlovi-jellyfin/repository.json) |
| **Jellyfin 12 / .NET 10** | Unified implementation in this source repository; official **2.0.0.0** package **not yet published**. | The [Jellyfin distribution repository](https://github.com/Titlovi/titlovi-jellyfin) will publish the approved ZIP. |
| **Emby Server 4.10.x** | Unified implementation in this source repository; official release **not yet published**. | The [Emby distribution repository](https://github.com/Titlovi/titlovi-emby) will publish the approved DLL for manual installation. |

**Do not install an older Jellyfin 1.x package on Jellyfin 12.** The compatibility requirements for the host and plugin version must match. The existing Jellyfin catalogue and historical releases remain unchanged while Unified 2.0 is being prepared.

### Distribution model

- **[Titlovi/titlovi-unified](https://github.com/Titlovi/titlovi-unified)** (this repository): canonical source, issues, development, tests and coordinated release preparation.
- **[Titlovi/titlovi-jellyfin](https://github.com/Titlovi/titlovi-jellyfin)**: Jellyfin ZIP packages, historical 1.x releases and the existing plugin-catalogue URL.
- **[Titlovi/titlovi-emby](https://github.com/Titlovi/titlovi-emby)**: Emby DLL releases and manual-installation instructions. **There is no third-party Emby plugin feed or automatic repository-based installation** in the supported distribution model.

The planned Jellyfin distribution ZIP contains exactly `Jellyfin.Titlovi.dll`, `meta.json` and `thumb.png`. Emby is distributed as `Emby.Titlovi.dll`. Neither plugin requires a separate Common/Core runtime DLL.

## Requirements

- A **Titlovi.com account with API access**. Account eligibility and access conditions are determined by Titlovi.com.
- A compatible Jellyfin or Emby server version as shown above.

The plugin uses the credentials configured in the media server to authenticate with Titlovi.com. Do not put credentials in GitHub issues or public log attachments.

## What the Unified implementation provides

- Subtitle search and download for **movies and TV episodes**.
- **IMDb-based search**, with title search used as a fallback where needed.
- Optional searching across **Serbian, Croatian and Bosnian** when one of these languages is requested.
- Search results that retain the **actual subtitle language reported by Titlovi.com**.
- **Release-aware ranking** based on signals such as release group, source, frame rate and edition; release matching does **not** remove otherwise valid results.
- Handling of **season packs, full-series packs, nested ZIP archives and multi-episode subtitle bundles**, including legacy episode naming conventions.
- Support for **SRT, SUB, TXT, SSA and ASS** subtitles, with optional conversion of downloaded subtitles to UTF-8.
- Optional detailed diagnostics for troubleshooting.

### Languages and distinct subtitle uploads

When alternative Serbian/Croatian/Bosnian searching is enabled, the plugin can display results in the other two languages. Results are labelled with their actual language as reported by Titlovi.com. Different **Titlovi.com media IDs remain separate choices**, even if the subtitles have similar titles or release names; only repeated results with an identical media ID may be deduplicated.

**Known Emby 4.10.x limitation:** After an alternative-language search, Emby may name the saved external subtitle file using the originally requested language, even if a different actual language is shown in the search results. For example, a Croatian result found during a Serbian search may be saved with an `.sr.srt` suffix. The plugin preserves the correct language in its search results; this Emby host limitation is not worked around by mislabelling subtitles.

## Installing and configuring

Use the **existing Jellyfin catalogue** for compatible, previously published Jellyfin versions. Installation and upgrade instructions for **Unified 2.0.0.0** will become effective **only when that release has been approved and published**.

If you are building from source or evaluating a development build, see the [development-build installation guide](INSTALL.md). The [draft Unified 2.0 installation and upgrade guide](OFFICIAL-INSTALL-DRAFT.md) is available for review but is **not** a published-release installation procedure.

Once a compatible plugin is installed, configure the Titlovi.com username and password in the server's plugin settings. You can then choose accepted subtitle formats and optionally enable B/S/H alternative-language searches, UTF-8 conversion and diagnostic logging.

The planned official Jellyfin successor keeps the **original Jellyfin plugin ID** and the **existing catalogue URL**. Credential migration from the original plugin has been tested against a Unified preview; the final official package still requires acceptance testing before release.

## Development and validation

One source tree builds two host-specific assemblies:

```text
src/Common/       Shared subtitle engine and platform-neutral models
src/Jellyfin/     Jellyfin 12 integration
src/Emby/         Emby 4.10.x integration
tests/            Offline regression, runtime smoke and opt-in E2E tests
scripts/windows/  Windows build and test scripts
scripts/linux/    Linux build and test scripts
```

The shared `src/Common` C# files are compiled **directly into each host plugin**; there is no separate runtime Common/Core assembly.

- [Build and developer guide](README-BUILD.txt) — requirements, build commands, outputs and troubleshooting.
- [Architecture](ARCHITECTURE.md) — separation of shared and host-specific responsibilities.
- [Changelog](CHANGELOG.md) — project change history.
- [Official release preparation](OFFICIAL-RELEASE-HANDOFF.md) — migration record and remaining 2.0 publication checks.

CI runs the offline regression suite and builds both host plugins on standard Windows and Ubuntu runners. Linux runtime smoke tests check whether the resulting assemblies load into the supported host servers. **Live authenticated Titlovi.com end-to-end tests are opt-in**, not part of ordinary pushes.

## Feedback and security

Please use [GitHub Issues](https://github.com/Titlovi/titlovi-unified/issues) for reproducible bug reports and improvement proposals. Include the **server version**, **plugin version**, relevant title/episode details and the steps needed to reproduce the problem.

**Never publish your Titlovi.com password, API tokens, plugin configuration XML or unredacted sensitive logs.** Diagnostic logs may also contain filenames and media titles.

## License and acknowledgements

Licensed under the **GNU General Public License, version 3**. See [LICENSE](LICENSE) and [NOTICE.txt](NOTICE.txt).

The project acknowledges **Matko's original open-source Jellyfin plugin and API-client work**, whose repository and historical releases remain available at [Titlovi/titlovi-jellyfin](https://github.com/Titlovi/titlovi-jellyfin).

Emby and Jellyfin are separate projects; their names and trademarks belong to their respective owners.

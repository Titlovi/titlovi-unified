# Titlovi.com user guide

[English](USER-GUIDE.md) · [Hrvatski](../hr/USER-GUIDE.md) · [Српски](../sr-Cyrl/USER-GUIDE.md)

The Unified plugin lets your Jellyfin or Emby server search for and download subtitles from Titlovi.com. **Unified 2.0.0.0 is published for both Jellyfin 12 and Emby 4.10.x**; see [Installation](INSTALL.md) for the host-specific downloads and upgrade procedures.

## Requirements and setup

Use a compatible server and a **Titlovi.com account**. After installing a compatible plugin and restarting the server, open its settings.

| Setting | Purpose |
| --- | --- |
| Username / Password | Authenticate with Titlovi.com. |
| Accepted formats | Enable or disable SRT, SUB, TXT, SSA and ASS. |
| Serbian/Croatian/Bosnian alternatives | Include results in the other two languages when searching in one of these languages. Off by default. |
| Convert downloaded subtitles to UTF-8 | Convert older character encodings when needed. Off by default. |
| Detailed diagnostic logging | Additional information for troubleshooting. Off by default. |

All supported subtitle formats are accepted by default. Save configuration changes before searching.

## Search and download

1. Open a movie or an episode in the media server.
2. Use the server's built-in **subtitle search/download** interface and select a language.
3. Review the results, including subtitle language and release information.
4. Choose a suitable subtitle and download it through the server.

The provider can search using an IMDb identifier and fall back to a title search. For episodes, it uses series and season/episode metadata when available.

### Language alternatives

When enabled, a Serbian search can also include Croatian and Bosnian results, and vice versa. The **actual language supplied by Titlovi.com** is preserved: an alternative Croatian result remains Croatian, even when discovered during a Serbian search.

Different subtitles with **different Titlovi.com media IDs remain separate results**; repeated appearances of the *same* ID can be deduplicated.

### Release matching and sorting

Source, release group, frame rate and named cut/edition are important signals used to rank likely matches; codec, resolution and related details may contribute weaker signals. **Release matching is for ranking only**: subtitles that do not match the video release exactly remain selectable.

### Archives and episodes

The provider handles individual files, season packs, complete-series packs, nested ZIP archives, legacy episode file names and several multi-episode patterns. It attempts to select the subtitle for the requested episode instead of accepting the first archive entry.

## Formats and text encoding

Formats supported: **SRT, SUB, TXT, SSA, ASS**. The configuration allows restricting the accepted formats. If regional characters appear incorrectly, consider the optional **UTF-8 conversion** setting.

## Jellyfin and Emby differences

Jellyfin 12 saves the subtitle using the language provided by the plugin.

In **Emby Server 4.10.x**, an alternative-language subtitle may be saved with a filename suffix for the language *originally requested* in the search. Example: a Croatian subtitle found during a Serbian search may be saved as `.sr.srt`, although the result correctly identifies it as Croatian. This is an accepted host limitation.

## Troubleshooting and privacy

| Problem | What to check |
| --- | --- |
| Plugin does not load | Server version, host-specific package, complete Jellyfin ZIP contents and restart. |
| Authentication fails | Titlovi.com username and password. |
| Search finds nothing | Media metadata, selected language, enabled subtitle formats and account access. |
| Incorrect episode subtitle | Season and episode metadata, archive naming and alternative results. |
| Character-encoding problems | Optional UTF-8 conversion. |
| Emby alternative-language suffix | Known Emby 4.10 filename limitation above. |

Diagnostic logging is optional. Logs can contain media titles, release names and filenames. **Never share passwords, API tokens or configuration XML** in public GitHub issues.

Report reproducible problems via [GitHub Issues](https://github.com/Titlovi/titlovi-unified/issues), providing the host version, plugin version and non-sensitive steps to reproduce the problem.

# Titlovi.com 2.0.0.0 for Emby Server — RELEASE NOTES DRAFT

**NOT YET RELEASED.** This English draft is for the first official Emby
GitHub Release, **after** approval, immutable Unified source tagging, live
acceptance and final download/checksum validation. Do not present it as a
currently downloadable release.

## First official Unified release for Emby

Titlovi.com 2.0.0.0 brings the Titlovi.com subtitle service to
**Emby Server 4.10.x** from the shared
[Unified source project](https://github.com/Titlovi/titlovi-unified).
This is the **first public version of this official Unified implementation**;
earlier internal development versions are not part of this release history.

The plugin supports searching for subtitles by film and series episode,
downloading matching subtitle files and selecting entries from episode/season
archives. It offers optional Serbian, Croatian and Bosnian alternative
searches, optional subtitle conversion to UTF-8, and diagnostic logging.

Search results retain **distinct Titlovi media IDs**. Release-name matching
ranks results but never excludes otherwise valid subtitles. Returned subtitle
language follows the actual language reported by Titlovi.com.

## Compatibility and package

- **Host:** Emby Server 4.10.x (other versions not asserted compatible).
- **Package:** `Emby.Titlovi.dll` (single Emby host plugin DLL).
- **Installation:** manual DLL download/copy into Emby's active plugins
  directory, followed by a server restart; there is no Emby catalogue or
  automatic update feed.
- **Account:** your own Titlovi.com account with required API access.
- **Source:** the exact approved and tagged
  `Titlovi/titlovi-unified` revision will be linked in the final release.
- **Integrity:** a SHA-256 checksum for the published DLL will accompany
  the release. Never install a CI `NOT-FOR-RELEASE` preview as a public build.

## Installation and upgrade

Before replacing an existing Titlovi plugin, **back up the existing DLL and
plugin configuration outside the Emby plugins directory**, then stop Emby.
Copy the new `Emby.Titlovi.dll` into the **active** Emby plugins directory,
start Emby and confirm that *Titlovi.com* appears under installed plugins.
Review the saved account settings and test a real subtitle search/download.

See the [Emby installation guide](https://github.com/Titlovi/titlovi-emby/blob/main/INSTALL.md)
for full instructions, troubleshooting and rollback guidance.

## Known Emby 4.10 host limitation

For subtitles found through Serbian/Croatian/Bosnian alternative-language
search, Emby 4.10.x may save the file with the suffix of the originally
requested language, even though the plugin reports the correct language
returned by Titlovi.com. This host limitation is accepted; it does not
change the returned subtitle's actual language and is not worked around in
shared logic.

## Project and acknowledgements

The code is distributed under GPL-3.0. This Unified work builds upon the
previous open-source Titlovi.com Jellyfin plugin by Matko, whose historical
releases and attribution are preserved in the
[Jellyfin repository](https://github.com/Titlovi/titlovi-jellyfin).

**Jellyfin 2.0 is a separate, later release.** The Emby release does not
install a Jellyfin plugin, change the existing Jellyfin catalogue or imply
availability of a Jellyfin 12 package.

## Release manager completion fields — leave pending until approved

- Approved Unified source commit/tag: **PENDING**.
- Final published `Emby.Titlovi.dll` SHA-256: **PENDING**.
- Explicit public-release authorization: **PENDING**.
- Final Emby rollback and public-download acceptance: **PENDING**.


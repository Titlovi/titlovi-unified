# Installation

This guide is for users who want to install the pre-built **Titlovi.com (Unofficial)** plugin from a GitHub Release.

A Titlovi.com account is required.

The same pre-built host plugin is used for the supported server version on Windows and Linux. Linux compatibility has been validated against the official Emby 4.10.0.40 and Jellyfin 12 server containers.

## Emby 4.10.x

Download:

`Emby.Titlovi.dll`

Then:

1. Stop Emby Server.
2. Copy `Emby.Titlovi.dll` into the Emby plugins directory used by your installation.
3. If an older `Emby.Titlovi.dll` already exists, replace it.
4. Start Emby Server.
5. Open the server dashboard and go to the plugin settings.
6. Open **Titlovi.com (Unofficial)**.
7. Enter your Titlovi.com username and password.
8. Configure the optional subtitle language, format and encoding settings.
9. Save the configuration.

For Docker/container installations, copy or mount the DLL into the server's persistent plugins directory rather than into the container's ephemeral filesystem.

The plugin keeps a stable Emby plugin ID, so replacing the DLL should preserve the existing plugin configuration.

## Jellyfin 12

Download:

`Titlovi.com.Unified-<version>-Jellyfin-12.zip`

Then:

1. Stop Jellyfin.
2. Create a dedicated plugin folder inside Jellyfin's plugins directory, for example:

   `Titlovi.com (Unofficial)_<version>`

3. Extract the downloaded ZIP into that folder.
4. Make sure these three files are together in the same folder:

   - `Jellyfin.Titlovi.dll`
   - `meta.json`
   - `thumb.png`

5. Start Jellyfin.
6. Open the server dashboard and go to the plugin settings.
7. Open **Titlovi.com (Unofficial)**.
8. Enter your Titlovi.com username and password.
9. Configure the optional subtitle language, format and encoding settings.
10. Save the configuration.

For Docker/container installations, place the complete plugin folder inside the persistent Jellyfin plugins volume (commonly exposed inside the container under `/config/plugins`).

Do not copy only `Jellyfin.Titlovi.dll`; the Jellyfin package also requires `meta.json` and `thumb.png`.

### Manual-install repository metadata

The current Jellyfin distribution is installed manually from the release ZIP and is not yet published through a public Jellyfin plugin repository. Jellyfin 12 may therefore show a warning such as:

`An error occurred while getting the plugin details from the repository.`

It may also display **Developer: Unknown** and **Repository: Unknown** on the plugin details page. This does not affect plugin configuration, subtitle searches or downloads. Repository metadata and repository-based installation/update support can be added later when a public distribution repository is available.

## Upgrading

### Emby

Stop Emby, replace the existing `Emby.Titlovi.dll` with the newer version, and start Emby again.

### Jellyfin

Stop Jellyfin, replace the contents of the existing **Titlovi.com (Unofficial)** plugin folder with the files from the newer release ZIP, and start Jellyfin again.

## Uninstalling

Stop the media server and remove the corresponding plugin files or plugin folder from its plugins directory.

Start the server again after removal.

## First use

The plugin logs in to Titlovi.com automatically when a subtitle search requires API access.

If authentication fails, verify the Titlovi.com username and password in the plugin configuration page.

## Supported versions

- Emby Server 4.10.x — validated on Windows builds and the official Emby 4.10.0.40 Linux container
- Jellyfin 12.0 / .NET 10 — validated on Windows builds and the official Jellyfin 12 Linux container

This is an unofficial community plugin and is not published or endorsed by Titlovi.com, Emby or Jellyfin.

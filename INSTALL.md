# Installation

This guide explains how to install the pre-built **Titlovi.com (Unofficial)** plugin for Emby and Jellyfin.

A **Titlovi.com account** is required.

Download the appropriate package from the [latest GitHub release](https://github.com/mihvel/Titlovi.com.Unified/releases/latest).

> This is an unofficial community plugin and is not published or endorsed by Titlovi.com, Emby or Jellyfin.

## Emby Server 4.10.x

Download:

```text
Emby.Titlovi.dll
```

Then:

1. Stop Emby Server.
2. Copy `Emby.Titlovi.dll` into the plugins directory used by your Emby installation.
3. If an older `Emby.Titlovi.dll` already exists, replace it.
4. Start Emby Server.
5. Open the server dashboard and verify that **Titlovi.com (Unofficial)** appears in the plugin settings.

### Common Emby plugin locations

The Emby `plugins` directory is located directly inside the Emby Server Data Folder.

Common locations include:

```text
Windows:
C:\Users\<user>\AppData\Roaming\Emby-Server\programdata\plugins

Linux:
/var/lib/emby/plugins
```

Other platforms, NAS installations and custom configurations may use a different data directory.

The active Emby Server Data Folder can be checked from the server dashboard using **Get Server Info**.

For Docker or other container installations, place the DLL in the persistent Emby plugins directory or volume rather than in the container's temporary filesystem.

## Jellyfin 12

Download:

```text
Titlovi.com.Unified-<version>-Jellyfin-12.zip
```

Then:

1. Stop Jellyfin.
2. Create a dedicated folder inside the Jellyfin plugins directory, for example:

   ```text
   Titlovi.com (Unofficial)_<version>
   ```

3. Extract the contents of the downloaded ZIP into that folder.
4. Make sure these three files are together in the same folder:

   ```text
   Jellyfin.Titlovi.dll
   meta.json
   thumb.png
   ```

5. Start Jellyfin.
6. Open the server dashboard and verify that **Titlovi.com (Unofficial)** appears in the plugin settings.

### Common Jellyfin plugin locations

Common locations include:

```text
Windows installer / tray:
C:\ProgramData\Jellyfin\Server\plugins

Windows direct or portable installation:
C:\Users\<user>\AppData\Local\jellyfin\plugins

Linux packages:
/var/lib/jellyfin/plugins

Docker:
/config/plugins
```

The actual location may differ if Jellyfin was configured with a custom data directory.

For Docker or other container installations, place the complete plugin folder inside the persistent Jellyfin plugins volume.

Do not install only `Jellyfin.Titlovi.dll`. The Jellyfin plugin package also requires `meta.json` and `thumb.png`.

### Manual installation notice

The plugin is currently installed manually from the GitHub release ZIP and is not published through a public Jellyfin plugin repository.

Because of this, Jellyfin may display a message such as:

```text
An error occurred while getting the plugin details from the repository.
```

The plugin details page may also show **Developer: Unknown** or **Repository: Unknown**.

These messages do not prevent the plugin from being configured or used for subtitle searches and downloads.

## First-time configuration

After installing the plugin and restarting the server:

1. Open the **Titlovi.com (Unofficial)** configuration page.
2. Enter your Titlovi.com username and password.
3. Select the subtitle formats you want to accept.
4. Optionally enable alternative Serbian/Croatian/Bosnian searches.
5. Optionally enable conversion of downloaded subtitles to UTF-8.
6. Save the configuration.

Authentication with Titlovi.com is handled automatically when the plugin needs to access the service.

## Upgrading

### Emby

1. Stop Emby Server.
2. Replace the existing `Emby.Titlovi.dll` with the file from the newer release.
3. Start Emby Server.

The plugin uses a stable Emby plugin ID, so the existing configuration should be preserved when the DLL is replaced.

### Jellyfin

1. Stop Jellyfin.
2. Remove the existing **Titlovi.com (Unofficial)** plugin folder.
3. Create a new plugin folder for the new version.
4. Extract the complete contents of the newer release ZIP into it.
5. Start Jellyfin.

Make sure that only one version of **Titlovi.com (Unofficial)** is present in the Jellyfin plugins directory.

## Uninstalling

### Emby

Stop Emby Server, remove `Emby.Titlovi.dll` from the plugins directory, and start the server again.

### Jellyfin

Stop Jellyfin, remove the **Titlovi.com (Unofficial)** plugin folder, and start the server again.

## Troubleshooting

If the plugin does not appear after installation:

- verify that the correct package was downloaded for the server;
- verify that the plugin files are in the server's active plugins directory;
- make sure the server was restarted after installing the plugin;
- for Jellyfin, make sure `Jellyfin.Titlovi.dll`, `meta.json` and `thumb.png` are all present in the same plugin folder.

If subtitle searches fail because of authentication, verify the Titlovi.com username and password on the plugin configuration page.

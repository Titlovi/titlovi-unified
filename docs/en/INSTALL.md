# Installing Titlovi.com

[English](INSTALL.md) · [Hrvatski](../hr/INSTALL.md) · [Српски](../sr-Cyrl/INSTALL.md)

> **Release status:** [Emby 2.0.0.0](https://github.com/Titlovi/titlovi-emby/releases/tag/v2.0.0) is published for **Emby Server 4.10.x**. **Jellyfin 2.0.0.0 for Jellyfin 12 is not yet published**; its installation steps below describe the planned process.

A Titlovi.com account **with API access** is required. Titlovi.com sets account eligibility and access conditions.

## Jellyfin 12 (.NET 10)

Distribution: [Titlovi/titlovi-jellyfin](https://github.com/Titlovi/titlovi-jellyfin). The **existing** Jellyfin plugin-catalogue URL will remain:

```text
https://titlovi.github.io/titlovi-jellyfin/repository.json
```

**Once an approved Jellyfin 12 Unified release is published:**

1. Open the Jellyfin administration dashboard, then **Plugins → Repositories**.
2. Confirm that the existing Titlovi catalogue URL is configured; add it if necessary.
3. In the plugin catalogue, find **Titlovi.com** and install the release explicitly compatible with **Jellyfin 12**.
4. Restart Jellyfin and open the plugin configuration.
5. Enter your Titlovi.com username and password, save, then try a subtitle search and download.

The existing catalogue also contains historical releases for **older Jellyfin versions**. Choose only a version compatible with your installed server. Unified 2.0 is **not** intended for Jellyfin 10.10/10.11.

### Upgrading from the original Jellyfin plugin

The successor preserves the original **Jellyfin plugin GUID** and the existing catalogue URL: it is intended as an upgrade, not a separate competing installation.

Before upgrading, **back up the server's data/configuration**, including `plugins/configurations/Titlovi.Plugin.xml` if it exists. On Jellyfin 12, install the compatible official successor from the catalogue after publication and restart the server. Check the username on the configuration page and verify that a subtitle downloads.

On first use, the successor is designed to import the original plugin's username and password **only if the newer configuration has no credentials**. The original configuration file remains intact, and its cached API token is **not** reused. After upgrading, verify your account details in the plugin settings and enter them manually if necessary.

**Upgrading the Jellyfin server itself from 10.11 to 12 is separate from upgrading the plugin.** Back up the entire server and follow Jellyfin's server migration instructions. A plugin test does not guarantee a server database upgrade.

### Manual Jellyfin installation

Once the official ZIP has been published, a manual alternative is to stop the server and extract the **entire** approved package into its own directory within the active Jellyfin plugins folder:

```text
Jellyfin.Titlovi.dll
meta.json
thumb.png
```

Restart the server. Do not install only the DLL or leave duplicate conflicting copies. The catalogue method is preferred for normal installations and updates.

## Emby Server 4.10.x

Distribution: [Titlovi/titlovi-emby](https://github.com/Titlovi/titlovi-emby). **Emby is installed manually**; there is no third-party Emby plugin catalogue to add.

**For the published Emby plugin:**

1. Download `Emby.Titlovi.dll` from the [official Emby releases](https://github.com/Titlovi/titlovi-emby/releases).
2. Stop Emby Server and back up the existing plugin/configuration if upgrading.
3. Copy the DLL into your server's **actual** plugins directory, replacing only an older copy of the same plugin if present.
4. Start Emby Server, open the plugin settings and provide your Titlovi.com username and password.
5. Verify a subtitle search and download.

Typical plugins directories:

```text
Windows: C:\Users\<user>\AppData\Roaming\Emby-Server\programdata\plugins
Linux:   /var/lib/emby/plugins
```

These are **examples, not guaranteed paths**. Check the active Emby **Server Data Folder** from the dashboard (for example via **Get Server Info**); custom installations and containers differ. For Docker, use a persistent plugins volume.

**Known Emby 4.10 limitation:** The saved subtitle filename may use the language selected for the search, even if an alternative-language result is correctly identified as Croatian or Bosnian. The plugin does not mislabel the actual language to work around this host behavior.

## Configuration, troubleshooting and rollback

After installation, choose subtitle formats and optional Serbian/Croatian/Bosnian alternatives, UTF-8 conversion and diagnostic logging in the plugin configuration. See the [User guide](USER-GUIDE.md).

If the plugin does not load, confirm server version, host-specific package, installation directory and server restart. If searches fail, verify Titlovi.com API access, the account credentials and the desired subtitle language.

To roll back, restore a **server-compatible** previous plugin version and your configuration backup. Do **not** downgrade a Jellyfin server database as part of plugin rollback.

**Security:** Never publish passwords, API tokens or configuration XML in public support requests.

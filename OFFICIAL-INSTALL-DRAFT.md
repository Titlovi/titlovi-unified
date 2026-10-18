# Official Titlovi.com plugin installation guide — DRAFT

**PRE-RELEASE WORKING DOCUMENT.** This is written for the future official
Unified 2.0.0.0 release, which has **not been published**. Do not replace the
current end-user `INSTALL.md` or link to unavailable 2.0 downloads until
the official release and catalogue update are confirmed.

The proposed unified plugin supports **Jellyfin 12.x / .NET 10** and
**Emby Server 4.10.x**. Other server versions are not included in this release.

A Titlovi.com account with API access is required. Access terms and account
eligibility are determined by Titlovi.com.

## Jellyfin 12 — new installation

1. In Jellyfin Dashboard → Plugins → Repositories, ensure the **Titlovi**
   repository is present, or add this existing official URL:

   ```
   https://titlovi.github.io/titlovi-jellyfin/repository.json
   ```

2. After the official 2.0 release appears, open the Titlovi.com entry in
   Jellyfin Plugins/Catalogue and install the newest version explicitly
   compatible with **Jellyfin 12**.
3. **Restart Jellyfin** to load the DLL.
4. Open Titlovi.com Settings, enter your Titlovi.com username and password,
   choose formats and optional language/UTF-8 settings, then Save.
5. Search for a subtitle and download one to validate API access.

A manual alternative is to download the future official asset
`titlovi-com_2.0.0.0.zip`, create a versioned Titlovi.com folder inside
Jellyfin's active data-directory plugins folder, extract the **three** package
files together and restart. Use the dashboard/catalogue method when available,
because it preserves normal upgrade behavior.

## Jellyfin 12 — upgrading the original Matko plugin

The official plugin GUID is **unchanged**. This is a successor, not a second
Jellyfin plugin.

1. **Back up** the Jellyfin data/configuration directory, including
   `plugins/configurations/Titlovi.Plugin.xml`, before beginning.
2. On a Jellyfin **12** server, use the existing Titlovi catalogue to upgrade
   the installed original Titlovi.com plugin to official 2.0 once it appears.
   Do not add the now-retired temporary `mihvel/titlovi-jellyfin-test`
   catalogue to production servers.
3. Restart Jellyfin. Wait for the new plugin to become **Active**.
4. Open Titlovi.com Settings. When the original configuration file is
   readable, the plugin imports its Username and Password if the new
   configuration has no credentials. It **does not** reuse the original
   cached token, because the token formats differ.
5. Confirm Settings display the expected username, then verify one
   subtitle search/download.

The old XML `Titlovi.Plugin.xml` remains intact for backup and rollback.
The new XML is `Jellyfin.Titlovi.xml`. **Never share or publish either
file**, because they may contain a password or API token.

If the newer plugin was previously configured, its credentials take
precedence; intentionally cleared newer credentials are not silently
restored from old XML after the migration has been marked complete.

**Validated during preview:** Windows Jellyfin 12 retained the same plugin
identity during catalogue updates, subsequently imported original credentials
using corrected 1.99.1.0, and downloaded a subtitle. The official 2.0 package
still requires its final acceptance check before release.

## Jellyfin 10.11 users — upgrade the server first

Official Unified 2.0 **does not support Jellyfin 10.11 / .NET 9**. Older
compatible plugin releases remain listed in the same official catalogue.

If upgrading from Jellyfin 10.11 to 12, first follow Jellyfin's official
**server** backup and upgrade instructions. Back up the full server data and
configuration, especially original plugin XML. Remove incompatible older
third-party plugin binaries before starting Jellyfin 12, retaining the
original configuration backup.

After the server itself starts successfully on 12, install the compatible
official Titlovi.com 2.0 plugin and check credentials as above.

**Do not interpret the plugin compatibility test as confirmation that a
particular Jellyfin server/database upgrade will succeed.** The full server
upgrade is independent and must be backed up and tested.

## Emby Server 4.10.x

The unified source produces a separate, self-contained asset:

```
Emby.Titlovi.dll
```

1. Back up existing Emby plugin/configuration data if upgrading.
2. Stop Emby Server.
3. Place the approved official `Emby.Titlovi.dll` release asset in the
   active Emby Server plugin directory, replacing only the same plugin DLL
   when appropriate.
4. Start Emby, check that Titlovi.com appears, and configure the account.
5. Validate a subtitle search/download.

Do **not** install Jellyfin's ZIP or its DLL in Emby. Common locations and
custom plugin paths depend on the installation method.

### Known Emby 4.10.x limitation

An alternate Croatian or Bosnian subtitle discovered in a Serbian-language
search may be saved with the originally requested Serbian language suffix,
even though the search result identifies the actual returned language.
This known host limitation is accepted; the shared language logic does not
misrepresent the result to work around it.

## Common troubleshooting

If the plugin is installed but not **Active**, restart the server and
confirm that you used the package for your supported host/version. For
Jellyfin, verify the package's `Jellyfin.Titlovi.dll`, `meta.json` and
`thumb.png` are installed together.

If credentials were not imported, preserve backups and inspect only the
**presence and filenames** of `Titlovi.Plugin.xml` and
`Jellyfin.Titlovi.xml` before further investigation. Do not post
credentials or XML contents to support channels.

If searches fail, verify the account has Titlovi.com API access and use
the optional plugin diagnostic logs (without posting secrets).

## Rollback

Keep a full configuration/data backup and the previously installed plugin
version before migration. For a rollback, restore a server-compatible older
plugin binary and its matching saved configuration, and restart the host.
Do not roll Jellyfin 12 back to 10.11 merely to revert a plugin; server
database downgrades are not part of plugin rollback.

**This draft becomes end-user documentation only after the official
maintainer approves and publishes 2.0.0.0.**

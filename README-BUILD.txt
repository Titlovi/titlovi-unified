Titlovi.com.Unified 1.1.9 - Build and installation guide
==========================================================

This source/build package produces two independent plugins registered as:

  Titlovi.com (Unofficial)

Host assemblies:

  Emby.Titlovi.dll
  Jellyfin.Titlovi.dll

Both host projects compile the same src\Common source directly into their own
DLL. There is no Titlovi.Core.dll and no shared runtime dependency between the
Emby and Jellyfin plugins.

REQUIREMENTS
------------

- Windows with PowerShell 5.1 or newer.
- Internet access may be needed during the first build to download compilation
  references or a portable .NET SDK.
- You do not need Visual Studio.
- The scripts do not install, stop, start or modify Emby/Jellyfin.

The build scripts prefer a suitable stable installed .NET SDK. If none is
available, they can download a portable SDK into the package's local .build
folder. Preview/RC SDKs are deliberately not selected.

QUICK BUILD
-----------

From the package root:

  BUILD-EMBY.cmd
  BUILD-JELLYFIN.cmd

To build both in sequence:

  BUILD-ALL.cmd

Before distributing a changed build, run:

  RUN-TESTS.cmd

EMBY BUILD
----------

Run:

  BUILD-EMBY.cmd

The script first looks for MediaBrowser.* reference assemblies from an
installed/running Emby Server. If they are not found, it downloads the official
Emby 4.10.0.40 fallback server archive and extracts only the three assemblies
needed for compilation.

Final output:

  src\Emby\output\Emby.Titlovi.dll
  src\Emby\output\build.log

The output directory is cleaned at the start of each build, so it contains only
artifacts from the current run.

Manual Emby installation:

  1. Stop Emby Server.
  2. Back up/remove any previous Emby.Titlovi.dll if you are replacing a build.
  3. Copy src\Emby\output\Emby.Titlovi.dll to Emby's plugins directory.
  4. Start Emby Server.
  5. Open Titlovi.com (Unofficial) in the server plugin settings and configure
     your Titlovi.com credentials/options.

The Emby plugin GUID is intentionally stable so an upgrade can keep the
existing plugin configuration.

JELLYFIN BUILD
--------------

Run:

  BUILD-JELLYFIN.cmd

Target:

  Jellyfin 12.0
  .NET 10

Compilation uses Jellyfin.Controller 12.0.0 and Jellyfin.Model 12.0.0 package
references. Jellyfin server assemblies are not bundled into the plugin.

Final output:

  src\Jellyfin\output\Titlovi.com (Unofficial)_1.1.9.0\
      Jellyfin.Titlovi.dll
      meta.json
      thumb.png

  src\Jellyfin\output\Titlovi.com.Unified-1.1.9-Jellyfin-12.zip
  src\Jellyfin\output\build.log

There is no separate release directory. The ready-to-copy plugin folder is
created directly under output, and the ZIP is created from that same folder.

The build generates meta.json as UTF-8 without BOM and validates the plugin
name, imagePath, assembly whitelist, DLL and thumbnail before producing the ZIP.

Manual Jellyfin installation:

  1. Stop Jellyfin.
  2. Copy the complete generated folder
     `Titlovi.com (Unofficial)_1.1.9.0` into Jellyfin's plugins directory.
     Alternatively, create a plugin subfolder and extract the generated ZIP
     there.
  3. Confirm that Jellyfin.Titlovi.dll, meta.json and thumb.png are together in
     the same plugin folder.
  4. Start Jellyfin.
  5. Open Titlovi.com (Unofficial) in the server plugin settings and configure
     your Titlovi.com credentials/options.

The Jellyfin plugin has its own GUID. It does not reuse the Emby GUID or the
GUID of the official Titlovi Jellyfin plugin.

REGRESSION TESTS
----------------

Run:

  RUN-TESTS.cmd

The suite is fully offline and does not contact Titlovi.com. It compiles the
same src\Common\*.cs files used by both plugins. A fake API client provides
search/download responses, and real ZIP/subtitle byte streams are generated in
memory for archive and selection tests.

Test output:

  tests\output\test.log
  tests\output\test-results.trx

See tests\README.md and tests\TEST-CORPUS.md for the covered regression cases.

TROUBLESHOOTING
---------------

If a build fails, first inspect the host-specific log:

  src\Emby\output\build.log
  src\Jellyfin\output\build.log

The log records the selected SDK and the full restore/build output. If a failure
is reproducible, keep the log together with the package version when reporting
it.

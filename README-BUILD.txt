Titlovi.com.Unified 1.1.9 - Build and developer guide
======================================================

This source/build package produces two independent plugins registered as:

  Titlovi.com (Unofficial)

Host assemblies:

  Emby.Titlovi.dll
  Jellyfin.Titlovi.dll

Both host projects compile the same src/Common source directly into their own
DLL. There is no Titlovi.Core.dll and no shared runtime dependency between the
Emby and Jellyfin plugins.

For end-user installation of pre-built releases, see INSTALL.md. The installation
steps below are intended for testing locally built artifacts.

REQUIREMENTS
------------

Windows convenience build scripts:

- Windows with PowerShell 5.1 or newer.
- Internet access may be needed during the first build to download compilation
  references or a portable .NET SDK.
- You do not need Visual Studio.
- The scripts do not install, stop, start or modify Emby/Jellyfin.

Cross-platform development/testing:

- .NET 10 SDK for the offline regression suite and Linux host builds.
- Linux runtime/build validation additionally uses Bash, Docker, curl and
  Python 3.
- Internet access is required when Docker images, NuGet packages or Emby
  reference assemblies are not already cached.

The source and complete regression suite are validated on both Windows and
Ubuntu in GitHub Actions.

The Windows build scripts prefer a suitable stable installed .NET SDK. If none
is available, they can download a portable SDK into the package's local .build
folder. Preview/RC SDKs are deliberately not selected.

QUICK BUILD - WINDOWS
---------------------

From the package root on Windows:

  BUILD-EMBY.cmd
  BUILD-JELLYFIN.cmd

To build both in sequence:

  BUILD-ALL.cmd

Before distributing a changed build, run:

  RUN-TESTS.cmd

The regression suite can also be run directly on Windows or Linux:

  dotnet test tests/Titlovi.Tests/Titlovi.Tests.csproj -c Release

LINUX BUILD / RUNTIME VALIDATION
--------------------------------

The canonical Linux validation path used by CI is:

  bash tests/smoke/linux-runtime-smoke.sh

This script:

  1. pulls the official Emby 4.10.0.40 and Jellyfin 12 Linux images;
  2. copies the exact MediaBrowser reference assemblies from the official Emby
     image;
  3. restores and builds Emby.Titlovi.dll on Linux;
  4. restores and builds Jellyfin.Titlovi.dll on Linux;
  5. loads both plugins into disposable Linux server containers;
  6. verifies host versions, plugin loading and absence of loader/ABI failures.

For Jellyfin only, a normal direct Linux build is also possible with:

  dotnet restore src/Jellyfin/Jellyfin.Titlovi.csproj --nologo -p:NuGetAudit=false
  dotnet build src/Jellyfin/Jellyfin.Titlovi.csproj -c Release --no-restore --nologo -p:NuGetAudit=false

The Emby project requires MediaBrowser.Model.dll, MediaBrowser.Common.dll and
MediaBrowser.Controller.dll compilation references. The Linux smoke script
obtains those references from the official Emby image before building, avoiding
machine-specific assumptions about an installed Emby server.

The Linux smoke path is validation-oriented; it does not replace the Windows
packaging scripts and does not publish release artifacts.

EMBY BUILD - WINDOWS CONVENIENCE SCRIPT
---------------------------------------

Run on Windows:

  BUILD-EMBY.cmd

The script first looks for MediaBrowser.* reference assemblies from an
installed/running Emby Server. If they are not found, it downloads the official
Emby 4.10.0.40 fallback server archive and extracts only the three assemblies
needed for compilation.

Final output:

  src/Emby/output/Emby.Titlovi.dll
  src/Emby/output/build.log

The output directory is cleaned at the start of each build, so it contains only
artifacts from the current run.

Manual test installation of a locally built Emby artifact:

  1. Stop Emby Server.
  2. Back up/remove any previous Emby.Titlovi.dll if you are replacing a build.
  3. Copy src/Emby/output/Emby.Titlovi.dll to Emby's plugins directory.
  4. Start Emby Server.
  5. Open Titlovi.com (Unofficial) in the server plugin settings and configure
     your Titlovi.com credentials/options.

The Emby plugin GUID is intentionally stable so an upgrade can keep the
existing plugin configuration.

The Emby project is also compiled and runtime-loaded on Ubuntu CI using the
exact MediaBrowser reference assemblies copied from the official
emby/embyserver:4.10.0.40 Linux image.

JELLYFIN BUILD - WINDOWS CONVENIENCE SCRIPT
-------------------------------------------

Run on Windows:

  BUILD-JELLYFIN.cmd

Target:

  Jellyfin 12.0
  .NET 10

Compilation uses Jellyfin.Controller 12.0.0 and Jellyfin.Model 12.0.0 package
references. Jellyfin server assemblies are not bundled into the plugin.

Final output:

  src/Jellyfin/output/Titlovi.com (Unofficial)_1.1.9.0/
      Jellyfin.Titlovi.dll
      meta.json
      thumb.png

  src/Jellyfin/output/Titlovi.com.Unified-1.1.9-Jellyfin-12.zip
  src/Jellyfin/output/build.log

There is no separate release directory. The ready-to-copy plugin folder is
created directly under output, and the ZIP is created from that same folder.

The build generates meta.json as UTF-8 without BOM and validates the plugin
name, imagePath, assembly whitelist, DLL and thumbnail before producing the ZIP.

Manual test installation of a locally built Jellyfin artifact:

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

The Jellyfin project is compiled and runtime-loaded on Ubuntu CI against the
official ghcr.io/jellyfin/jellyfin:12.0 Linux image.

CROSS-PLATFORM VALIDATION
-------------------------

GitHub Actions runs the complete offline regression suite on both Windows and
Ubuntu. The current baseline is 110 passing tests on each operating system.

A Linux runtime smoke workflow builds and loads both plugins in the official
Emby 4.10 and Jellyfin 12 Linux server containers. Separate manually triggered
live end-to-end workflows have also validated authenticated Titlovi.com movie
and episode search, download, host-side subtitle saving and UTF-8 output on both
Linux hosts.

These Linux checks do not create a separate Linux plugin product. They validate
that the same self-contained host plugin assemblies and shared source behave
correctly with native Unix media paths.

REGRESSION TESTS
----------------

Run with the Windows convenience wrapper:

  RUN-TESTS.cmd

Or directly on Windows/Linux:

  dotnet test tests/Titlovi.Tests/Titlovi.Tests.csproj -c Release

The suite is fully offline and does not contact Titlovi.com. It compiles the
same src/Common/*.cs files used by both plugins. A fake API client provides
search/download responses, and real ZIP/subtitle byte streams are generated in
memory for archive and selection tests.

Test output from RUN-TESTS.cmd:

  tests/output/test.log
  tests/output/test-results.trx

See tests/README.md and tests/TEST-CORPUS.md for the covered regression cases.

TROUBLESHOOTING
---------------

For Windows convenience-script failures, inspect the host-specific log:

  src/Emby/output/build.log
  src/Jellyfin/output/build.log

For Linux smoke failures, inspect the GitHub Actions job output or run
`tests/smoke/linux-runtime-smoke.sh` locally so Docker/server startup and plugin
loader diagnostics are visible directly in the terminal.

If a failure is reproducible, keep the relevant build/test output together with
the package version when reporting it.

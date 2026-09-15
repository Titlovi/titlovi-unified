Titlovi.com.Unified 1.1.9 - Build and developer guide
======================================================

This repository produces two independent plugins registered as:

  Titlovi.com (Unofficial)

Host assemblies:

  Emby.Titlovi.dll
  Jellyfin.Titlovi.dll

Both host projects compile the same src/Common source directly into their own
DLL. There is no Titlovi.Core.dll and no shared runtime dependency between the
Emby and Jellyfin plugins.

For end-user installation of pre-built releases, see INSTALL.md.

BUILD LAYOUT
------------

Source code stays under src/. Build infrastructure is separated by operating
system:

  scripts/windows/
  scripts/linux/

All generated/intermediate state goes under the repository-root .build/
directory. This includes normal .NET bin/obj trees, Emby reference assemblies,
logs, NuGet configuration, test results and temporary SDK/tooling state.

Installable artifacts from both Windows and Linux builds are collected under:

  dist/Emby/
  dist/Jellyfin/

There are intentionally no dist/windows and dist/linux directories. The plugin
artifacts are host-specific (Emby or Jellyfin), not OS-specific; the same
artifact is used by a supported host version on Windows or Linux.

Both .build/ and dist/ are ignored by Git and can be deleted safely.

WINDOWS REQUIREMENTS
--------------------

- Windows with PowerShell 5.1 or newer.
- Internet access may be needed to obtain NuGet packages, Emby references or a
  portable .NET SDK.
- Visual Studio is not required.

The Windows scripts prefer a stable installed .NET 10+ SDK. If none is found,
they can download portable Microsoft .NET SDK 10.0.302 into .build/tooling.
Preview/RC SDKs are not selected.

WINDOWS COMMANDS
----------------

From the repository root:

  scripts\windows\run-tests.cmd
  scripts\windows\build-emby.cmd
  scripts\windows\build-jellyfin.cmd
  scripts\windows\build-all.cmd

The .cmd files are small entry points; shared PowerShell implementation lives
beside them under scripts/windows.

LINUX REQUIREMENTS
------------------

- Bash.
- Stable .NET 10 SDK or newer.
- Python 3 (used for package metadata/ZIP validation).
- Docker for the automatic Emby-reference fallback and runtime smoke test.
- Internet access when NuGet packages or container images are not cached.

LINUX COMMANDS
--------------

From the repository root:

  ./scripts/linux/run-tests.sh
  ./scripts/linux/build-emby.sh
  ./scripts/linux/build-jellyfin.sh
  ./scripts/linux/build-all.sh

If executable bits were not preserved by a copied source package, the same
commands may be invoked explicitly through Bash, for example:

  bash scripts/linux/build-all.sh

EMBY BUILD
----------

Windows:

  scripts\windows\build-emby.cmd

Linux:

  ./scripts/linux/build-emby.sh

The Emby project requires these compile-time reference assemblies:

  MediaBrowser.Model.dll
  MediaBrowser.Common.dll
  MediaBrowser.Controller.dll

On Windows, the build first checks an installed/running Emby Server and falls
back to the official Emby 4.10.0.40 release archive.

On Linux, the build checks EMBY_REFERENCE_DIR when supplied, then common
installed Emby locations. If no suitable references are found, it copies the
exact assemblies from the official emby/embyserver:4.10.0.40 Docker image.

Intermediate output:

  .build/Emby/bin/
  .build/Emby/obj/
  .build/Emby/refs/
  .build/logs/Emby/build.log

Final installable output:

  dist/Emby/Emby.Titlovi.dll

JELLYFIN BUILD
--------------

Windows:

  scripts\windows\build-jellyfin.cmd

Linux:

  ./scripts/linux/build-jellyfin.sh

Target:

  Jellyfin 12.0
  .NET 10

Compilation uses Jellyfin.Controller 12.0.0 and Jellyfin.Model 12.0.0 package
references. Jellyfin server assemblies are not bundled into the plugin.

Intermediate output:

  .build/Jellyfin/bin/
  .build/Jellyfin/obj/
  .build/logs/Jellyfin/build.log

Final installable output:

  dist/Jellyfin/Titlovi.com (Unofficial)_1.1.9.0/
      Jellyfin.Titlovi.dll
      meta.json
      thumb.png

  dist/Jellyfin/Titlovi.com.Unified-1.1.9-Jellyfin-12.zip

Both Windows and Linux packaging paths generate the same three-file Jellyfin
plugin folder and distribution ZIP. meta.json is written as UTF-8 without BOM.

REGRESSION TESTS
----------------

Windows:

  scripts\windows\run-tests.cmd

Linux:

  ./scripts/linux/run-tests.sh

The suite is fully offline and does not contact Titlovi.com. It compiles the
same src/Common/*.cs files used by both plugins and currently contains 110
regression cases.

Generated test state:

  .build/Tests/bin/
  .build/Tests/obj/
  .build/Tests/results/test-results.trx
  .build/logs/Tests/test.log

LINUX RUNTIME SMOKE
-------------------

Run:

  bash tests/smoke/linux-runtime-smoke.sh

The smoke test first invokes the canonical Linux build-all script, then loads
the actual dist/Emby and dist/Jellyfin artifacts into the official Emby 4.10
and Jellyfin 12 Linux server containers. It checks host versions, plugin loading
and loader/ABI errors. It does not contact Titlovi.com.

LIVE END-TO-END VALIDATION
--------------------------

Live Titlovi.com E2E workflows are intentionally explicit/manual because they
use real account credentials and external-service requests. See
tests/e2e/README.md for details.

MANUAL TEST INSTALLATION
------------------------

Emby:

  1. Stop Emby Server.
  2. Copy dist/Emby/Emby.Titlovi.dll into Emby's plugins directory.
  3. Start Emby Server.
  4. Configure Titlovi.com (Unofficial) from the server dashboard.

Jellyfin:

  1. Stop Jellyfin.
  2. Copy the complete generated Titlovi.com (Unofficial)_<version> folder into
     Jellyfin's plugins directory, or extract the generated ZIP into a dedicated
     plugin folder.
  3. Keep Jellyfin.Titlovi.dll, meta.json and thumb.png together.
  4. Start Jellyfin and configure Titlovi.com (Unofficial).

TROUBLESHOOTING
---------------

Build/test logs are centralized under:

  .build/logs/Emby/build.log
  .build/logs/Jellyfin/build.log
  .build/logs/Tests/test.log

If an old checkout still contains generated src/Emby/output,
src/Jellyfin/output, src/*/bin, src/*/obj, src/Emby/embylibs or tests/output
directories, they are legacy generated artifacts and may be deleted. Current
scripts do not use them.

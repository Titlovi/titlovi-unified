Titlovi.com.Unified - Build and developer guide
================================================

This repository builds two independent plugins registered as:

  Titlovi.com (Unofficial)

Host assemblies:

  Emby.Titlovi.dll
  Jellyfin.Titlovi.dll

Both host projects compile the same src/Common source directly into their own
assembly. There is no separate Common/Core runtime DLL and no shared runtime
dependency between the Emby and Jellyfin plugins.

For installation of pre-built releases, see INSTALL.md.

QUICK START
-----------

Windows, from the repository root:

  scripts\windows\run-tests.cmd
  scripts\windows\build-emby.cmd
  scripts\windows\build-jellyfin.cmd
  scripts\windows\build-all.cmd

Linux, from the repository root:

  ./scripts/linux/run-tests.sh
  ./scripts/linux/build-emby.sh
  ./scripts/linux/build-jellyfin.sh
  ./scripts/linux/build-all.sh

If executable bits were not preserved, invoke the Linux scripts through Bash:

  bash scripts/linux/run-tests.sh
  bash scripts/linux/build-all.sh

BUILD LAYOUT
------------

Production source is kept under:

  src/Common/
  src/Emby/
  src/Jellyfin/

Platform-specific build/test entry points are under:

  scripts/windows/
  scripts/linux/

All generated and intermediate state is written under the repository-root
.build/ directory. This includes normal .NET bin/obj output, Emby reference
assemblies, logs, NuGet configuration, test results and temporary SDK/tooling
state.

Installable artifacts are collected under:

  dist/Emby/
  dist/Jellyfin/

There are intentionally no Windows- and Linux-specific distribution
directories. The generated plugins are host-specific, not operating-system-
specific.

Both .build/ and dist/ are ignored by Git and may be deleted safely.

WINDOWS REQUIREMENTS
--------------------

Required:

- Windows with PowerShell 5.1 or newer.
- Internet access when required packages, references or build tools are not
  already available locally.

Visual Studio is not required.

The build scripts prefer an installed stable .NET 10 SDK or newer. Preview and
release-candidate SDKs are not selected.

If no suitable installed SDK is found, the scripts download the configured
portable .NET 10 SDK into:

  .build/tooling/windows/

An installed Emby Server is not required to build the Emby plugin. If suitable
Emby reference assemblies cannot be found locally, the Windows build downloads
the configured official Emby release archive and extracts the required
assemblies from it.

The fallback Emby-reference path requires the Windows tar command.

LINUX REQUIREMENTS
------------------

Required for all Linux builds and tests:

- Bash.
- Stable .NET 10 SDK or newer.
- Internet access when required NuGet packages are not already cached.

Additionally:

- Python 3 is required for Jellyfin packaging.
- Docker is required by the Emby build only when suitable Emby reference
  assemblies are not available locally.
- Docker, Python 3 and curl are required for the Linux runtime smoke test.

EMBY BUILD
----------

Windows:

  scripts\windows\build-emby.cmd

Linux:

  ./scripts/linux/build-emby.sh

The Emby project targets Emby Server 4.10.x and compiles against these host
assemblies:

  MediaBrowser.Model.dll
  MediaBrowser.Common.dll
  MediaBrowser.Controller.dll

These assemblies are compile-time references only and are not bundled into the
plugin.

On Windows, the build first looks for a local Emby installation, including a
running Emby Server. If suitable references are not found, it downloads the
configured official Emby 4.10 release archive and extracts the required
assemblies.

On Linux, reference resolution is performed in this order:

  1. EMBY_REFERENCE_DIR, when explicitly supplied.
  2. Common installed Emby server locations.
  3. The configured official emby/embyserver Docker image.

If EMBY_REFERENCE_DIR is supplied, it must contain all three required
MediaBrowser assemblies.

Generated state:

  .build/Emby/bin/
  .build/Emby/obj/
  .build/Emby/refs/
  .build/logs/Emby/build.log

Installable output:

  dist/Emby/Emby.Titlovi.dll

JELLYFIN BUILD
--------------

Windows:

  scripts\windows\build-jellyfin.cmd

Linux:

  ./scripts/linux/build-jellyfin.sh

Target:

  Jellyfin 12
  .NET 10

Compilation uses the Jellyfin.Controller and Jellyfin.Model 12.0 package
references. Jellyfin server assemblies are not bundled into the plugin.

Generated state:

  .build/Jellyfin/bin/
  .build/Jellyfin/obj/
  .build/logs/Jellyfin/build.log

Installable output:

  dist/Jellyfin/Titlovi.com (Unofficial)_<package-version>/
      Jellyfin.Titlovi.dll
      meta.json
      thumb.png

  dist/Jellyfin/Titlovi.com.Unified-<version>-Jellyfin-12.zip

The generated ZIP contains these three files directly at its root:

  Jellyfin.Titlovi.dll
  meta.json
  thumb.png

Both Windows and Linux packaging paths produce the same Jellyfin distribution
layout. meta.json is written as UTF-8 without BOM.

REGRESSION TESTS
----------------

Windows:

  scripts\windows\run-tests.cmd

Linux:

  ./scripts/linux/run-tests.sh

The regression suite is fully offline and does not contact Titlovi.com.

It compiles the same src/Common source used by both host plugins, so changes to
the shared subtitle engine are exercised independently of Emby and Jellyfin
runtime APIs.

Generated test state:

  .build/Tests/bin/
  .build/Tests/obj/
  .build/Tests/results/test-results.trx
  .build/logs/Tests/test.log

GitHub Actions runs the regression suite on both Windows and Ubuntu.

LINUX RUNTIME SMOKE
-------------------

Run:

  bash tests/smoke/linux-runtime-smoke.sh

The smoke test:

  1. Builds the distributable artifacts through the canonical Linux build
     scripts.
  2. Loads the generated Jellyfin package into the official Jellyfin 12 Linux
     container.
  3. Loads the generated Emby DLL into the official Emby 4.10 Linux container.
  4. Verifies the host versions.
  5. Verifies that each plugin assembly loads without loader/ABI errors.

The runtime smoke test does not contact Titlovi.com.

LIVE END-TO-END VALIDATION
--------------------------

Live Titlovi.com end-to-end tests are intentionally not run on ordinary pushes
or pull-request updates because they use real account credentials and make
requests to the external Titlovi.com service.

They validate real host-side search, download and subtitle saving for both
supported Linux hosts.

See:

  tests/e2e/README.md

for required GitHub Actions secrets, manual workflow triggers and validation
details.

MANUAL TEST INSTALLATION
------------------------

For local host testing, install the generated artifacts from dist/ in the same
way as the corresponding pre-built release.

See INSTALL.md for plugin locations and installation procedure.

Use:

  dist/Emby/Emby.Titlovi.dll

for Emby, and the complete generated Jellyfin plugin folder or ZIP from:

  dist/Jellyfin/

for Jellyfin.

TROUBLESHOOTING
---------------

Build and test logs are centralized under:

  .build/logs/Emby/build.log
  .build/logs/Jellyfin/build.log
  .build/logs/Tests/test.log

The canonical Windows and Linux scripts automatically remove generated
directories used by older repository layouts before building or testing.

This prevents stale bin/obj/output state from earlier versions of the project
from interfering with the current centralized .build/ layout.

If unusual build errors remain after switching branches or updating an older
checkout, both generated roots can safely be removed:

  .build/
  dist/

They will be recreated by the build scripts.

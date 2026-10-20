# Build and developer guide

[English](BUILD.md) · [Hrvatski](../hr/BUILD.md) · [Српски](../sr-Cyrl/BUILD.md)

This guide describes building the two independent **Jellyfin 12 / .NET 10** and **Emby Server 4.10.x** plugins from the [canonical source repository](https://github.com/Titlovi/titlovi-unified).

> The official [Unified source tag `v2.0.0`](https://github.com/Titlovi/titlovi-unified/tree/v2.0.0) supplies both published plugins: [Emby 2.0](https://github.com/Titlovi/titlovi-emby/releases/tag/v2.0.0) and [Jellyfin 2.0](https://github.com/Titlovi/titlovi-jellyfin/releases/tag/v2.0.0). Source builds are intended for development; end users should install the host-specific official release.

## Source tree and prerequisites

```text
src/Common/       Shared, host-neutral C# subtitle engine
src/Jellyfin/     Jellyfin 12 integration
src/Emby/         Emby 4.10.x integration
tests/            Offline regression, runtime and end-to-end tests
scripts/windows/  Windows build/test entry points
scripts/linux/    Linux build/test entry points
```

The shared `src/Common` C# files compile **directly into each host DLL**.

| Platform | Requirements |
| --- | --- |
| Windows | PowerShell 5.1+, Git, internet access for missing packages and references. **Visual Studio is not required**. |
| Linux | Bash, Git, stable **.NET 10 SDK or newer**, internet access for uncached NuGet packages. |
| Jellyfin packaging on Linux | Python 3. |
| Emby compilation | `MediaBrowser.Model.dll`, `MediaBrowser.Common.dll`, `MediaBrowser.Controller.dll` as **compile-time** host references. |

**An installed Emby Server is NOT required on the build machine.** Only its
three `MediaBrowser.*` DLLs are needed as compile-time references; the resulting
`Emby.Titlovi.dll` runs inside Emby but building it does not start or install
Emby Server. The canonical build scripts first look for suitable local
references and can obtain missing references automatically from official
Emby distributions. On a clean machine this fallback needs network access
and the platform-specific extraction tool described below. The build must
continue to support machines **without a local Emby installation**.

Windows scripts prefer an installed stable .NET 10 SDK and can fetch the configured portable SDK if none is suitable. They can download official Emby 4.10 references when not locally available; Windows `tar` is needed for extraction. Preview/RC SDKs are not selected.

On Linux, Emby references are resolved from `EMBY_REFERENCE_DIR` if set, common local Emby installation directories, or the configured `emby/embyserver:4.10.0.40` Docker image. Docker is needed only for the fallback and extended runtime tests.

## Clone and build

```console
git clone https://github.com/Titlovi/titlovi-unified.git
cd titlovi-unified
```

Run the following commands from the **repository root**, regardless of where this guide lives.

### Windows Command Prompt

```bat
scripts\windows\run-tests.cmd
scripts\windows\build-emby.cmd
scripts\windows\build-jellyfin.cmd
```

To compile both host plugins:

```bat
scripts\windows\build-all.cmd
```

### Linux

```bash
bash scripts/linux/run-tests.sh
bash scripts/linux/build-emby.sh
bash scripts/linux/build-jellyfin.sh
```

Or build both:

```bash
bash scripts/linux/build-all.sh
```

Set `EMBY_REFERENCE_DIR` to a directory containing all three Emby reference DLLs if you do not want to use the Linux Docker fallback.

## Output and packages

Temporary build intermediates, host references, logs and test results go under **`.build/`**. Installable files go to **`dist/`**. Both are excluded from Git.

| Host | Output |
| --- | --- |
| Emby | `dist/Emby/Emby.Titlovi.dll` |
| Jellyfin | `dist/Jellyfin/titlovi-com_<version>.zip` |
| Jellyfin plugin directory | `dist/Jellyfin/Titlovi.com_<version>/` |

The Jellyfin ZIP must contain **exactly** `Jellyfin.Titlovi.dll`, `meta.json` and `thumb.png` at its root. Both Windows and Linux build scripts create the same platform-independent host packages.

Do **not** install the Emby DLL into Jellyfin or the Jellyfin DLL into Emby. For installation, see [Installation](INSTALL.md).

## Tests

The regression test cases **do not contact Titlovi.com**; an initial NuGet package restore may still require internet access. The suite compiles the same Common engine as the production plugins and checks subtitle identity, language handling, title and release matching, archives and episode filename rules.

The Linux host-loading smoke test also requires Docker, curl and Python 3:

```bash
bash tests/smoke/linux-runtime-smoke.sh
```

This builds and loads both plugins into supported **Emby 4.10** and **Jellyfin 12** Linux server containers, checking host ABI compatibility. It does not contact Titlovi.com.

**Live authenticated end-to-end tests are opt-in**. They contact the real service and use repository secrets. See [the E2E instructions](../../tests/e2e/README.md). Never commit credentials, tokens or configuration XML.

The public canonical repository runs ordinary offline regression/build CI on Windows and Ubuntu. Standard GitHub-hosted runners in a public repository are not subject to the former private repository's monthly 2,000-minute allowance. Live API tests and costly extended checks remain controlled.

## Diagnostics

```text
.build/logs/Tests/test.log
.build/logs/Emby/build.log
.build/logs/Jellyfin/build.log
```

To clear stale generated output after switching revisions, delete `.build/` and `dist/`; the scripts recreate them. Check the host-specific log for compiler or restore errors.

For implementation details, see [Architecture](../development/ARCHITECTURE.md); for test scenarios, see [the regression corpus](../../tests/TEST-CORPUS.md).

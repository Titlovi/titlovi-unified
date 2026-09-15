# Titlovi.com.Unified architecture

## Purpose

Titlovi.com.Unified keeps subtitle-provider behavior in one platform-neutral source set while producing two independent host plugins:

```text
Emby.Titlovi.dll
Jellyfin.Titlovi.dll
```

Both plugins are registered to users as **Titlovi.com (Unofficial)**. There is no shared runtime DLL; each host plugin contains its own compiled copy of the Common source and can be installed independently. The same host artifact is used for the supported server version on Windows and Linux.

## Repository layout

```text
src/Common/       shared Titlovi.com engine and data models
src/Emby/         Emby adapter, bootstrap and configuration UI
src/Jellyfin/     Jellyfin adapter, bootstrap and configuration UI
scripts/windows/  Windows build/test entry points
scripts/linux/    Linux build/test entry points
tests/            regression suite and Linux smoke/E2E validation
.build/           generated build/intermediate state (ignored)
dist/             generated installable artifacts (ignored)
```

Build tooling is intentionally outside `src/`, so the source tree contains production source/resources rather than build wrappers and generated output.

Both host project files link the same Common files directly:

```xml
<Compile Include="../Common/*.cs" Link="Common/%(Filename)%(Extension)" />
```

Repository-relative MSBuild paths use forward slashes so the same project files can be evaluated on Windows and Linux. Root `Directory.Build.props` redirects normal .NET `bin` and `obj` output for the two plugins and the test project into `.build/`.

## Build artifact boundary

`.build/` is disposable implementation state: compiler output, MSBuild intermediates, Emby reference DLLs, logs, test results and temporary tooling. It is never a distribution source.

`dist/` is the build-product boundary. Both operating-system-specific build front ends must produce the same host-oriented layout:

```text
dist/Emby/Emby.Titlovi.dll

dist/Jellyfin/Titlovi.com (Unofficial)_<version>/
  Jellyfin.Titlovi.dll
  meta.json
  thumb.png

dist/Jellyfin/Titlovi.com.Unified-<version>-Jellyfin-12.zip
```

The distribution tree is not split by Windows/Linux because plugin binaries are not OS-specific products.

## Common layer

`src/Common` contains no `MediaBrowser.*` host API types. It owns the provider behavior that should be identical on both servers, including:

- Titlovi.com API authentication, search and download calls;
- token caching;
- language mapping and optional B/S/H alternative searches;
- duplicate merging by Titlovi media ID;
- search result ordering and release matching;
- movie/episode request handling and IMDb/title fallback;
- legacy episode filename parsing;
- season/whole-series/nested ZIP selection;
- multi-episode bundle safety;
- subtitle format detection;
- optional legacy-encoding conversion to UTF-8;
- provider diagnostics.

The Common boundary uses neutral models such as `CoreSubtitleSearchRequest`, `CoreRemoteSubtitleInfo` and `CoreSubtitleResponse`, plus small serialization/configuration interfaces supplied by each host.

## Cross-platform path handling

Host-native media paths are passed through unchanged. A Windows path remains a Windows path and a Linux/Docker/NAS path remains the path known to the host server.

Shared filename and archive-entry parsing is separator-independent. Common code treats both `\` and `/` as virtual separators when it needs to extract a filename, episode token or release name. This allows the same parsing logic to handle Windows media paths, Unix media paths and non-conforming ZIP entry names without rewriting the filesystem path supplied by Emby or Jellyfin.

## Emby adapter

`src/Emby/TitloviSubtitleProvider.cs` maps Emby's `SubtitleSearchRequest` to the Common request and maps Common results back to Emby `RemoteSubtitleInfo` / `SubtitleResponse` objects.

The plugin and subtitle provider are exposed as **Titlovi.com (Unofficial)**. The assembly remains `Emby.Titlovi.dll` and the plugin GUID remains stable for configuration continuity.

Emby 4.10.x can retain the original requested language when it names an externally saved subtitle after a merged B/S/H search. The adapter still returns the actual language metadata; the known filename behavior is left to the host rather than corrupting the shared language model.

## Jellyfin adapter

`src/Jellyfin/TitloviSubtitleProvider.cs` performs the equivalent model mapping for Jellyfin 12.

Jellyfin 12 does not expose `SeriesProviderIds` directly on `SubtitleSearchRequest`. The adapter therefore resolves the episode by media path through `ILibraryManager` and, when possible, supplies provider IDs from the parent series. This preserves IMDb-based series searches without moving host-specific library logic into Common.

On download, `SubtitleResponse.Language` receives the actual two-letter Common language code, which Jellyfin uses when naming the external subtitle file.

## Tests and validation

`tests/Titlovi.Tests` links the same `src/Common/*.cs` files into the test assembly. An internal test seam injects a fake `ITitloviApiClient`; production construction still uses the real `TitloviApiClient`.

The offline regression suite does not contact Titlovi.com. GitHub Actions runs it through the platform-specific test entry points on Windows and Ubuntu.

Linux runtime smoke invokes the canonical Linux packaging scripts and then loads the resulting `dist/` artifacts into the official Emby 4.10 and Jellyfin 12 Linux server containers. Explicitly triggered live E2E workflows can additionally validate authenticated Titlovi.com search, download and host-side subtitle saving.

No test assembly or test dependency is shipped with either plugin.

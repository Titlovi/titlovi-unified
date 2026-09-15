# Titlovi.com.Unified architecture

## Purpose

Titlovi.com.Unified keeps all subtitle-provider behavior in one platform-neutral source set while producing two independent host plugins:

```text
Emby.Titlovi.dll
Jellyfin.Titlovi.dll
```

Both plugins are registered to users as **Titlovi.com (Unofficial)**.

There is no shared runtime DLL. Each host plugin contains its own compiled copy of the Common source so it can be installed independently.

The same host plugin binaries are used for the supported server version on Windows and Linux.

## Source layout

```text
src/Common/       shared Titlovi.com engine and data models
src/Emby/         Emby adapter, bootstrap, configuration UI and build script
src/Jellyfin/     Jellyfin adapter, bootstrap, configuration UI and build script
tests/            offline regression suite plus Linux smoke/E2E validation
```

Both host project files link the same Common files directly:

```xml
<Compile Include="../Common/*.cs" Link="Common/%(Filename)%(Extension)" />
```

Repository-relative MSBuild paths use forward slashes so the same project files can be evaluated on Windows and Linux.

This keeps one functional implementation without introducing a `Titlovi.Core.dll` deployment dependency.

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

## Jellyfin package

Jellyfin requires the manifest and thumbnail beside the plugin DLL. The build therefore produces one ready-to-copy directory:

```text
output/Titlovi.com (Unofficial)_1.1.9.0/
  Jellyfin.Titlovi.dll
  meta.json
  thumb.png
```

The same directory is also compressed into a distribution ZIP. `meta.json` is written as UTF-8 without BOM and is validated before the ZIP is created.

## Tests and validation

`tests/Titlovi.Tests` links the same `src/Common/*.cs` files into the test assembly. An internal test seam injects a fake `ITitloviApiClient`; production construction still uses the real `TitloviApiClient`.

The offline regression suite does not contact Titlovi.com. ZIP fixtures are real ZIP byte streams generated in memory, so archive parsing, nested archives, file selection and encoding conversion execute the same Common code as production. GitHub Actions runs this suite on both Windows and Ubuntu.

Separate Linux runtime-smoke validation builds and loads both plugins in the official Emby 4.10 and Jellyfin 12 Linux server containers. Explicitly triggered live E2E workflows can additionally validate authenticated Titlovi.com search, download and host-side subtitle saving on both Linux hosts.

No test assembly or test dependency is shipped with either plugin.

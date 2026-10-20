# Titlovi.com.Unified architecture

## Purpose

Titlovi.com.Unified maintains one platform-neutral subtitle engine while producing two independent host plugins:

```text
Emby.Titlovi.dll
Jellyfin.Titlovi.dll
```

Both plugins are presented to users as **Titlovi.com (Unofficial)**.

There is no separate Common/Core runtime assembly. Each host project compiles the same `src/Common` source directly into its own plugin assembly, so Emby and Jellyfin remain independently installable and self-contained.

The same host artifact is used on supported Windows and Linux installations.

## Architecture rules

The project follows these boundaries:

1. Shared subtitle-provider behavior belongs in `src/Common`.
2. Host API types and host-specific behavior belong in `src/Emby` or `src/Jellyfin`.
3. `src/Common` must not depend on Emby or Jellyfin `MediaBrowser.*` types.
4. Both host plugins compile the same Common source rather than referencing a separate runtime Common DLL.
5. Different Titlovi.com media IDs remain separate search results. Only duplicate results referring to the same media ID may be merged.
6. Release matching affects ranking only. A release mismatch must not remove an otherwise valid subtitle result.
7. The actual subtitle language reported by Titlovi.com is preserved in the shared model whenever it can be mapped to a supported language.
8. Host-specific limitations must remain host-specific and must not distort shared provider behavior.

**Emby build portability:** The Emby plugin build **must not depend on an
installed Emby Server**. The three required `MediaBrowser.Model.dll`,
`MediaBrowser.Common.dll` and `MediaBrowser.Controller.dll` are
compile-time host references, not extra plugin runtime assemblies.
Canonical Windows/Linux build scripts must remain able to obtain these
from an official Emby distribution when no suitable local installation
exists (Windows: official release archive; Linux: official Docker image
if local references or `EMBY_REFERENCE_DIR` are unavailable). Downloading
missing references on a clean machine requires network access; the Linux
fallback requires Docker and the Windows fallback requires `tar`.
Changing this behavior requires a deliberate, documented design decision.

These rules are intended to keep the two host implementations behaviorally aligned without hiding genuine differences between Emby and Jellyfin.

## Repository layout

```text
src/Common/       shared subtitle engine and platform-neutral models
src/Emby/         Emby adapter, plugin bootstrap and configuration UI
src/Jellyfin/     Jellyfin adapter, plugin bootstrap and configuration UI

scripts/windows/  Windows build/test entry points
scripts/linux/    Linux build/test entry points

tests/            regression, runtime smoke and live E2E validation

.build/           generated build/intermediate state
dist/             generated installable artifacts
```

Build and generated state are intentionally kept outside the production source directories.

## Runtime composition

Both host project files include the shared Common source directly:

```xml
<Compile Include="../Common/*.cs" Link="Common/%(Filename)%(Extension)" />
```

Conceptually, each resulting plugin contains:

```text
Host plugin assembly
├── shared Titlovi.com engine
├── shared models and subtitle/archive logic
├── shared language and release matching logic
└── host-specific adapter, configuration and bootstrap code
```

There is therefore no runtime dependency between the Emby and Jellyfin plugins.

## Search and download flow

The host adapters translate between the server API and neutral Common models.

Search:

```text
Emby/Jellyfin SubtitleSearchRequest
        │
        ▼
host adapter
        │
        ▼
CoreSubtitleSearchRequest
        │
        ▼
TitloviEngine
        │
        ▼
Titlovi.com API
        │
        ▼
Common search results
        │
        ▼
host adapter
        │
        ▼
Emby/Jellyfin RemoteSubtitleInfo
```

Download:

```text
host subtitle ID
        │
        ▼
host adapter
        │
        ▼
TitloviEngine
        │
        ▼
download / archive selection / conversion
        │
        ▼
CoreSubtitleResponse
        │
        ▼
host SubtitleResponse
```

The host adapters should remain thin. Search strategy, subtitle selection, archive handling, language handling and other reusable provider behavior belong in Common unless a host API requires different treatment.

## Common layer

`src/Common` contains the platform-neutral provider implementation.

Its responsibilities include:

- Titlovi.com authentication, search and download requests;
- API token lifetime handling;
- conservative search request throttling and short-lived search caching;
- language mapping and optional Serbian/Croatian/Bosnian alternative searches;
- preservation of the actual language reported for each result;
- duplicate merging by identical Titlovi.com media ID;
- deterministic result ranking;
- release-aware ranking;
- movie and episode request handling;
- IMDb-based search with title fallback;
- title-result validation;
- legacy episode filename parsing;
- season packs and whole-series packs;
- nested ZIP handling;
- safe selection from multi-episode subtitle bundles;
- subtitle format detection;
- optional legacy character-encoding conversion to UTF-8;
- shared diagnostics;
- separator-independent media/archive filename parsing.

The Common API boundary uses neutral models such as:

```text
CoreSubtitleSearchRequest
CoreRemoteSubtitleInfo
CoreSubtitleResponse
```

Host-specific services are exposed to Common only through small neutral interfaces such as configuration and JSON deserialization abstractions.

## Result identity

Titlovi.com media ID is the identity used when merging API results.

The same media ID may appear more than once, for example when overlapping B/S/H searches return the same subtitle. Those duplicate representations may be merged.

Different media IDs are never collapsed merely because they have the same title, release name, language or other metadata.

This preserves distinct subtitle uploads as distinct choices for the user.

## Release matching

Release matching is a ranking mechanism, not a compatibility filter.

Common may compare the local media filename with subtitle release information using signals such as source, release group, FPS, cut or edition, resolution, codec, HDR and audio markers.

A stronger match should rank higher, but a weak or missing match does not make the subtitle invalid.

This separation is important because real-world subtitle release names are often incomplete, inconsistent or absent.

## Language handling

Common keeps the actual language reported by Titlovi.com whenever that value maps to a supported provider language.

The language used to initiate an API search is only used as a fallback when the returned language is missing or unsupported.

This is particularly important when optional Serbian/Croatian/Bosnian alternative searching is enabled: a subtitle returned during a Serbian API search can still be represented internally as Croatian or Bosnian when Titlovi.com reports it that way.

The shared language model must not be changed to compensate for differences in how individual hosts later save subtitle files.

## Cross-platform path handling

Media paths supplied by the host are passed through without converting them between Windows and Unix filesystem syntax.

Shared parsing code treats both:

```text
\
/
```

as possible separators when extracting filenames, release information or episode tokens.

The same logic can therefore operate on Windows paths, Unix paths and archive entry names while preserving the original path supplied by the host.

## Emby adapter

`src/Emby` contains the Emby-specific integration.

`TitloviSubtitleProvider` maps Emby's `SubtitleSearchRequest` into `CoreSubtitleSearchRequest`, invokes the shared engine and maps Common results back into Emby `RemoteSubtitleInfo` and `SubtitleResponse` objects.

Emby supplies item and parent-series provider IDs through its subtitle request model, so the adapter can pass those IDs directly into Common.

The plugin name exposed to the server is:

```text
Titlovi.com (Unofficial)
```

The assembly remains:

```text
Emby.Titlovi.dll
```

The Emby plugin GUID remains stable so that replacing the DLL does not create a new plugin identity.

### Emby 4.10 language filename limitation

The adapter returns the actual Common subtitle language to Emby.

However, Emby 4.10.x may use the language originally selected for the search when naming a downloaded external subtitle after an alternative B/S/H search.

For example, a Croatian subtitle found during a Serbian search may be saved with a `.sr.srt` suffix.

This is treated as a host limitation. Shared language data is deliberately not falsified to work around the filename behavior.

## Jellyfin adapter

`src/Jellyfin` contains the Jellyfin 12 integration.

`TitloviSubtitleProvider` performs the equivalent mapping between Jellyfin subtitle API objects and the neutral Common models.

Jellyfin 12 does not provide parent-series provider IDs directly on `SubtitleSearchRequest`.

For episode searches, the adapter therefore uses `ILibraryManager` and the media path to resolve the episode and, when possible, obtain provider IDs from its parent series.

This host-specific library lookup remains in `src/Jellyfin` rather than being introduced into Common.

For downloaded subtitles, Jellyfin receives the actual language returned by Common and uses it when naming the external subtitle file.

## Build and distribution boundary

`.build/` contains disposable implementation state such as compiler output, MSBuild intermediates, Emby reference assemblies, logs, test results and temporary tooling.

`dist/` contains installable build products:

```text
dist/Emby/Emby.Titlovi.dll

dist/Jellyfin/Titlovi.com (Unofficial)_<package-version>/
  Jellyfin.Titlovi.dll
  meta.json
  thumb.png

dist/Jellyfin/Titlovi.com.Unified-<version>-Jellyfin-12.zip
```

The distribution tree is separated by host rather than operating system.

Detailed build requirements and commands are documented in [Build guide](../en/BUILD.md).

## Tests and validation

`tests/Titlovi.Tests` compiles the same `src/Common` source used by the production plugins.

The engine exposes an internal test seam through `ITitloviApiClient`, allowing the regression suite to exercise Common search and download behavior without contacting the live Titlovi.com service.

Validation is divided into three levels:

1. **Offline regression tests**  
   Exercise shared behavior deterministically without contacting Titlovi.com.

2. **Linux runtime smoke tests**  
   Build the real distribution artifacts and load them into supported Emby and Jellyfin containers to detect packaging, loader and ABI problems.

3. **Live end-to-end validation**  
   Explicitly triggered tests use real credentials and the live Titlovi.com service to validate host-side search, download and subtitle saving.

No test assembly or test dependency is shipped with either plugin.

For build and test commands, see [Build guide](../en/BUILD.md).

For live validation details, see [Live E2E guide](../../tests/e2e/README.md).

# Titlovi.com.Unified regression tests

Run the suite from the package root on Windows:

```text
RUN-TESTS.cmd
```

or directly on any supported .NET development host:

```text
dotnet test tests/Titlovi.Tests/Titlovi.Tests.csproj -c Release
```

## What the offline suite actually tests

The suite does **not** contact Titlovi.com. A `FakeTitloviApiClient` supplies deterministic API search/download responses so the tests are fast, repeatable and independent of the service/network.

For archive tests, the suite creates **real ZIP byte streams in memory**. Those bytes are passed to the same Common download/archive-selection code used by Emby and Jellyfin. Nested ZIPs are created the same way. Subtitle entries contain small deterministic payloads so each test can verify which file was actually selected.

This means the suite tests production parsing, archive traversal, filename matching, subtitle selection and encoding conversion without downloading a live archive from the website.

## Source under test

The test project compiles the exact same `src/Common/*.cs` source files as both host plugins. No copied implementation and no separate Core assembly is used.

When using `RUN-TESTS.cmd`, results are written to:

```text
tests\output\test.log
tests\output\test-results.trx
```

GitHub Actions also runs the complete suite on Windows and Ubuntu.

## Coverage

The current suite contains 110 cases covering, among other things:

- B/S/H language mapping, alternative-search ordering and actual-language preservation;
- Seinfeld/Blackadder and other legacy episode filename conventions;
- Season 0 and large year-like season numbers;
- season packs, whole-series folders, nested ZIPs and exact multi-episode ranges;
- rejection of grouped subtitles for ordinary single-episode files;
- Windows- and Unix-style archive path separators;
- Linux/Docker-style absolute media paths and filename-only release ranking;
- SRT/SUB/TXT/SSA/ASS format handling;
- UTF-8/BOM, UTF-16 and Windows-1250 conversion;
- IMDb/title fallback and zero-result behavior;
- token reuse/refresh behavior;
- synchronization-aware release ranking.

## Filename fixture note

`/` is the canonical directory separator in ZIP entry names. Some legacy or non-conforming archives can expose `\`, so shared archive-path parsing deliberately treats both `/` and `\` as virtual separators regardless of the host operating system. Regression fixtures cover both forms.

## Adding regressions

When a real subtitle/archive exposes a new failure, add the smallest representative filename/archive shape and expected result to the suite before fixing the parser. This keeps each resolved edge case permanently covered.

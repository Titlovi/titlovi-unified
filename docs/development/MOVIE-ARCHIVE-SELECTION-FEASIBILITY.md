# Issue #21 — movie ZIP subtitle selection: feasibility study

**Status:** investigation only; implementation not approved or completed. Working branch: `research/movie-archive-choice-issue-21`. Target hosts: Emby Server 4.10.x and Jellyfin 12 / .NET 10.

## User requirement

For a movie subtitle media ID whose downloaded ZIP contains several versions, the viewer should be able to choose **any permitted internal subtitle file**, with its distinguishing filename/release details. The plugin must respect the existing SRT/SUB/TXT/SSA/ASS switches. Do not replace this request with automatic “pick the best match” behavior. Do not change episode handling.

## Confirmed source behavior (current main)

- `src/Common/TitloviEngine.cs`: `Search` returns one `CoreRemoteSubtitleInfo` per distinct Titlovi.com media ID. The API search record does not expose internal ZIP filenames/formats.
- `src/Common/TitloviEngine.cs`: `GetSubtitles` calls `DownloadAsync`, recursively extracts ZIP entries, filters by permitted formats, then uses `files[0]` for movies; episodes use `SelectEpisodeSubtitle`.
- Both `src/Emby/TitloviSubtitleProvider.cs` and `src/Jellyfin/TitloviSubtitleProvider.cs` implement `ISubtitleProvider.Search` returning a list of `RemoteSubtitleInfo`, and `ISubtitleProvider.GetSubtitles(id)` returning **one** `SubtitleResponse` with a **single** stream.
- The host search-result ID is URL-safe encoded and then restored; the current ID identifies the Titlovi.com media record and episode metadata, **not** an archive member.
- Automatic library subtitle downloads and manual subtitle search both call this provider contract; we must not introduce a human-selection requirement that breaks automatic downloading.

## Host/UI feasibility

The documented Emby `ISubtitleProvider` contract has only `Search(SubtitleSearchRequest)` and `GetSubtitles(string id)`. Jellyfin's code used in this repository exposes the same pattern. There is no confirmed method for a provider to return a ZIP and **instruct the existing host subtitle dialog to open a second picker**. Emby's documented manual workflow selects one result and invokes download; Jellyfin's remote-subtitle API separates search and selected-result download.

**Therefore a popup after `GetSubtitles` must not be promised.** We need to confirm whether a supported, documented host extension point exists for a custom interactive dialog, or use a separate, explicitly designed UI. A non-interactive provider callback cannot ask the viewer a question.

## Candidate designs and constraints

1. **Unconditional archive enumeration during Search** could create independently selectable host search rows, but only if we download ZIPs *while searching*. This would add requests for each search result, violate the intended conservative API behavior, increase latency and potentially load Titlovi.com. **Do not implement without explicit user approval.**
2. **Post-download host-native popup** would match the user's preferred flow but is **not supported by the known `ISubtitleProvider` callback contract**. Investigate genuine host-specific extensibility before treating this as possible.
3. **Opt-in, separate archive inspection UI** might download one user-selected archive, enumerate and present accepted candidates, then install a chosen subtitle. This needs a new custom interface/action, user-accessible authentication/authorization, safe per-user or per-session archive state, stable file identifiers, cleanup, and host-specific UI/endpoint support. The current `GetSubtitles` hook alone cannot open this UI. This is a research option, not an approved implementation.
4. **Automatic best-match selection** would not satisfy the requirement and must not be silently substituted.
5. **Installing every extracted file** directly would require bypassing the one-stream provider contract through host-specific filesystem or metadata operations; may be unsafe and may break naming/language expectations. Not an approved workaround.

## Feasible preparatory work without extra network activity

- Extract a pure, deterministic *archive inventory* routine from the existing ZIP processing flow: safe entry identity, display names, recognized formats, nested archive source path and multipart grouping, filtering by existing configuration. Maintain the current download path unchanged until host UX is decided.
- Extend the opaque, URL-safe subtitle ID design **only if** there is an actual supported selection step. Treat archive paths as untrusted; avoid path traversal and duplicate/misleading names. Never rely on ZIP insertion order as file identity.
- Offline regression fixtures: single file; multiple releases within one ZIP; mixed enabled/disabled formats; nested ZIP; duplicate basenames in different folders; CD1/CD2 multi-part sets; archive without permitted subtitles; movie vs episode cases.
- Investigate how manual and automatic host downloads differ, whether they can be distinguished via provider input, and whether a new user-facing action can be offered without breaking default server workflows.
- Measure network requests before/after. No speculative search-time prefetch, retries or additional API traffic without a separate explicit decision.

## Decision gate before coding behavior

1. Verify with supported **Emby 4.10.x** and **Jellyfin 12** APIs/UX whether the existing host can display a second picker **after one chosen ZIP has been downloaded**.
2. If not, prepare a concrete *opt-in* UX mock/flow for user approval (including manual vs automatic download implications).
3. Agree how “all files” should represent multipart CD1/CD2 archives.
4. Then implement and test on this branch, with all 166 baseline regressions plus new cases and both host builds, locally where practical. GitHub Actions only when necessary and explicitly approved.

## Sources examined

- Canonical source: `src/Common/TitloviEngine.cs`, `src/Common/Models.cs`, `src/Common/SubtitleIdTransport.cs`, `src/Jellyfin/TitloviSubtitleProvider.cs`, `src/Emby/TitloviSubtitleProvider.cs`.
- Emby plugin API: https://dev.emby.media/reference/pluginapi/MediaBrowser.Controller.Subtitles.ISubtitleProvider.html
- Emby manual download workflow: https://www.emby.media/support/articles/Manual-Subtitle-Downloads.html
- Jellyfin subtitle API: https://kotlin-sdk.jellyfin.org/dokka/jellyfin-api/org.jellyfin.sdk.api.operations/-subtitle-api/index.html
- Tracking: https://github.com/Titlovi/titlovi-unified/issues/21

**No production behavior, version, release or catalogue modification is included in this investigation.**

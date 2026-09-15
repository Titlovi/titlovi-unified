# Titlovi.com.Unified regression corpus

The automated suite is deliberately offline. It uses synthetic subtitle text and real in-memory ZIP byte streams, while its filenames and scenarios are based on regressions observed during development of the Emby/Jellyfin provider.

## Current regression families

- **Seinfeld** — `01xNN`, dotted `SxxEyy`, leading ordinals (`001 - ...`), trailing spaces, whole-series packs with season folders, Season=0 package metadata, series IMDb fallback and exact episode selection.
- **Monty Python's Flying Circus** — legacy `01x02_-_sex_and_violence` style.
- **Blackadder** — Roman season numerals, separated season/episode tokens and compact multi-episode bundles.
- **Berserk** — Season 0 search requests.
- **Disney Animated Shorts** — large year-like season numbers such as 1947.
- **B/H/S searches** — primary-language priority, actual API-reported language, deduplication by media ID and exact `sr`/`hr`/`bs` metadata.
- **Archives** — season packs, whole-series folders, nested ZIPs, raw subtitle payloads, exact multi-episode ranges and rejection of grouped subtitles for ordinary single-episode video files.
- **Formats/encoding** — SRT, MicroDVD SUB, SSA, ASS, TXT, UTF-8/BOM, UTF-16 and Windows-1250 to UTF-8.
- **Search orchestration** — cached/expired tokens, title fallback after IMDb miss, zero-result behavior and release-match ordering.
- **Release matching** — source family, release group, explicit FPS, cut/edition conflicts, weak resolution/codec weighting and multiple release descriptions in one API field.

## Reference series for future corpus work

These series are useful starting points when a broader set of real archive/file-name patterns is needed because they are old or long-running, popular in the B/H/S region and likely to contain legacy subtitle packs:

- Friends
- Seinfeld
- The Simpsons
- The X-Files
- Stargate SG-1
- CSI
- Supernatural
- NCIS
- Grey's Anatomy
- House M.D.
- Lost
- Bones
- Two and a Half Men
- How I Met Your Mother
- The Sopranos
- Only Fools and Horses
- Blackadder
- The Wire

A newly discovered real-world failure should first become a regression case and then be fixed. This keeps every resolved edge case permanently covered.

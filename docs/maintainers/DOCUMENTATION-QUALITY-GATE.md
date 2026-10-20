# Project-wide documentation quality gate

Documentation consistency and factual accuracy are part of **Titlovi.com Unified project quality**, not optional cosmetic maintenance. The audit covers the canonical source plus both distribution repositories:

- [Titlovi/titlovi-unified](https://github.com/Titlovi/titlovi-unified)
- [Titlovi/titlovi-jellyfin](https://github.com/Titlovi/titlovi-jellyfin)
- [Titlovi/titlovi-emby](https://github.com/Titlovi/titlovi-emby)

## When to run the gate

Review documentation **when there is a concrete reason**: preparing or completing a release, changing a supported host/ABI/installation procedure, modifying packaging or the Jellyfin catalogue, migrating repos, introducing a significant user-visible feature or limitation, correcting author/owner attribution, receiving a report of contradictory instructions, or preparing a project-wide quality review.

Small internal edits with no user-facing effects do not require a repeated full three-repository audit. This gate is **on demand**, not a scheduled GitHub Actions job or a reason to generate releases.

## Scope

1. Enumerate **every tracked `.md` and `.txt` file** on the intended branch in all three repositories; do not assume only README/INSTALL files matter. Include files under `docs/`, `tests/`, `scripts/`, maintainer directories and release notes.
2. Classify each document as **current user-facing guidance**, **current developer/maintainer guidance**, **historical archive/release-preparation record**, or **machine/helper input**. Clearly label older plans as historical; do not rewrite their original decisions into fabricated history.
3. Check the English, Croatian (Latin script) and Serbian (Cyrillic script) user-facing sets for **substantive equivalence**: supported versions, installed artifact names, steps, safety/rollback, features, known limitations, and warnings. Use appropriate natural language, not mechanical transliteration.
4. Check **cross-repository consistency**: canonical source location, separate Emby/Jellyfin distributions, source tag and commit, published release assets, GUIDs, Jellyfin plugin catalogue URL, `targetAbi`, owner/developer display, checksum and download instructions, release date/status, licensing, Matko acknowledgement and legacy 1.x preservation.
5. Verify technical claims against **current tracked source/build scripts and tested behavior**. Source-code declarations and verified release metadata are authoritative; do not describe planned work as implemented or assume other server versions are supported.
6. Validate **all relative Markdown links** against tracked paths. Check relevant external release, catalogue and source URLs against authoritative endpoints; report unverified external links rather than declaring them reachable without a check.
7. Inspect actual **release/package metadata**, not only README text, where availability and compatibility matter. Keep the Jellyfin catalogue's legacy versions intact and do not confuse `owner` in an installed ZIP with the catalogue-level developer attribution.
8. Verify configuration/security guidance: no exposed usernames/passwords/tokens, API credentials not requested publicly, backup before manual upgrade, no two active copies of the same Jellyfin plugin, and no recommendation to downgrade Jellyfin's database as plugin rollback.
9. Record what was inspected, confirmed discrepancies, exact edits, unresolved questions and evidence in a maintainer audit note or relevant PR.
10. If user-visible behavior or procedure changes, update all applicable languages and the appropriate official changelog. Do **not** bump plugin versions or launch CI builds solely because documentation changed.

## Required outcome

A quality review is complete only when:
- scope and reviewed repository revisions are recorded;
- actionable factual contradictions are corrected or tracked explicitly;
- user-facing translations agree in substance;
- relevant links and release/ABI/package claims are checked;
- historical documents are unmistakably historical;
- changes use focused PRs and do not touch release artifacts or catalogues unless separately justified and approved.

## Audit performed 2026-10-21

Audited the latest `main` inventories before changes: **32 files** in Unified, **9** in Jellyfin and **6** in Emby, **47 `.md`/`.txt` files total**.

- Verified relative Markdown link targets against the tracked Git trees for all 47 files: **no missing local targets found**.
- Compared three-language README, INSTALL, USER-GUIDE and BUILD content and package-specific documentation for both hosts.
- Checked official Emby and Jellyfin `v2.0.0` releases: assets exist, and published SHA-256 values match the Unified changelog.
- Checked Jellyfin catalogue: developer `mihvel`, original GUID, new 2.0.0.0 ABI `12.0.0.0`, all nine historical 1.x entries preserved.
- Confirmed a gap in marking the 2026-10-19 migration audit and one release-notes preparation file as historical; fixed as part of this documentation review.
- Simplified repetitive BUILD banners and corrected the regression-suite verification date to 2026-10-20.
- The Jellyfin installed ZIP uses `owner: Titlovi.com`, while the public catalogue uses `owner: mihvel`. These are different metadata surfaces; the public Jellyfin plugin details were confirmed in practice to show `mihvel`. Do not alter immutable 2.0 package bytes merely to synchronize these fields.
- Historical preparatory files intentionally still contain obsolete *past-tense checkpoints*. Their archive notices identify the documents as non-current; current instructions live in README, INSTALL and CHANGELOG.
- External URLs were checked selectively (official GitHub release metadata and official catalogue), **not exhaustively for every external hyperlink**; any future full URL crawl must report reachability results separately.

This is a documentation-only quality gate, not a release gate sign-off for untested future software changes.

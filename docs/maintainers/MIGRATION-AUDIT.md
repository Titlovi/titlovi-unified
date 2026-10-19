# Official source migration completeness audit — 2026-10-19

**Status: Git source/PR/branch audit and historical decision reconciliation completed; official 2.0.0.0 release review NOT yet complete.** This is a
historical verification checkpoint, **not** an assertion that all release
acceptance tests have passed.

## Scope and immutable reference points

- Former development repository: `mihvel/Titlovi.com.Unified`.
- Former selected source ref: `migration/official-release-preparation`,
  commit `1621749f5901dd4c056a16b9590f35659ac98166`.
- Official import commit: `8161e0686cd22e6e71f5c88f38712bd14bb0df18`,
  with the former source commit as its parent.
- **Both source/import commits have exactly the same Git tree SHA:**
  `8fd979085639ab8e21d8a4ab85893f9d26280407`.
- Official `main` at audit: `8949da3253ced1dd8b8ba2276c608a9895ddd6db`.
  The later official PRs #1 and #2 changed CI/documentation and layout,
  not the source or test contents **at that historical checkpoint**;
  PR #3 subsequently adds the recovered, missing functionality.
- **All 49 Git-tracked file paths under `src/` and `tests/` in the selected
  original source tree had identical Git blob SHAs at the audited official `main`
  commit `8949da3` (before PR #3).**
  This establishes exact file-content parity **with the selected migration
  tree**, not necessarily with every unmerged historical development branch.

## Original pull request inventory

At this checkpoint the former development repository has **14 PRs**:

| Former PRs | Git state | Migration disposition |
| --- | --- | --- |
| #1–#11 and #14 | Merged | Original `main` is an ancestor of the selected migration ref; changes are included in that ancestry (some later revised). |
| [#12 — host-specific Search X-App](https://github.com/mihvel/Titlovi.com.Unified/pull/12) | **Open; not merged** | **Confirmed omission.** Original commit `9b4f942e1c1a703924fbcc05345d216dae02048d` was recovered in [official PR #3](https://github.com/Titlovi/titlovi-unified/pull/3), merged on 2026-10-19 after passing CI, including `SearchRequestHeaderTests.cs`. |
| [#13 — official Jellyfin identity](https://github.com/mihvel/Titlovi.com.Unified/pull/13) | Open; not merged as that PR | Its source branch is behind the selected migration ref; later implementation of the original GUID and legacy credentials import and regression cases exists in official `main`. The old PR remains a historical record. |

**Root cause of #12 omission:** Migration selected the preparation branch, but did
not first inventory and resolve all unmerged PRs. The Git import itself
preserved the *selected* tree exactly; it was the selection/completeness
control that failed.

## Former branch inventory

All **22** heads in the former repository were inventoried against
`migration/official-release-preparation`. Ten heads are ancestors
(`behind`), one is the selected migration head itself, and eleven are
divergent. Branch divergence alone is **not** proof that a change is missing;
many historical branches have older baselines, were merged by other means,
or were intentionally experimental.

### Ancestors and selected source

- `main`
- `docs/readme-refresh`
- `feature/official-jellyfin-identity`
- `fix/aka-series-title-matching`
- `fix/movie-title-fuzzy-collisions`
- `fix/rebelove-aka-anchor-filter`
- `fix/reject-cross-series-fuzzy-matches`
- `fix/url-safe-subtitle-ids-monty-python-regression`
- `migration/legacy-credentials-fix`
- `migration/official-jellyfin-identity`
- `migration/official-release-preparation` (selected head)

### Divergent heads: disposition

| Branch | Assessment |
| --- | --- |
| `feature/host-specific-search-x-app` | **Missed accepted functionality, now recovered.** Original source and regression test merged via official PR #3. |
| `feature/bsh-alternative-priority` | Earlier B/S/H search ordering and tests; relevant behavior exists in selected shared implementation. No separate missing new file identified. |
| `feature/linux-path-compatibility` | Historical cross-platform path/host/CI work; selected source has the later cross-platform support. Branch also retains older scripts. |
| `fix/legacy-obj-migration` | Earlier build-folder fix; selected source has the build exclusions and cleaning scripts. |
| `refactor/build-layout` | Earlier build layout; selected source has the later Windows/Linux build script layout. |
| `docs/cross-platform-audit` | Earlier documentation and build script entry points; superseded by present documentation and build layout. |
| `migration/jellyfin-upgrade-validation` | **Test harness intentionally not copied.** Separate experimental Jellyfin 10.11-to-12 *server database* upgrade rehearsal had a blocked result; this is not a proved end-to-end server upgrade. Historical harness remains in the old repository. |
| `migration/test-catalogue` | Temporary preview catalogue build and trial; not production release infrastructure. |
| `migration/test-catalogue-v1991` | Temporary 1.99.1 upgrade/credentials preview tooling; corresponding credentials behavior is now implemented and separately validated, but preview publishing machinery is not production infrastructure. |
| `migration/test-catalogue-publisher` | Experimental opt-in test-catalogue publish/dry-run workflow; not to be enabled as official release publishing. |
| `planning/official-migration` | Former living migration plan reconciled against [the official release handoff](release-preparation/HANDOFF.md#historical-migration-plan-decision-reconciliation-2026-10-19) and the installation/release drafts on 2026-10-19. Relevant technical/release decisions are recorded there; interim test logs and superseded preparatory workflows remain historical, not production instructions. |

The old repository and its branch references must be preserved for historical
traceability until maintainers explicitly decide on their long-term archive.

## Recovery and remaining gates

1. [Official PR #3](https://github.com/Titlovi/titlovi-unified/pull/3)
   merged after passing Windows/Linux regression and both host builds plus
   Linux runtime smoke. It restored only the four changed C# files and the
   missing Search-header regression test from original commit `9b4f942`.
   The public
   official release changelog intentionally does not import an unrelated
   historical unreleased changelog entry.
2. PR #3's Windows/Linux regression and host-build jobs and Linux runtime smoke
   passed before merging. The official 2.0 release candidate must still
   undergo its own fresh build and acceptance tests.
3. **Completed (2026-10-19):** reconciled the old migration plan's decisions
   with the [official release handoff](release-preparation/HANDOFF.md) and
   install/release drafts. Historical preview-publishing and server-upgrade
   rehearsal scripts remain in the old repository; they are not production
   release mechanisms. Final release sign-off still requires live acceptance.
4. Keep published 1.x Jellyfin catalogue and release history unchanged.
5. Perform final official 2.0 packaging, compatibility, fresh-install and
   actual original-plugin upgrade acceptance checks independently. Explicit
   maintainer approval is still required before any tag/release/catalogue update.

This audit covers remotely recorded GitHub refs, PRs, source files and test
files; it **does not** establish the absence of uncommitted local work, nor
replace live functional testing. No release or catalogue changes were made.

# Documentation maintenance policy

This project is the canonical source for the Titlovi.com Emby and Jellyfin plugins. Documentation is part of the product and must be **accurate, comprehensible, current and consistent** across languages.

## Languages

| Language | Location | Script |
| --- | --- | --- |
| English (reference) | `README.md` at root; `docs/en/` for other user guides | Latin |
| Croatian | `docs/hr/` | Latin |
| Serbian | `docs/sr-Cyrl/` | Cyrillic |

Additional translations, including Macedonian (`docs/mk/`), can be added later without changing existing locations. The main English README is intentionally kept in the root for GitHub visitors; no duplicate English README is maintained under `docs/en/`.

## Document ownership

- **User-facing:** `README.md`, `docs/{en,hr,sr-Cyrl}/INSTALL.md`, `USER-GUIDE.md` and `BUILD.md`. The Croatian/Serbian README files link to the same facts in their respective languages.
- **Technical:** `docs/development/` — architecture and filename/archive matching notes. English only unless translations become necessary.
- **Maintainers:** `docs/maintainers/release-preparation/` contains unpublished release drafts; `docs/maintainers/archive/` contains historical material not intended for users.
- **Maintenance policy:** [Bug triage and maintenance releases](BUG-TRIAGE-AND-RELEASE-POLICY.md) defines issue priorities, investigation standards and release thresholds.
- **Root:** `README.md`, `CHANGELOG.md`, `LICENSE` and `NOTICE.txt` (plus project/build configuration files).

Historical changelog entries from the private/unofficial development repository are retained at `docs/maintainers/archive/CHANGELOG-PRE-OFFICIAL.md`. The root `CHANGELOG.md` covers official releases starting with 2.0.0.0.

## Editorial and release rules

1. **English is authoritative.** Update the Croatian and Serbian versions whenever user-visible behavior, requirements, installation procedures or release status changes.
2. Use idiomatic standard Croatian in Latin script and standard Serbian in Cyrillic, not mechanically transliterated text. Preserve filenames, commands, IDs and URLs exactly.
3. Distinguish **implemented behavior**, **proposed features** and **platform limitations**. Never imply a package or update is available until its real release asset is published and verified.
4. Jellyfin 12 / .NET 10 and Emby 4.10.x are the upcoming Unified release targets. Preserve all earlier Jellyfin 1.x catalogue entries. Emby distribution is a manually installed DLL, not a Jellyfin-style repository.
5. Verify all relative links, installation paths, command examples, filenames and compatibility claims. All build commands execute from the **repository root**, even though the documentation lives in `docs/`.
6. Keep the GPL-3.0 license and the attribution to the original Jellyfin developer. Do not post API credentials or configuration XML in documentation or test fixtures.
7. Release notes should be translated when **actual releases** are prepared. Do not translate obsolete internal developmental changelogs for public end users.
8. Review translated material for natural technical terminology and factual equivalence before merging.

Repository documentation reorganization must not change source code, plugin version numbers or published catalogue entries.

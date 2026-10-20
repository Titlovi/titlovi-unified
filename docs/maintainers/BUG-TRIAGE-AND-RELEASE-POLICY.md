# Bug triage and maintenance release policy

This policy applies to both **Titlovi.com for Emby** and **Titlovi.com for Jellyfin**, built from the canonical [Unified source repository](https://github.com/Titlovi/titlovi-unified). The goal is to maintain stable public releases while responding proportionately to real user problems.

## Guiding principles

- **A bug report does not automatically justify a new release.** A failure involving one unusual subtitle file or a single episode may be real and worth fixing, but normally does not warrant an emergency update.
- **Separate investigation, implementation, and publication.** A confirmed fix may remain on a development branch until enough tested changes justify a maintenance release.
- Preserve working behavior. Prefer fixes supported by actual subtitle/API responses or archive samples over speculative logic changes.
- Do not add automatic API retries, extra request volume, aggressive fallback searches, or strict release matching without an explicitly reviewed requirement.
- Keep host-specific fixes in `src/Emby` or `src/Jellyfin`; place shared, host-neutral fixes in `src/Common`. Do not introduce a separate runtime Common/Core assembly.
- Treat accepted host limitations, especially the Emby 4.10 alternative-language filename suffix limitation, as documented limitations rather than shared-code bugs to work around.

## Priorities

| Priority | Examples | Response and release expectation |
| --- | --- | --- |
| **P0 — Critical** | Server crash attributable to the plugin, severe security exposure, destructive behavior, widespread service disruption | Investigate immediately; prepare an urgent, narrowly scoped, fully validated hotfix when confirmed. |
| **P1 — High** | Authentication, search or downloads systematically fail for many users on a supported host; serious common regression | Prioritize investigation and a tested fix; expedite a maintenance release if user impact warrants it. |
| **P2 — Medium** | Reproducible problem affecting a specific archive layout, episode pattern, subtitle encoding or other meaningful subset of cases | Fix in the normal development cycle, preferably with a regression test; batch into a planned release. |
| **P3 — Low** | One unusual subtitle/archive, one problematic episode with a workaround, minor UI or documentation issue | Record and investigate as worthwhile; do not make a dedicated release solely for this. |

Priority reflects **impact and urgency**, not proof that the plugin is at fault. Reassess if the frequency, affected users or severity changes.

## Intake and investigation

1. **Record the report** in GitHub Issues with affected host and server version, installed plugin version, media type, expected versus actual behavior, reproducible steps and a clear symptom.
2. **Protect private data.** Ask for sanitized logs or minimal samples. Never request public credentials, API tokens, personal account XML or unredacted request headers.
3. **Classify the cause:** Unified plugin, Emby/Jellyfin host behavior, Titlovi.com API or availability, malformed/unusual subtitle content, configuration/environment, or insufficient evidence.
4. **Check scope:** one media ID or episode, a repeatable class of cases, a supported host release, or a broad outage. Different Titlovi media IDs must stay distinct; release matching remains ranking rather than a filter; retain the actual language reported by Titlovi.com.
5. **Reproduce before changing code** when feasible. Preserve redacted, minimal real-world samples and record the expected behavior.
6. **Fix the narrowest confirmed cause.** Add or update a regression test when feasible; test for accidental changes to currently working scenarios.
7. **Track disposition:** confirmed bug, host/API limitation, needs information, duplicate, won't fix, or fixed pending release. Document any workaround separately from a permanent fix.

## Publication decision

A code fix is **not** the same as a release decision.

- Normally batch compatible **P2/P3** fixes into one maintenance release rather than publishing each fix independently.
- Release a **P0** hotfix promptly after confirming the problem and validating the proposed remedy.
- Consider an expedited **P1** maintenance release if the impact is broad or serious, balancing urgency with regression risk.
- A lone edge-case subtitle or episode does **not** justify an immediate release unless its impact proves materially wider.
- Avoid version bumps until preparing an actual release. Maintain a short changelog of accepted changes awaiting publication.
- Require the regression suite before declaring a functional fix complete. For compilation or packaging changes, build and validate **both Emby and Jellyfin**. Verify the affected host in practice when appropriate.
- Publish each host distribution deliberately, with appropriate release notes and package checks. Preserve Jellyfin's existing plugin GUID, catalogue URL and historical releases. Do not silently expand support beyond **Jellyfin 12 / .NET 10** or **Emby 4.10.x**.

## Review cadence

Review open issues periodically, especially after release, for repeated failure patterns or rising impact. The number of reports alone is not a release trigger: prioritize verified severity, prevalence, availability of workarounds and confidence in the fix.

This policy is maintainers' guidance, not a promise of fixed response or release dates.

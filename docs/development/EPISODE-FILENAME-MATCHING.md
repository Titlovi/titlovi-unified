# Episode filename matching

This describes how the platform-neutral **Common engine** chooses an episode subtitle when a downloaded archive contains several candidate files.

## Selection priority for a normal single-episode video

1. Explicit season and episode (`S01E01`, `1x01`, etc.).
2. Legacy individual forms using separated tokens or Roman season numbers.
3. Episode-only numbering (`E01`, `Part 1`, `01`, `001`).
4. Episode-title fallback.
5. A sole subtitle file, **only if it is not explicitly a multi-episode bundle**.

Examples of supported older individual filenames:

```text
BlackAdder_I_01                              -> S1E1
BlackAdder_II_03                             -> S2E3
BlackAdder_IV_06                             -> S4E6
season_01_blackadder_season_01_episode_01   -> S1E1
```

Examples of grouped subtitles:

```text
The Black Adder Ep 1,2,3-Srp.sub  -> E1-E3 bundle
BlackAdder_I_123_ser.sub          -> S1 E1-E3 bundle
BlackAdder_I_456_ser.sub          -> S1 E4-E6 bundle
```

## Safety rule

A subtitle explicitly covering **multiple episodes is not a valid match for an ordinary single-episode video**. A grouped subtitle is considered only when the host request itself identifies an exact multi-episode range. For instance, a video covering E01–E03 may use an E1,2,3 bundle, while an E01-only video may not.

Whole-season and whole-series archives, including nested ZIP files, are examined using the same rules. The engine attempts to select the exact requested season and episode rather than taking the first subtitle entry found in an archive.

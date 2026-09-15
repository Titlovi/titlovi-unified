# Manual Linux end-to-end validation

This directory contains live end-to-end validation for both supported Linux hosts. These workflows are intentionally **not** run on ordinary pushes or pull-request updates.

The automated regression suite and Linux runtime smoke do not contact Titlovi.com. The live validations are the final integration checks: they start a real host server, create a disposable media library, configure the plugin with repository secrets, search Titlovi.com through the host API, download subtitles through the host, and verify the files saved by that host.

## Required GitHub Actions secrets

Create these repository Actions secrets:

- `TITLOVI_USERNAME`
- `TITLOVI_PASSWORD`

In GitHub, open:

`Settings -> Secrets and variables -> Actions -> New repository secret`

Do not put the credentials in a workflow file, issue, pull request, commit, label, comment, or Actions input.

## Triggering a live validation

The workflows are available on `main` and can be started explicitly from the GitHub Actions UI with **Run workflow**:

- `Jellyfin Linux end-to-end`
- `Emby Linux end-to-end`

For a future same-repository pull request, either workflow can also be triggered deliberately by adding its exact label:

- Jellyfin 12: `run-live-e2e`
- Emby 4.10: `run-emby-live-e2e`

Each pull-request workflow checks both the exact label and that the pull-request head repository is this repository. Ordinary pushes, PR synchronization, comments, reviews, and unrelated labels do not run either live Titlovi.com test.

If a label-triggered validation needs to be run again on the same pull request, remove its label and add it again.

## Jellyfin 12 validation

The Jellyfin validation uses the official `ghcr.io/jellyfin/jellyfin:12.0` image and:

1. builds `Jellyfin.Titlovi.dll` on Ubuntu;
2. loads the plugin in Jellyfin 12;
3. creates one real one-second movie file and one real one-second TV episode;
4. supplies local NFO metadata for The Matrix (1999), Friends (1994), and Friends S01E01;
5. completes the Jellyfin setup wizard and creates a disposable administrator;
6. configures the Titlovi plugin with the two Actions secrets and verifies the saved configuration;
7. creates real Jellyfin movie and TV libraries on Linux paths under `/media`;
8. verifies that Jellyfin loaded the movie and parent-series IMDb ids from NFO;
9. searches Serbian subtitles for the movie and episode through Jellyfin;
10. verifies in plugin diagnostics that the episode search resolved parent-series IMDb `tt0108778` through Jellyfin `FindByPath()`;
11. downloads one movie subtitle and one episode subtitle through Jellyfin;
12. verifies that Jellyfin saved non-empty subtitle files next to the Linux media paths;
13. verifies that the downloaded subtitle files are valid UTF-8 when conversion is enabled.

The Jellyfin validation completed successfully in GitHub Actions run `35006721961`.

## Emby 4.10 validation

The Emby validation uses the official `emby/embyserver:4.10.0.40` image and:

1. copies the exact Emby 4.10 reference assemblies from the official Linux image;
2. builds `Emby.Titlovi.dll` on Ubuntu and loads it in the official Emby container;
3. creates the same disposable The Matrix (1999) and Friends S01E01 media files and NFO metadata on `/media` Linux paths;
4. completes the Emby startup wizard and creates a disposable administrator;
5. configures the Titlovi plugin with the two Actions secrets and verifies the saved configuration;
6. creates real Emby movie and TV libraries with `SaveSubtitlesWithMedia` enabled;
7. verifies that Emby indexed the movie and parent Friends series with the expected IMDb ids;
8. searches Serbian subtitles for the movie and episode through Emby;
9. verifies in plugin diagnostics that the Emby adapter received the real Linux episode `MediaPath`;
10. records whether Emby 4.10 supplied the parent-series IMDb through `SeriesProviderIds` or used the existing series-title fallback path;
11. downloads one movie subtitle and one episode subtitle through Emby;
12. verifies that Emby saved non-empty subtitle files next to the Linux media paths;
13. verifies that the downloaded subtitle files are valid UTF-8 when conversion is enabled.

The Emby workflow normalizes the disposable startup-wizard payload to the current Emby 4.10 schema before running the E2E harness. This is test-host setup only and does not modify plugin behavior.

The Emby validation completed successfully in GitHub Actions run `35013082174`. In that run Emby 4.10 supplied parent-series IMDb `tt0108778` through `SeriesProviderIds`; the title fallback was therefore not needed. Serbian Titlovi.com results were returned for both The Matrix and Friends S01E01, and Emby saved `The Matrix (1999).sr.srt` and `Friends S01E01 - The One Where Monica Gets a Roommate.sr.srt` next to the Linux media files as non-empty valid UTF-8.

The validation still accepts either legitimate host behavior for episode parent-series metadata. `SeriesProviderIds` is used when Emby supplies it; otherwise the existing shared engine behavior searches by `SeriesName`. The validation does not introduce a Linux-specific or host-specific workaround.

## Request volume

B/S/H alternative-language searching is disabled for both live validations to keep the number of real Titlovi.com requests low. Each successful run performs one movie search and one episode search, followed by one download for each item.

## Credentials and cleanup

Repository secrets are available to these same-repository pull-request workflows, while GitHub does not pass Actions secrets to pull requests from forks. Each workflow also checks that the pull-request head repository is this repository before the live job can run.

GitHub masks repository secret values in Actions logs. The scripts additionally register the Titlovi.com username and password, generated administrator password, and host access token as masked values where applicable.

Credentials are written only to disposable host configuration inside the ephemeral runner. Containers, temporary media files, generated plugin configuration, administrator credentials, access tokens, search responses, and downloaded subtitles are removed when the job exits.

For additional caution, the repository secrets can be deleted again after the required live validations are complete.

## External-service failures

These validations deliberately use the real Titlovi.com service. A failure can therefore represent either a plugin/host regression or an external-service condition such as account restrictions, Titlovi.com availability, download quota, or access restrictions affecting the GitHub-hosted runner. Inspect the host and plugin diagnostics before treating a live-test failure as a regression.

The ordinary regression suite and Linux runtime smoke remain deterministic and do not depend on Titlovi.com.

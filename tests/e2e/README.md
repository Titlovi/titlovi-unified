# Manual Linux end-to-end validation

This directory contains live end-to-end validation for both supported Linux hosts. These workflows are intentionally **not** run on ordinary pushes or pull-request updates.

The automated regression suite and Linux runtime smoke do not contact Titlovi.com. The live validations are final integration checks: they start a real host server, create a disposable media library, configure the plugin with repository secrets, search Titlovi.com through the host API, download subtitles through the host, and verify the files saved by that host.

The workflows prepare plugin artifacts through the canonical `scripts/linux` build/package entry points and use the resulting `dist/` artifacts. The E2E harnesses therefore exercise the same plugin packaging contract as normal Linux builds.

## Required GitHub Actions secrets

Create these repository Actions secrets:

- `TITLOVI_USERNAME`
- `TITLOVI_PASSWORD`

Do not put the credentials in a workflow file, issue, pull request, commit, label, comment, or Actions input.

## Triggering a live validation

The workflows can be started explicitly from the GitHub Actions UI with **Run workflow**:

- `Jellyfin Linux end-to-end`
- `Emby Linux end-to-end`

For a same-repository pull request, either workflow can also be triggered deliberately by adding its exact label:

- Jellyfin 12: `run-live-e2e`
- Emby 4.10: `run-emby-live-e2e`

Ordinary pushes, PR synchronization, comments, reviews and unrelated labels do not run either live Titlovi.com test.

## Jellyfin 12 validation

The Jellyfin validation uses the official `ghcr.io/jellyfin/jellyfin:12.0` image and validates:

1. canonical Linux Jellyfin build/package output;
2. plugin loading in Jellyfin 12;
3. disposable The Matrix (1999) and Friends S01E01 media/NFO metadata;
4. plugin configuration through repository secrets;
5. real Linux media paths under `/media`;
6. movie and parent-series IMDb metadata;
7. Serbian movie and episode subtitle search through Jellyfin;
8. parent-series IMDb resolution through Jellyfin `FindByPath()`;
9. subtitle download and host-side save;
10. non-empty valid UTF-8 output when conversion is enabled.

The previously recorded successful live run is `35006721961`.

## Emby 4.10 validation

The Emby validation uses the official `emby/embyserver:4.10.0.40` image and validates:

1. canonical Linux Emby build output using exact Emby 4.10 references;
2. plugin loading in the official Emby container;
3. disposable The Matrix (1999) and Friends S01E01 media/NFO metadata;
4. plugin configuration through repository secrets;
5. real Linux media paths under `/media`;
6. expected movie and parent-series IMDb metadata;
7. Serbian movie and episode subtitle search through Emby;
8. the real Linux episode `MediaPath` reaching the adapter;
9. `SeriesProviderIds` when supplied, with the existing `SeriesName` fallback still accepted;
10. subtitle download, host-side save and valid UTF-8 output.

The Emby workflow also normalizes the disposable startup-wizard payload to the current Emby 4.10 schema before running the E2E harness. This is test-host setup only and does not modify plugin behavior.

The previously recorded successful live run is `35013082174`.

## Request volume

B/S/H alternative-language searching is disabled for both live validations to keep real Titlovi.com request volume low. Each successful run performs one movie search and one episode search, followed by one download for each item.

## Credentials and cleanup

Repository secrets are available to same-repository pull-request workflows, while GitHub does not pass Actions secrets to pull requests from forks. Each workflow also verifies that the pull-request head repository is this repository before a label-triggered live job can run.

GitHub masks repository secret values in Actions logs. The harnesses additionally mask generated administrator credentials and host access tokens where applicable.

Credentials are written only to disposable host configuration inside the ephemeral runner. Containers, temporary media files, generated plugin configuration, administrator credentials, access tokens, search responses and downloaded subtitles are removed when the job exits.

## External-service failures

These validations deliberately use the real Titlovi.com service. A failure can represent either a plugin/host regression or an external-service condition such as account restrictions, Titlovi.com availability, download quota or access restrictions affecting the GitHub-hosted runner. Inspect host and plugin diagnostics before treating a live-test failure as a regression.

The ordinary regression suite and Linux runtime smoke remain deterministic and do not depend on Titlovi.com.

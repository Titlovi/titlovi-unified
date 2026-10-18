#!/usr/bin/env python3
"""Prepare a review-only Jellyfin catalogue update; never publish it.

Input: existing official gh-pages/repository.json plus validated local
2.0.0.0 candidate ZIP. Output: a new JSON file alongside the candidate.
No GitHub API calls, pushes, release creations, or destructive edits.
"""
from __future__ import annotations

import datetime
import hashlib
import json
from pathlib import Path
import sys
import zipfile

GUID = "6e25df50-638e-4109-a50b-03c14fc93fdd"
VERSION = "2.0.0.0"
ABI = "12.0.0.0"
ZIP_NAME = f"titlovi-com_{VERSION}.zip"
RELEASE_URL = (
    "https://github.com/Titlovi/titlovi-jellyfin/releases/"
    f"download/v2.0.0/{ZIP_NAME}"
)


def fail(message: str) -> None:
    raise SystemExit("OFFICIAL CATALOGUE VALIDATION FAILED: " + message)


def validate_existing(official: object) -> tuple[dict, list[dict]]:
    if not isinstance(official, list) or len(official) != 1:
        fail("Expected exactly one historical Titlovi.com plugin entry.")
    plugin = official[0]
    if not isinstance(plugin, dict) or plugin.get("guid") != GUID:
        fail("Original official Jellyfin plugin GUID is missing or unexpected.")
    if plugin.get("name") != "Titlovi.com":
        fail("Unexpected official plugin name.")
    old_versions = plugin.get("versions")
    if not isinstance(old_versions, list) or not old_versions:
        fail("Missing previously published Jellyfin versions.")

    seen = set()
    for entry in old_versions:
        if not isinstance(entry, dict):
            fail("Malformed historical release entry.")
        version = entry.get("version")
        if not isinstance(version, str) or version in seen:
            fail("Missing or duplicate historical release version.")
        seen.add(version)
        if version == VERSION:
            fail("Candidate version is already present in official catalogue.")
        if not entry.get("checksum") or not entry.get("sourceUrl"):
            fail(f"Historical release {version} is missing download/checksum.")
        if not entry["sourceUrl"].startswith(
            "https://github.com/Titlovi/titlovi-jellyfin/releases/download/"
        ):
            fail(f"Historical release {version} has unexpected download host.")

    # The entire already published set (including legacy 10.10.x ABI)
    # must remain available. Do not silently drop Matko releases.
    if not {"1.2.5.0", "1.2.4.0", "1.2.3.0", "1.2.2.0", "1.2.1.0",
            "1.1.2.0", "1.1.1.0", "1.1.0.0", "1.0.0.0"}.issubset(seen):
        fail("Some known previously released Matko versions are missing.")
    return plugin, old_versions


def validate_candidate(candidate: Path) -> str:
    if not candidate.is_file() or candidate.name != ZIP_NAME:
        fail("Candidate must be named titlovi-com_2.0.0.0.zip.")
    try:
        with zipfile.ZipFile(candidate) as archive:
            if set(archive.namelist()) != {"Jellyfin.Titlovi.dll", "meta.json", "thumb.png"}:
                fail("Unexpected candidate Jellyfin ZIP contents.")
            metadata = json.loads(archive.read("meta.json").decode("utf-8"))
    except (OSError, ValueError, zipfile.BadZipFile) as exc:
        fail(f"Cannot inspect candidate ZIP: {exc}")
    for key, value in {"guid": GUID, "version": VERSION, "targetAbi": ABI,
                       "name": "Titlovi.com"}.items():
        if metadata.get(key) != value:
            fail(f"Candidate ZIP metadata {key} mismatch.")
    return hashlib.md5(candidate.read_bytes()).hexdigest()


def main() -> None:
    if len(sys.argv) != 3:
        fail("Usage: prepare-official-catalogue.py ORIGINAL_REPOSITORY_JSON CANDIDATE_ZIP")
    original = Path(sys.argv[1]).resolve()
    candidate = Path(sys.argv[2]).resolve()
    if not original.is_file():
        fail("The unchanged original official gh-pages/repository.json is required.")

    official = json.loads(original.read_text(encoding="utf-8"))
    entry, historical = validate_existing(official)
    md5 = validate_candidate(candidate)

    # Preserve every historical version object verbatim (including its original
    # source URL, checksum, published timestamp, description and target ABI).
    current = {
        "changelog": (
            "Titlovi.com Unified for Jellyfin 12 / .NET 10. "
            "Preserves existing plugin identity and imports credentials from "
            "the original Titlovi.com plugin. Built from the shared Emby/Jellyfin source."
        ),
        "checksum": md5,
        "sourceUrl": RELEASE_URL,
        "targetAbi": ABI,
        "timestamp": datetime.datetime.now(datetime.timezone.utc).strftime(
            "%Y-%m-%dT%H:%M:%SZ"
        ),
        "version": VERSION,
    }
    updated = dict(entry)
    updated["owner"] = "Titlovi.com"
    updated["category"] = "Subtitles"
    updated["description"] = "Titlovi.com subtitle provider for Jellyfin."
    updated["overview"] = (
        "Unified Emby and Jellyfin subtitle provider. Supports Jellyfin 12 and "
        "retains existing Matko plugin users, settings and catalogue history."
    )
    updated["versions"] = [current, *historical]

    output = candidate.parent / "repository.review-only.json"
    if output.resolve() == original or output.exists():
        fail("Refusing to overwrite existing original or output catalogue.")
    output.write_text(json.dumps([updated], indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    verified = json.loads(output.read_text(encoding="utf-8"))
    if verified[0]["versions"][1:] != historical:
        fail("Historical release metadata was modified.")
    if verified[0]["versions"][0]["checksum"] != md5:
        fail("Candidate checksum changed unexpectedly.")

    print(f"Review-only catalogue generated: {output}")
    print(f"Preserved {len(historical)} historical official versions, unchanged.")
    print(f"Appended {VERSION} for ABI {ABI} with original GUID {GUID}.")
    print("Expected release ZIP URL:", RELEASE_URL)
    print("CANDIDATE ONLY — no release, tag or catalogue publication performed.")


if __name__ == "__main__":
    main()

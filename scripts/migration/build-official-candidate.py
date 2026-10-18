#!/usr/bin/env python3
"""Build a NOT-FOR-RELEASE Titlovi.com 2.0.0.0 candidate from isolated source.

This does NOT edit tracked source, push a tag, create a release, or publish a
Jellyfin catalogue. It is a migration review/verification aid only.
"""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[2]
VERSION = "2.0.0.0"
GUID = "6e25df50-638e-4109-a50b-03c14fc93fdd"
EMBY_GUID = "1dbf2e1e-3187-4f63-9a60-4ca7ace4232e"
TARGET_ABI = "12.0.0.0"
ZIP_NAME = f"titlovi-com_{VERSION}.zip"


def patch(root: Path, relative: str, changes: list[tuple[str, str]]) -> None:
    target = root / relative
    before = target.read_text(encoding="utf-8-sig")
    after = before
    for original, replacement in changes:
        count = after.count(original)
        if count != 1:
            raise RuntimeError(
                f"{relative}: expected EXACTLY one {original!r}; found {count}"
            )
        after = after.replace(original, replacement, 1)
    target.write_text(after, encoding="utf-8", newline="\n")


def validate_zip(path: Path) -> None:
    with zipfile.ZipFile(path) as archive:
        expected = {"Jellyfin.Titlovi.dll", "meta.json", "thumb.png"}
        names = archive.namelist()
        if len(names) != 3 or set(names) != expected:
            raise RuntimeError(f"Unexpected Jellyfin package contents: {names}")
        if any(archive.getinfo(p).file_size == 0 for p in expected):
            raise RuntimeError("Empty packaged Jellyfin asset")
        metadata = json.loads(archive.read("meta.json").decode("utf-8"))
        required = {
            "guid": GUID,
            "name": "Titlovi.com",
            "version": VERSION,
            "targetAbi": TARGET_ABI,
        }
        for key, value in required.items():
            if metadata.get(key) != value:
                raise RuntimeError(
                    f"Jellyfin metadata {key}={metadata.get(key)!r}; expected {value!r}"
                )
        if metadata.get("assemblies") != ["Jellyfin.Titlovi.dll"]:
            raise RuntimeError("Jellyfin meta.json has unexpected assembly allowlist")
        if metadata.get("imagePath") != "thumb.png":
            raise RuntimeError("Jellyfin meta.json must reference packaged thumb.png")


def main() -> None:
    if len(sys.argv) != 1:
        raise SystemExit("Usage: python3 scripts/migration/build-official-candidate.py")
    output = ROOT / "dist" / "official-candidate"
    if output.exists():
        shutil.rmtree(output)
    output.mkdir(parents=True)

    with tempfile.TemporaryDirectory(prefix="titlovi-official-candidate-") as temp:
        tree = Path(temp) / "src"
        shutil.copytree(
            ROOT,
            tree,
            ignore=shutil.ignore_patterns(
                ".git", ".build", "dist", "bin", "obj", ".vs", ".nuget",
                "__pycache__", "*.pyc"
            ),
        )

        # Only this throwaway source copy receives an official version/brand.
        # The canonical version remains 1.1.9 until release authorization.
        for host in ("Emby", "Jellyfin"):
            patch(tree, f"src/{host}/{host}.Titlovi.csproj", [
                ("<Version>1.1.9</Version>", f"<Version>{VERSION}</Version>"),
            ])
            patch(tree, f"src/{host}/Plugin.cs", [
                ('PluginVersion = "1.1.9"', f'PluginVersion = "{VERSION}"'),
                ('public override string Name => "Titlovi.com (Unofficial)"',
                 'public override string Name => "Titlovi.com"'),
                (f'"Unofficial Titlovi.com subtitle provider for {host}."',
                 f'"Titlovi.com subtitle provider for {host}."'),
                ('DisplayName = "Titlovi.com (Unofficial)"',
                 'DisplayName = "Titlovi.com"'),
            ])
            patch(tree, f"src/{host}/TitloviSubtitleProvider.cs", [
                ('"Titlovi.com (Unofficial)"', '"Titlovi.com"')
            ])
            patch(tree, f"src/{host}/Configuration/configPage.html", [
                ("<h1>Titlovi.com (Unofficial)</h1>", "<h1>Titlovi.com</h1>"),
                (f"Unofficial Titlovi.com subtitle provider for {host}.",
                 f"Titlovi.com subtitle provider for {host}."),
            ])

        patch(tree, "scripts/linux/build-jellyfin.sh", [
            ('PACKAGE_NAME="Titlovi.com (Unofficial)_$PACKAGE_VERSION"',
             'PACKAGE_NAME="Titlovi.com_$PACKAGE_VERSION"'),
            ('PACKAGE_ZIP="$DIST_DIR/Titlovi.com.Unified-$VERSION-Jellyfin-12.zip"',
             'PACKAGE_ZIP="$DIST_DIR/titlovi-com_$PACKAGE_VERSION.zip"'),
            ('"description": "Unofficial Titlovi.com subtitle provider for Jellyfin."',
             '"description": "Titlovi.com subtitle provider for Jellyfin."'),
            ('"name": "Titlovi.com (Unofficial)"',
             '"name": "Titlovi.com"'),
            ('"overview": "Unofficial Titlovi.com subtitle provider built from the shared Titlovi.com.Unified engine."',
             '"overview": "Titlovi.com for Jellyfin 12, built from the unified Emby and Jellyfin source tree."'),
            ('"owner": "Unofficial community plugin"',
             '"owner": "Titlovi.com"'),
            ('"Initial Titlovi.com.Unified release with shared Emby/Jellyfin subtitle logic, B/S/H alternative search, archive handling, encoding conversion and synchronization-aware release ranking."',
             '"Official Unified 2.0 preview: Jellyfin 12 support, preservation of Matko credentials, shared Emby and Jellyfin subtitle engine."'),
        ])
        patch(tree, "scripts/windows/Build.ps1", [
            ('$packageFolderName = "Titlovi.com (Unofficial)_$packageVersion"',
             '$packageFolderName = "Titlovi.com_$packageVersion"'),
            ('name = \'Titlovi.com (Unofficial)\'', "name = 'Titlovi.com'"),
            ("description = 'Unofficial Titlovi.com subtitle provider for Jellyfin.'",
             "description = 'Titlovi.com subtitle provider for Jellyfin.'"),
            ("overview = 'Unofficial Titlovi.com subtitle provider built from the shared Titlovi.com.Unified engine.'",
             "overview = 'Titlovi.com for Jellyfin 12, built from the unified Emby and Jellyfin source tree.'"),
            ("owner = 'Unofficial community plugin'", "owner = 'Titlovi.com'"),
            ("changelog = 'Initial Titlovi.com.Unified release with shared Emby/Jellyfin subtitle logic, B/S/H alternative search, archive handling, encoding conversion and synchronization-aware release ranking.'",
             "changelog = 'Official Unified 2.0 preview: Jellyfin 12 support, preservation of Matko credentials, shared Emby and Jellyfin subtitle engine.'"),
            ("if ($parsedMeta.name -ne 'Titlovi.com (Unofficial)')",
             "if ($parsedMeta.name -ne 'Titlovi.com')"),
            ('$zip = Join-Path $dist "Titlovi.com.Unified-$version-Jellyfin-12.zip"',
             '$zip = Join-Path $dist "titlovi-com_$packageVersion.zip"'),
        ])

        print("Building official-branded CANDIDATE from an isolated source copy", flush=True)
        print("No publication is performed. Release version changes are NOT committed.", flush=True)
        if os.name == "nt":
            subprocess.run(
                ["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass",
                 "-File", str(tree / "scripts" / "windows" / "Build.ps1"),
                 "-Target", "All"],
                cwd=tree, check=True
            )
        else:
            subprocess.run(["bash", "scripts/linux/build-all.sh"], cwd=tree, check=True)

        embedded = tree / "dist"
        jellyfin = embedded / "Jellyfin" / ZIP_NAME
        emby = embedded / "Emby" / "Emby.Titlovi.dll"
        if not jellyfin.is_file() or not emby.is_file():
            raise RuntimeError("Expected both Emby and Jellyfin build outputs")
        if emby.stat().st_size == 0:
            raise RuntimeError("Emby DLL is empty")
        validate_zip(jellyfin)

        for item in (jellyfin, emby):
            shutil.copy2(item, output / item.name)

    hashes = []
    for name in ("Emby.Titlovi.dll", ZIP_NAME):
        payload = (output / name).read_bytes()
        hashes.append((name, hashlib.md5(payload).hexdigest(), hashlib.sha256(payload).hexdigest()))
    (output / "SHA256SUMS.txt").write_text(
        "".join(f"{sha256}  {name}\n" for name, _, sha256 in hashes),
        encoding="utf-8",
    )
    (output / "MD5SUMS.txt").write_text(
        "".join(f"{md5}  {name}\n" for name, md5, _ in hashes),
        encoding="utf-8",
    )
    (output / "README-RELEASE-CANDIDATE.txt").write_text(
        "Titlovi.com Unified official 2.0.0.0 CANDIDATE, NOT A RELEASE.\n"
        "Built from temporary branded source. Do not distribute or publish before\n"
        "official Titlovi.com maintainer approval and final release validation.\n"
        "Jellyfin ABI: 12.0.0.0. Emby host: 4.10.x.\n"
        "Historical Matko releases and the official Jellyfin catalogue must be preserved.\n",
        encoding="utf-8",
    )
    for name, md5, sha256 in hashes:
        print(f"Validated {name}: MD5={md5} SHA256={sha256}", flush=True)
    print("OFFICIAL CANDIDATE BUILD OK — no release or catalogue publication", flush=True)


if __name__ == "__main__":
    main()

#!/usr/bin/env bash
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/_common.sh"

PROJECT="$ROOT/src/Jellyfin/Jellyfin.Titlovi.csproj"
BIN_ROOT="$BUILD_ROOT/Jellyfin/bin"
OBJ_ROOT="$BUILD_ROOT/Jellyfin/obj"
DIST_DIR="$DIST_ROOT/Jellyfin"
LOG="$BUILD_ROOT/logs/Jellyfin/build.log"
THUMB="$ROOT/src/Jellyfin/thumb.png"

require_dotnet10
require_command python3
ensure_nuget_config
clean_dir "$BIN_ROOT"
clean_dir "$OBJ_ROOT"
clean_dir "$DIST_DIR"
ensure_dir "$(dirname "$LOG")"
: > "$LOG"

VERSION="$(read_project_version "$PROJECT")"
PACKAGE_VERSION="$VERSION"
if [[ "$PACKAGE_VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  PACKAGE_VERSION="$PACKAGE_VERSION.0"
fi
PACKAGE_NAME="Titlovi.com (Unofficial)_$PACKAGE_VERSION"
PACKAGE_DIR="$DIST_DIR/$PACKAGE_NAME"
PACKAGE_ZIP="$DIST_DIR/Titlovi.com.Unified-$VERSION-Jellyfin-12.zip"

echo "Titlovi.com.Unified $VERSION - Jellyfin build (Linux)" | tee -a "$LOG"
echo "Target: Jellyfin 12.0 / .NET 10" | tee -a "$LOG"

run_logged "$LOG" dotnet restore "$PROJECT" --configfile "$NUGET_CONFIG" --nologo --verbosity minimal --ignore-failed-sources -p:NuGetAudit=false
run_logged "$LOG" dotnet build "$PROJECT" -c Release --no-restore --nologo --verbosity normal -p:NuGetAudit=false

BUILT_DLL="$BUILD_ROOT/Jellyfin/bin/Release/net10.0/Jellyfin.Titlovi.dll"
if [ ! -s "$BUILT_DLL" ]; then
  echo "ERROR: build succeeded but Jellyfin.Titlovi.dll was not found at $BUILT_DLL" >&2
  exit 1
fi
if [ ! -s "$THUMB" ]; then
  echo "ERROR: thumb.png was not found at $THUMB" >&2
  exit 1
fi

mkdir -p "$PACKAGE_DIR"
cp "$BUILT_DLL" "$PACKAGE_DIR/Jellyfin.Titlovi.dll"
cp "$THUMB" "$PACKAGE_DIR/thumb.png"

python3 - "$PACKAGE_DIR/meta.json" "$PACKAGE_VERSION" <<'PY'
import datetime
import json
import sys

path, version = sys.argv[1:]
meta = {
    "category": "Subtitles",
    "changelog": "Initial Titlovi.com.Unified release with shared Emby/Jellyfin subtitle logic, B/S/H alternative search, archive handling, encoding conversion and synchronization-aware release ranking.",
    "description": "Unofficial Titlovi.com subtitle provider for Jellyfin.",
    "guid": "6e25df50-638e-4109-a50b-03c14fc93fdd",
    "name": "Titlovi.com (Unofficial)",
    "overview": "Unofficial Titlovi.com subtitle provider built from the shared Titlovi.com.Unified engine.",
    "owner": "Unofficial community plugin",
    "targetAbi": "12.0.0.0",
    "timestamp": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "version": version,
    "status": "Active",
    "autoUpdate": False,
    "imagePath": "thumb.png",
    "assemblies": ["Jellyfin.Titlovi.dll"],
}
with open(path, "w", encoding="utf-8", newline="\n") as f:
    json.dump(meta, f, ensure_ascii=False, indent=2)
    f.write("\n")
PY

python3 - "$PACKAGE_DIR" "$PACKAGE_ZIP" <<'PY'
import os
import sys
import zipfile

source, destination = sys.argv[1:]
required = ["Jellyfin.Titlovi.dll", "meta.json", "thumb.png"]
for name in required:
    path = os.path.join(source, name)
    if not os.path.isfile(path) or os.path.getsize(path) == 0:
        raise SystemExit(f"Missing package file: {name}")
with zipfile.ZipFile(destination, "w", compression=zipfile.ZIP_DEFLATED) as zf:
    for name in required:
        zf.write(os.path.join(source, name), arcname=name)
with zipfile.ZipFile(destination, "r") as zf:
    names = sorted(zf.namelist())
if names != sorted(required):
    raise SystemExit(f"Unexpected ZIP contents: {names}")
PY

echo "SUCCESS" | tee -a "$LOG"
echo "Plugin folder: $PACKAGE_DIR" | tee -a "$LOG"
echo "Package ZIP:   $PACKAGE_ZIP" | tee -a "$LOG"

#!/usr/bin/env bash
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/_common.sh"

PROJECT="$ROOT/src/Emby/Emby.Titlovi.csproj"
REF_ROOT="$BUILD_ROOT/Emby/refs"
BIN_ROOT="$BUILD_ROOT/Emby/bin"
OBJ_ROOT="$BUILD_ROOT/Emby/obj"
DIST_DIR="$DIST_ROOT/Emby"
LOG="$BUILD_ROOT/logs/Emby/build.log"
EMBY_IMAGE="${EMBY_IMAGE:-emby/embyserver:4.10.0.40}"
REF_CONTAINER="titlovi-emby-build-refs-$$"

cleanup() {
  docker rm -f "$REF_CONTAINER" >/dev/null 2>&1 || true
}
trap cleanup EXIT

have_refs() {
  local dir="$1"
  [ -f "$dir/MediaBrowser.Model.dll" ] && \
  [ -f "$dir/MediaBrowser.Common.dll" ] && \
  [ -f "$dir/MediaBrowser.Controller.dll" ]
}

copy_refs() {
  local src="$1"
  clean_dir "$REF_ROOT"
  for dll in MediaBrowser.Model.dll MediaBrowser.Common.dll MediaBrowser.Controller.dll; do
    cp "$src/$dll" "$REF_ROOT/$dll"
  done
}

resolve_emby_refs() {
  if [ -n "${EMBY_REFERENCE_DIR:-}" ]; then
    if ! have_refs "$EMBY_REFERENCE_DIR"; then
      echo "ERROR: EMBY_REFERENCE_DIR does not contain all required MediaBrowser assemblies: $EMBY_REFERENCE_DIR" >&2
      exit 1
    fi
    echo "Using Emby reference assemblies from EMBY_REFERENCE_DIR: $EMBY_REFERENCE_DIR"
    copy_refs "$EMBY_REFERENCE_DIR"
    return
  fi

  local candidate
  for candidate in /opt/emby-server/system /usr/lib/emby-server/system /usr/lib/emby-server; do
    if have_refs "$candidate"; then
      echo "Using installed Emby reference assemblies from: $candidate"
      copy_refs "$candidate"
      return
    fi
  done

  require_command docker
  echo "Using Emby reference assemblies from official Linux image: $EMBY_IMAGE"
  if ! docker image inspect "$EMBY_IMAGE" >/dev/null 2>&1; then
    docker pull "$EMBY_IMAGE"
  fi
  clean_dir "$REF_ROOT"
  docker create --name "$REF_CONTAINER" "$EMBY_IMAGE" >/dev/null
  for dll in MediaBrowser.Model.dll MediaBrowser.Common.dll MediaBrowser.Controller.dll; do
    docker cp "$REF_CONTAINER:/system/$dll" "$REF_ROOT/$dll"
  done
  docker rm "$REF_CONTAINER" >/dev/null
}

require_dotnet10
ensure_nuget_config
resolve_emby_refs
clean_dir "$BIN_ROOT"
clean_dir "$OBJ_ROOT"
clean_dir "$DIST_DIR"
ensure_dir "$(dirname "$LOG")"
: > "$LOG"

echo "Titlovi.com.Unified - Emby build (Linux)" | tee -a "$LOG"
echo "Target: Emby 4.10.x" | tee -a "$LOG"

run_logged "$LOG" dotnet restore "$PROJECT" --configfile "$NUGET_CONFIG" --nologo --verbosity minimal --ignore-failed-sources -p:NuGetAudit=false
run_logged "$LOG" dotnet build "$PROJECT" -c Release --no-restore --nologo --verbosity normal -p:NuGetAudit=false

BUILT_DLL="$BUILD_ROOT/Emby/bin/Release/netstandard2.0/Emby.Titlovi.dll"
if [ ! -s "$BUILT_DLL" ]; then
  echo "ERROR: build succeeded but Emby.Titlovi.dll was not found at $BUILT_DLL" >&2
  exit 1
fi
cp "$BUILT_DLL" "$DIST_DIR/Emby.Titlovi.dll"

echo "SUCCESS" | tee -a "$LOG"
echo "Built plugin: $DIST_DIR/Emby.Titlovi.dll" | tee -a "$LOG"

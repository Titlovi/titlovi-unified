#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SMOKE_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/titlovi-linux-smoke.XXXXXX")"
EMBY_IMAGE="${EMBY_IMAGE:-emby/embyserver:4.10.0.40}"
JELLYFIN_IMAGE="${JELLYFIN_IMAGE:-ghcr.io/jellyfin/jellyfin:12.0}"
EMBY_CONTAINER="titlovi-emby-smoke"
EMBY_REF_CONTAINER="titlovi-emby-ref"
JELLYFIN_CONTAINER="titlovi-jellyfin-smoke"

cleanup() {
  docker rm -f "$EMBY_CONTAINER" "$EMBY_REF_CONTAINER" "$JELLYFIN_CONTAINER" >/dev/null 2>&1 || true

  # Both official server images write some bind-mounted config files as root.
  # Cleanup must never turn an otherwise successful runtime smoke into a failed job.
  if command -v sudo >/dev/null 2>&1; then
    sudo rm -rf "$SMOKE_ROOT" >/dev/null 2>&1 || true
  else
    rm -rf "$SMOKE_ROOT" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

wait_for_http() {
  local url="$1"
  local output="$2"
  local label="$3"

  for _ in $(seq 1 90); do
    if curl --silent --show-error --fail "$url" -o "$output"; then
      return 0
    fi
    sleep 2
  done

  echo "ERROR: $label did not become ready at $url" >&2
  return 1
}

collect_logs() {
  local container="$1"
  local config_root="$2"
  local output="$3"

  {
    echo "===== docker logs: $container ====="
    docker logs "$container" 2>&1 || true
    echo
    echo "===== files under config root ====="
    if [ -d "$config_root" ]; then
      find "$config_root" -maxdepth 4 -type f -print 2>/dev/null || true
    fi
    echo
    echo "===== text log contents ====="
    if [ -d "$config_root" ]; then
      find "$config_root" -maxdepth 4 -type f \( -name '*.log' -o -name '*.txt' \) -print0 2>/dev/null \
        | xargs -0 -r cat 2>/dev/null || true
    fi
  } > "$output"
}

assert_json_version_prefix() {
  local json_file="$1"
  local expected_prefix="$2"
  local label="$3"

  python3 - "$json_file" "$expected_prefix" "$label" <<'PY'
import json
import sys

path, prefix, label = sys.argv[1:]
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)
version = str(data.get("Version") or data.get("version") or "")
if not version.startswith(prefix):
    raise SystemExit(f"{label} reported unexpected version: {version!r}; expected prefix {prefix!r}")
print(f"{label} version: {version}")
PY
}

assert_log_contains() {
  local log_file="$1"
  local pattern="$2"
  local label="$3"

  if ! grep -aE "$pattern" "$log_file" >/dev/null; then
    echo "ERROR: $label startup logs do not show the Titlovi plugin being loaded." >&2
    echo "Expected pattern: $pattern" >&2
    cat "$log_file" >&2
    return 1
  fi
}

assert_no_loader_error() {
  local log_file="$1"
  local assembly_name="$2"
  local label="$3"

  if grep -aiE "Failed to load assembly.*${assembly_name}|${assembly_name}.*(TypeLoadException|FileLoadException)|Error registering plugin services.*${assembly_name}" "$log_file" >/dev/null; then
    echo "ERROR: $label reported a plugin loader/ABI failure for $assembly_name." >&2
    cat "$log_file" >&2
    return 1
  fi
}

echo "==> Pulling official Linux server images"
docker pull "$EMBY_IMAGE"
docker pull "$JELLYFIN_IMAGE"

echo "==> Copying Emby 4.10 reference assemblies from the official Linux image"
rm -rf "$ROOT/src/Emby/embylibs"
mkdir -p "$ROOT/src/Emby/embylibs"
docker create --name "$EMBY_REF_CONTAINER" "$EMBY_IMAGE" >/dev/null
for dll in MediaBrowser.Model.dll MediaBrowser.Common.dll MediaBrowser.Controller.dll; do
  docker cp "$EMBY_REF_CONTAINER:/system/$dll" "$ROOT/src/Emby/embylibs/$dll"
done
docker rm "$EMBY_REF_CONTAINER" >/dev/null

echo "==> Building Emby plugin on Linux"
dotnet restore "$ROOT/src/Emby/Emby.Titlovi.csproj" --nologo -p:NuGetAudit=false
dotnet build "$ROOT/src/Emby/Emby.Titlovi.csproj" -c Release --no-restore --nologo -p:NuGetAudit=false
EMBY_DLL="$ROOT/src/Emby/bin/Release/netstandard2.0/Emby.Titlovi.dll"
test -s "$EMBY_DLL"

echo "==> Building Jellyfin plugin on Linux"
dotnet restore "$ROOT/src/Jellyfin/Jellyfin.Titlovi.csproj" --nologo -p:NuGetAudit=false
dotnet build "$ROOT/src/Jellyfin/Jellyfin.Titlovi.csproj" -c Release --no-restore --nologo -p:NuGetAudit=false
JELLYFIN_DLL="$ROOT/src/Jellyfin/bin/Release/net10.0/Jellyfin.Titlovi.dll"
test -s "$JELLYFIN_DLL"

echo "==> Preparing Jellyfin 12 plugin folder"
JF_CONFIG="$SMOKE_ROOT/jellyfin-config"
JF_PLUGIN="$JF_CONFIG/plugins/Titlovi.com (Unofficial)_1.1.9.0"
mkdir -p "$JF_PLUGIN"
cp "$JELLYFIN_DLL" "$JF_PLUGIN/Jellyfin.Titlovi.dll"
cp "$ROOT/src/Jellyfin/thumb.png" "$JF_PLUGIN/thumb.png"
cat > "$JF_PLUGIN/meta.json" <<'JSON'
{
  "category": "Subtitles",
  "changelog": "Linux runtime smoke package",
  "description": "Unofficial Titlovi.com subtitle provider for Jellyfin.",
  "guid": "8d8e1f97-ae88-4b46-acac-a16f6a2d88dd",
  "name": "Titlovi.com (Unofficial)",
  "overview": "Unofficial Titlovi.com subtitle provider built from the shared Titlovi.com.Unified engine.",
  "owner": "Unofficial community plugin",
  "targetAbi": "12.0.0.0",
  "timestamp": "2026-09-15T00:00:00Z",
  "version": "1.1.9.0",
  "status": "Active",
  "autoUpdate": false,
  "imagePath": "thumb.png",
  "assemblies": ["Jellyfin.Titlovi.dll"]
}
JSON

echo "==> Starting Jellyfin 12 Linux container with Titlovi plugin"
docker run -d \
  --name "$JELLYFIN_CONTAINER" \
  --user 0:0 \
  -p 18096:8096 \
  -v "$JF_CONFIG:/config" \
  "$JELLYFIN_IMAGE" >/dev/null

JF_INFO="$SMOKE_ROOT/jellyfin-system-info.json"
if ! wait_for_http "http://127.0.0.1:18096/System/Info/Public" "$JF_INFO" "Jellyfin"; then
  docker logs "$JELLYFIN_CONTAINER" >&2 || true
  exit 1
fi
assert_json_version_prefix "$JF_INFO" "12." "Jellyfin"
JF_LOG="$SMOKE_ROOT/jellyfin.log"
collect_logs "$JELLYFIN_CONTAINER" "$JF_CONFIG" "$JF_LOG"
assert_no_loader_error "$JF_LOG" "Jellyfin\\.Titlovi" "Jellyfin"
assert_log_contains "$JF_LOG" "Loaded assembly Jellyfin\\.Titlovi|Jellyfin\\.Titlovi.*Titlovi\\.com \\(Unofficial\\)|Titlovi\\.com \\(Unofficial\\).*Jellyfin\\.Titlovi" "Jellyfin"
docker rm -f "$JELLYFIN_CONTAINER" >/dev/null

echo "==> Preparing Emby 4.10 plugin folder"
EMBY_CONFIG="$SMOKE_ROOT/emby-config"
mkdir -p "$EMBY_CONFIG/plugins"
cp "$EMBY_DLL" "$EMBY_CONFIG/plugins/Emby.Titlovi.dll"

echo "==> Starting Emby 4.10 Linux container with Titlovi plugin"
docker run -d \
  --name "$EMBY_CONTAINER" \
  -e UID=0 \
  -e GID=0 \
  -e GIDLIST=0 \
  -p 18097:8096 \
  -v "$EMBY_CONFIG:/config" \
  "$EMBY_IMAGE" >/dev/null

EMBY_INFO="$SMOKE_ROOT/emby-system-info.json"
if ! wait_for_http "http://127.0.0.1:18097/emby/System/Info/Public" "$EMBY_INFO" "Emby"; then
  if ! wait_for_http "http://127.0.0.1:18097/System/Info/Public" "$EMBY_INFO" "Emby"; then
    docker logs "$EMBY_CONTAINER" >&2 || true
    exit 1
  fi
fi
assert_json_version_prefix "$EMBY_INFO" "4.10." "Emby"
EMBY_LOG="$SMOKE_ROOT/emby.log"
collect_logs "$EMBY_CONTAINER" "$EMBY_CONFIG" "$EMBY_LOG"
assert_no_loader_error "$EMBY_LOG" "Emby\\.Titlovi" "Emby"
assert_log_contains "$EMBY_LOG" "Emby\\.Titlovi|Titlovi\\.com \\(Unofficial\\)" "Emby"

echo "==> Linux runtime smoke passed for Jellyfin 12 and Emby 4.10"

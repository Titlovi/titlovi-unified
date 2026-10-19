#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SMOKE_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/titlovi-linux-smoke.XXXXXX")"
EMBY_IMAGE="${EMBY_IMAGE:-emby/embyserver:4.10.0.40}"
JELLYFIN_IMAGE="${JELLYFIN_IMAGE:-ghcr.io/jellyfin/jellyfin:12.0}"
EMBY_CONTAINER="titlovi-emby-smoke"
JELLYFIN_CONTAINER="titlovi-jellyfin-smoke"

cleanup() {
  docker rm -f "$EMBY_CONTAINER" "$JELLYFIN_CONTAINER" >/dev/null 2>&1 || true
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
    find "$config_root" -maxdepth 4 -type f -print 2>/dev/null || true
    echo
    echo "===== text log contents ====="
    find "$config_root" -maxdepth 4 -type f \( -name '*.log' -o -name '*.txt' \) -print0 2>/dev/null \
      | xargs -0 -r cat 2>/dev/null || true
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

echo "==> Building distributable artifacts through canonical Linux scripts"
EMBY_IMAGE="$EMBY_IMAGE" bash "$ROOT/scripts/linux/build-all.sh"

EMBY_DLL="$ROOT/dist/Emby/Emby.Titlovi.dll"
JF_PACKAGE="$(find "$ROOT/dist/Jellyfin" -mindepth 1 -maxdepth 1 -type d -name 'Titlovi.com_*' -print -quit)"
test -s "$EMBY_DLL"
test -n "$JF_PACKAGE"
test -s "$JF_PACKAGE/Jellyfin.Titlovi.dll"
test -s "$JF_PACKAGE/meta.json"
test -s "$JF_PACKAGE/thumb.png"

echo "==> Pulling official Linux server images"
docker pull "$EMBY_IMAGE" >/dev/null
docker pull "$JELLYFIN_IMAGE" >/dev/null

echo "==> Starting Jellyfin 12 Linux container with packaged Titlovi plugin"
JF_CONFIG="$SMOKE_ROOT/jellyfin-config"
JF_PLUGIN="$JF_CONFIG/plugins/$(basename "$JF_PACKAGE")"
mkdir -p "$JF_PLUGIN"
cp -a "$JF_PACKAGE/." "$JF_PLUGIN/"

# Emulate credentials left behind by Matko's original Jellyfin plugin.
# The import occurs during startup, without contacting Titlovi.com.
JF_LEGACY_CONFIG="$JF_CONFIG/plugins/configurations/Titlovi.Plugin.xml"
mkdir -p "$(dirname "$JF_LEGACY_CONFIG")"
cat > "$JF_LEGACY_CONFIG" <<'XML'
<?xml version="1.0" encoding="utf-8"?>
<TitloviConfiguration>
  <Username>migration-smoke-user</Username>
  <Password>migration-smoke-password</Password>
  <Token><Id>obsolete-test-token</Id><UserId>42</UserId></Token>
</TitloviConfiguration>
XML

# Regression: a prior preview install can leave a BLANK new-format XML behind.
# The old importer incorrectly treated that file's mere existence as "migrated".
JF_NEW_CONFIG="$JF_CONFIG/plugins/configurations/Jellyfin.Titlovi.xml"
cat > "$JF_NEW_CONFIG" <<'XML'
<?xml version="1.0" encoding="utf-8"?>
<PluginConfiguration>
  <Username></Username>
  <Password></Password>
  <IncludeBshAlternativeLanguages>true</IncludeBshAlternativeLanguages>
  <EnableDebugLogging>false</EnableDebugLogging>
</PluginConfiguration>
XML
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
assert_log_contains "$JF_LOG" "Loaded assembly Jellyfin\\.Titlovi|Jellyfin\\.Titlovi.*Titlovi\\.com|Titlovi\\.com.*Jellyfin\\.Titlovi" "Jellyfin"

echo "==> Verifying original Jellyfin configuration import"
JF_NEW_CONFIG="$JF_CONFIG/plugins/configurations/Jellyfin.Titlovi.xml"
for _ in $(seq 1 20); do
  if [ -s "$JF_NEW_CONFIG" ]; then
    break
  fi
  sleep 1
done
test -s "$JF_NEW_CONFIG" || { echo "ERROR: Jellyfin did not create the imported configuration file." >&2; exit 1; }

python3 - "$JF_LEGACY_CONFIG" "$JF_NEW_CONFIG" <<'PY'
import sys
import xml.etree.ElementTree as ET

def fields(path):
    root = ET.parse(path).getroot()
    return {child.tag.split("}")[-1]: child.text for child in root}, ET.tostring(root, encoding="unicode")

legacy, original_xml = fields(sys.argv[1])
new, imported_xml = fields(sys.argv[2])
if legacy.get("Username") != "migration-smoke-user" or legacy.get("Password") != "migration-smoke-password":
    raise SystemExit("Original plugin credentials changed unexpectedly.")
if new.get("Username") != legacy["Username"] or new.get("Password") != legacy["Password"]:
    raise SystemExit("Original plugin credentials were not imported correctly.")
if "obsolete-test-token" in imported_xml:
    raise SystemExit("Incompatible original authentication token was copied to the new plugin.")
if "obsolete-test-token" not in original_xml:
    raise SystemExit("Original plugin token/configuration was unexpectedly modified.")
if new.get("LegacyCredentialsImportHandled", "").lower() != "true":
    raise SystemExit("Import completion marker not persisted.")
if new.get("IncludeBshAlternativeLanguages", "").lower() != "true":
    raise SystemExit("Existing plugin options were not preserved during import.")
print("Jellyfin original configuration import into existing blank XML verified.")
PY
docker rm -f "$JELLYFIN_CONTAINER" >/dev/null

echo "==> Starting Emby 4.10 Linux container with packaged Titlovi plugin"
EMBY_CONFIG="$SMOKE_ROOT/emby-config"
mkdir -p "$EMBY_CONFIG/plugins"
cp "$EMBY_DLL" "$EMBY_CONFIG/plugins/Emby.Titlovi.dll"
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
assert_log_contains "$EMBY_LOG" "Emby\\.Titlovi|Titlovi\\.com" "Emby"

echo "==> Linux runtime smoke passed for packaged Jellyfin 12 and Emby 4.10 artifacts"

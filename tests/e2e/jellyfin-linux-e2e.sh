#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
E2E_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/titlovi-jellyfin-e2e.XXXXXX")"
JELLYFIN_IMAGE="${JELLYFIN_IMAGE:-ghcr.io/jellyfin/jellyfin:12.0}"
JELLYFIN_CONTAINER="titlovi-jellyfin-e2e"
BASE_URL="http://127.0.0.1:18096"
PLUGIN_ID="6e25df50-638e-4109-a50b-03c14fc93fdd"
ADMIN_USER="titlovi-e2e-admin"
ADMIN_PASSWORD="$(python3 - <<'PY'
import secrets
print(secrets.token_urlsafe(24))
PY
)"

cleanup() {
  docker rm -f "$JELLYFIN_CONTAINER" >/dev/null 2>&1 || true
  if command -v sudo >/dev/null 2>&1; then
    sudo rm -rf "$E2E_ROOT" >/dev/null 2>&1 || true
  else
    rm -rf "$E2E_ROOT" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

fail() {
  echo "ERROR: $*" >&2
  if docker ps -a --format '{{.Names}}' | grep -Fx "$JELLYFIN_CONTAINER" >/dev/null 2>&1; then
    echo "===== Jellyfin container log =====" >&2
    docker logs "$JELLYFIN_CONTAINER" >&2 || true
  fi
  for diagnostic_log in \
    "${JF_CONFIG:-}/log/jellyfin-titlovi.log" \
    "${JF_CONFIG:-}/log/jellyfin-titlovi-debug.log"; do
    if [ -f "$diagnostic_log" ]; then
      echo "===== Titlovi plugin diagnostics: $(basename "$diagnostic_log") =====" >&2
      tail -n 200 "$diagnostic_log" >&2 || true
    fi
  done
  exit 1
}

wait_for_http() {
  local url="$1"
  local output="$2"
  for _ in $(seq 1 90); do
    if curl --silent --show-error --fail "$url" -o "$output"; then
      return 0
    fi
    sleep 2
  done
  return 1
}

auth_curl() {
  curl --silent --show-error --fail \
    -H "Authorization: $AUTH_HEADER" \
    "$@"
}

require_secret() {
  local name="$1"
  local value="$2"
  if [ -z "$value" ]; then
    echo "Missing required GitHub Actions secret: $name" >&2
    exit 2
  fi
}

require_secret "TITLOVI_USERNAME" "${TITLOVI_USERNAME:-}"
require_secret "TITLOVI_PASSWORD" "${TITLOVI_PASSWORD:-}"

echo "::add-mask::$TITLOVI_USERNAME"
echo "::add-mask::$TITLOVI_PASSWORD"
echo "::add-mask::$ADMIN_PASSWORD"

command -v ffmpeg >/dev/null 2>&1 || fail "ffmpeg is required on the GitHub runner."

MEDIA_ROOT="$E2E_ROOT/media"
JF_CONFIG="$E2E_ROOT/jellyfin-config"
JF_PLUGIN="$JF_CONFIG/plugins/Titlovi.com_2.0.0.0"
MOVIE_DIR="$MEDIA_ROOT/movies/The Matrix (1999)"
EPISODE_DIR="$MEDIA_ROOT/tv/Friends/Season 01"
MOVIE_FILE="$MOVIE_DIR/The Matrix (1999).mkv"
EPISODE_FILE="$EPISODE_DIR/Friends S01E01 - The One Where Monica Gets a Roommate.mkv"

mkdir -p "$MOVIE_DIR" "$EPISODE_DIR" "$JF_PLUGIN"

echo "==> Generating minimal real media files"
ffmpeg -hide_banner -loglevel error -y \
  -f lavfi -i "color=c=black:s=320x180:r=1:d=1" \
  -c:v mpeg4 -q:v 5 -an "$MOVIE_FILE"
cp "$MOVIE_FILE" "$EPISODE_FILE"

cat > "$MOVIE_DIR/movie.nfo" <<'NFO'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<movie>
  <title>The Matrix</title>
  <originaltitle>The Matrix</originaltitle>
  <year>1999</year>
  <imdbid>tt0133093</imdbid>
  <uniqueid type="imdb" default="true">tt0133093</uniqueid>
</movie>
NFO

mkdir -p "$MEDIA_ROOT/tv/Friends"
cat > "$MEDIA_ROOT/tv/Friends/tvshow.nfo" <<'NFO'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<tvshow>
  <title>Friends</title>
  <originaltitle>Friends</originaltitle>
  <year>1994</year>
  <imdbid>tt0108778</imdbid>
  <uniqueid type="imdb" default="true">tt0108778</uniqueid>
</tvshow>
NFO

cat > "${EPISODE_FILE%.mkv}.nfo" <<'NFO'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<episodedetails>
  <title>The One Where Monica Gets a Roommate</title>
  <season>1</season>
  <episode>1</episode>
  <aired>1994-09-22</aired>
</episodedetails>
NFO

echo "==> Building Jellyfin plugin on Ubuntu"
dotnet restore "$ROOT/src/Jellyfin/Jellyfin.Titlovi.csproj" --nologo -p:NuGetAudit=false
dotnet build "$ROOT/src/Jellyfin/Jellyfin.Titlovi.csproj" -c Release --no-restore --nologo -p:NuGetAudit=false
JELLYFIN_DLL="$ROOT/src/Jellyfin/bin/Release/net10.0/Jellyfin.Titlovi.dll"
test -s "$JELLYFIN_DLL" || fail "Jellyfin.Titlovi.dll was not produced."

cp "$JELLYFIN_DLL" "$JF_PLUGIN/Jellyfin.Titlovi.dll"
cp "$ROOT/src/Jellyfin/thumb.png" "$JF_PLUGIN/thumb.png"
cat > "$JF_PLUGIN/meta.json" <<'JSON'
{
  "category": "Subtitles",
  "changelog": "Manual Linux end-to-end validation package",
  "description": "Titlovi.com subtitle provider for Jellyfin.",
  "guid": "6e25df50-638e-4109-a50b-03c14fc93fdd",
  "name": "Titlovi.com",
  "overview": "Titlovi.com for Jellyfin 12, built from the unified Emby and Jellyfin source tree.",
  "owner": "Titlovi.com",
  "targetAbi": "12.0.0.0",
  "timestamp": "2026-09-15T00:00:00Z",
  "version": "2.0.0.0",
  "status": "Active",
  "autoUpdate": false,
  "imagePath": "thumb.png",
  "assemblies": ["Jellyfin.Titlovi.dll"]
}
JSON

echo "==> Starting official Jellyfin 12 Linux container"
docker pull "$JELLYFIN_IMAGE" >/dev/null
docker run -d \
  --name "$JELLYFIN_CONTAINER" \
  --user 0:0 \
  -p 18096:8096 \
  -v "$JF_CONFIG:/config" \
  -v "$MEDIA_ROOT:/media" \
  "$JELLYFIN_IMAGE" >/dev/null

PUBLIC_INFO="$E2E_ROOT/system-info.json"
wait_for_http "$BASE_URL/System/Info/Public" "$PUBLIC_INFO" || fail "Jellyfin did not become ready."

python3 - "$PUBLIC_INFO" <<'PY'
import json, sys
data = json.load(open(sys.argv[1], encoding="utf-8"))
version = str(data.get("Version") or data.get("version") or "")
if not version.startswith("12."):
    raise SystemExit(f"Unexpected Jellyfin version: {version!r}")
print(f"Jellyfin version: {version}")
PY

echo "==> Completing isolated Jellyfin startup wizard"
STARTUP_READY=false
for _ in $(seq 1 30); do
  if curl --silent --show-error --fail "$BASE_URL/Startup/Configuration" -o /dev/null; then
    STARTUP_READY=true
    break
  fi
  sleep 2
done
if [ "$STARTUP_READY" != "true" ]; then
  fail "Jellyfin startup wizard API did not become ready."
fi

curl --silent --show-error --fail \
  -X POST "$BASE_URL/Startup/Configuration" \
  -H "Content-Type: application/json" \
  --data '{"ServerName":"Titlovi Linux E2E","UICulture":"en-US","MetadataCountryCode":"RS","PreferredMetadataLanguage":"en"}' \
  >/dev/null

echo "==> Initializing Jellyfin first user"
FIRST_USER_READY=false
for _ in $(seq 1 15); do
  if curl --silent --show-error --fail "$BASE_URL/Startup/User" -o /dev/null; then
    FIRST_USER_READY=true
    break
  fi
  sleep 2
done
if [ "$FIRST_USER_READY" != "true" ]; then
  fail "Jellyfin first startup user did not initialize."
fi

ADMIN_JSON="$E2E_ROOT/admin.json"
python3 - "$ADMIN_JSON" "$ADMIN_USER" "$ADMIN_PASSWORD" <<'PY'
import json, sys
path, user, password = sys.argv[1:]
with open(path, "w", encoding="utf-8") as f:
    json.dump({"Name": user, "Password": password}, f)
PY
chmod 600 "$ADMIN_JSON"

curl --silent --show-error --fail \
  -X POST "$BASE_URL/Startup/User" \
  -H "Content-Type: application/json" \
  --data-binary "@$ADMIN_JSON" \
  >/dev/null
curl --silent --show-error --fail \
  -X POST "$BASE_URL/Startup/Complete" \
  --data '' \
  >/dev/null

LOGIN_JSON="$E2E_ROOT/login.json"
python3 - "$LOGIN_JSON" "$ADMIN_USER" "$ADMIN_PASSWORD" <<'PY'
import json, sys
path, user, password = sys.argv[1:]
with open(path, "w", encoding="utf-8") as f:
    json.dump({"Username": user, "Pw": password}, f)
PY
chmod 600 "$LOGIN_JSON"

LOGIN_RESPONSE="$E2E_ROOT/login-response.json"
LOGIN_AUTH='MediaBrowser Client="TitloviE2E", Device="GitHub Actions", DeviceId="titlovi-linux-e2e", Version="1.0.0"'
curl --silent --show-error --fail \
  -X POST "$BASE_URL/Users/AuthenticateByName" \
  -H "Authorization: $LOGIN_AUTH" \
  -H "Content-Type: application/json" \
  --data-binary "@$LOGIN_JSON" \
  -o "$LOGIN_RESPONSE"

read -r ACCESS_TOKEN USER_ID < <(python3 - "$LOGIN_RESPONSE" <<'PY'
import json, sys
data = json.load(open(sys.argv[1], encoding="utf-8"))
token = data.get("AccessToken") or ""
user_id = (data.get("User") or {}).get("Id") or ""
if not token or not user_id:
    raise SystemExit("Jellyfin authentication response did not contain AccessToken and User.Id")
print(token, user_id)
PY
)
echo "::add-mask::$ACCESS_TOKEN"
AUTH_HEADER="MediaBrowser Token=\"$ACCESS_TOKEN\", Client=\"TitloviE2E\", Device=\"GitHub Actions\", DeviceId=\"titlovi-linux-e2e\", Version=\"1.0.0\""

echo "==> Configuring Titlovi plugin from GitHub Secrets"
PLUGIN_CONFIG="$E2E_ROOT/plugin-config.json"
python3 - "$PLUGIN_CONFIG" <<'PY'
import json, os, sys
config = {
    "Username": os.environ["TITLOVI_USERNAME"],
    "Password": os.environ["TITLOVI_PASSWORD"],
    "Token": None,
    "ConvertDownloadedSubtitlesToUtf8": True,
    "EnableDebugLogging": True,
    "IncludeBshAlternativeLanguages": False,
    "AcceptSrt": True,
    "AcceptSub": True,
    "AcceptTxt": True,
    "AcceptSsa": True,
    "AcceptAss": True,
}
with open(sys.argv[1], "w", encoding="utf-8") as f:
    json.dump(config, f)
PY
chmod 600 "$PLUGIN_CONFIG"

auth_curl \
  -X POST "$BASE_URL/Plugins/$PLUGIN_ID/Configuration" \
  -H "Content-Type: application/json" \
  --data-binary "@$PLUGIN_CONFIG" \
  >/dev/null

PLUGIN_CONFIG_READBACK="$E2E_ROOT/plugin-config-readback.json"
auth_curl "$BASE_URL/Plugins/$PLUGIN_ID/Configuration" -o "$PLUGIN_CONFIG_READBACK"
python3 - "$PLUGIN_CONFIG_READBACK" <<'PY'
import json, sys
data = json.load(open(sys.argv[1], encoding="utf-8"))
values = {str(k).lower(): v for k, v in data.items()}
if not str(values.get("username") or "").strip():
    raise SystemExit("Plugin configuration read-back has an empty username")
if not str(values.get("password") or "").strip():
    raise SystemExit("Plugin configuration read-back has an empty password")
if values.get("enabledebuglogging") is not True:
    raise SystemExit("Plugin configuration read-back did not enable debug logging")
if values.get("convertdownloadedsubtitlestoutf8") is not True:
    raise SystemExit("Plugin configuration read-back did not enable UTF-8 conversion")
if values.get("includebshalternativelanguages") is not False:
    raise SystemExit("Plugin configuration read-back unexpectedly enabled B/S/H alternatives")
print("Plugin configuration read-back confirmed: credentials present; debug and UTF-8 enabled")
PY

echo "==> Adding isolated movie and TV libraries"
auth_curl \
  -X POST "$BASE_URL/Library/VirtualFolders?name=Movies&collectionType=movies&paths=/media/movies&refreshLibrary=true" \
  -H "Content-Type: application/json" \
  --data '{}' \
  >/dev/null
auth_curl \
  -X POST "$BASE_URL/Library/VirtualFolders?name=TV&collectionType=tvshows&paths=/media/tv&refreshLibrary=true" \
  -H "Content-Type: application/json" \
  --data '{}' \
  >/dev/null

ITEMS_JSON="$E2E_ROOT/items.json"
IDS_ENV="$E2E_ROOT/ids.env"
echo "==> Waiting for Jellyfin to index local NFO metadata"
for _ in $(seq 1 90); do
  if auth_curl \
    "$BASE_URL/Items?Recursive=true&IncludeItemTypes=Movie,Series,Episode&Fields=Path,ProviderIds,SeriesId&Limit=100" \
    -o "$ITEMS_JSON"; then
    if python3 - "$ITEMS_JSON" "$IDS_ENV" <<'PY'
import json, shlex, sys
src, dst = sys.argv[1:]
data = json.load(open(src, encoding="utf-8"))
items = data.get("Items") or []

def by_type_and_path(kind, suffix):
    for item in items:
        if item.get("Type") == kind and str(item.get("Path") or "").endswith(suffix):
            return item
    return None

movie = by_type_and_path("Movie", "/The Matrix (1999).mkv")
episode = by_type_and_path("Episode", "/Friends S01E01 - The One Where Monica Gets a Roommate.mkv")
series = next((i for i in items if i.get("Type") == "Series" and i.get("Name") == "Friends"), None)
if not movie or not episode or not series:
    raise SystemExit(1)

movie_imdb = (movie.get("ProviderIds") or {}).get("Imdb")
series_imdb = (series.get("ProviderIds") or {}).get("Imdb")
if movie_imdb != "tt0133093":
    raise SystemExit(f"Movie IMDb id was not loaded from NFO: {movie_imdb!r}")
if series_imdb != "tt0108778":
    raise SystemExit(f"Series IMDb id was not loaded from NFO: {series_imdb!r}")

with open(dst, "w", encoding="utf-8") as f:
    f.write("MOVIE_ID=" + shlex.quote(str(movie["Id"])) + "\n")
    f.write("EPISODE_ID=" + shlex.quote(str(episode["Id"])) + "\n")
    f.write("SERIES_ID=" + shlex.quote(str(series["Id"])) + "\n")
PY
    then
      break
    fi
  fi
  sleep 2
done
test -s "$IDS_ENV" || fail "Jellyfin did not index the expected movie, series and episode with NFO IMDb ids."
# shellcheck disable=SC1090
source "$IDS_ENV"

echo "Indexed movie item: $MOVIE_ID"
echo "Indexed episode item: $EPISODE_ID"
echo "Indexed series item: $SERIES_ID"

extract_titlovi_result() {
  local search_json="$1"
  local id_file="$2"
  local label="$3"

  python3 - "$search_json" "$id_file" "$label" <<'PY'
import json, sys
src, dst, label = sys.argv[1:]
data = json.load(open(src, encoding="utf-8"))
if isinstance(data, dict):
    results = data.get("Items") or data.get("items") or []
else:
    results = data
matches = [r for r in results if (r.get("ProviderName") or r.get("providerName")) == "Titlovi.com"]
if not matches:
    raise SystemExit(f"{label}: Jellyfin returned no Titlovi.com results")
result = matches[0]
subtitle_id = result.get("Id") or result.get("id") or ""
if not subtitle_id:
    raise SystemExit(f"{label}: Titlovi.com result did not contain an Id")
with open(dst, "w", encoding="utf-8") as f:
    f.write(subtitle_id)
name = result.get("Name") or result.get("name") or "<unnamed>"
language = result.get("ThreeLetterISOLanguageName") or result.get("threeLetterISOLanguageName") or ""
fmt = result.get("Format") or result.get("format") or ""
print(f"{label}: Titlovi result found; language={language!r}, format={fmt!r}, name={name!r}")
PY
}

urlencode_file_value() {
  python3 - "$1" <<'PY'
from pathlib import Path
from urllib.parse import quote
import sys
value = Path(sys.argv[1]).read_text(encoding="utf-8")
print(quote(value, safe=""))
PY
}

wait_for_subtitle_file() {
  local directory="$1"
  local output="$2"
  for _ in $(seq 1 30); do
    local found
    found="$(find "$directory" -maxdepth 1 -type f \
      \( -iname '*.srt' -o -iname '*.sub' -o -iname '*.txt' -o -iname '*.ssa' -o -iname '*.ass' \) \
      -print -quit 2>/dev/null || true)"
    if [ -n "$found" ]; then
      printf '%s\n' "$found" > "$output"
      return 0
    fi
    sleep 1
  done
  return 1
}

validate_utf8_subtitle() {
  local path_file="$1"
  local label="$2"
  python3 - "$path_file" "$label" <<'PY'
from pathlib import Path
import sys
path = Path(Path(sys.argv[1]).read_text(encoding="utf-8").strip())
label = sys.argv[2]
data = path.read_bytes()
if not data:
    raise SystemExit(f"{label}: downloaded subtitle is empty")
try:
    data.decode("utf-8")
except UnicodeDecodeError as exc:
    raise SystemExit(f"{label}: downloaded subtitle is not valid UTF-8: {exc}")
print(f"{label}: saved subtitle is non-empty valid UTF-8 ({path.name})")
PY
}

echo "==> Searching Titlovi.com for The Matrix through Jellyfin"
MOVIE_SEARCH="$E2E_ROOT/movie-search.json"
auth_curl \
  "$BASE_URL/Items/$MOVIE_ID/RemoteSearch/Subtitles/srp?isPerfectMatch=false" \
  -o "$MOVIE_SEARCH" \
  || fail "Movie subtitle search request failed."
MOVIE_RESULT_ID="$E2E_ROOT/movie-result-id.txt"
extract_titlovi_result "$MOVIE_SEARCH" "$MOVIE_RESULT_ID" "Movie" \
  || fail "Movie search produced no Titlovi.com result."

echo "==> Searching Titlovi.com for Friends S01E01 through Jellyfin"
EPISODE_SEARCH="$E2E_ROOT/episode-search.json"
auth_curl \
  "$BASE_URL/Items/$EPISODE_ID/RemoteSearch/Subtitles/srp?isPerfectMatch=false" \
  -o "$EPISODE_SEARCH" \
  || fail "Episode subtitle search request failed."
EPISODE_RESULT_ID="$E2E_ROOT/episode-result-id.txt"
extract_titlovi_result "$EPISODE_SEARCH" "$EPISODE_RESULT_ID" "Episode" \
  || fail "Episode search produced no Titlovi.com result."

PLUGIN_LOG="$JF_CONFIG/log/jellyfin-titlovi-debug.log"
for _ in $(seq 1 20); do
  if [ -f "$PLUGIN_LOG" ] && grep -F "Parent series IMDb id from SeriesProviderIds='tt0108778'." "$PLUGIN_LOG" >/dev/null; then
    break
  fi
  sleep 1
done
if ! [ -f "$PLUGIN_LOG" ] || ! grep -F "Parent series IMDb id from SeriesProviderIds='tt0108778'." "$PLUGIN_LOG" >/dev/null; then
  fail "Episode search did not prove Jellyfin FindByPath() resolved the parent series IMDb id."
fi
echo "Jellyfin FindByPath(): parent series IMDb tt0108778 confirmed in plugin diagnostics"

echo "==> Downloading one movie subtitle through Jellyfin"
MOVIE_ENCODED_ID="$(urlencode_file_value "$MOVIE_RESULT_ID")"
auth_curl -X POST "$BASE_URL/Items/$MOVIE_ID/RemoteSearch/Subtitles/$MOVIE_ENCODED_ID" >/dev/null \
  || fail "Movie subtitle download request failed."
MOVIE_SUBTITLE_PATH="$E2E_ROOT/movie-subtitle-path.txt"
wait_for_subtitle_file "$MOVIE_DIR" "$MOVIE_SUBTITLE_PATH" \
  || fail "Jellyfin did not save the downloaded movie subtitle next to the media file."
validate_utf8_subtitle "$MOVIE_SUBTITLE_PATH" "Movie"

echo "==> Downloading one episode subtitle through Jellyfin"
EPISODE_ENCODED_ID="$(urlencode_file_value "$EPISODE_RESULT_ID")"
auth_curl -X POST "$BASE_URL/Items/$EPISODE_ID/RemoteSearch/Subtitles/$EPISODE_ENCODED_ID" >/dev/null \
  || fail "Episode subtitle download request failed."
EPISODE_SUBTITLE_PATH="$E2E_ROOT/episode-subtitle-path.txt"
wait_for_subtitle_file "$EPISODE_DIR" "$EPISODE_SUBTITLE_PATH" \
  || fail "Jellyfin did not save the downloaded episode subtitle next to the media file."
validate_utf8_subtitle "$EPISODE_SUBTITLE_PATH" "Episode"

echo "==> Jellyfin 12 Linux end-to-end Titlovi.com validation passed"
echo "Validated: real Jellyfin library -> plugin search -> parent-series FindByPath -> Titlovi.com -> download -> host save -> UTF-8."

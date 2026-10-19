#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
E2E_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/titlovi-emby-e2e.XXXXXX")"
EMBY_IMAGE="${EMBY_IMAGE:-emby/embyserver:4.10.0.40}"
EMBY_CONTAINER="titlovi-emby-e2e"
EMBY_REF_CONTAINER="titlovi-emby-e2e-ref"
BASE_URL="http://127.0.0.1:18097/emby"
PLUGIN_ID="1dbf2e1e-3187-4f63-9a60-4ca7ace4232e"
ADMIN_USER="titlovi-e2e-admin"
ADMIN_PASSWORD="$(python3 - <<'PY'
import secrets
print(secrets.token_urlsafe(24))
PY
)"

cleanup() {
  docker rm -f "$EMBY_CONTAINER" "$EMBY_REF_CONTAINER" >/dev/null 2>&1 || true
  if command -v sudo >/dev/null 2>&1; then
    sudo rm -rf "$E2E_ROOT" >/dev/null 2>&1 || true
  else
    rm -rf "$E2E_ROOT" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

find_plugin_log() {
  local name="$1"
  find "${EMBY_CONFIG:-/nonexistent}" -type f -name "$name" -print -quit 2>/dev/null || true
}

fail() {
  echo "ERROR: $*" >&2
  if docker ps -a --format '{{.Names}}' | grep -Fx "$EMBY_CONTAINER" >/dev/null 2>&1; then
    echo "===== Emby container log =====" >&2
    docker logs "$EMBY_CONTAINER" >&2 || true
  fi
  for log_name in emby-titlovi.log emby-titlovi-debug.log; do
    diagnostic_log="$(find_plugin_log "$log_name")"
    if [ -n "$diagnostic_log" ] && [ -f "$diagnostic_log" ]; then
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
    -H "X-Emby-Token: $ACCESS_TOKEN" \
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
EMBY_CONFIG="$E2E_ROOT/emby-config"
MOVIE_DIR="$MEDIA_ROOT/movies/The Matrix (1999)"
EPISODE_DIR="$MEDIA_ROOT/tv/Friends/Season 01"
MOVIE_FILE="$MOVIE_DIR/The Matrix (1999).mkv"
EPISODE_FILE="$EPISODE_DIR/Friends S01E01 - The One Where Monica Gets a Roommate.mkv"

mkdir -p "$MOVIE_DIR" "$EPISODE_DIR" "$EMBY_CONFIG/plugins"

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

echo "==> Pulling official Emby 4.10 Linux image"
docker pull "$EMBY_IMAGE" >/dev/null

echo "==> Copying Emby 4.10 reference assemblies from official Linux image"
rm -rf "$ROOT/src/Emby/embylibs"
mkdir -p "$ROOT/src/Emby/embylibs"
docker create --name "$EMBY_REF_CONTAINER" "$EMBY_IMAGE" >/dev/null
for dll in MediaBrowser.Model.dll MediaBrowser.Common.dll MediaBrowser.Controller.dll; do
  docker cp "$EMBY_REF_CONTAINER:/system/$dll" "$ROOT/src/Emby/embylibs/$dll"
done
docker rm "$EMBY_REF_CONTAINER" >/dev/null

echo "==> Building Emby plugin on Ubuntu"
dotnet restore "$ROOT/src/Emby/Emby.Titlovi.csproj" --nologo -p:NuGetAudit=false
dotnet build "$ROOT/src/Emby/Emby.Titlovi.csproj" -c Release --no-restore --nologo -p:NuGetAudit=false
EMBY_DLL="$ROOT/src/Emby/bin/Release/netstandard2.0/Emby.Titlovi.dll"
test -s "$EMBY_DLL" || fail "Emby.Titlovi.dll was not produced."
cp "$EMBY_DLL" "$EMBY_CONFIG/plugins/Emby.Titlovi.dll"

echo "==> Starting official Emby 4.10 Linux container"
docker run -d \
  --name "$EMBY_CONTAINER" \
  -e UID=0 \
  -e GID=0 \
  -e GIDLIST=0 \
  -p 18097:8096 \
  -v "$EMBY_CONFIG:/config" \
  -v "$MEDIA_ROOT:/media" \
  "$EMBY_IMAGE" >/dev/null

PUBLIC_INFO="$E2E_ROOT/system-info.json"
wait_for_http "$BASE_URL/System/Info/Public" "$PUBLIC_INFO" || fail "Emby did not become ready."

python3 - "$PUBLIC_INFO" <<'PY'
import json, sys
data = json.load(open(sys.argv[1], encoding="utf-8"))
version = str(data.get("Version") or data.get("version") or "")
if not version.startswith("4.10."):
    raise SystemExit(f"Unexpected Emby version: {version!r}")
print(f"Emby version: {version}")
PY

echo "==> Completing isolated Emby startup wizard"
for _ in $(seq 1 30); do
  if curl --silent --show-error --fail "$BASE_URL/Startup/Configuration" -o /dev/null; then
    break
  fi
  sleep 2
done
curl --silent --show-error --fail "$BASE_URL/Startup/Configuration" -o /dev/null \
  || fail "Emby startup configuration API did not become ready."

curl --silent --show-error --fail \
  -X POST "$BASE_URL/Startup/Configuration" \
  -H "Content-Type: application/json" \
  --data '{"UICulture":"en-US","MetadataCountryCode":"RS","PreferredMetadataLanguage":"en"}' \
  >/dev/null

STARTUP_USER="$E2E_ROOT/startup-user.json"
curl --silent --show-error --fail "$BASE_URL/Startup/User" -o "$STARTUP_USER" \
  || fail "Emby startup user was not available."

ADMIN_JSON="$E2E_ROOT/admin.json"
python3 - "$ADMIN_JSON" "$ADMIN_USER" <<'PY'
import json, sys
with open(sys.argv[1], "w", encoding="utf-8") as f:
    json.dump({"Name": sys.argv[2], "ConnectUserName": ""}, f)
PY

curl --silent --show-error --fail \
  -X POST "$BASE_URL/Startup/User" \
  -H "Content-Type: application/json" \
  --data-binary "@$ADMIN_JSON" \
  >/dev/null

curl --silent --show-error --fail \
  -X POST "$BASE_URL/Startup/RemoteAccess" \
  -H "Content-Type: application/json" \
  --data '{"EnableRemoteAccess":false,"EnableAutomaticPortMapping":false}' \
  >/dev/null

curl --silent --show-error --fail \
  -X POST "$BASE_URL/Startup/Complete" \
  --data '' \
  >/dev/null

echo "==> Authenticating disposable Emby administrator"
LOGIN_JSON="$E2E_ROOT/login.json"
python3 - "$LOGIN_JSON" "$ADMIN_USER" <<'PY'
import json, sys
with open(sys.argv[1], "w", encoding="utf-8") as f:
    json.dump({"Username": sys.argv[2], "Pw": ""}, f)
PY

LOGIN_RESPONSE="$E2E_ROOT/login-response.json"
LOGIN_AUTH='Emby Client="TitloviE2E", Device="GitHub Actions", DeviceId="titlovi-emby-linux-e2e", Version="1.0.0"'
curl --silent --show-error --fail \
  -X POST "$BASE_URL/Users/AuthenticateByName" \
  -H "X-Emby-Authorization: $LOGIN_AUTH" \
  -H "Content-Type: application/json" \
  --data-binary "@$LOGIN_JSON" \
  -o "$LOGIN_RESPONSE"

read -r ACCESS_TOKEN USER_ID < <(python3 - "$LOGIN_RESPONSE" <<'PY'
import json, sys
data = json.load(open(sys.argv[1], encoding="utf-8"))
token = data.get("AccessToken") or data.get("accessToken") or ""
user = data.get("User") or data.get("user") or {}
user_id = user.get("Id") or user.get("id") or ""
if not token or not user_id:
    raise SystemExit("Emby authentication response did not contain AccessToken and User.Id")
print(token, user_id)
PY
)
echo "::add-mask::$ACCESS_TOKEN"

PASSWORD_JSON="$E2E_ROOT/password.json"
python3 - "$PASSWORD_JSON" "$USER_ID" "$ADMIN_PASSWORD" <<'PY'
import json, sys
with open(sys.argv[1], "w", encoding="utf-8") as f:
    json.dump({"Id": sys.argv[2], "NewPw": sys.argv[3], "ResetPassword": False}, f)
PY
auth_curl \
  -X POST "$BASE_URL/Users/$USER_ID/Password" \
  -H "Content-Type: application/json" \
  --data-binary "@$PASSWORD_JSON" \
  >/dev/null

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
MOVIE_LIBRARY="$E2E_ROOT/movie-library.json"
TV_LIBRARY="$E2E_ROOT/tv-library.json"
python3 - "$MOVIE_LIBRARY" "$TV_LIBRARY" <<'PY'
import json, sys
movie_path, tv_path = sys.argv[1:]
common_options = {"SaveSubtitlesWithMedia": True}
with open(movie_path, "w", encoding="utf-8") as f:
    json.dump({
        "Name": "Movies",
        "CollectionType": "movies",
        "RefreshLibrary": True,
        "Paths": ["/media/movies"],
        "LibraryOptions": common_options,
    }, f)
with open(tv_path, "w", encoding="utf-8") as f:
    json.dump({
        "Name": "TV",
        "CollectionType": "tvshows",
        "RefreshLibrary": True,
        "Paths": ["/media/tv"],
        "LibraryOptions": common_options,
    }, f)
PY

auth_curl \
  -X POST "$BASE_URL/Library/VirtualFolders" \
  -H "Content-Type: application/json" \
  --data-binary "@$MOVIE_LIBRARY" \
  >/dev/null
auth_curl \
  -X POST "$BASE_URL/Library/VirtualFolders" \
  -H "Content-Type: application/json" \
  --data-binary "@$TV_LIBRARY" \
  >/dev/null

ITEMS_JSON="$E2E_ROOT/items.json"
IDS_ENV="$E2E_ROOT/ids.env"
echo "==> Waiting for Emby to index local NFO metadata"
for _ in $(seq 1 90); do
  if auth_curl \
    "$BASE_URL/Users/$USER_ID/Items?Recursive=true&IncludeItemTypes=Movie,Series,Episode&Fields=Path,ProviderIds&Limit=100" \
    -o "$ITEMS_JSON"; then
    if python3 - "$ITEMS_JSON" "$IDS_ENV" <<'PY'
import json, shlex, sys
src, dst = sys.argv[1:]
data = json.load(open(src, encoding="utf-8"))
items = data.get("Items") or data.get("items") or []

def value(item, key):
    return item.get(key) if key in item else item.get(key[:1].lower() + key[1:])

def by_type_and_path(kind, suffix):
    for item in items:
        if value(item, "Type") == kind and str(value(item, "Path") or "").endswith(suffix):
            return item
    return None

movie = by_type_and_path("Movie", "/The Matrix (1999).mkv")
episode = by_type_and_path("Episode", "/Friends S01E01 - The One Where Monica Gets a Roommate.mkv")
series = next((i for i in items if value(i, "Type") == "Series" and value(i, "Name") == "Friends"), None)
if not movie or not episode or not series:
    raise SystemExit(1)

movie_ids = value(movie, "ProviderIds") or {}
series_ids = value(series, "ProviderIds") or {}
movie_imdb = movie_ids.get("Imdb") or movie_ids.get("imdb")
series_imdb = series_ids.get("Imdb") or series_ids.get("imdb")
if movie_imdb != "tt0133093":
    raise SystemExit(f"Movie IMDb id was not loaded from NFO: {movie_imdb!r}")
if series_imdb != "tt0108778":
    raise SystemExit(f"Series IMDb id was not loaded from NFO: {series_imdb!r}")

with open(dst, "w", encoding="utf-8") as f:
    f.write("MOVIE_ID=" + shlex.quote(str(value(movie, "Id"))) + "\n")
    f.write("EPISODE_ID=" + shlex.quote(str(value(episode, "Id"))) + "\n")
    f.write("SERIES_ID=" + shlex.quote(str(value(series, "Id"))) + "\n")
PY
    then
      break
    fi
  fi
  sleep 2
done
test -s "$IDS_ENV" || fail "Emby did not index the expected movie, series and episode with NFO IMDb ids."
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
results = (data.get("Items") or data.get("items") or []) if isinstance(data, dict) else data
matches = [
    r for r in (results or [])
    if (r.get("ProviderName") or r.get("providerName")) == "Titlovi.com"
]
if not matches:
    raise SystemExit(f"{label}: Emby returned no Titlovi.com results")
result = matches[0]
subtitle_id = result.get("Id") or result.get("id") or ""
if not subtitle_id:
    raise SystemExit(f"{label}: Titlovi.com result did not contain an Id")
with open(dst, "w", encoding="utf-8") as f:
    f.write(subtitle_id)
name = result.get("Name") or result.get("name") or "<unnamed>"
language = result.get("Language") or result.get("language") or result.get("ThreeLetterISOLanguageName") or ""
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
  for _ in $(seq 1 45); do
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

echo "==> Searching Titlovi.com for The Matrix through Emby"
MOVIE_SEARCH="$E2E_ROOT/movie-search.json"
auth_curl \
  "$BASE_URL/Items/$MOVIE_ID/RemoteSearch/Subtitles/srp?IsPerfectMatch=false" \
  -o "$MOVIE_SEARCH" \
  || fail "Movie subtitle search request failed."
MOVIE_RESULT_ID="$E2E_ROOT/movie-result-id.txt"
extract_titlovi_result "$MOVIE_SEARCH" "$MOVIE_RESULT_ID" "Movie" \
  || fail "Movie search produced no Titlovi.com result."

echo "==> Searching Titlovi.com for Friends S01E01 through Emby"
EPISODE_SEARCH="$E2E_ROOT/episode-search.json"
auth_curl \
  "$BASE_URL/Items/$EPISODE_ID/RemoteSearch/Subtitles/srp?IsPerfectMatch=false" \
  -o "$EPISODE_SEARCH" \
  || fail "Episode subtitle search request failed."
EPISODE_RESULT_ID="$E2E_ROOT/episode-result-id.txt"
extract_titlovi_result "$EPISODE_SEARCH" "$EPISODE_RESULT_ID" "Episode" \
  || fail "Episode search produced no Titlovi.com result."

DEBUG_LOG=""
for _ in $(seq 1 20); do
  DEBUG_LOG="$(find_plugin_log emby-titlovi-debug.log)"
  if [ -n "$DEBUG_LOG" ] && [ -f "$DEBUG_LOG" ]; then
    break
  fi
  sleep 1
done
[ -n "$DEBUG_LOG" ] && [ -f "$DEBUG_LOG" ] \
  || fail "Emby Titlovi debug log was not created."

grep -F "mediaPath='/media/tv/Friends/Season 01/Friends S01E01 - The One Where Monica Gets a Roommate.mkv'" "$DEBUG_LOG" >/dev/null \
  || fail "Episode search did not prove the Emby adapter received the real Linux MediaPath."

if grep -F "Parent series IMDb id from SeriesProviderIds='tt0108778'." "$DEBUG_LOG" >/dev/null; then
  echo "Emby supplied parent-series IMDb tt0108778 through SeriesProviderIds"
elif grep -F "Search parameters resolved: type=Episode, query='Friends'" "$DEBUG_LOG" >/dev/null; then
  echo "Emby did not supply SeriesProviderIds; existing series-title fallback path was validated"
else
  fail "Episode diagnostics did not show either parent-series IMDb or the expected title fallback."
fi

echo "==> Downloading one movie subtitle through Emby"
MOVIE_ENCODED_ID="$(urlencode_file_value "$MOVIE_RESULT_ID")"
auth_curl -X POST "$BASE_URL/Items/$MOVIE_ID/RemoteSearch/Subtitles/$MOVIE_ENCODED_ID" >/dev/null \
  || fail "Movie subtitle download request failed."
MOVIE_SUBTITLE_PATH="$E2E_ROOT/movie-subtitle-path.txt"
wait_for_subtitle_file "$MOVIE_DIR" "$MOVIE_SUBTITLE_PATH" \
  || fail "Emby did not save the downloaded movie subtitle next to the Linux media file."
validate_utf8_subtitle "$MOVIE_SUBTITLE_PATH" "Movie"

echo "==> Downloading one episode subtitle through Emby"
EPISODE_ENCODED_ID="$(urlencode_file_value "$EPISODE_RESULT_ID")"
auth_curl -X POST "$BASE_URL/Items/$EPISODE_ID/RemoteSearch/Subtitles/$EPISODE_ENCODED_ID" >/dev/null \
  || fail "Episode subtitle download request failed."
EPISODE_SUBTITLE_PATH="$E2E_ROOT/episode-subtitle-path.txt"
wait_for_subtitle_file "$EPISODE_DIR" "$EPISODE_SUBTITLE_PATH" \
  || fail "Emby did not save the downloaded episode subtitle next to the Linux media file."
validate_utf8_subtitle "$EPISODE_SUBTITLE_PATH" "Episode"

echo "==> Emby 4.10 Linux end-to-end Titlovi.com validation passed"
echo "Validated: real Emby library -> plugin search -> Linux MediaPath -> Titlovi.com -> download -> host save -> UTF-8."

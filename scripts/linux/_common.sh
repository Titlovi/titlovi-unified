#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
BUILD_ROOT="$ROOT/.build"
DIST_ROOT="$ROOT/dist"
NUGET_CONFIG="$BUILD_ROOT/nuget/NuGet.Config"

ensure_dir() {
  mkdir -p "$1"
}

clean_dir() {
  local dir="$1"
  rm -rf "$dir"
  mkdir -p "$dir"
}

require_command() {
  local name="$1"
  if ! command -v "$name" >/dev/null 2>&1; then
    echo "ERROR: required command '$name' was not found." >&2
    exit 1
  fi
}

require_dotnet10() {
  require_command dotnet
  local version major
  version="$(dotnet --version)"
  major="${version%%.*}"
  if [[ "$version" == *-* ]] || ! [[ "$major" =~ ^[0-9]+$ ]] || (( major < 10 )); then
    echo "ERROR: a stable .NET 10 SDK or newer is required; found '$version'." >&2
    exit 1
  fi
  echo "Using .NET SDK $version"
}

ensure_nuget_config() {
  ensure_dir "$(dirname "$NUGET_CONFIG")"
  cat > "$NUGET_CONFIG" <<'NUGET'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
NUGET
}

run_logged() {
  local log="$1"
  shift
  ensure_dir "$(dirname "$log")"
  set +e
  "$@" 2>&1 | tee -a "$log"
  local status=${PIPESTATUS[0]}
  set -e
  return "$status"
}

read_project_version() {
  local project="$1"
  python3 - "$project" <<'PY'
import sys
import xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
for group in root.findall('PropertyGroup'):
    version = group.find('Version')
    if version is not None and version.text:
        print(version.text.strip())
        raise SystemExit(0)
raise SystemExit('Project Version was not found')
PY
}

#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

"$SCRIPT_DIR/build-emby.sh"
"$SCRIPT_DIR/build-jellyfin.sh"

echo
echo "Both Linux builds completed successfully."
echo "Distribution artifacts are under: $(cd "$SCRIPT_DIR/../.." && pwd)/dist"

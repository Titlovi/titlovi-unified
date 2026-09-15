#!/usr/bin/env bash
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/_common.sh"

PROJECT="$ROOT/tests/Titlovi.Tests/Titlovi.Tests.csproj"
BIN_ROOT="$BUILD_ROOT/Tests/bin"
OBJ_ROOT="$BUILD_ROOT/Tests/obj"
RESULTS_ROOT="$BUILD_ROOT/Tests/results"
LOG="$BUILD_ROOT/logs/Tests/test.log"
TRX="$RESULTS_ROOT/test-results.trx"

require_dotnet10
ensure_nuget_config
clean_dir "$BIN_ROOT"
clean_dir "$OBJ_ROOT"
clean_dir "$RESULTS_ROOT"
ensure_dir "$(dirname "$LOG")"
: > "$LOG"

echo "Titlovi.com.Unified regression suite (Linux)" | tee -a "$LOG"
echo "No live Titlovi.com requests are made." | tee -a "$LOG"

run_logged "$LOG" dotnet restore "$PROJECT" --configfile "$NUGET_CONFIG" --nologo --verbosity minimal -p:NuGetAudit=false
run_logged "$LOG" dotnet test "$PROJECT" -c Release --no-restore --nologo --verbosity normal --results-directory "$RESULTS_ROOT" --logger "trx;LogFileName=test-results.trx"

if [ ! -s "$TRX" ]; then
  echo "ERROR: test-results.trx was not produced." >&2
  exit 1
fi

echo "SUCCESS - regression suite passed." | tee -a "$LOG"
echo "Log: $LOG" | tee -a "$LOG"
echo "TRX: $TRX" | tee -a "$LOG"

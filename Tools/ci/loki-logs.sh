#!/usr/bin/env bash
# Query snake-game logs from Grafana Cloud Loki.
# Requires: logcli (sudo pacman -S logcli)
#
# Usage:
#   ./Tools/ci/loki-logs.sh              # last 20 logs from past hour
#   ./Tools/ci/loki-logs.sh --tail       # live tail (streaming)
#   ./Tools/ci/loki-logs.sh --errors     # only errors/exceptions
#   ./Tools/ci/loki-logs.sh --since 24h  # last 24 hours
set -euo pipefail

# Load .env from repo root if it exists
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
if [ -f "$REPO_ROOT/.env" ]; then
  set -a; source "$REPO_ROOT/.env"; set +a
fi

export LOKI_ADDR="https://logs-prod-036.grafana.net"
export LOKI_USERNAME="1538330"
export LOKI_PASSWORD="${GRAFANA_LOKI_READ_TOKEN:-}"

if [ -z "$LOKI_PASSWORD" ]; then
  echo "Set GRAFANA_LOKI_READ_TOKEN in .env (needs logs:read scope)"
  exit 1
fi

QUERY='{job="snake-game"}'
SINCE="1h"
LIMIT=50
MODE="query"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --tail) MODE="tail"; shift ;;
    --errors) QUERY='{job="snake-game"} |~ "Error|Exception"'; shift ;;
    --since) SINCE="$2"; shift 2 ;;
    --limit) LIMIT="$2"; shift 2 ;;
    --query) QUERY="$2"; shift 2 ;;
    *) echo "Unknown arg: $1"; exit 1 ;;
  esac
done

if [ "$MODE" = "tail" ]; then
  echo "Tailing logs from {job=\"snake-game\"}... (Ctrl+C to stop)"
  logcli query "$QUERY" --tail
else
  logcli query "$QUERY" --limit "$LIMIT" --since "$SINCE"
fi

#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:15075}"
WORKERS="${WORKERS:-16}"
REQUEST_SLEEP="${REQUEST_SLEEP:-0.03}"
MAX_AMOUNT="${MAX_AMOUNT:-10}"
DATABASE="${DATABASE:-postgres}"
RUN_DIR=".pi/tmp"
PID_FILE="$RUN_DIR/demo-search-loop.pid"
STOP_FILE="$RUN_DIR/demo-search-loop.stop"
LOG_FILE="$RUN_DIR/demo-search-loop.log"

mkdir -p "$RUN_DIR"
if [ -f "$PID_FILE" ] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
  echo "Search loop already running with supervisor PID $(cat "$PID_FILE")"
  exit 0
fi
rm -f "$STOP_FILE" "$LOG_FILE"

terms=(socal energy market search pipeline azure redis postgres kubernetes grafana prometheus loki cache latency throughput failover scaling replica database index query document cluster service pod observability metrics logs performance availability customer order invoice product region finance sales support security compliance email report contract analysis forecast north south east west global)

(
  echo "Starting random search loop: workers=$WORKERS base_url=$BASE_URL" | tee -a "$LOG_FILE"
  pids=()
  trap 'touch "$STOP_FILE"; for p in "${pids[@]:-}"; do kill "$p" 2>/dev/null || true; done; wait || true' EXIT INT TERM
  for worker in $(seq 1 "$WORKERS"); do
    (
      while [ ! -f "$STOP_FILE" ]; do
        a=${terms[$RANDOM % ${#terms[@]}]}
        b=${terms[$RANDOM % ${#terms[@]}]}
        c=${terms[$RANDOM % ${#terms[@]}]}
        case $((RANDOM % 3)) in
          0) query="$a" ;;
          1) query="$a $b" ;;
          *) query="$a $b $c" ;;
        esac
        payload=$(printf '{"query":"%s","maxAmount":%s,"caseSensitive":false,"database":"%s"}' "$query" "$MAX_AMOUNT" "$DATABASE")
        curl -fs --connect-timeout 3 --max-time 30 -o /dev/null -X POST "$BASE_URL/api/search" -H 'Content-Type: application/json' -d "$payload" 2>/dev/null || true
        sleep "$REQUEST_SLEEP"
      done
    ) &
    pids+=("$!")
  done
  wait
) &

echo $! > "$PID_FILE"
echo "Search loop started. PID: $(cat "$PID_FILE")"
echo "Stop with: scripts/demo-search-loop-stop.sh"

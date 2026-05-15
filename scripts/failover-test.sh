#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5075}"
BEFORE_REQUESTS="${BEFORE_REQUESTS:-10}"
DURING_REQUESTS="${DURING_REQUESTS:-40}"
AFTER_REQUESTS="${AFTER_REQUESTS:-10}"
REQUEST_DELAY_SECONDS="${REQUEST_DELAY_SECONDS:-0.05}"
TARGET_SERVICE="${TARGET_SERVICE:-searchapi1}"
TARGET_CONTAINER="${TARGET_CONTAINER:-search-api-1}"
OUTPUT="${OUTPUT:-dokumentation/failover-results.csv}"

QUERY="${QUERY:-socal energy}"
DATABASE="${DATABASE:-postgres}"
MAX_AMOUNT="${MAX_AMOUNT:-10}"
CASE_SENSITIVE="${CASE_SENSITIVE:-false}"

payload=$(printf '{"query":"%s","maxAmount":%s,"caseSensitive":%s,"database":"%s"}' \
  "$QUERY" "$MAX_AMOUNT" "$CASE_SENSITIVE" "$DATABASE")

mkdir -p "$(dirname "$OUTPUT")"

for command in curl docker awk date; do
  if ! command -v "$command" >/dev/null 2>&1; then
    echo "Missing required command: $command" >&2
    exit 1
  fi
done

if ! curl -fsS "$BASE_URL/api/health" >/dev/null; then
  echo "Load balancer is not reachable at $BASE_URL/api/health" >&2
  exit 1
fi

if ! docker compose ps "$TARGET_SERVICE" --format '{{.State}}' | grep -q '^running$'; then
  echo "Target service $TARGET_SERVICE must be running before the failover test." >&2
  exit 1
fi

target_was_stopped=false
restore_target() {
  if [ "$target_was_stopped" = true ]; then
    echo "Restoring $TARGET_SERVICE..." >&2
    docker compose start "$TARGET_SERVICE" >/dev/null
  fi
}
trap restore_target EXIT

header_value() {
  local file="$1"
  local name="$2"
  awk -v header="$name" '
    BEGIN { IGNORECASE = 1 }
    index($0, header ":") == 1 {
      sub("^[^:]+:[[:space:]]*", "", $0)
      gsub("\r", "", $0)
      print $0
      exit
    }
  ' "$file"
}

request_once() {
  local phase="$1"
  local iteration="$2"
  local headers
  local body
  local curl_result
  local curl_exit=0
  local status="000"
  local seconds="0"
  local cache_status=""
  local backend=""
  local instance=""
  local timestamp

  headers="$(mktemp)"
  body="$(mktemp)"
  timestamp="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"

  if ! curl_result=$(curl -sS \
    -D "$headers" \
    -o "$body" \
    -w "%{http_code} %{time_total}" \
    -X POST "$BASE_URL/api/search" \
    -H "Content-Type: application/json" \
    -d "$payload" 2>/dev/null); then
    curl_exit=$?
  fi

  if [ "$curl_exit" -eq 0 ]; then
    status="${curl_result%% *}"
    seconds="${curl_result##* }"
    cache_status="$(header_value "$headers" "X-Search-Cache")"
    backend="$(header_value "$headers" "X-LB-Backend")"
    instance="$(header_value "$headers" "X-SearchApi-Instance")"
  fi

  rm -f "$headers" "$body"

  awk -v timestamp="$timestamp" \
      -v phase="$phase" \
      -v iteration="$iteration" \
      -v status="$status" \
      -v curl_exit="$curl_exit" \
      -v cache="$cache_status" \
      -v backend="$backend" \
      -v instance="$instance" \
      -v seconds="$seconds" \
      'BEGIN { printf "%s,%s,%s,%s,%s,%s,%s,%s,%.3f\n", timestamp, phase, iteration, status, curl_exit, cache, backend, instance, seconds * 1000 }'
}

run_phase() {
  local phase="$1"
  local count="$2"
  local i

  echo "Running phase '$phase' with $count requests..." >&2
  for i in $(seq 1 "$count"); do
    request_once "$phase" "$i"
    sleep "$REQUEST_DELAY_SECONDS"
  done
}

print_stats() {
  awk -F, '
    NR > 1 {
      total[$2]++
      if ($4 == 200 && $5 == 0) success[$2]++
      else failure[$2]++
      if ($4 == 200 && $5 == 0) {
        latency[$2] += $9
        countLatency[$2]++
        if (!($2 in min) || $9 < min[$2]) min[$2] = $9
        if ($9 > max[$2]) max[$2] = $9
      }
      if ($8 != "") instance[$2 "," $8]++
    }
    END {
      for (phase in total) {
        avg = countLatency[phase] ? latency[phase] / countLatency[phase] : 0
        printf "%-16s total=%d success=%d failure=%d success_rate=%.1f%% avg=%.2fms min=%.2fms max=%.2fms\n",
          phase, total[phase], success[phase], failure[phase], success[phase] * 100 / total[phase], avg, min[phase], max[phase]
      }
      print ""
      print "Instances used:"
      for (key in instance) {
        print key "=" instance[key]
      }
    }
  ' "$OUTPUT" | sort
}

{
  echo "timestamp,phase,iteration,http_status,curl_exit,cache_status,backend,instance,time_ms"
  run_phase "before-stop" "$BEFORE_REQUESTS"

  echo "Stopping $TARGET_SERVICE..." >&2
  docker compose stop "$TARGET_SERVICE" >/dev/null
  target_was_stopped=true

  run_phase "during-stop" "$DURING_REQUESTS"

  echo "Starting $TARGET_SERVICE again..." >&2
  docker compose start "$TARGET_SERVICE" >/dev/null
  target_was_stopped=false

  run_phase "after-restart" "$AFTER_REQUESTS"
} > "$OUTPUT"

echo "Wrote raw results to $OUTPUT"
echo
print_stats

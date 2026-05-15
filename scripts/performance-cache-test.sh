#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5075}"
ITERATIONS="${ITERATIONS:-50}"
QUERY="${QUERY:-socal energy}"
DATABASE="${DATABASE:-postgres}"
MAX_AMOUNT="${MAX_AMOUNT:-10}"
CASE_SENSITIVE="${CASE_SENSITIVE:-false}"
OUTPUT="${OUTPUT:-dokumentation/performance-cache-results.csv}"

payload=$(printf '{"query":"%s","maxAmount":%s,"caseSensitive":%s,"database":"%s"}' \
  "$QUERY" "$MAX_AMOUNT" "$CASE_SENSITIVE" "$DATABASE")

mkdir -p "$(dirname "$OUTPUT")"

for command in curl docker awk sort; do
  if ! command -v "$command" >/dev/null 2>&1; then
    echo "Missing required command: $command" >&2
    exit 1
  fi
done

if ! curl -fsS "$BASE_URL/api/health" >/dev/null; then
  echo "Nginx/API endpoint is not reachable at $BASE_URL/api/health" >&2
  exit 1
fi

if ! docker exec search-redis redis-cli PING >/dev/null; then
  echo "Redis container 'search-redis' is not reachable." >&2
  exit 1
fi

flush_cache() {
  docker exec search-redis redis-cli FLUSHDB >/dev/null
}

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
  local scenario="$1"
  local iteration="$2"
  local headers
  local body
  local curl_result
  local status
  local seconds
  local cache_status
  local backend
  local instance

  headers="$(mktemp)"
  body="$(mktemp)"

  curl_result=$(curl -sS \
    -D "$headers" \
    -o "$body" \
    -w "%{http_code} %{time_total}" \
    -X POST "$BASE_URL/api/search" \
    -H "Content-Type: application/json" \
    -d "$payload")

  status="${curl_result%% *}"
  seconds="${curl_result##* }"
  cache_status="$(header_value "$headers" "X-Search-Cache")"
  backend="$(header_value "$headers" "X-LB-Backend" | tr ',' ';')"
  instance="$(header_value "$headers" "X-SearchApi-Instance" | tr ',' ';')"

  rm -f "$headers" "$body"

  awk -v scenario="$scenario" \
      -v iteration="$iteration" \
      -v status="$status" \
      -v cache="$cache_status" \
      -v backend="$backend" \
      -v instance="$instance" \
      -v seconds="$seconds" \
      'BEGIN { printf "%s,%s,%s,%s,%s,%s,%.3f\n", scenario, iteration, status, cache, backend, instance, seconds * 1000 }'
}

run_cold_scenario() {
  local i
  echo "Running cold-cache scenario: $ITERATIONS requests, Redis flushed before each request..." >&2
  for i in $(seq 1 "$ITERATIONS"); do
    flush_cache
    request_once "cold-cache" "$i"
  done
}

run_hot_scenario() {
  local i
  echo "Running hot-cache scenario: one prime request, then $ITERATIONS measured requests..." >&2
  flush_cache
  request_once "warmup" 0 >/dev/null
  for i in $(seq 1 "$ITERATIONS"); do
    request_once "hot-cache" "$i"
  done
}

print_stats() {
  local scenario="$1"
  awk -F, -v scenario="$scenario" '
    NR > 1 && $1 == scenario && $3 == 200 {
      print $7
    }
  ' "$OUTPUT" | sort -n | awk -v scenario="$scenario" -v iterations="$ITERATIONS" '
    {
      values[++count] = $1
      sum += $1
    }
    END {
      if (count == 0) {
        printf "%-12s no successful requests\n", scenario
        next
      }

      p50Index = int(count * 0.50 + 0.999999)
      p95Index = int(count * 0.95 + 0.999999)
      p99Index = int(count * 0.99 + 0.999999)
      if (p50Index < 1) p50Index = 1
      if (p95Index < 1) p95Index = 1
      if (p99Index < 1) p99Index = 1
      if (p50Index > count) p50Index = count
      if (p95Index > count) p95Index = count
      if (p99Index > count) p99Index = count

      throughput = count / (sum / 1000)
      printf "%-12s count=%d avg=%.2fms p50=%.2fms p95=%.2fms p99=%.2fms min=%.2fms max=%.2fms throughput=%.1f req/s\n",
        scenario, count, sum / count, values[p50Index], values[p95Index], values[p99Index], values[1], values[count], throughput
    }
  '
}

{
  echo "scenario,iteration,http_status,cache_status,backend,instance,time_ms"
  run_cold_scenario
  run_hot_scenario
} > "$OUTPUT"

echo "Wrote raw results to $OUTPUT"
echo
print_stats "cold-cache"
print_stats "hot-cache"
echo
echo "Cache status counts:"
awk -F, 'NR > 1 { counts[$1 "," $4]++ } END { for (key in counts) print key "=" counts[key] }' "$OUTPUT" | sort

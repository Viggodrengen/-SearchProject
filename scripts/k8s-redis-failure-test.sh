#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:15075}"
NAMESPACE="${NAMESPACE:-searchproject}"
REQUESTS="${REQUESTS:-20}"
QUERY="${QUERY:-socal energy}"
DATABASE="${DATABASE:-postgres}"
OUTPUT="${OUTPUT:-dokumentation/k8s-redis-failure-results.csv}"

payload=$(printf '{"query":"%s","maxAmount":10,"caseSensitive":false,"database":"%s"}' "$QUERY" "$DATABASE")
mkdir -p "$(dirname "$OUTPUT")"

for command in kubectl curl awk date; do
  if ! command -v "$command" >/dev/null 2>&1; then
    echo "Missing required command: $command" >&2
    exit 1
  fi
done

restore_redis() {
  kubectl scale deployment/redis -n "$NAMESPACE" --replicas=1 >/dev/null || true
}
trap restore_redis EXIT

request_once() {
  local phase="$1" iteration="$2" headers body curl_result status seconds cache_status timestamp
  headers="$(mktemp)"; body="$(mktemp)"; timestamp="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
  curl_result=$(curl -sS -D "$headers" -o "$body" -w "%{http_code} %{time_total}" \
    -X POST "$BASE_URL/api/search" -H "Content-Type: application/json" -d "$payload" || echo "000 0")
  status="${curl_result%% *}"; seconds="${curl_result##* }"
  cache_status=$(awk 'BEGIN{IGNORECASE=1} /^X-Search-Cache:/ {sub("^[^:]+:[[:space:]]*",""); gsub("\r",""); print; exit}' "$headers")
  rm -f "$headers" "$body"
  awk -v ts="$timestamp" -v phase="$phase" -v iteration="$iteration" -v status="$status" -v cache="$cache_status" -v seconds="$seconds" \
    'BEGIN { printf "%s,%s,%s,%s,%s,%.3f\n", ts, phase, iteration, status, cache, seconds * 1000 }'
}

run_phase() {
  local phase="$1" count="$2"
  echo "Running $phase with $count requests..." >&2
  for i in $(seq 1 "$count"); do
    request_once "$phase" "$i"
    sleep 0.05
  done
}

{
  echo "timestamp,phase,iteration,http_status,cache_status,time_ms"
  run_phase "redis-up" "$REQUESTS"
  echo "Scaling Redis to 0 replicas..." >&2
  kubectl scale deployment/redis -n "$NAMESPACE" --replicas=0 >/dev/null
  sleep 8
  run_phase "redis-down" "$REQUESTS"
  echo "Scaling Redis back to 1 replica..." >&2
  kubectl scale deployment/redis -n "$NAMESPACE" --replicas=1 >/dev/null
  kubectl rollout status deployment/redis -n "$NAMESPACE" --timeout=120s >/dev/null
  sleep 5
  run_phase "redis-restored" "$REQUESTS"
} > "$OUTPUT"

echo "Wrote raw results to $OUTPUT"
awk -F, '
  NR > 1 { total[$2]++; if ($4 == 200) success[$2]++; sum[$2]+=$6; if ($5 != "") cache[$2 "," $5]++ }
  END {
    for (p in total) printf "%-15s total=%d success=%d success_rate=%.1f%% avg=%.2fms\n", p, total[p], success[p], success[p]*100/total[p], sum[p]/total[p]
    print ""; print "Cache statuses:"; for (k in cache) print k "=" cache[k]
  }
' "$OUTPUT" | sort

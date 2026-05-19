#!/usr/bin/env bash
set -euo pipefail

NAMESPACE="${NAMESPACE:-searchproject}"
BASE_URL="${BASE_URL:-http://localhost:15075}"
BEFORE_REQUESTS="${BEFORE_REQUESTS:-10}"
DURING_REQUESTS="${DURING_REQUESTS:-40}"
AFTER_REQUESTS="${AFTER_REQUESTS:-10}"
REQUEST_DELAY_SECONDS="${REQUEST_DELAY_SECONDS:-0.05}"
OUTPUT="${OUTPUT:-dokumentation/k8s-failover-results.csv}"
QUERY="${QUERY:-socal energy}"
DATABASE="${DATABASE:-postgres}"
MAX_AMOUNT="${MAX_AMOUNT:-10}"
CASE_SENSITIVE="${CASE_SENSITIVE:-false}"

for command in kubectl curl awk date; do
  if ! command -v "$command" >/dev/null 2>&1; then
    echo "Missing required command: $command" >&2
    exit 1
  fi
done

payload=$(printf '{"query":"%s","maxAmount":%s,"caseSensitive":%s,"database":"%s"}' \
  "$QUERY" "$MAX_AMOUNT" "$CASE_SENSITIVE" "$DATABASE")

mkdir -p "$(dirname "$OUTPUT")"

if ! curl -fsS "$BASE_URL/api/health" >/dev/null; then
  echo "Kubernetes Nginx/API endpoint is not reachable at $BASE_URL/api/health" >&2
  exit 1
fi

restore_replicas() {
  kubectl scale deployment/search-api -n "$NAMESPACE" --replicas=2 >/dev/null || true
}
trap restore_replicas EXIT

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
  local phase="$1" iteration="$2" headers body curl_result curl_exit status seconds cache_status backend instance timestamp
  headers="$(mktemp)"
  body="$(mktemp)"
  curl_exit=0; status="000"; seconds="0"; cache_status=""; backend=""; instance=""
  timestamp="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
  if ! curl_result=$(curl -sS -D "$headers" -o "$body" -w "%{http_code} %{time_total}" \
    -X POST "$BASE_URL/api/search" -H "Content-Type: application/json" -d "$payload" 2>/dev/null); then
    curl_exit=$?
  fi
  if [ "$curl_exit" -eq 0 ]; then
    status="${curl_result%% *}"
    seconds="${curl_result##* }"
    cache_status="$(header_value "$headers" "X-Search-Cache")"
    backend="$(header_value "$headers" "X-LB-Backend" | tr ',' ';')"
    instance="$(header_value "$headers" "X-SearchApi-Instance" | tr ',' ';')"
  fi
  rm -f "$headers" "$body"
  awk -v timestamp="$timestamp" -v phase="$phase" -v iteration="$iteration" -v status="$status" -v curl_exit="$curl_exit" \
      -v cache="$cache_status" -v backend="$backend" -v instance="$instance" -v seconds="$seconds" \
      'BEGIN { printf "%s,%s,%s,%s,%s,%s,%s,%s,%.3f\n", timestamp, phase, iteration, status, curl_exit, cache, backend, instance, seconds * 1000 }'
}

run_phase() {
  local phase="$1" count="$2"
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
      if ($4 == 200 && $5 == 0) success[$2]++; else failure[$2]++
      if ($4 == 200 && $5 == 0) { latency[$2] += $9; countLatency[$2]++; if (!($2 in min) || $9 < min[$2]) min[$2] = $9; if ($9 > max[$2]) max[$2] = $9 }
      if ($8 != "") instance[$2 "," $8]++
    }
    END {
      for (phase in total) {
        avg = countLatency[phase] ? latency[phase] / countLatency[phase] : 0
        printf "%-16s total=%d success=%d failure=%d success_rate=%.1f%% avg=%.2fms min=%.2fms max=%.2fms\n", phase, total[phase], success[phase], failure[phase], success[phase] * 100 / total[phase], avg, min[phase], max[phase]
      }
      print ""; print "Instances used:"; for (key in instance) print key "=" instance[key]
    }
  ' "$OUTPUT" | sort
}

{
  echo "timestamp,phase,iteration,http_status,curl_exit,cache_status,backend,instance,time_ms"
  kubectl scale deployment/search-api -n "$NAMESPACE" --replicas=2 >/dev/null
  kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=120s >/dev/null
  run_phase "before-scale-down" "$BEFORE_REQUESTS"
  echo "Scaling search-api deployment from 2 replicas to 1..." >&2
  kubectl scale deployment/search-api -n "$NAMESPACE" --replicas=1 >/dev/null
  kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=120s >/dev/null
  run_phase "during-one-replica" "$DURING_REQUESTS"
  echo "Scaling search-api deployment back to 2 replicas..." >&2
  kubectl scale deployment/search-api -n "$NAMESPACE" --replicas=2 >/dev/null
  kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=120s >/dev/null
  run_phase "after-scale-up" "$AFTER_REQUESTS"
} > "$OUTPUT"

echo "Wrote raw results to $OUTPUT"
echo
print_stats

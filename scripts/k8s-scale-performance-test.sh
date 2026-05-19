#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:15075}"
NAMESPACE="${NAMESPACE:-searchproject}"
LOW_REPLICAS="${LOW_REPLICAS:-1}"
HIGH_REPLICAS="${HIGH_REPLICAS:-10}"
REQUESTS="${REQUESTS:-120}"
CONCURRENCY="${CONCURRENCY:-20}"
QUERY="${QUERY:-socal energy}"
DATABASE="${DATABASE:-postgres}"
OUTPUT="${OUTPUT:-dokumentation/k8s-scale-performance-results.csv}"

payload=$(printf '{"query":"%s","maxAmount":10,"caseSensitive":false,"database":"%s"}' "$QUERY" "$DATABASE")
mkdir -p "$(dirname "$OUTPUT")"

for command in kubectl curl awk date xargs seq; do
  if ! command -v "$command" >/dev/null 2>&1; then
    echo "Missing required command: $command" >&2
    exit 1
  fi
done

restore_replicas() {
  kubectl scale deployment/search-api -n "$NAMESPACE" --replicas=2 >/dev/null 2>&1 || true
}
trap restore_replicas EXIT

request_once() {
  local scenario="$1"
  local iteration="$2"
  local started headers body curl_result curl_exit status seconds cache_status instance
  started="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
  headers="$(mktemp)"
  body="$(mktemp)"
  curl_exit=0
  status="000"
  seconds="0"
  cache_status=""
  instance=""

  if ! curl_result=$(curl -sS -D "$headers" -o "$body" -w "%{http_code} %{time_total}" \
    -X POST "$BASE_URL/api/search" \
    -H "Content-Type: application/json" \
    -d "$payload" 2>/dev/null); then
    curl_exit=$?
  fi

  if [ "$curl_exit" -eq 0 ]; then
    status="${curl_result%% *}"
    seconds="${curl_result##* }"
    cache_status=$(awk 'BEGIN{IGNORECASE=1} /^X-Search-Cache:/ {sub("^[^:]+:[[:space:]]*",""); gsub("\r",""); print; exit}' "$headers")
    instance=$(awk 'BEGIN{IGNORECASE=1} /^X-SearchApi-Instance:/ {sub("^[^:]+:[[:space:]]*",""); gsub("\r",""); print; exit}' "$headers")
  fi

  rm -f "$headers" "$body"
  awk -v ts="$started" -v scenario="$scenario" -v iteration="$iteration" -v status="$status" -v curl_exit="$curl_exit" -v cache="$cache_status" -v instance="$instance" -v seconds="$seconds" \
    'BEGIN { printf "%s,%s,%s,%s,%s,%s,%s,%.3f\n", ts, scenario, iteration, status, curl_exit, cache, instance, seconds * 1000 }'
}

run_load() {
  local scenario="$1"
  echo "Running $scenario: $REQUESTS requests with concurrency=$CONCURRENCY..." >&2
  export BASE_URL payload
  export -f request_once
  seq 1 "$REQUESTS" | xargs -P "$CONCURRENCY" -I{} bash -c 'request_once "$0" "$1"' "$scenario" {}
}

print_stats() {
  awk -F, '
    NR > 1 {
      total[$2]++
      if ($4 == 200 && $5 == 0) { ok[$2]++; lat[$2,++n[$2]]=$8; sum[$2]+=$8; instances[$2 "," $7]++ }
      else fail[$2]++
    }
    END {
      for (s in total) {
        # simple insertion sort per scenario
        for (i=1;i<=n[s];i++) for (j=i+1;j<=n[s];j++) if (lat[s,i] > lat[s,j]) { tmp=lat[s,i]; lat[s,i]=lat[s,j]; lat[s,j]=tmp }
        p95i=int(n[s]*0.95+0.999999); if(p95i<1)p95i=1; if(p95i>n[s])p95i=n[s]
        avg=n[s]?sum[s]/n[s]:0
        throughput=n[s]?1000*n[s]/sum[s]:0
        printf "%-12s total=%d success=%d failure=%d avg=%.2fms p95=%.2fms synthetic_throughput=%.1f req/s\n", s,total[s],ok[s],fail[s],avg,lat[s,p95i],throughput
      }
      print ""; print "Instances used:"; for(k in instances) print k "=" instances[k]
    }
  ' "$OUTPUT" | sort
}

{
  echo "timestamp,scenario,iteration,http_status,curl_exit,cache_status,instance,time_ms"

  echo "Scaling SearchApi to $LOW_REPLICAS replica(s)..." >&2
  kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$LOW_REPLICAS" >/dev/null
  kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=180s >/dev/null
  sleep 5
  run_load "${LOW_REPLICAS}-replica"

  echo "Scaling SearchApi to $HIGH_REPLICAS replicas..." >&2
  kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$HIGH_REPLICAS" >/dev/null
  kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=240s >/dev/null
  sleep 8
  run_load "${HIGH_REPLICAS}-replicas"
} > "$OUTPUT"

echo "Wrote raw results to $OUTPUT"
echo
print_stats

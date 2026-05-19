#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:15075}"
NAMESPACE="${NAMESPACE:-searchproject}"
ITERATIONS="${ITERATIONS:-40}"
REDIS_DOWN_REQUESTS="${REDIS_DOWN_REQUESTS:-3}"
API_REPLICAS_NORMAL="${API_REPLICAS_NORMAL:-2}"
API_REPLICAS_FAILOVER="${API_REPLICAS_FAILOVER:-1}"
RUN_SCALE_PERFORMANCE="${RUN_SCALE_PERFORMANCE:-true}"
SCALE_LOW_REPLICAS="${SCALE_LOW_REPLICAS:-1}"
SCALE_HIGH_REPLICAS="${SCALE_HIGH_REPLICAS:-10}"
SCALE_REQUESTS="${SCALE_REQUESTS:-160}"
SCALE_CONCURRENCY="${SCALE_CONCURRENCY:-30}"
QUERY="${QUERY:-socal energy}"
DATABASE="${DATABASE:-postgres}"
MAX_AMOUNT="${MAX_AMOUNT:-10}"
CASE_SENSITIVE="${CASE_SENSITIVE:-false}"
SLEEP_BETWEEN_PHASES="${SLEEP_BETWEEN_PHASES:-20}"

payload=$(printf '{"query":"%s","maxAmount":%s,"caseSensitive":%s,"database":"%s"}' \
  "$QUERY" "$MAX_AMOUNT" "$CASE_SENSITIVE" "$DATABASE")

restore_cluster_state() {
  kubectl scale deployment/redis -n "$NAMESPACE" --replicas=1 >/dev/null 2>&1 || true
  kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$API_REPLICAS_NORMAL" >/dev/null 2>&1 || true
}
trap restore_cluster_state EXIT

say() {
  echo
  echo "============================================================"
  echo "$1"
  echo "============================================================"
}

request_loop() {
  local label="$1"
  local count="$2"
  local i
  for i in $(seq 1 "$count"); do
    curl -fsS -o /dev/null -X POST "$BASE_URL/api/search" \
      -H "Content-Type: application/json" \
      -d "$payload"
    printf "%s request %s/%s\r" "$label" "$i" "$count"
    sleep 0.05
  done
  echo
}

redis_pod() {
  kubectl get pod -n "$NAMESPACE" -l app=redis -o jsonpath='{.items[0].metadata.name}'
}

flush_cache() {
  kubectl exec -n "$NAMESPACE" "$(redis_pod)" -- redis-cli FLUSHDB >/dev/null
}

say "0) Smoke test: API health"
curl -fsS "$BASE_URL/api/health"
echo

say "1) Cold-cache phase: Redis tømmes før requests"
echo "Grafana: request rate bør stige, cache decisions bør vise MISS."
for i in $(seq 1 "$ITERATIONS"); do
  flush_cache
  curl -fsS -o /dev/null -X POST "$BASE_URL/api/search" \
    -H "Content-Type: application/json" \
    -d "$payload"
  printf "cold-cache request %s/%s\r" "$i" "$ITERATIONS"
  sleep 0.05
done
echo
sleep "$SLEEP_BETWEEN_PHASES"

say "2) Hot-cache phase: én warmup, derefter gentagne requests"
echo "Grafana: cache decisions bør vise HIT, og cache hit ratio bør stige."
flush_cache
curl -fsS -o /dev/null -X POST "$BASE_URL/api/search" -H "Content-Type: application/json" -d "$payload"
request_loop "hot-cache" "$ITERATIONS"
sleep "$SLEEP_BETWEEN_PHASES"

say "3) Redis failure phase: cachelaget fjernes kort"
echo "Grafana: requests bør stadig give 200, men cache hits forsvinder og søgetiden kan stige."
kubectl scale deployment/redis -n "$NAMESPACE" --replicas=0 >/dev/null
sleep 8
request_loop "redis-down" "$REDIS_DOWN_REQUESTS"
kubectl scale deployment/redis -n "$NAMESPACE" --replicas=1 >/dev/null
kubectl rollout status deployment/redis -n "$NAMESPACE" --timeout=120s >/dev/null
sleep "$SLEEP_BETWEEN_PHASES"

say "4) Scale-down phase: SearchApi fra $API_REPLICAS_NORMAL replikaer til $API_REPLICAS_FAILOVER"
echo "Grafana: SearchApi desired/available bør falde, mens 200 responses fortsætter."
kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$API_REPLICAS_FAILOVER" >/dev/null
kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=120s >/dev/null
request_loop "one-replica" "$ITERATIONS"
sleep "$SLEEP_BETWEEN_PHASES"

say "5) Scale-up phase: SearchApi tilbage til $API_REPLICAS_NORMAL replikaer"
echo "Grafana: SearchApi desired/available bør gå tilbage til $API_REPLICAS_NORMAL."
kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$API_REPLICAS_NORMAL" >/dev/null
kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=120s >/dev/null
request_loop "two-replicas" "$ITERATIONS"

if [ "$RUN_SCALE_PERFORMANCE" = "true" ]; then
  say "6) Performance scaling phase: få vs. mange API-replikaer"
  echo "Grafana: request rate, latency og SearchApi desired/available sammenlignes under parallel load."
  BASE_URL="$BASE_URL" \
    NAMESPACE="$NAMESPACE" \
    LOW_REPLICAS="$SCALE_LOW_REPLICAS" \
    HIGH_REPLICAS="$SCALE_HIGH_REPLICAS" \
    REQUESTS="$SCALE_REQUESTS" \
    CONCURRENCY="$SCALE_CONCURRENCY" \
    scripts/k8s-scale-performance-test.sh
fi

say "Demo story complete"
echo "Åbn Grafana: http://localhost:13000"
echo "Dashboard: SearchProject Walking Skeleton - Demo Dashboard"

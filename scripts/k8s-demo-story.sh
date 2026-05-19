#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:15075}"
NAMESPACE="${NAMESPACE:-searchproject}"
ITERATIONS="${ITERATIONS:-300}"
REDIS_DOWN_REQUESTS="${REDIS_DOWN_REQUESTS:-120}"
API_REPLICAS_NORMAL="${API_REPLICAS_NORMAL:-2}"
API_REPLICAS_FAILOVER="${API_REPLICAS_FAILOVER:-1}"
RUN_SCALE_PERFORMANCE="${RUN_SCALE_PERFORMANCE:-true}"
SCALE_LOW_REPLICAS="${SCALE_LOW_REPLICAS:-1}"
SCALE_HIGH_REPLICAS="${SCALE_HIGH_REPLICAS:-10}"
SCALE_REQUESTS="${SCALE_REQUESTS:-800}"
SCALE_CONCURRENCY="${SCALE_CONCURRENCY:-40}"
QUERY="${QUERY:-socal energy}"
DATABASE="${DATABASE:-postgres}"
MAX_AMOUNT="${MAX_AMOUNT:-10}"
CASE_SENSITIVE="${CASE_SENSITIVE:-false}"
SLEEP_BETWEEN_PHASES="${SLEEP_BETWEEN_PHASES:-12}"
REQUEST_SLEEP="${REQUEST_SLEEP:-0.03}"
DEMO_PAUSE="${DEMO_PAUSE:-false}"

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

pause_for_observation() {
  local message="$1"
  echo
  echo "👀 Kig i Grafana: $message"
  if [ "$DEMO_PAUSE" = "true" ] && [ -t 0 ]; then
    read -r -p "Tryk Enter for næste fase... " _
  else
    sleep "$SLEEP_BETWEEN_PHASES"
  fi
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
    sleep "$REQUEST_SLEEP"
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
echo "Demoen kører automatisk videre. Følg terminalens faseoverskrifter og Grafana-dashboardet ved siden af."

say "1) Cold-cache phase: Redis tømmes før requests"
echo "Grafana: panel 3 viser MISS, panel 4 viser database pressure, panel 5 viser højere search duration."
for i in $(seq 1 "$ITERATIONS"); do
  flush_cache
  curl -fsS -o /dev/null -X POST "$BASE_URL/api/search" \
    -H "Content-Type: application/json" \
    -d "$payload"
  printf "cold-cache request %s/%s\r" "$i" "$ITERATIONS"
  sleep "$REQUEST_SLEEP"
done
echo
pause_for_observation "Cache decisions viser MISS, database pressure stiger, og search duration er højere end ved hits."

say "2) Hot-cache phase: én warmup, derefter gentagne requests"
echo "Grafana: panel 3 viser HIT, panel 4 falder, og cache hit ratio stiger."
flush_cache
curl -fsS -o /dev/null -X POST "$BASE_URL/api/search" -H "Content-Type: application/json" -d "$payload"
request_loop "hot-cache" "$ITERATIONS"
pause_for_observation "Cache decisions viser HIT, cache hit ratio stiger, og database pressure falder."

say "3) Redis failure phase: cachelaget fjernes kort"
echo "Grafana: panel 6 viser Redis=0, panel 1 health forbliver 200, og panel 3 viser fallback."
kubectl scale deployment/redis -n "$NAMESPACE" --replicas=0 >/dev/null
sleep 8
request_loop "redis-down" "$REDIS_DOWN_REQUESTS"
kubectl scale deployment/redis -n "$NAMESPACE" --replicas=1 >/dev/null
kubectl rollout status deployment/redis -n "$NAMESPACE" --timeout=120s >/dev/null
pause_for_observation "Redis availability falder til 0, men API traffic er stadig HTTP 200, og cache status viser fallback."

say "4) Scale-down phase: SearchApi fra $API_REPLICAS_NORMAL replikaer til $API_REPLICAS_FAILOVER"
echo "Grafana: panel 7 desired/available falder, mens panel 1 stadig viser succesfulde søgninger."
kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$API_REPLICAS_FAILOVER" >/dev/null
kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=120s >/dev/null
request_loop "one-replica" "$ITERATIONS"
pause_for_observation "SearchApi desired/available falder, men HTTP 200 fortsætter via samme Service endpoint."

say "5) Scale-up phase: SearchApi tilbage til $API_REPLICAS_NORMAL replikaer"
echo "Grafana: panel 7 går tilbage til $API_REPLICAS_NORMAL, og panel 8 viser hvilke pods der modtager trafik."
kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$API_REPLICAS_NORMAL" >/dev/null
kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=120s >/dev/null
request_loop "two-replicas" "$ITERATIONS"
pause_for_observation "SearchApi desired/available er tilbage på normal kapacitet."

if [ "$RUN_SCALE_PERFORMANCE" = "true" ]; then
  say "6) Performance scaling phase: få vs. mange API-replikaer"
  echo "Grafana: panel 8 viser request rate pr. API-pod, panel 2 viser latency, og panel 7 viser antal replikaer."
  BASE_URL="$BASE_URL" \
    NAMESPACE="$NAMESPACE" \
    LOW_REPLICAS="$SCALE_LOW_REPLICAS" \
    HIGH_REPLICAS="$SCALE_HIGH_REPLICAS" \
    REQUESTS="$SCALE_REQUESTS" \
    CONCURRENCY="$SCALE_CONCURRENCY" \
    scripts/k8s-scale-performance-test.sh
  pause_for_observation "Sammenlign 1 vs. mange replikaer: request rate, latency og pod-fordeling. I Minikube kan flere replikaer også afsløre lokale ressourcegrænser."
fi

say "Demo story complete"
echo "Åbn Grafana: http://localhost:13000"
echo "Dashboard: SearchProject Walking Skeleton - Demo Dashboard"

#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:15075}"
NAMESPACE="${NAMESPACE:-searchproject}"
ITERATIONS="${ITERATIONS:-160}"
REDIS_DOWN_REQUESTS="${REDIS_DOWN_REQUESTS:-50}"
API_REPLICAS_NORMAL="${API_REPLICAS_NORMAL:-2}"
API_REPLICAS_FAILOVER="${API_REPLICAS_FAILOVER:-1}"
RUN_SCALE_PERFORMANCE="${RUN_SCALE_PERFORMANCE:-true}"
SCALE_LOW_REPLICAS="${SCALE_LOW_REPLICAS:-1}"
SCALE_HIGH_REPLICAS="${SCALE_HIGH_REPLICAS:-10}"
SCALE_REQUESTS="${SCALE_REQUESTS:-1000}"
SCALE_CONCURRENCY="${SCALE_CONCURRENCY:-50}"
QUERY="${QUERY:-socal energy}"
DATABASE="${DATABASE:-postgres}"
MAX_AMOUNT="${MAX_AMOUNT:-10}"
CASE_SENSITIVE="${CASE_SENSITIVE:-false}"
SLEEP_BETWEEN_PHASES="${SLEEP_BETWEEN_PHASES:-12}"
REQUEST_SLEEP="${REQUEST_SLEEP:-0.01}"
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
  echo "Observation: $message"
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

clear_cache() {
  curl -fsS -o /dev/null -X POST "$BASE_URL/api/cache/clear"
}

say "0) Smoke test: API health"
curl -fsS "$BASE_URL/api/health"
echo
echo "Scriptet kører tre scenarier: cache-performance, Redis-fallback og API-skalering."

say "1) Cache-performance: cold-cache tvinger databaseopslag"
echo "Formål: vise baseline uden cache-hit. Forvent: miss og database pressure stiger."
for i in $(seq 1 "$ITERATIONS"); do
  clear_cache
  curl -fsS -o /dev/null -X POST "$BASE_URL/api/search" \
    -H "Content-Type: application/json" \
    -d "$payload"
  printf "cold-cache request %s/%s\r" "$i" "$ITERATIONS"
  sleep "$REQUEST_SLEEP"
done
echo
pause_for_observation "Cold-cache er baseline: mange miss og tydelig databasebelastning."

say "2) Cache-performance: hot-cache aflaster databasen"
echo "Formål: vise Redis-værdien. Forvent: hit stiger, database pressure falder, search duration bliver lavere."
clear_cache
curl -fsS -o /dev/null -X POST "$BASE_URL/api/search" -H "Content-Type: application/json" -d "$payload"
request_loop "hot-cache" "$((ITERATIONS * 2))"
pause_for_observation "Hot-cache viser at Redis aflaster Postgres ved gentagne søgninger."

say "3) Redis-fallback: cachelaget fjernes kort"
echo "Formål: vise fejladfærd. Forvent: Redis availability går til 0, søgninger svarer stadig 200, database pressure stiger igen."
kubectl scale deployment/redis -n "$NAMESPACE" --replicas=0 >/dev/null
sleep 8
request_loop "redis-down" "$REDIS_DOWN_REQUESTS"
kubectl scale deployment/redis -n "$NAMESPACE" --replicas=1 >/dev/null
kubectl rollout status deployment/redis -n "$NAMESPACE" --timeout=120s >/dev/null
pause_for_observation "Redis-fallback viser at Redis ikke er source of truth; Postgres kan tage over."

say "4) API-skalering: først én API-replika under load"
echo "Formål: vise baseline for API-load med få replikaer. Forvent: trafik samles på én pod."
kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$API_REPLICAS_FAILOVER" >/dev/null
kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=120s >/dev/null
request_loop "one-replica" "$((ITERATIONS * 2))"
pause_for_observation "Én replika viser baseline: samme endpoint virker, men load fordeles ikke."

if [ "$RUN_SCALE_PERFORMANCE" = "true" ]; then
  say "5) API-skalering: mange API-replikaer under parallel load"
  echo "Formål: vise x-akse-skalering af stateless API. Forvent: flere pods kommer op, og trafik fordeles på flere pod-linjer."
  BASE_URL="$BASE_URL" \
    NAMESPACE="$NAMESPACE" \
    LOW_REPLICAS="$SCALE_LOW_REPLICAS" \
    HIGH_REPLICAS="$SCALE_HIGH_REPLICAS" \
    REQUESTS="$SCALE_REQUESTS" \
    CONCURRENCY="$SCALE_CONCURRENCY" \
    scripts/k8s-scale-performance-test.sh
  pause_for_observation "Sammenlign 1 vs. mange replikaer: request rate, latency og pod-fordeling. I Minikube kan flere replikaer også afsløre lokale ressourcegrænser."
fi

say "Valideringskørsel færdig"
echo "Grafana: http://localhost:13000"
echo "Dashboard: SearchProject Walking Skeleton - Demo Dashboard"

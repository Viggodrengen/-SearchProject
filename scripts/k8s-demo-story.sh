#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:15075}"
NAMESPACE="${NAMESPACE:-searchproject}"
API_REPLICAS_NORMAL="${API_REPLICAS_NORMAL:-2}"
API_REPLICAS_FAILOVER="${API_REPLICAS_FAILOVER:-1}"
API_REPLICAS_SCALE="${API_REPLICAS_SCALE:-10}"
QUERY="${QUERY:-socal energy}"
DATABASE="${DATABASE:-postgres}"
MAX_AMOUNT="${MAX_AMOUNT:-10}"
CASE_SENSITIVE="${CASE_SENSITIVE:-false}"
REQUEST_SLEEP="${REQUEST_SLEEP:-0.01}"
BASE_WORKERS="${BASE_WORKERS:-12}"
HIGH_WORKERS="${HIGH_WORKERS:-120}"
BASELINE_SECONDS="${BASELINE_SECONDS:-35}"
COLD_CACHE_SECONDS="${COLD_CACHE_SECONDS:-35}"
REDIS_DOWN_SECONDS="${REDIS_DOWN_SECONDS:-10}"
REDIS_RECOVERY_SECONDS="${REDIS_RECOVERY_SECONDS:-25}"
ONE_REPLICA_SECONDS="${ONE_REPLICA_SECONDS:-35}"
SCALE_UP_SECONDS="${SCALE_UP_SECONDS:-55}"
CACHE_CLEAR_INTERVAL="${CACHE_CLEAR_INTERVAL:-0.25}"

payload=$(printf '{"query":"%s","maxAmount":%s,"caseSensitive":%s,"database":"%s"}' \
  "$QUERY" "$MAX_AMOUNT" "$CASE_SENSITIVE" "$DATABASE")

RUN_DIR="$(mktemp -d)"
PIDS=()
STOP_FILES=()

say() {
  echo
  echo "============================================================"
  echo "$1"
  echo "============================================================"
}

wait_seconds() {
  local seconds="$1"
  local label="$2"
  local i
  for i in $(seq 1 "$seconds"); do
    printf "%s %ss/%ss\r" "$label" "$i" "$seconds"
    sleep 1
  done
  echo
}

clear_cache() {
  curl -fsS -o /dev/null -X POST "$BASE_URL/api/cache/clear" || true
}

start_load() {
  local workers="$1"
  local label="$2"
  local stop_file="$RUN_DIR/stop-$label"
  rm -f "$stop_file"
  STOP_FILES+=("$stop_file")

  echo "Starter background load: $workers workers ($label)"
  local i
  for i in $(seq 1 "$workers"); do
    (
      while [ ! -f "$stop_file" ]; do
        curl -fsS --max-time 8 -o /dev/null -X POST "$BASE_URL/api/search" \
          -H "Content-Type: application/json" \
          -d "$payload" || true
        sleep "$REQUEST_SLEEP"
      done
    ) &
    PIDS+=("$!")
  done
}

stop_load() {
  local label="${1:-load}"
  echo "Stopper background load ($label)"
  local f
  for f in "${STOP_FILES[@]:-}"; do
    touch "$f" 2>/dev/null || true
  done
  local pid
  for pid in "${PIDS[@]:-}"; do
    wait "$pid" 2>/dev/null || true
  done
  PIDS=()
  STOP_FILES=()
}

start_cache_clear_loop() {
  local stop_file="$RUN_DIR/stop-cache-clear"
  rm -f "$stop_file"
  STOP_FILES+=("$stop_file")
  echo "Starter cache-clear loop hvert ${CACHE_CLEAR_INTERVAL}s"
  (
    while [ ! -f "$stop_file" ]; do
      clear_cache
      sleep "$CACHE_CLEAR_INTERVAL"
    done
  ) &
  PIDS+=("$!")
}

restore_cluster_state() {
  stop_load "cleanup" >/dev/null 2>&1 || true
  kubectl scale deployment/redis -n "$NAMESPACE" --replicas=1 >/dev/null 2>&1 || true
  kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$API_REPLICAS_NORMAL" >/dev/null 2>&1 || true
  rm -rf "$RUN_DIR" >/dev/null 2>&1 || true
}
trap restore_cluster_state EXIT

say "0) Smoke test: API health"
curl -fsS "$BASE_URL/api/health"
echo
echo "Demoen kører som kontinuerlig belastning: dashboardet viser systemet i drift, mens scriptet ændrer arkitekturen bagved."

say "1) Baseline: Redis er varm, og brugere søger konstant"
clear_cache
curl -fsS -o /dev/null -X POST "$BASE_URL/api/search" -H "Content-Type: application/json" -d "$payload"
start_load "$BASE_WORKERS" "baseline-hot-cache"
wait_seconds "$BASELINE_SECONDS" "baseline/hot-cache"

say "2) Cache-performance: vi tvinger cold-cache mens brugerne stadig søger"
echo "Forventning: cache miss og database pressure stiger; søgetid bliver højere end hot-cache."
start_cache_clear_loop
wait_seconds "$COLD_CACHE_SECONDS" "cold-cache/database-pressure"
# Stop kun cache-clear loop; selve bruger-load fortsætter.
touch "$RUN_DIR/stop-cache-clear"

say "3) Recovery: Redis-cache får lov at blive varm igen"
echo "Forventning: cache hits stiger, og database pressure falder igen."
wait_seconds "$REDIS_RECOVERY_SECONDS" "redis-hot-cache-recovery"

say "4) Redis-fallback: Redis fjernes kort, mens søgninger fortsætter"
echo "Forventning: Redis-kapacitet falder til 0; health bør forblive høj; database pressure stiger midlertidigt."
kubectl scale deployment/redis -n "$NAMESPACE" --replicas=0 >/dev/null
wait_seconds "$REDIS_DOWN_SECONDS" "redis-down"
kubectl scale deployment/redis -n "$NAMESPACE" --replicas=1 >/dev/null
kubectl rollout status deployment/redis -n "$NAMESPACE" --timeout=60s >/dev/null
wait_seconds "$REDIS_RECOVERY_SECONDS" "redis-recovered"

say "5) API-skalering: én API-replika under højere load"
echo "Forventning: traffic samles på én pod."
stop_load "baseline"
kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$API_REPLICAS_FAILOVER" >/dev/null
kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=120s >/dev/null
start_load "$HIGH_WORKERS" "high-load-one-replica"
wait_seconds "$ONE_REPLICA_SECONDS" "one-api-replica-high-load"

say "6) API-skalering: flere API-replikaer overtager samme load"
echo "Forventning: SearchApi-kapacitet stiger, og load-balancing-panelet viser trafik på flere pod-linjer."
kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$API_REPLICAS_SCALE" >/dev/null
kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=240s >/dev/null
wait_seconds "$SCALE_UP_SECONDS" "scaled-api-high-load"

stop_load "demo"

say "Valideringskørsel færdig"
echo "Grafana: http://localhost:13000"
echo "Dashboard: SearchProject Walking Skeleton - Demo Dashboard"

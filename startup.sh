#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

PROFILE="${MINIKUBE_PROFILE:-searchproject}"
NAMESPACE="${NAMESPACE:-searchproject}"
SKIP_HELM_UPDATE="${SKIP_HELM_UPDATE:-false}"

require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

require docker
require minikube
require kubectl
require helm
require curl

echo "== SearchProject walking skeleton startup =="
echo "Minikube profile: $PROFILE"
echo "Namespace: $NAMESPACE"

echo
echo "[1/9] Starting Minikube"
minikube start -p "$PROFILE" --driver=docker --cpus=4 --memory="${MINIKUBE_MEMORY:-6144}"
kubectl config use-context "$PROFILE" >/dev/null

echo
echo "[2/9] Installing/upgrading kube-prometheus-stack (Prometheus + Grafana)"
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts >/dev/null
if [ "$SKIP_HELM_UPDATE" != "true" ]; then
  helm repo update >/dev/null
fi
helm upgrade --install kube-prometheus-stack prometheus-community/kube-prometheus-stack \
  --namespace monitoring \
  --create-namespace \
  --values k8s/monitoring-values.yaml \
  --wait \
  --timeout 10m

echo
echo "[3/9] Building SearchProject images inside Minikube Docker daemon"
eval "$(minikube -p "$PROFILE" docker-env)"
docker build -t search-api:local -f SearchApi/Dockerfile .
docker build -t search-webapp:local -f SearchWebApp/Dockerfile .

echo
echo "[4/9] Deploying SearchProject Kubernetes manifests"
kubectl apply -f k8s/searchproject.yaml
kubectl apply -f k8s/searchproject-servicemonitor.yaml
kubectl apply -f k8s/grafana-dashboard-configmap.yaml

echo
echo "[5/9] Waiting for rollout"
kubectl rollout status deployment/postgres -n "$NAMESPACE" --timeout=180s
kubectl rollout status deployment/redis -n "$NAMESPACE" --timeout=180s
kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=240s
kubectl rollout status deployment/nginx -n "$NAMESPACE" --timeout=180s
kubectl rollout status deployment/search-webapp -n "$NAMESPACE" --timeout=180s

echo
echo "[6/9] Starting local port-forwards"
mkdir -p .pi/tmp
pkill -f "kubectl.*port-forward.*svc/nginx.*15075" 2>/dev/null || true
pkill -f "kubectl.*port-forward.*svc/search-webapp.*15249" 2>/dev/null || true
pkill -f "kubectl.*port-forward.*svc/kube-prometheus-stack-grafana.*13000" 2>/dev/null || true
pkill -f "kubectl.*port-forward.*svc/kube-prometheus-stack-prometheus.*19090" 2>/dev/null || true
kubectl port-forward -n "$NAMESPACE" svc/nginx 15075:8080 > .pi/tmp/port-forward-api.log 2>&1 & echo $! > .pi/tmp/port-forward-api.pid
kubectl port-forward -n "$NAMESPACE" svc/search-webapp 15249:8080 > .pi/tmp/port-forward-web.log 2>&1 & echo $! > .pi/tmp/port-forward-web.pid
kubectl port-forward -n monitoring svc/kube-prometheus-stack-grafana 13000:80 > .pi/tmp/port-forward-grafana.log 2>&1 & echo $! > .pi/tmp/port-forward-grafana.pid
kubectl port-forward -n monitoring svc/kube-prometheus-stack-prometheus 19090:9090 > .pi/tmp/port-forward-prometheus.log 2>&1 & echo $! > .pi/tmp/port-forward-prometheus.pid
sleep 4
API_URL="http://localhost:15075"
WEB_URL="http://localhost:15249"
GRAFANA_URL="http://localhost:13000"
PROM_URL="http://localhost:19090"

echo "API URL:      $API_URL"
echo "Web URL:      $WEB_URL"
echo "Grafana URL:  $GRAFANA_URL  (admin/admin)"
echo "Prometheus:   $PROM_URL"

echo
echo "[7/9] Smoke test"
curl -fsS "$API_URL/api/health"; echo
curl -fsS -X POST "$API_URL/api/search" \
  -H 'Content-Type: application/json' \
  -d '{"query":"socal energy","maxAmount":10,"caseSensitive":false,"database":"postgres"}' >/dev/null
echo "Search endpoint OK"

echo
echo "[8/9] Waiting for Prometheus scrape"
sleep 10
kubectl get servicemonitor -n "$NAMESPACE" search-api >/dev/null
kubectl get pods -n "$NAMESPACE" -o wide

echo
echo "[9/9] Ready for manual demo commands"
chmod +x scripts/k8s-demo-story.sh scripts/k8s-scale-performance-test.sh scripts/k8s-performance-cache-test.sh scripts/k8s-failover-test.sh scripts/k8s-redis-failure-test.sh

echo
echo "== Demo ready =="
echo "Open web app:    $WEB_URL"
echo "Open Grafana:    $GRAFANA_URL  (admin/admin)"
echo "Dashboard:       SearchProject Walking Skeleton - Demo Dashboard"
echo "Prometheus:      $PROM_URL"
echo
echo "Demo command:"
echo "  BASE_URL=$API_URL scripts/k8s-demo-story.sh"
echo "Useful commands:"
echo "  kubectl get pods -n $NAMESPACE"
echo "  kubectl logs -n $NAMESPACE deployment/search-api"

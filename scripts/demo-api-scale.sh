#!/usr/bin/env bash
set -euo pipefail
NAMESPACE="${NAMESPACE:-searchproject}"
REPLICAS="${1:-${REPLICAS:-2}}"

echo "Scaling SearchApi to ${REPLICAS} replicas..."
kubectl scale deployment/search-api -n "$NAMESPACE" --replicas="$REPLICAS" >/dev/null
kubectl rollout status deployment/search-api -n "$NAMESPACE" --timeout=240s >/dev/null
echo "SearchApi scaled to ${REPLICAS}."

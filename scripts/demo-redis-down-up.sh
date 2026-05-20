#!/usr/bin/env bash
set -euo pipefail
NAMESPACE="${NAMESPACE:-searchproject}"
DOWN_SECONDS="${DOWN_SECONDS:-10}"

echo "Scaling Redis to 0 for ${DOWN_SECONDS}s..."
kubectl scale deployment/redis -n "$NAMESPACE" --replicas=0 >/dev/null
sleep "$DOWN_SECONDS"
echo "Scaling Redis back to 1..."
kubectl scale deployment/redis -n "$NAMESPACE" --replicas=1 >/dev/null
kubectl rollout status deployment/redis -n "$NAMESPACE" --timeout=60s >/dev/null
echo "Redis restored."

#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${BASE_URL:-http://localhost:15075}"
SECONDS_TO_RUN="${SECONDS_TO_RUN:-25}"
INTERVAL="${INTERVAL:-0.25}"

echo "Clearing cache repeatedly for ${SECONDS_TO_RUN}s..."
end=$((SECONDS + SECONDS_TO_RUN))
while [ "$SECONDS" -lt "$end" ]; do
  curl -fsS -o /dev/null -X POST "$BASE_URL/api/cache/clear" || true
  sleep "$INTERVAL"
done
echo "Cache clear phase done."

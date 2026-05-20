#!/usr/bin/env bash
set -euo pipefail
RUN_DIR=".pi/tmp"
PID_FILE="$RUN_DIR/demo-search-loop.pid"
STOP_FILE="$RUN_DIR/demo-search-loop.stop"

touch "$STOP_FILE"
if [ -f "$PID_FILE" ]; then
  pid=$(cat "$PID_FILE")
  if kill -0 "$pid" 2>/dev/null; then
    echo "Stopping search loop PID $pid..."
    kill "$pid" 2>/dev/null || true
    wait "$pid" 2>/dev/null || true
  fi
  rm -f "$PID_FILE"
fi
rm -f "$STOP_FILE"
echo "Search loop stopped."

#!/usr/bin/env bash
# TEMPORARY diagnostic helper for investigating the "Optimizing assemblies for
# size" (ILLink) hang on iOS/Mac Catalyst Release publishes in CI.
#
# While a `dotnet publish` process is running, this periodically records:
#   - a process snapshot (pid/ppid/%cpu/%mem/elapsed time/rss/command) filtered
#     to dotnet/MSBuild/ILLink/mtouch/codesign-related processes
#   - system-wide load and memory pressure
#   - a macOS `sample` stack-trace capture of any process that looks like the
#     ILLink task host (it runs out-of-process via TaskHostFactory), when the
#     `sample` tool is available
#
# The goal is to distinguish, after the fact, whether ILLink was actively
# consuming CPU and making progress, was stuck/blocked, or was waiting on
# another process/resource - without needing to attach to the runner live.
#
# This script (and its call sites) is a temporary diagnostic addition; it does
# not change any build, trimming, signing, or timeout behavior.
#
# Usage: monitor-illink.sh <output-dir> <watch-pid> [interval-seconds]
set -uo pipefail

OUTPUT_DIR="$1"
WATCH_PID="$2"
INTERVAL="${3:-15}"

mkdir -p "$OUTPUT_DIR/samples"
SNAPSHOT_LOG="$OUTPUT_DIR/process-snapshots.log"
SAMPLE_SEQ=0

log() {
  printf '%s\n' "$1" >> "$SNAPSHOT_LOG"
}

log "Monitoring watch-pid $WATCH_PID every ${INTERVAL}s starting at $(date -u +"%Y-%m-%dT%H:%M:%SZ"). Output dir: $OUTPUT_DIR"

while kill -0 "$WATCH_PID" 2>/dev/null; do
  TIMESTAMP="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
  {
    echo "===== $TIMESTAMP (watch-pid $WATCH_PID alive) ====="
    echo "-- dotnet/MSBuild/ILLink/mtouch/codesign processes (pid ppid %cpu %mem etime rss command) --"
    ps -axo pid,ppid,%cpu,%mem,etime,rss,command 2>/dev/null | { IFS= read -r header; printf '%s\n' "$header"; grep -iE 'dotnet|illink|msbuild|mtouch|actool|ibtool|codesign' || true; }
    echo "-- top 10 CPU consumers (system-wide) --"
    top -l 1 -o cpu -n 10 -stats pid,command,cpu,mem,time 2>/dev/null || true
    echo "-- load / memory pressure --"
    uptime 2>/dev/null || true
    vm_stat 2>/dev/null || true
  } >> "$SNAPSHOT_LOG"

  # Identify any process that looks like the ILLink task host and grab a short
  # stack sample from it, so we can see what it's actually doing/blocked on.
  ILLINK_PIDS="$(ps -axo pid,command 2>/dev/null | grep -i 'illink' | grep -v grep | awk '{print $1}' || true)"
  for ILLINK_PID in $ILLINK_PIDS; do
    if command -v sample >/dev/null 2>&1; then
      SAMPLE_SEQ=$((SAMPLE_SEQ + 1))
      SAMPLE_FILE="$OUTPUT_DIR/samples/illink-${SAMPLE_SEQ}-pid${ILLINK_PID}-${TIMESTAMP//:/}.txt"
      if ! sample "$ILLINK_PID" 5 -f "$SAMPLE_FILE" >/dev/null 2>&1; then
        if ! sudo -n sample "$ILLINK_PID" 5 -f "$SAMPLE_FILE" >/dev/null 2>&1; then
          log "sample capture failed for pid $ILLINK_PID at $TIMESTAMP (tried with and without sudo)"
        fi
      fi
    fi
  done

  sleep "$INTERVAL"
done

log "Watch-pid $WATCH_PID exited (or was no longer running) at $(date -u +"%Y-%m-%dT%H:%M:%SZ"). Monitoring stopped."

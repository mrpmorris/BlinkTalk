#!/usr/bin/env bash
# TEMPORARY diagnostic helper for investigating the /usr/bin/codesign hang
# (0% CPU, never returns) on iOS/Mac Catalyst Release publishes in CI.
#
# The earlier monitor-illink.sh was defective for this purpose: it located
# "the ILLink task host" with `ps ... | grep -i illink`, which also matches
# the monitor script's own command line (it is invoked as
# ".../monitor-illink.sh") and therefore only ever sampled itself.
#
# This script matches processes by *authoritatively confirming their actual
# executable image*, never by a substring grep over the full command line -
# so it cannot match itself, its parent shell, the MSBuild "Codesign" task
# name, or any other unrelated process/log line that merely mentions the
# word "codesign". It also explicitly excludes its own pid and its parent's
# pid from the candidate list as a second guard.
#
# Note on `ps -o comm=`: per POSIX/BSD `ps(1)` semantics, the `comm`/`ucomm`
# fields report only the executable's short name (e.g. "codesign"), never
# its full path - only `command`/`args` include the path and arguments. So
# `[ "$comm" = "/usr/bin/codesign" ]` can never match and would be a latent
# bug. This script therefore only uses `comm` as a cheap, name-only
# pre-filter (`${comm##*/}` = "codesign", handling either behavior) to avoid
# running expensive checks against every process on the system, and then
# authoritatively verifies each candidate by asking the kernel (via `lsof`,
# not the process's self-reported argv/comm) what executable image is
# actually mapped into that pid: a candidate is only treated as the real
# `/usr/bin/codesign` when `lsof -p <pid>` reports a "txt" (executable text
# segment) mapping whose path is *exactly* `/usr/bin/codesign` - an exact
# equality check on one field of kernel-reported process/file info, not a
# substring grep over command lines or log text.
#
# While a `dotnet publish` process is running, this periodically:
#   - looks for a live process that is authoritatively confirmed (via lsof,
#     not ps argv/comm) to be running the /usr/bin/codesign executable
#   - once found, immediately starts the security-focused unified log
#     stream (before any of the slower captures below), then captures:
#       * a `ps` snapshot (pid, ppid, state, %cpu, elapsed time, rss, full
#         command line)
#       * the exact codesign command line (recorded separately too)
#       * its parent/ancestor process chain, up to pid 1
#       * `lsof -p <pid>` (if available)
#       * a macOS `sample <pid> 5 -file <output>` thread-stack capture
#   - captures the temporary build.keychain's state (show-keychain-info,
#     find-identity, and a redacted attribute/ACL/partition-list dump that
#     elides long/binary attribute values so no certificate or private-key
#     material is written to the log)
#   - the unified log stream (`log stream`, filtered to security-related
#     subsystems/processes) runs for as long as a codesign process is
#     observed alive (started the instant codesign is first seen - before
#     ps/ancestor/lsof/sample/keychain captures, so a fast Security.framework
#     /securityd authorization event right at process start cannot be
#     missed - and stopped once codesign is gone), so the log capture window
#     is bounded to the actual hang rather than the whole job.
#
# This script (and its call sites) is a temporary diagnostic addition; it
# does not change any build, trimming, signing, or timeout behavior.
#
# Usage: monitor-codesign.sh <output-dir> <watch-pid> <keychain-name> [interval-seconds]
set -uo pipefail

OUTPUT_DIR="$1"
WATCH_PID="$2"
KEYCHAIN_NAME="$3"
INTERVAL="${4:-15}"
MONITOR_PID=$$
MONITOR_PPID=$PPID

mkdir -p "$OUTPUT_DIR/samples" "$OUTPUT_DIR/keychain"
SNAPSHOT_LOG="$OUTPUT_DIR/codesign-snapshots.log"
CMDLINE_LOG="$OUTPUT_DIR/codesign-commandline.log"
UNIFIED_LOG_FILE="$OUTPUT_DIR/security-unified-log.txt"
SAMPLE_SEQ=0
LOG_STREAM_PID=""
LOG_STREAM_ACTIVE=0

log() {
  printf '%s\n' "$1" >> "$SNAPSHOT_LOG"
}

# Authoritatively confirms that the given pid's actual executable image
# (as reported by the kernel via lsof, not the process's self-reported argv
# or comm) is exactly /usr/bin/codesign. Every "txt" entry lsof reports for
# a process is a text/executable-segment mapping - the main executable's own
# mapping as well as every loaded dylib/framework - but only the main
# executable itself will have a NAME field that is *exactly*
# "/usr/bin/codesign" (dylibs/frameworks are named differently), so this is
# an exact-equality check on kernel-reported data, not a substring grep.
is_exact_usr_bin_codesign() {
  local pid="$1"
  lsof -p "$pid" 2>/dev/null | awk '
    $4 == "txt" && $NF == "/usr/bin/codesign" { found=1 }
    END { exit !found }
  '
}

# Finds live processes that are authoritatively confirmed to be running the
# /usr/bin/codesign executable. `ps -o comm=` is used only as a cheap,
# name-only pre-filter to shortlist candidates (per POSIX/BSD ps semantics,
# `comm` reports just the short executable name, e.g. "codesign", not a full
# path - so this compares basenames, not full paths, and is never treated as
# the authoritative answer). Each shortlisted candidate is then verified via
# is_exact_usr_bin_codesign before being reported. The monitor's own pid and
# its parent's pid are additionally excluded explicitly as a second,
# belt-and-braces guard, and neither this script nor "bash" nor MSBuild's
# "Codesign" task name can ever satisfy the lsof-based check.
find_codesign_pids() {
  if ! command -v lsof >/dev/null 2>&1; then
    log "lsof not available; cannot authoritatively verify codesign candidates, skipping this interval"
    return
  fi
  ps -axo pid=,comm= 2>/dev/null | while read -r pid comm; do
    [ "$pid" = "$MONITOR_PID" ] && continue
    [ "$pid" = "$MONITOR_PPID" ] && continue
    exe_name="${comm##*/}"
    [ "$exe_name" = "codesign" ] || continue
    if is_exact_usr_bin_codesign "$pid"; then
      printf '%s\n' "$pid"
    fi
  done
}

stop_log_stream() {
  if [ -n "$LOG_STREAM_PID" ] && kill -0 "$LOG_STREAM_PID" 2>/dev/null; then
    kill "$LOG_STREAM_PID" 2>/dev/null || true
    wait "$LOG_STREAM_PID" 2>/dev/null || true
  fi
  LOG_STREAM_PID=""
  LOG_STREAM_ACTIVE=0
}

# Captures build.keychain state without printing certificate/private-key
# material: `dump-keychain` never prints raw exportable key bytes for
# codesigning keys, but attribute values (e.g. certificate DER blobs) can
# still be long/binary, so any long <blob> value is truncated and marked
# <redacted>.
capture_keychain_state() {
  local ts="$1"
  local out="$OUTPUT_DIR/keychain/keychain-state-${ts//:/}.log"
  {
    echo "===== keychain state at $ts ====="
    echo "-- security show-keychain-info $KEYCHAIN_NAME --"
    security show-keychain-info "$KEYCHAIN_NAME" 2>&1
    echo "-- security find-identity -v -p codesigning $KEYCHAIN_NAME --"
    security find-identity -v -p codesigning "$KEYCHAIN_NAME" 2>&1
    echo "-- security list-keychains --"
    security list-keychains 2>&1
    echo "-- ACL / partition-list info (redacted; long/binary attribute values elided) --"
    security dump-keychain -a "$KEYCHAIN_NAME" 2>&1 \
      | grep -aiE 'keychain:|class:|"labl"|"acct"|"cusr"|"agrp"|partition|trust|acl|access control|generic|application' \
      | sed -E 's/(<blob>=".{60}).+/\1...<redacted>"/'
  } > "$out" 2>&1 || true
}

# Walks the parent/ancestor chain of a pid up to pid 1 (or 20 hops, as a
# safety bound), recording pid/ppid/comm/full args at each step.
capture_ancestor_chain() {
  local pid="$1"
  local out="$2"
  {
    echo "-- ancestor chain for pid $pid --"
    local current="$pid"
    local depth=0
    while [ -n "$current" ] && [ "$current" != "0" ] && [ "$current" != "1" ] && [ "$depth" -lt 20 ]; do
      ps -o pid=,ppid=,comm=,args= -p "$current" 2>/dev/null
      current="$(ps -o ppid= -p "$current" 2>/dev/null | tr -d ' ')"
      depth=$((depth + 1))
    done
    ps -o pid=,ppid=,comm=,args= -p 1 2>/dev/null
  } >> "$out"
}

log "Monitoring watch-pid $WATCH_PID for /usr/bin/codesign every ${INTERVAL}s starting at $(date -u +"%Y-%m-%dT%H:%M:%SZ"). Output dir: $OUTPUT_DIR. Monitor pid=$MONITOR_PID ppid=$MONITOR_PPID (both excluded from matching)."

while kill -0 "$WATCH_PID" 2>/dev/null; do
  TIMESTAMP="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
  CODESIGN_PIDS="$(find_codesign_pids)"

  if [ -z "$CODESIGN_PIDS" ]; then
    log "===== $TIMESTAMP (watch-pid $WATCH_PID alive) ===== no /usr/bin/codesign process currently running"
  fi

  for CODESIGN_PID in $CODESIGN_PIDS; do
    if [ "$LOG_STREAM_ACTIVE" -eq 0 ]; then
      log "codesign pid $CODESIGN_PID observed at $TIMESTAMP; starting security-focused unified log stream immediately (before other, slower captures) so an early authorization event cannot be missed."
      log stream --style syslog \
        --predicate 'subsystem == "com.apple.security" OR process == "securityd" OR process == "SecurityAgent" OR process == "codesign" OR eventMessage CONTAINS[c] "authoriz"' \
        >> "$UNIFIED_LOG_FILE" 2>&1 &
      LOG_STREAM_PID=$!
      LOG_STREAM_ACTIVE=1
    fi

    {
      echo "===== $TIMESTAMP (watch-pid $WATCH_PID alive) ====="
      echo "-- ps snapshot for codesign pid $CODESIGN_PID (pid ppid state %cpu etime rss command) --"
      ps -axo pid,ppid,stat,%cpu,etime,rss,command -p "$CODESIGN_PID" 2>/dev/null
    } >> "$SNAPSHOT_LOG"

    FULL_CMD="$(ps -o command= -p "$CODESIGN_PID" 2>/dev/null)"
    if [ -n "$FULL_CMD" ]; then
      printf '%s [pid %s] codesign command line: %s\n' "$TIMESTAMP" "$CODESIGN_PID" "$FULL_CMD" >> "$CMDLINE_LOG"
    fi

    capture_ancestor_chain "$CODESIGN_PID" "$SNAPSHOT_LOG"

    if command -v lsof >/dev/null 2>&1; then
      {
        echo "-- lsof -p $CODESIGN_PID --"
        lsof -p "$CODESIGN_PID" 2>&1
      } >> "$SNAPSHOT_LOG"
    else
      log "lsof not available; skipped for codesign pid $CODESIGN_PID"
    fi

    if command -v sample >/dev/null 2>&1; then
      SAMPLE_SEQ=$((SAMPLE_SEQ + 1))
      SAMPLE_FILE="$OUTPUT_DIR/samples/codesign-${SAMPLE_SEQ}-pid${CODESIGN_PID}-${TIMESTAMP//:/}.txt"
      if ! sample "$CODESIGN_PID" 5 -file "$SAMPLE_FILE" >/dev/null 2>&1; then
        if ! sudo -n sample "$CODESIGN_PID" 5 -file "$SAMPLE_FILE" >/dev/null 2>&1; then
          log "sample capture failed for codesign pid $CODESIGN_PID at $TIMESTAMP (tried with and without sudo)"
        fi
      fi
    else
      log "sample tool not available; skipped for codesign pid $CODESIGN_PID"
    fi

    capture_keychain_state "$TIMESTAMP"
  done

  if [ -z "$CODESIGN_PIDS" ] && [ "$LOG_STREAM_ACTIVE" -eq 1 ]; then
    log "codesign process no longer present at $TIMESTAMP; stopping unified log stream."
    stop_log_stream
  fi

  sleep "$INTERVAL"
done

stop_log_stream
log "Watch-pid $WATCH_PID exited (or was no longer running) at $(date -u +"%Y-%m-%dT%H:%M:%SZ"). Monitoring stopped."

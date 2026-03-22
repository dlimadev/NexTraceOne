#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Observability Pipeline Verification
# Verifies end-to-end: OTLP Collector → ClickHouse (traces, logs, metrics)
#
# Providers suportados: ClickHouse (default local) ou Elastic (enterprise)
# Modos de coleta: OpenTelemetry Collector (Kubernetes) ou CLR Profiler (IIS)
# ═══════════════════════════════════════════════════════════════════════════════
set -euo pipefail

# ── Defaults ──────────────────────────────────────────────────────────────────
OTEL_COLLECTOR_URL="${OTEL_COLLECTOR_URL:-http://localhost:4318}"
CLICKHOUSE_URL="${CLICKHOUSE_URL:-http://localhost:8123}"
TIMEOUT="${TIMEOUT:-10}"
VERBOSE="${VERBOSE:-false}"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

PASS=0
FAIL=0
WARN=0

# ── Functions ─────────────────────────────────────────────────────────────────
log()  { echo -e "[$(date '+%Y-%m-%d %H:%M:%S')] $*"; }
pass() { log "${GREEN}✓ PASS${NC}: $*"; PASS=$((PASS + 1)); }
fail() { log "${RED}✗ FAIL${NC}: $*"; FAIL=$((FAIL + 1)); }
warn() { log "${YELLOW}⚠ WARN${NC}: $*"; WARN=$((WARN + 1)); }
info() { log "${CYAN}ℹ INFO${NC}: $*"; }

check_endpoint() {
    local name="$1"
    local url="$2"
    local expected_code="${3:-200}"

    local http_code
    http_code=$(curl -s -o /dev/null -w "%{http_code}" --max-time "$TIMEOUT" "$url" 2>/dev/null) || true

    if [[ "$http_code" == "$expected_code" ]]; then
        pass "$name — HTTP $http_code at $url"
        return 0
    elif [[ "$http_code" == "000" ]]; then
        fail "$name — Connection refused at $url"
        return 1
    else
        fail "$name — Expected HTTP $expected_code, got $http_code at $url"
        return 1
    fi
}

usage() {
    cat <<EOF
Usage: $(basename "$0") [options]

Verifies the NexTraceOne observability pipeline end-to-end.
Supports configurable providers (ClickHouse/Elastic) and collection modes.

Options:
  --otel-url <url>       OTel Collector HTTP endpoint (default: $OTEL_COLLECTOR_URL)
  --clickhouse-url <url> ClickHouse HTTP endpoint (default: $CLICKHOUSE_URL)
  --timeout <seconds>    HTTP timeout per check (default: $TIMEOUT)
  --verbose              Verbose output
  --help                 Show this help

Environment Variables:
  OTEL_COLLECTOR_URL   OTel Collector HTTP endpoint
  CLICKHOUSE_URL       ClickHouse HTTP endpoint

Exit Codes:
  0  All checks passed
  1  One or more checks failed
EOF
    exit 0
}

# ── Parse Arguments ───────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
    case "$1" in
        --otel-url)       OTEL_COLLECTOR_URL="$2"; shift 2 ;;
        --clickhouse-url) CLICKHOUSE_URL="$2"; shift 2 ;;
        --timeout)        TIMEOUT="$2"; shift 2 ;;
        --verbose)        VERBOSE="true"; shift ;;
        --help)           usage ;;
        *)                echo "Unknown option: $1"; usage ;;
    esac
done

# ── Header ────────────────────────────────────────────────────────────────────
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  NexTraceOne — Observability Pipeline Verification"
echo "═══════════════════════════════════════════════════════════════"
echo ""
info "OTel Collector: $OTEL_COLLECTOR_URL"
info "ClickHouse:     $CLICKHOUSE_URL"
echo ""

# ── 1. OTel Collector Health ─────────────────────────────────────────────────
log "── OpenTelemetry Collector ──"
check_endpoint "OTel Collector OTLP/HTTP endpoint" "$OTEL_COLLECTOR_URL/v1/traces" "405" || true
# 405 = Method Not Allowed is expected for GET on a POST-only endpoint — means it's alive

# ── 2. ClickHouse Health ────────────────────────────────────────────────────
log "── ClickHouse (Observability Provider) ──"
check_endpoint "ClickHouse HTTP interface" "$CLICKHOUSE_URL/?query=SELECT%201" || true

# Check ClickHouse database exists
info "Checking ClickHouse database..."
ch_db_check=$(curl -s --max-time "$TIMEOUT" \
    "$CLICKHOUSE_URL/?query=SELECT+name+FROM+system.databases+WHERE+name='nextraceone_obs'" \
    2>/dev/null) || ch_db_check=""

if echo "$ch_db_check" | grep -q "nextraceone_obs" 2>/dev/null; then
    pass "ClickHouse database 'nextraceone_obs' exists"
else
    warn "ClickHouse database 'nextraceone_obs' not found (may need initialization)"
fi

# Check ClickHouse tables exist
for table in otel_logs otel_traces otel_metrics; do
    ch_table_check=$(curl -s --max-time "$TIMEOUT" \
        "$CLICKHOUSE_URL/?query=SELECT+name+FROM+system.tables+WHERE+database='nextraceone_obs'+AND+name='$table'" \
        2>/dev/null) || ch_table_check=""

    if echo "$ch_table_check" | grep -q "$table" 2>/dev/null; then
        pass "ClickHouse table '$table' exists"
    else
        warn "ClickHouse table '$table' not found"
    fi
done

# ── 3. Send Test Trace ───────────────────────────────────────────────────────
log "── OTLP Trace Ingestion Test ──"
TRACE_ID=$(printf '%032x' $((RANDOM * RANDOM * RANDOM)))
SPAN_ID=$(printf '%016x' $((RANDOM * RANDOM)))

trace_payload=$(cat <<EOJSON
{
  "resourceSpans": [{
    "resource": {
      "attributes": [{
        "key": "service.name",
        "value": { "stringValue": "nextraceone-verification" }
      }]
    },
    "scopeSpans": [{
      "scope": { "name": "verify-pipeline" },
      "spans": [{
        "traceId": "$TRACE_ID",
        "spanId": "$SPAN_ID",
        "name": "pipeline-verification-check",
        "kind": 1,
        "startTimeUnixNano": "$(date +%s)000000000",
        "endTimeUnixNano": "$(date +%s)100000000",
        "attributes": [{
          "key": "verification.type",
          "value": { "stringValue": "clickhouse-provider" }
        }]
      }]
    }]
  }]
}
EOJSON
)

trace_response=$(curl -s -o /dev/null -w "%{http_code}" --max-time "$TIMEOUT" \
    -X POST "$OTEL_COLLECTOR_URL/v1/traces" \
    -H "Content-Type: application/json" \
    -d "$trace_payload" 2>/dev/null) || trace_response="000"

if [[ "$trace_response" == "200" ]]; then
    pass "OTLP trace ingestion — accepted (HTTP 200), traceId=$TRACE_ID"
else
    fail "OTLP trace ingestion — HTTP $trace_response (expected 200)"
fi

# ── 4. Send Test Log ─────────────────────────────────────────────────────────
log "── OTLP Log Ingestion Test ──"
log_payload=$(cat <<EOJSON
{
  "resourceLogs": [{
    "resource": {
      "attributes": [{
        "key": "service.name",
        "value": { "stringValue": "nextraceone-verification" }
      }]
    },
    "scopeLogs": [{
      "scope": { "name": "verify-pipeline" },
      "logRecords": [{
        "timeUnixNano": "$(date +%s)000000000",
        "severityNumber": 9,
        "severityText": "INFO",
        "body": { "stringValue": "NexTraceOne pipeline verification — ClickHouse provider" },
        "attributes": [{
          "key": "verification.type",
          "value": { "stringValue": "clickhouse-provider" }
        }]
      }]
    }]
  }]
}
EOJSON
)

log_response=$(curl -s -o /dev/null -w "%{http_code}" --max-time "$TIMEOUT" \
    -X POST "$OTEL_COLLECTOR_URL/v1/logs" \
    -H "Content-Type: application/json" \
    -d "$log_payload" 2>/dev/null) || log_response="000"

if [[ "$log_response" == "200" ]]; then
    pass "OTLP log ingestion — accepted (HTTP 200)"
else
    fail "OTLP log ingestion — HTTP $log_response (expected 200)"
fi

# ── 5. Verify Data in ClickHouse ─────────────────────────────────────────────
log "── Data Verification in ClickHouse ──"
sleep 3  # Allow propagation through Collector pipeline

ch_traces=$(curl -s --max-time "$TIMEOUT" \
    "$CLICKHOUSE_URL/?query=SELECT+count()+FROM+nextraceone_obs.otel_traces+WHERE+TraceId='$TRACE_ID'" \
    2>/dev/null) || ch_traces=""

if [[ -n "$ch_traces" && "$ch_traces" != "0" && "$ch_traces" != "" ]]; then
    pass "Trace found in ClickHouse — traceId=$TRACE_ID"
else
    warn "Trace not yet visible in ClickHouse (propagation delay or Collector not connected)"
fi

ch_logs=$(curl -s --max-time "$TIMEOUT" \
    "$CLICKHOUSE_URL/?query=SELECT+count()+FROM+nextraceone_obs.otel_logs+WHERE+ServiceName='nextraceone-verification'" \
    2>/dev/null) || ch_logs=""

if [[ -n "$ch_logs" && "$ch_logs" != "0" && "$ch_logs" != "" ]]; then
    pass "Logs found in ClickHouse for verification service"
else
    warn "Logs not yet visible in ClickHouse (propagation delay or Collector not connected)"
fi

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Verification Summary"
echo "═══════════════════════════════════════════════════════════════"
echo -e "  ${GREEN}Passed${NC}: $PASS"
echo -e "  ${RED}Failed${NC}: $FAIL"
echo -e "  ${YELLOW}Warnings${NC}: $WARN"
echo "═══════════════════════════════════════════════════════════════"

if [[ $FAIL -gt 0 ]]; then
    echo ""
    fail "Observability pipeline verification completed with failures."
    echo "  Review the output above and check service connectivity."
    exit 1
else
    echo ""
    pass "Observability pipeline verification completed successfully."
    exit 0
fi

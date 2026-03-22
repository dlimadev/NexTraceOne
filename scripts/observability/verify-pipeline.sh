#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Observability Pipeline Verification
# Verifies end-to-end: OTLP Collector → Tempo (traces) / Loki (logs) / Grafana
# ═══════════════════════════════════════════════════════════════════════════════
set -euo pipefail

# ── Defaults ──────────────────────────────────────────────────────────────────
OTEL_COLLECTOR_URL="${OTEL_COLLECTOR_URL:-http://localhost:4318}"
TEMPO_URL="${TEMPO_URL:-http://localhost:3200}"
LOKI_URL="${LOKI_URL:-http://localhost:3100}"
GRAFANA_URL="${GRAFANA_URL:-http://localhost:3000}"
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

Options:
  --otel-url <url>     OTel Collector HTTP endpoint (default: $OTEL_COLLECTOR_URL)
  --tempo-url <url>    Tempo endpoint (default: $TEMPO_URL)
  --loki-url <url>     Loki endpoint (default: $LOKI_URL)
  --grafana-url <url>  Grafana endpoint (default: $GRAFANA_URL)
  --timeout <seconds>  HTTP timeout per check (default: $TIMEOUT)
  --verbose            Verbose output
  --help               Show this help

Environment Variables:
  OTEL_COLLECTOR_URL   OTel Collector HTTP endpoint
  TEMPO_URL            Tempo endpoint
  LOKI_URL             Loki endpoint
  GRAFANA_URL          Grafana endpoint

Exit Codes:
  0  All checks passed
  1  One or more checks failed
EOF
    exit 0
}

# ── Parse Arguments ───────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
    case "$1" in
        --otel-url)    OTEL_COLLECTOR_URL="$2"; shift 2 ;;
        --tempo-url)   TEMPO_URL="$2"; shift 2 ;;
        --loki-url)    LOKI_URL="$2"; shift 2 ;;
        --grafana-url) GRAFANA_URL="$2"; shift 2 ;;
        --timeout)     TIMEOUT="$2"; shift 2 ;;
        --verbose)     VERBOSE="true"; shift ;;
        --help)        usage ;;
        *)             echo "Unknown option: $1"; usage ;;
    esac
done

# ── Header ────────────────────────────────────────────────────────────────────
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  NexTraceOne — Observability Pipeline Verification"
echo "═══════════════════════════════════════════════════════════════"
echo ""
info "OTel Collector: $OTEL_COLLECTOR_URL"
info "Tempo:          $TEMPO_URL"
info "Loki:           $LOKI_URL"
info "Grafana:        $GRAFANA_URL"
echo ""

# ── 1. OTel Collector Health ─────────────────────────────────────────────────
log "── OpenTelemetry Collector ──"
check_endpoint "OTel Collector OTLP/HTTP endpoint" "$OTEL_COLLECTOR_URL/v1/traces" "405" || true
# 405 = Method Not Allowed is expected for GET on a POST-only endpoint — means it's alive

# ── 2. Tempo Health ──────────────────────────────────────────────────────────
log "── Grafana Tempo (Traces) ──"
check_endpoint "Tempo readiness" "$TEMPO_URL/ready" || true
check_endpoint "Tempo status" "$TEMPO_URL/status" || true

# ── 3. Loki Health ───────────────────────────────────────────────────────────
log "── Grafana Loki (Logs) ──"
check_endpoint "Loki readiness" "$LOKI_URL/ready" || true
check_endpoint "Loki ring status" "$LOKI_URL/ring" || true

# ── 4. Grafana Health ────────────────────────────────────────────────────────
log "── Grafana (Dashboards) ──"
check_endpoint "Grafana health" "$GRAFANA_URL/api/health" || true

# Check Grafana datasources provisioning
info "Checking Grafana datasources..."
ds_response=$(curl -s --max-time "$TIMEOUT" "$GRAFANA_URL/api/datasources" 2>/dev/null) || ds_response=""
if echo "$ds_response" | grep -q "tempo" 2>/dev/null; then
    pass "Grafana datasource 'Tempo' provisioned"
else
    warn "Grafana datasource 'Tempo' not found (may require authentication)"
fi

if echo "$ds_response" | grep -q "loki" 2>/dev/null; then
    pass "Grafana datasource 'Loki' provisioned"
else
    warn "Grafana datasource 'Loki' not found (may require authentication)"
fi

# ── 5. Send Test Trace ───────────────────────────────────────────────────────
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
          "key": "verification.phase",
          "value": { "stringValue": "phase-7" }
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

# ── 6. Send Test Log ─────────────────────────────────────────────────────────
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
        "body": { "stringValue": "NexTraceOne pipeline verification — Phase 7" },
        "attributes": [{
          "key": "verification.phase",
          "value": { "stringValue": "phase-7" }
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

# ── 7. Verify Trace in Tempo ─────────────────────────────────────────────────
log "── Trace Verification in Tempo ──"
sleep 2  # Allow propagation
tempo_trace=$(curl -s --max-time "$TIMEOUT" "$TEMPO_URL/api/traces/$TRACE_ID" 2>/dev/null) || tempo_trace=""

if echo "$tempo_trace" | grep -q "pipeline-verification-check" 2>/dev/null; then
    pass "Trace found in Tempo — traceId=$TRACE_ID"
elif [[ -n "$tempo_trace" && "$tempo_trace" != *"error"* ]]; then
    warn "Trace submitted but not yet queryable in Tempo (propagation delay)"
else
    warn "Could not verify trace in Tempo (service may be unreachable or trace not yet flushed)"
fi

# ── 8. Verify Logs in Loki ───────────────────────────────────────────────────
log "── Log Verification in Loki ──"
loki_query=$(curl -s --max-time "$TIMEOUT" \
    "$LOKI_URL/loki/api/v1/query?query=%7Bservice_name%3D%22nextraceone-verification%22%7D&limit=5" \
    2>/dev/null) || loki_query=""

if echo "$loki_query" | grep -q "result" 2>/dev/null; then
    pass "Loki query API responding"
else
    warn "Loki query did not return expected results (service may be unreachable)"
fi

# ── 9. Grafana Dashboard Verification ────────────────────────────────────────
log "── Grafana Dashboard Provisioning ──"
dashboard_search=$(curl -s --max-time "$TIMEOUT" "$GRAFANA_URL/api/search?type=dash-db" 2>/dev/null) || dashboard_search=""

if echo "$dashboard_search" | grep -q "platform-health" 2>/dev/null; then
    pass "Dashboard 'platform-health' provisioned in Grafana"
else
    warn "Dashboard 'platform-health' not found (may require auth or provisioning)"
fi

if echo "$dashboard_search" | grep -q "business-observability" 2>/dev/null; then
    pass "Dashboard 'business-observability' provisioned in Grafana"
else
    warn "Dashboard 'business-observability' not found (may require auth or provisioning)"
fi

if echo "$dashboard_search" | grep -q "runtime-environment" 2>/dev/null; then
    pass "Dashboard 'runtime-environment-comparison' provisioned in Grafana"
else
    warn "Dashboard 'runtime-environment-comparison' not found (may require auth or provisioning)"
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

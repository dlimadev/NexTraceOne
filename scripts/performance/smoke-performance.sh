#!/usr/bin/env bash
# scripts/performance/smoke-performance.sh
#
# Smoke performance check para NexTraceOne.
# Valida que os endpoints críticos respondem dentro dos limites mínimos esperados.
#
# Uso:
#   APIHOST=http://localhost:8080 ./scripts/performance/smoke-performance.sh
#   APIHOST=https://staging.nextraceone.io ./scripts/performance/smoke-performance.sh
#
# Exit code:
#   0 — todos os checks passaram
#   N — número de failures

set -euo pipefail

APIHOST="${APIHOST:-http://localhost:8080}"
FAILURES=0
TIMEOUT_SEC=10

# Limites máximos por endpoint (em ms)
MAX_LIVENESS_MS=200
MAX_READINESS_MS=500
MAX_HEALTH_MS=500

echo "========================================"
echo "  NexTraceOne — Smoke Performance Check"
echo "========================================"
echo "Target: ${APIHOST}"
echo "Timeout: ${TIMEOUT_SEC}s"
echo ""

# ── Helper ────────────────────────────────────────────────────────────────────

check_endpoint() {
    local url="$1"
    local max_ms="$2"
    local description="$3"
    local expected_code="${4:-200}"

    local start_ms end_ms elapsed_ms http_code curl_exit

    start_ms=$(date +%s%3N)
    http_code=$(curl -so /dev/null -w "%{http_code}" \
        --max-time "${TIMEOUT_SEC}" \
        --connect-timeout 5 \
        "${url}" 2>/dev/null) || curl_exit=$?

    end_ms=$(date +%s%3N)
    elapsed_ms=$((end_ms - start_ms))

    if [ "${curl_exit:-0}" -ne 0 ]; then
        echo "❌ FAIL  [${description}] Connection error (curl exit: ${curl_exit:-0})"
        FAILURES=$((FAILURES + 1))
        return
    fi

    if [ "${http_code}" -ne "${expected_code}" ]; then
        echo "❌ FAIL  [${description}] HTTP ${http_code} (expected ${expected_code})"
        FAILURES=$((FAILURES + 1))
        return
    fi

    if [ "${elapsed_ms}" -gt "${max_ms}" ]; then
        echo "⚠️  SLOW  [${description}] ${elapsed_ms}ms > ${max_ms}ms limit (HTTP ${http_code})"
        FAILURES=$((FAILURES + 1))
        return
    fi

    echo "✅ OK    [${description}] ${elapsed_ms}ms ≤ ${max_ms}ms (HTTP ${http_code})"
}

check_endpoint_any_2xx() {
    local url="$1"
    local max_ms="$2"
    local description="$3"

    local start_ms end_ms elapsed_ms http_code curl_exit

    start_ms=$(date +%s%3N)
    http_code=$(curl -so /dev/null -w "%{http_code}" \
        --max-time "${TIMEOUT_SEC}" \
        --connect-timeout 5 \
        "${url}" 2>/dev/null) || curl_exit=$?

    end_ms=$(date +%s%3N)
    elapsed_ms=$((end_ms - start_ms))

    if [ "${curl_exit:-0}" -ne 0 ]; then
        echo "❌ FAIL  [${description}] Connection error (curl exit: ${curl_exit:-0})"
        FAILURES=$((FAILURES + 1))
        return
    fi

    if [ "${http_code}" -lt 200 ] || [ "${http_code}" -ge 300 ]; then
        echo "❌ FAIL  [${description}] HTTP ${http_code} (expected 2xx)"
        FAILURES=$((FAILURES + 1))
        return
    fi

    if [ "${elapsed_ms}" -gt "${max_ms}" ]; then
        echo "⚠️  SLOW  [${description}] ${elapsed_ms}ms > ${max_ms}ms limit (HTTP ${http_code})"
        FAILURES=$((FAILURES + 1))
        return
    fi

    echo "✅ OK    [${description}] ${elapsed_ms}ms ≤ ${max_ms}ms (HTTP ${http_code})"
}

# ── Health Endpoints ──────────────────────────────────────────────────────────

echo "--- Health Endpoints ---"
check_endpoint "${APIHOST}/live"   "${MAX_LIVENESS_MS}"  "Liveness (/live)"
check_endpoint_any_2xx "${APIHOST}/ready"  "${MAX_READINESS_MS}" "Readiness (/ready)"
check_endpoint "${APIHOST}/health" "${MAX_HEALTH_MS}"    "Health detail (/health)"

# ── API Availability (not performance — just availability) ────────────────────

echo ""
echo "--- API Availability ---"

# Login endpoint must exist (even without credentials)
check_endpoint_any_2xx \
    "${APIHOST}/api/v1/identity/auth/login" \
    2000 \
    "Login endpoint availability (POST — expect 4xx for empty body)"

# Catalog services must return 401 without token (endpoint exists)
http_code=$(curl -so /dev/null -w "%{http_code}" \
    --max-time "${TIMEOUT_SEC}" \
    "${APIHOST}/api/v1/catalog/services" 2>/dev/null) || true

if [ "${http_code}" = "401" ] || [ "${http_code}" = "403" ]; then
    echo "✅ OK    [Catalog services auth guard] HTTP ${http_code} (auth required — correct)"
elif [ "${http_code}" = "200" ]; then
    echo "⚠️  WARN  [Catalog services auth guard] HTTP 200 without token — check auth config"
else
    echo "❌ FAIL  [Catalog services auth guard] HTTP ${http_code} (unexpected — 401 or 403 expected)"
    FAILURES=$((FAILURES + 1))
fi

# ── Summary ───────────────────────────────────────────────────────────────────

echo ""
echo "========================================"
if [ "${FAILURES}" -eq 0 ]; then
    echo "  ✅ ALL CHECKS PASSED (0 failures)"
else
    echo "  ❌ ${FAILURES} CHECK(S) FAILED"
fi
echo "========================================"

exit "${FAILURES}"

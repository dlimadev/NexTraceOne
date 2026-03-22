#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Smoke Check Script
#
# Verifica saúde dos serviços após deploy.
# Reutilizável em qualquer pipeline (staging, production, local).
#
# Uso:
#   bash scripts/deploy/smoke-check.sh --api-url https://api.nextraceone.io --frontend-url https://app.nextraceone.io
#   bash scripts/deploy/smoke-check.sh --api-url http://localhost:8080 --timeout 60
#   bash scripts/deploy/smoke-check.sh --help
#
# Exit code:
#   0 — todos os checks passaram
#   1 — um ou mais checks falharam
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

# ── Defaults ─────────────────────────────────────────────────────────────────
API_URL=""
FRONTEND_URL=""
TIMEOUT=30
RETRY_COUNT=5
RETRY_DELAY=10
FAILURES=0

# ── Colors ────────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; BLUE='\033[0;34m'; NC='\033[0m'

log_info()    { echo -e "${BLUE}[INFO]${NC}  $(date -u '+%H:%M:%S') $*"; }
log_success() { echo -e "${GREEN}[OK]${NC}    $(date -u '+%H:%M:%S') $*"; }
log_warn()    { echo -e "${YELLOW}[WARN]${NC}  $(date -u '+%H:%M:%S') $*"; }
log_error()   { echo -e "${RED}[ERROR]${NC} $(date -u '+%H:%M:%S') $*" >&2; }

# ── Help ──────────────────────────────────────────────────────────────────────
usage() {
  cat <<EOF
Uso: $0 [opções]

Opções:
  --api-url <url>        URL base do ApiHost (ex: https://api.nextraceone.io)
  --frontend-url <url>   URL do frontend (ex: https://app.nextraceone.io)
  --timeout <seconds>    Timeout por request em segundos (padrão: 30)
  --help                 Exibir esta ajuda

Exemplos:
  bash scripts/deploy/smoke-check.sh \\
    --api-url https://api.nextraceone.io \\
    --frontend-url https://app.nextraceone.io

  bash scripts/deploy/smoke-check.sh \\
    --api-url http://localhost:8080 \\
    --timeout 60
EOF
}

# ── Parse arguments ───────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --api-url)      API_URL="$2"; shift 2 ;;
    --frontend-url) FRONTEND_URL="$2"; shift 2 ;;
    --timeout)      TIMEOUT="$2"; shift 2 ;;
    --help|-h)      usage; exit 0 ;;
    *) log_error "Argumento desconhecido: $1"; usage; exit 1 ;;
  esac
done

# ── Validation ────────────────────────────────────────────────────────────────
if [[ -z "$API_URL" && -z "$FRONTEND_URL" ]]; then
  log_error "Pelo menos --api-url ou --frontend-url deve ser fornecido."
  usage
  exit 1
fi

# ── Helpers ───────────────────────────────────────────────────────────────────
check_endpoint() {
  local url="$1"
  local description="$2"
  local expected_status="${3:-200}"
  local use_jq_health="${4:-false}"

  local http_code body curl_exit=0

  body=$(curl -sf --retry "$RETRY_COUNT" --retry-delay "$RETRY_DELAY" \
    --max-time "$TIMEOUT" --connect-timeout 10 \
    -w "\n%{http_code}" "$url" 2>/dev/null) || curl_exit=$?

  if [[ $curl_exit -ne 0 ]]; then
    log_error "✗ ${description} — Connection failed (curl exit: ${curl_exit})"
    FAILURES=$((FAILURES + 1))
    return
  fi

  http_code=$(echo "$body" | tail -1)
  local response_body
  response_body=$(echo "$body" | sed '$d')

  if [[ "$http_code" != "$expected_status" ]]; then
    log_error "✗ ${description} — HTTP ${http_code} (expected ${expected_status})"
    FAILURES=$((FAILURES + 1))
    return
  fi

  if [[ "$use_jq_health" == "true" ]]; then
    if ! echo "$response_body" | jq -e '.status == "Healthy"' &>/dev/null; then
      log_error "✗ ${description} — Status is not 'Healthy'"
      FAILURES=$((FAILURES + 1))
      return
    fi
  fi

  log_success "✓ ${description} — HTTP ${http_code}"
}

check_frontend() {
  local url="$1"
  local http_code curl_exit=0

  http_code=$(curl -sLo /dev/null -w "%{http_code}" \
    --max-time "$TIMEOUT" --connect-timeout 10 \
    --retry "$RETRY_COUNT" --retry-delay "$RETRY_DELAY" \
    "$url" 2>/dev/null) || curl_exit=$?

  if [[ $curl_exit -ne 0 ]]; then
    log_error "✗ Frontend — Connection failed (curl exit: ${curl_exit})"
    FAILURES=$((FAILURES + 1))
    return
  fi

  if [[ "$http_code" != "200" ]]; then
    log_error "✗ Frontend — HTTP ${http_code} (expected 200)"
    FAILURES=$((FAILURES + 1))
    return
  fi

  log_success "✓ Frontend — HTTP 200"
}

# ── Run checks ────────────────────────────────────────────────────────────────
echo ""
log_info "═══════════════════════════════════════"
log_info "NexTraceOne — Smoke Check"
log_info "API URL:      ${API_URL:-'(not set)'}"
log_info "Frontend URL: ${FRONTEND_URL:-'(not set)'}"
log_info "Timeout:      ${TIMEOUT}s"
log_info "═══════════════════════════════════════"
echo ""

if [[ -n "$API_URL" ]]; then
  log_info "--- API Health Endpoints ---"
  check_endpoint "${API_URL}/live"   "ApiHost /live"   "200" "true"
  check_endpoint "${API_URL}/ready"  "ApiHost /ready"  "200" "true"
  check_endpoint "${API_URL}/health" "ApiHost /health" "200" "false"
  echo ""
fi

if [[ -n "$FRONTEND_URL" ]]; then
  log_info "--- Frontend ---"
  check_frontend "$FRONTEND_URL"
  echo ""
fi

# ── Summary ───────────────────────────────────────────────────────────────────
log_info "═══════════════════════════════════════"
if [[ $FAILURES -eq 0 ]]; then
  log_success "ALL CHECKS PASSED (0 failures)"
else
  log_error "${FAILURES} CHECK(S) FAILED"
fi
log_info "═══════════════════════════════════════"

if [[ $FAILURES -gt 0 ]]; then
  exit 1
fi

exit 0

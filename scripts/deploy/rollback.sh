#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Production Rollback Script
#
# Efetua rollback para uma tag anterior, re-tagging as imagens
# e verificando saúde após o rollback.
#
# Uso:
#   bash scripts/deploy/rollback.sh --tag abc12345 --registry ghcr.io/owner/nextraceone
#   bash scripts/deploy/rollback.sh --tag abc12345 --registry ghcr.io/owner/nextraceone --skip-health
#   bash scripts/deploy/rollback.sh --help
#
# Exit code:
#   0 — rollback bem-sucedido
#   1 — falha no rollback
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

# ── Defaults ─────────────────────────────────────────────────────────────────
TAG=""
REGISTRY=""
API_URL="${PRODUCTION_APIHOST_URL:-}"
FRONTEND_URL="${PRODUCTION_FRONTEND_URL:-}"
SKIP_HEALTH=false

SERVICES=("apihost" "workers" "ingestion" "frontend")

# ── Colors ────────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; BLUE='\033[0;34m'; NC='\033[0m'

log_info()    { echo -e "${BLUE}[INFO]${NC}  $(date -u '+%Y-%m-%dT%H:%M:%SZ') $*"; }
log_success() { echo -e "${GREEN}[OK]${NC}    $(date -u '+%Y-%m-%dT%H:%M:%SZ') $*"; }
log_warn()    { echo -e "${YELLOW}[WARN]${NC}  $(date -u '+%Y-%m-%dT%H:%M:%SZ') $*"; }
log_error()   { echo -e "${RED}[ERROR]${NC} $(date -u '+%Y-%m-%dT%H:%M:%SZ') $*" >&2; }

# ── Help ──────────────────────────────────────────────────────────────────────
usage() {
  cat <<EOF
Uso: $0 [opções]

Opções:
  --tag <tag>              Tag da imagem para rollback (obrigatório)
  --registry <url>         Prefixo do registry (ex: ghcr.io/owner/nextraceone) (obrigatório)
  --api-url <url>          URL do ApiHost para health check (opcional, usa PRODUCTION_APIHOST_URL)
  --frontend-url <url>     URL do frontend para health check (opcional, usa PRODUCTION_FRONTEND_URL)
  --skip-health            Pular verificações de saúde após rollback
  --help                   Exibir esta ajuda

Variáveis de ambiente:
  PRODUCTION_APIHOST_URL   URL do ApiHost (fallback se --api-url não fornecido)
  PRODUCTION_FRONTEND_URL  URL do frontend (fallback se --frontend-url não fornecido)

Exemplos:
  # Rollback completo com health check
  bash scripts/deploy/rollback.sh \\
    --tag abc12345 \\
    --registry ghcr.io/myorg/nextraceone \\
    --api-url https://api.nextraceone.io \\
    --frontend-url https://app.nextraceone.io

  # Rollback sem health check
  bash scripts/deploy/rollback.sh \\
    --tag abc12345 \\
    --registry ghcr.io/myorg/nextraceone \\
    --skip-health
EOF
}

# ── Parse arguments ───────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --tag)          TAG="$2"; shift 2 ;;
    --registry)     REGISTRY="$2"; shift 2 ;;
    --api-url)      API_URL="$2"; shift 2 ;;
    --frontend-url) FRONTEND_URL="$2"; shift 2 ;;
    --skip-health)  SKIP_HEALTH=true; shift ;;
    --help|-h)      usage; exit 0 ;;
    *) log_error "Argumento desconhecido: $1"; usage; exit 1 ;;
  esac
done

# ── Validation ────────────────────────────────────────────────────────────────
if [[ -z "$TAG" ]]; then
  log_error "--tag é obrigatório."
  usage
  exit 1
fi

if [[ -z "$REGISTRY" ]]; then
  log_error "--registry é obrigatório."
  usage
  exit 1
fi

# ── Rollback ──────────────────────────────────────────────────────────────────
echo ""
log_info "═══════════════════════════════════════════════════════"
log_info "NexTraceOne — PRODUCTION ROLLBACK"
log_info "Rolling back to tag: ${TAG}"
log_info "Registry: ${REGISTRY}"
log_info "═══════════════════════════════════════════════════════"
echo ""

ROLLBACK_FAILURES=0

for SERVICE in "${SERVICES[@]}"; do
  SOURCE="${REGISTRY}-${SERVICE}:${TAG}"
  TARGET="${REGISTRY}-${SERVICE}:production"

  log_info "Pulling ${SOURCE}..."
  if ! docker pull "$SOURCE"; then
    log_error "✗ Failed to pull ${SOURCE}"
    ROLLBACK_FAILURES=$((ROLLBACK_FAILURES + 1))
    continue
  fi

  log_info "Tagging ${TARGET}..."
  if ! docker tag "$SOURCE" "$TARGET"; then
    log_error "✗ Failed to tag ${TARGET}"
    ROLLBACK_FAILURES=$((ROLLBACK_FAILURES + 1))
    continue
  fi

  log_info "Pushing ${TARGET}..."
  if ! docker push "$TARGET"; then
    log_error "✗ Failed to push ${TARGET}"
    ROLLBACK_FAILURES=$((ROLLBACK_FAILURES + 1))
    continue
  fi

  log_success "✓ ${SERVICE} rolled back to ${TAG}"
done

if [[ $ROLLBACK_FAILURES -gt 0 ]]; then
  log_error "${ROLLBACK_FAILURES} service(s) failed to rollback."
  exit 1
fi

log_success "All services re-tagged to ${TAG}"

# ── Health check ──────────────────────────────────────────────────────────────
if [[ "$SKIP_HEALTH" == "true" ]]; then
  log_warn "Health check skipped (--skip-health)"
  exit 0
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SMOKE_SCRIPT="${SCRIPT_DIR}/smoke-check.sh"

if [[ ! -f "$SMOKE_SCRIPT" ]]; then
  log_warn "smoke-check.sh not found at ${SMOKE_SCRIPT} — skipping health verification"
  exit 0
fi

echo ""
log_info "Waiting 30s for services to stabilize after rollback..."
sleep 30

SMOKE_ARGS=()
if [[ -n "$API_URL" ]]; then
  SMOKE_ARGS+=("--api-url" "$API_URL")
fi
if [[ -n "$FRONTEND_URL" ]]; then
  SMOKE_ARGS+=("--frontend-url" "$FRONTEND_URL")
fi

if [[ ${#SMOKE_ARGS[@]} -eq 0 ]]; then
  log_warn "No API or frontend URL configured — skipping health verification"
  exit 0
fi

log_info "Running post-rollback health checks..."
if bash "$SMOKE_SCRIPT" "${SMOKE_ARGS[@]}"; then
  log_success "Post-rollback health checks passed ✓"
else
  log_error "Post-rollback health checks failed ✗"
  exit 1
fi

echo ""
log_info "═══════════════════════════════════════════════════════"
log_success "ROLLBACK COMPLETE — tag: ${TAG}"
log_info "═══════════════════════════════════════════════════════"

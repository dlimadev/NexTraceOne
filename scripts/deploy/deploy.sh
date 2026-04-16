#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Deploy Automático com Migrações (Bash / Linux / macOS)
#
# Orquestra o deploy completo de uma nova versão:
#   1. Pull das imagens Docker do registo
#   2. Aplicação de migrations de base de dados (todos os DbContexts)
#   3. Recriação dos containers com as novas imagens
#   4. Smoke check de saúde pós-deploy
#   5. Rollback automático em caso de falha
#
# Uso:
#   bash scripts/deploy/deploy.sh --tag v1.2.3 --registry ghcr.io/owner/nextraceone
#   bash scripts/deploy/deploy.sh --tag abc123 --registry ghcr.io/owner/nextraceone --skip-smoke
#   bash scripts/deploy/deploy.sh --tag latest --registry ghcr.io/owner/nextraceone --env Staging
#   bash scripts/deploy/deploy.sh --dry-run --tag v1.2.3 --registry ghcr.io/owner/nextraceone
#   bash scripts/deploy/deploy.sh --help
#
# Variáveis de ambiente suportadas:
#   DEPLOY_TAG               Tag da imagem a deployar (obrigatório se não passado via --tag)
#   DEPLOY_REGISTRY          Registo Docker (obrigatório se não passado via --registry)
#   MIGRATION_ENV            Ambiente alvo (Development|Staging|Production) [default: Staging]
#   CONN_IDENTITY            Connection string para nextraceone_identity
#   CONN_CATALOG             Connection string para nextraceone_catalog
#   CONN_OPERATIONS          Connection string para nextraceone_operations
#   CONN_AI                  Connection string para nextraceone_ai
#   APIHOST_URL              URL do API Host para smoke check
#   FRONTEND_URL             URL do Frontend para smoke check
#
# Exit codes:
#   0 — deploy bem-sucedido
#   1 — falha no deploy (rollback foi executado se possível)
#   2 — argumentos inválidos
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

# ── Defaults ─────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
TAG="${DEPLOY_TAG:-}"
REGISTRY="${DEPLOY_REGISTRY:-}"
ENV="${MIGRATION_ENV:-Staging}"
DRY_RUN=false
SKIP_SMOKE=false
SKIP_MIGRATION=false
SKIP_ROLLBACK=false
SMOKE_TIMEOUT=60
PREVIOUS_TAG=""

SERVICES=("apihost" "workers" "ingestion" "frontend")
COMPOSE_FILE="${REPO_ROOT}/docker-compose.production.yml"
APIHOST_URL="${APIHOST_URL:-}"
FRONTEND_URL="${FRONTEND_URL:-}"

# ── Colors ────────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; BLUE='\033[0;34m'
BOLD='\033[1m'; NC='\033[0m'

log_info()    { echo -e "${BLUE}[INFO]${NC}  $(date -u '+%Y-%m-%dT%H:%M:%SZ') $*"; }
log_success() { echo -e "${GREEN}[OK]${NC}    $(date -u '+%Y-%m-%dT%H:%M:%SZ') $*"; }
log_warn()    { echo -e "${YELLOW}[WARN]${NC}  $(date -u '+%Y-%m-%dT%H:%M:%SZ') $*" >&2; }
log_error()   { echo -e "${RED}[ERROR]${NC} $(date -u '+%Y-%m-%dT%H:%M:%SZ') $*" >&2; }
log_step()    { echo -e "\n${BOLD}${BLUE}══ $* ══${NC}"; }

# ── Help ──────────────────────────────────────────────────────────────────────
usage() {
  cat <<EOF
${BOLD}NexTraceOne — Deploy Automático com Migrações${NC}

Uso: $0 [opções]

Opções:
  --tag <tag>           Tag da imagem Docker a deployar (obrigatório)
  --registry <url>      URL do registo Docker (obrigatório)
  --env <env>           Ambiente alvo: Development|Staging|Production [default: Staging]
  --skip-smoke          Ignorar smoke check pós-deploy
  --skip-migration      Ignorar aplicação de migrations (apenas troca containers)
  --skip-rollback       Não executar rollback em caso de falha
  --dry-run             Simular sem executar (mostra comandos)
  --api-url <url>       URL do API Host para smoke check
  --frontend-url <url>  URL do Frontend para smoke check
  --smoke-timeout <s>   Timeout em segundos para smoke check [default: 60]
  -h, --help            Mostrar esta ajuda

Exemplos:
  $0 --tag v1.2.3 --registry ghcr.io/owner/nextraceone --env Production
  $0 --tag abc123 --registry ghcr.io/owner/nextraceone --skip-smoke --env Staging
  $0 --dry-run --tag v1.2.3 --registry ghcr.io/owner/nextraceone
EOF
  exit 0
}

# ── Argument parsing ──────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --tag)           TAG="$2"; shift 2 ;;
    --registry)      REGISTRY="$2"; shift 2 ;;
    --env)           ENV="$2"; shift 2 ;;
    --skip-smoke)    SKIP_SMOKE=true; shift ;;
    --skip-migration) SKIP_MIGRATION=true; shift ;;
    --skip-rollback) SKIP_ROLLBACK=true; shift ;;
    --dry-run)       DRY_RUN=true; shift ;;
    --api-url)       APIHOST_URL="$2"; shift 2 ;;
    --frontend-url)  FRONTEND_URL="$2"; shift 2 ;;
    --smoke-timeout) SMOKE_TIMEOUT="$2"; shift 2 ;;
    -h|--help)       usage ;;
    *) log_error "Argumento desconhecido: $1"; usage ;;
  esac
done

# ── Validação de argumentos obrigatórios ─────────────────────────────────────
if [[ -z "${TAG}" ]]; then
  log_error "--tag é obrigatório (ou definir DEPLOY_TAG)"
  exit 2
fi

if [[ -z "${REGISTRY}" ]]; then
  log_error "--registry é obrigatório (ou definir DEPLOY_REGISTRY)"
  exit 2
fi

if [[ ! "${ENV}" =~ ^(Development|Staging|Production)$ ]]; then
  log_error "--env deve ser Development, Staging ou Production"
  exit 2
fi

# ── Helper: executa ou simula comando ─────────────────────────────────────────
run() {
  if [[ "${DRY_RUN}" == "true" ]]; then
    echo -e "${YELLOW}[DRY-RUN]${NC} $*"
    return 0
  fi
  "$@"
}

# ── Step 1: Capturar tag anterior para rollback ───────────────────────────────
capture_previous_tag() {
  log_step "Capturando estado anterior para rollback"

  if command -v docker &>/dev/null; then
    PREVIOUS_TAG=$(docker inspect \
      --format='{{index .Config.Image}}' \
      "nextraceone-apihost" 2>/dev/null \
      | grep -oP ':[^:]+$' | tr -d ':' || echo "")
  fi

  if [[ -n "${PREVIOUS_TAG}" ]]; then
    log_info "Tag anterior detectada: ${PREVIOUS_TAG}"
  else
    log_warn "Não foi possível detectar a tag anterior — rollback não estará disponível"
    SKIP_ROLLBACK=true
  fi
}

# ── Step 2: Pull de imagens ──────────────────────────────────────────────────
pull_images() {
  log_step "Pulling imagens do registo ${REGISTRY} (tag: ${TAG})"

  for service in "${SERVICES[@]}"; do
    local image="${REGISTRY}/${service}:${TAG}"
    log_info "Pulling ${image}..."
    run docker pull "${image}"
    log_success "${service} → ${image}"
  done
}

# ── Step 3: Aplicar migrations ───────────────────────────────────────────────
apply_migrations() {
  log_step "Aplicando migrations (ambiente: ${ENV})"

  local migration_script="${REPO_ROOT}/scripts/db/apply-migrations.sh"

  if [[ ! -f "${migration_script}" ]]; then
    log_error "Script de migrations não encontrado: ${migration_script}"
    return 1
  fi

  local extra_args=""
  [[ "${DRY_RUN}" == "true" ]] && extra_args="--dry-run"

  run bash "${migration_script}" \
    --env "${ENV}" \
    ${CONN_IDENTITY:+--conn-identity "${CONN_IDENTITY}"} \
    ${CONN_CATALOG:+--conn-catalog "${CONN_CATALOG}"} \
    ${CONN_OPERATIONS:+--conn-operations "${CONN_OPERATIONS}"} \
    ${CONN_AI:+--conn-ai "${CONN_AI}"} \
    ${extra_args}

  log_success "Migrations aplicadas com sucesso"
}

# ── Step 4: Recriar containers ───────────────────────────────────────────────
recreate_containers() {
  log_step "Recreando containers com tag ${TAG}"

  if [[ ! -f "${COMPOSE_FILE}" ]]; then
    log_error "docker-compose.production.yml não encontrado em: ${COMPOSE_FILE}"
    return 1
  fi

  run env NEXTRACEONE_IMAGE_TAG="${TAG}" NEXTRACEONE_REGISTRY="${REGISTRY}" \
    docker compose -f "${COMPOSE_FILE}" up -d --remove-orphans

  log_success "Containers recriados com tag ${TAG}"
}

# ── Step 5: Smoke check ──────────────────────────────────────────────────────
smoke_check() {
  log_step "Executando smoke check (timeout: ${SMOKE_TIMEOUT}s)"

  local smoke_script="${REPO_ROOT}/scripts/deploy/smoke-check.sh"

  if [[ ! -f "${smoke_script}" ]]; then
    log_warn "smoke-check.sh não encontrado — saltando smoke check"
    return 0
  fi

  local args=("--timeout" "${SMOKE_TIMEOUT}")
  [[ -n "${APIHOST_URL}" ]]   && args+=("--api-url" "${APIHOST_URL}")
  [[ -n "${FRONTEND_URL}" ]]  && args+=("--frontend-url" "${FRONTEND_URL}")

  run bash "${smoke_script}" "${args[@]}"
  log_success "Smoke check passou"
}

# ── Step 6: Rollback em caso de falha ────────────────────────────────────────
rollback() {
  if [[ "${SKIP_ROLLBACK}" == "true" ]]; then
    log_warn "Rollback desactivado (--skip-rollback)"
    return 0
  fi

  if [[ -z "${PREVIOUS_TAG}" ]]; then
    log_error "Rollback impossível: tag anterior não conhecida"
    return 1
  fi

  log_warn "Iniciando rollback para tag: ${PREVIOUS_TAG}"

  local rollback_script="${REPO_ROOT}/scripts/deploy/rollback.sh"
  if [[ -f "${rollback_script}" ]]; then
    run bash "${rollback_script}" \
      --tag "${PREVIOUS_TAG}" \
      --registry "${REGISTRY}" \
      --skip-health
  else
    run env NEXTRACEONE_IMAGE_TAG="${PREVIOUS_TAG}" NEXTRACEONE_REGISTRY="${REGISTRY}" \
      docker compose -f "${COMPOSE_FILE}" up -d --remove-orphans
  fi

  log_warn "Rollback concluído para tag: ${PREVIOUS_TAG}"
}

# ── Main ─────────────────────────────────────────────────────────────────────
main() {
  echo -e "\n${BOLD}${GREEN}╔════════════════════════════════════════════╗"
  echo -e "║  NexTraceOne — Deploy Automático           ║"
  echo -e "╚════════════════════════════════════════════╝${NC}"
  log_info "Tag: ${TAG} | Registo: ${REGISTRY} | Env: ${ENV}"
  [[ "${DRY_RUN}" == "true" ]] && log_warn "MODO DRY-RUN ACTIVO — nenhum comando será executado"

  DEPLOY_START=$(date +%s)

  # Captura tag anterior para rollback
  capture_previous_tag

  # Executa pipeline de deploy com trap para rollback automático
  if ! run_deploy_pipeline; then
    log_error "Deploy falhou — iniciando rollback"
    rollback || true
    log_error "Deploy de ${TAG} FALHOU"
    exit 1
  fi

  DEPLOY_END=$(date +%s)
  ELAPSED=$((DEPLOY_END - DEPLOY_START))

  echo ""
  echo -e "${GREEN}${BOLD}╔══════════════════════════════════════════════════╗"
  echo -e "║  Deploy concluído com sucesso!                   ║"
  echo -e "║  Tag: ${TAG}"
  echo -e "║  Duração: ${ELAPSED}s"
  echo -e "╚══════════════════════════════════════════════════╝${NC}"
}

run_deploy_pipeline() {
  pull_images || return 1

  if [[ "${SKIP_MIGRATION}" == "false" ]]; then
    apply_migrations || return 1
  else
    log_warn "Migrations ignoradas (--skip-migration)"
  fi

  recreate_containers || return 1

  if [[ "${SKIP_SMOKE}" == "false" ]]; then
    smoke_check || return 1
  else
    log_warn "Smoke check ignorado (--skip-smoke)"
  fi

  return 0
}

main "$@"

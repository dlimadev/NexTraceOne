#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════════
# NexTraceOne — Apply Database Migrations (Bash)
#
# Aplica migrações de todos os DbContexts nos 4 bancos lógicos.
# Uso seguro em CI/CD, Staging e procedimentos manuais de Production.
#
# Uso:
#   bash scripts/db/apply-migrations.sh --env Staging --connection-prefix "Host=..."
#   bash scripts/db/apply-migrations.sh --dry-run
#   bash scripts/db/apply-migrations.sh --help
#
# Variáveis de ambiente suportadas:
#   MIGRATION_ENV              Ambiente alvo (Development|Staging|Production)
#   CONN_IDENTITY              Connection string para nextraceone_identity
#   CONN_CATALOG               Connection string para nextraceone_catalog
#   CONN_OPERATIONS            Connection string para nextraceone_operations
#   CONN_AI                    Connection string para nextraceone_ai
# ═══════════════════════════════════════════════════════════════════════════════

set -euo pipefail

# ── Defaults ─────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
DRY_RUN=false
ENV="${MIGRATION_ENV:-Staging}"
APIHOST_PROJECT="${REPO_ROOT}/src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj"

# ── Colors ────────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; BLUE='\033[0;34m'; NC='\033[0m'

log_info()    { echo -e "${BLUE}[INFO]${NC}  $*"; }
log_success() { echo -e "${GREEN}[OK]${NC}    $*"; }
log_warn()    { echo -e "${YELLOW}[WARN]${NC}  $*"; }
log_error()   { echo -e "${RED}[ERROR]${NC} $*" >&2; }

# ── Help ──────────────────────────────────────────────────────────────────────
usage() {
  cat <<EOF
Uso: $0 [opções]

Opções:
  --env <env>         Ambiente alvo (Development|Staging|Production). Padrão: Staging
  --dry-run           Listar migrações pendentes sem aplicar
  --help              Exibir esta ajuda

Variáveis de ambiente:
  CONN_IDENTITY       Connection string para nextraceone_identity
  CONN_CATALOG        Connection string para nextraceone_catalog
  CONN_OPERATIONS     Connection string para nextraceone_operations
  CONN_AI             Connection string para nextraceone_ai

Exemplos:
  # Staging via variáveis de ambiente
  export CONN_IDENTITY="Host=pg;Database=nextraceone_identity;Username=app;Password=secret"
  bash scripts/db/apply-migrations.sh --env Staging

  # Dry-run para verificar pendências
  bash scripts/db/apply-migrations.sh --dry-run

  # CI/CD (Production — requer confirmação explícita)
  bash scripts/db/apply-migrations.sh --env Production
EOF
}

# ── Parse arguments ───────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --env)       ENV="$2"; shift 2 ;;
    --dry-run)   DRY_RUN=true; shift ;;
    --help|-h)   usage; exit 0 ;;
    *) log_error "Argumento desconhecido: $1"; usage; exit 1 ;;
  esac
done

# ── Validation ────────────────────────────────────────────────────────────────
if [[ "$ENV" == "Production" ]]; then
  log_warn "═══════════════════════════════════════════════════════"
  log_warn "  ATENÇÃO: Aplicando migrations em PRODUCTION"
  log_warn "  Certifique-se de ter backup recente antes de prosseguir."
  log_warn "═══════════════════════════════════════════════════════"
  echo -n "Digite 'confirmo' para prosseguir: "
  read -r CONFIRM
  if [[ "$CONFIRM" != "confirmo" ]]; then
    log_error "Operação cancelada pelo utilizador."
    exit 1
  fi
fi

# ── Check prerequisites ───────────────────────────────────────────────────────
if ! command -v dotnet &>/dev/null; then
  log_error "dotnet SDK não encontrado. Instale .NET SDK 10."
  exit 1
fi

EF_TOOL_VERSION="10.0.5"

if ! dotnet tool list --global 2>/dev/null | grep -q "dotnet-ef"; then
  log_info "Instalando dotnet-ef tool (versão ${EF_TOOL_VERSION})..."
  dotnet tool install --global dotnet-ef --version "${EF_TOOL_VERSION}"
fi

# ── DbContexts mapeados por banco ─────────────────────────────────────────────
declare -A CONTEXT_DB_MAP=(
  # Identity (database: nextraceone_identity)
  ["NexTraceOne.IdentityAccess.Infrastructure.Persistence.IdentityDbContext"]="IDENTITY"
  ["NexTraceOne.AuditCompliance.Infrastructure.Persistence.AuditDbContext"]="IDENTITY"

  # Catalog (database: nextraceone_catalog)
  ["NexTraceOne.Catalog.Infrastructure.Graph.Persistence.CatalogGraphDbContext"]="CATALOG"
  ["NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.ContractsDbContext"]="CATALOG"
  ["NexTraceOne.Catalog.Infrastructure.Portal.Persistence.DeveloperPortalDbContext"]="CATALOG"
  ["NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.LegacyAssetsDbContext"]="CATALOG"

  # Operations (database: nextraceone_operations)
  ["NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.ChangeIntelligenceDbContext"]="OPERATIONS"
  ["NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence.RulesetGovernanceDbContext"]="OPERATIONS"
  ["NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.WorkflowDbContext"]="OPERATIONS"
  ["NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.PromotionDbContext"]="OPERATIONS"
  ["NexTraceOne.Governance.Infrastructure.Persistence.GovernanceDbContext"]="OPERATIONS"
  ["NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.IncidentDbContext"]="OPERATIONS"
  ["NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.RuntimeIntelligenceDbContext"]="OPERATIONS"
  ["NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.ReliabilityDbContext"]="OPERATIONS"
  ["NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.CostIntelligenceDbContext"]="OPERATIONS"
  ["NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence.AutomationDbContext"]="OPERATIONS"
  ["NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.TelemetryStoreDbContext"]="OPERATIONS"
  ["NexTraceOne.Integrations.Infrastructure.Persistence.IntegrationsDbContext"]="OPERATIONS"
  ["NexTraceOne.Knowledge.Infrastructure.Persistence.KnowledgeDbContext"]="OPERATIONS"
  ["NexTraceOne.ProductAnalytics.Infrastructure.Persistence.ProductAnalyticsDbContext"]="OPERATIONS"
  ["NexTraceOne.Notifications.Infrastructure.Persistence.NotificationsDbContext"]="OPERATIONS"
  ["NexTraceOne.Configuration.Infrastructure.Persistence.ConfigurationDbContext"]="OPERATIONS"

  # AI (database: nextraceone_ai)
  ["NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.AiGovernanceDbContext"]="AI"
  ["NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.ExternalAiDbContext"]="AI"
  ["NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.AiOrchestrationDbContext"]="AI"
)

FAILED_CONTEXTS=()
SUCCESS_CONTEXTS=()

# ── Run migrations ─────────────────────────────────────────────────────────────
log_info "Iniciando migrations — Ambiente: ${ENV} | Dry-run: ${DRY_RUN}"
log_info "Projeto: ${APIHOST_PROJECT}"
echo ""

for CONTEXT in "${!CONTEXT_DB_MAP[@]}"; do
  DB_KEY="${CONTEXT_DB_MAP[$CONTEXT]}"
  CONN_VAR="CONN_${DB_KEY}"
  CONN="${!CONN_VAR:-}"

  # Short name for display
  SHORT_NAME=$(echo "$CONTEXT" | rev | cut -d. -f1 | rev)

  if [[ -z "$CONN" ]]; then
    log_warn "Skipping ${SHORT_NAME} — ${CONN_VAR} não definida"
    continue
  fi

  if [[ "$DRY_RUN" == "true" ]]; then
    log_info "Listando migrações pendentes: ${SHORT_NAME}"
    dotnet ef migrations list \
      --project "${APIHOST_PROJECT}" \
      --context "${CONTEXT}" \
      --connection "${CONN}" \
      --no-build 2>/dev/null || log_warn "Não foi possível listar: ${SHORT_NAME}"
  else
    log_info "Aplicando migrations: ${SHORT_NAME}"
    if dotnet ef database update \
      --project "${APIHOST_PROJECT}" \
      --context "${CONTEXT}" \
      --connection "${CONN}" \
      --no-build; then
      log_success "✓ ${SHORT_NAME}"
      SUCCESS_CONTEXTS+=("$SHORT_NAME")
    else
      log_error "✗ ${SHORT_NAME} — FALHOU"
      FAILED_CONTEXTS+=("$SHORT_NAME")
    fi
  fi
done

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
log_info "═══════════════════════════════════════"
log_info "RESULTADO DAS MIGRATIONS"
log_info "Ambiente: ${ENV}"
log_info "Bem-sucedidos: ${#SUCCESS_CONTEXTS[@]}"
log_info "Falharam: ${#FAILED_CONTEXTS[@]}"
log_info "═══════════════════════════════════════"

if [[ ${#FAILED_CONTEXTS[@]} -gt 0 ]]; then
  log_error "Contexts com falha:"
  for ctx in "${FAILED_CONTEXTS[@]}"; do
    log_error "  - ${ctx}"
  done
  exit 1
fi

log_success "Todas as migrations aplicadas com sucesso."

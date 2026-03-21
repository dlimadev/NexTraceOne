#!/usr/bin/env bash
# =============================================================================
# check-no-demo-artifacts.sh
# NexTraceOne — Phase 0 Anti-Demo Guardrail
#
# Varre o repositório em busca de padrões proibidos de demo/preview/MVP.
# Falha com exit code 1 se padrões críticos forem encontrados fora de
# zonas permitidas (testes, fixtures, docs, este próprio script).
#
# Uso:
#   bash scripts/quality/check-no-demo-artifacts.sh
#   bash scripts/quality/check-no-demo-artifacts.sh --warn-only
#
# Opções:
#   --warn-only   Exibe achados mas não falha (útil para relatórios)
#   --verbose     Exibe todas as linhas encontradas (padrão: apenas count)
#
# Saída:
#   Exit 0 — nenhum padrão crítico encontrado
#   Exit 1 — padrões críticos encontrados (modo padrão)
#
# Para integração em CI (GitHub Actions):
#   - jobs:
#       quality-gate:
#         steps:
#           - run: bash scripts/quality/check-no-demo-artifacts.sh
#
# Política: docs/engineering/PHASE-0-PRODUCT-FREEZE-POLICY.md
# Inventário: docs/audits/PHASE-0-DEMO-DEBT-INVENTORY.md
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

# ── Opções ────────────────────────────────────────────────────────────────────
WARN_ONLY=false
VERBOSE=false
for arg in "$@"; do
  case "$arg" in
    --warn-only) WARN_ONLY=true ;;
    --verbose)   VERBOSE=true ;;
  esac
done

# ── Cores ─────────────────────────────────────────────────────────────────────
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
BOLD='\033[1m'
RESET='\033[0m'

# ── Contadores ────────────────────────────────────────────────────────────────
TOTAL_CRITICAL=0
TOTAL_WARNING=0
TOTAL_INFO=0

# ── Diretórios e ficheiros excluídos ─────────────────────────────────────────
# Zonas onde padrões demo são legítimos (testes, fixtures, documentação, etc.).
# A exclusão é aplicada pela função is_excluded() abaixo.

# ── Whitelist explícita ───────────────────────────────────────────────────────
# Ficheiros que contêm padrões proibidos mas estão catalogados na dívida existente.
# Estes ficheiros são conhecidos e serão corrigidos nas fases seguintes.
# NÃO adicionar novos ficheiros aqui sem registar no inventário de dívida.
# Documentado em: docs/audits/PHASE-0-DEMO-DEBT-INVENTORY.md
KNOWN_DEBT_FILES=(
  # Backend — IsSimulated catalogado (D-005 a D-023)
  "src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Reliability/Features/ListServiceReliability/ListServiceReliability.cs"
  "src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Reliability/Features/GetServiceReliabilityDetail/GetServiceReliabilityDetail.cs"
  "src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Reliability/Features/GetServiceReliabilityCoverage/GetServiceReliabilityCoverage.cs"
  "src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Reliability/Features/GetServiceReliabilityTrend/GetServiceReliabilityTrend.cs"
  "src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Reliability/Features/GetDomainReliabilitySummary/GetDomainReliabilitySummary.cs"
  "src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Reliability/Features/GetTeamReliabilitySummary/GetTeamReliabilitySummary.cs"
  "src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Reliability/Features/GetTeamReliabilityTrend/GetTeamReliabilityTrend.cs"
  "src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Automation/Features/GetAutomationAuditTrail/GetAutomationAuditTrail.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetExecutiveTrends/GetExecutiveTrends.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetWasteSignals/GetWasteSignals.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetTeamFinOps/GetTeamFinOps.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetFinOpsTrends/GetFinOpsTrends.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetExecutiveDrillDown/GetExecutiveDrillDown.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetFinOpsSummary/GetFinOpsSummary.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetFrictionIndicators/GetFrictionIndicators.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetDomainFinOps/GetDomainFinOps.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetEfficiencyIndicators/GetEfficiencyIndicators.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetBenchmarking/GetBenchmarking.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetServiceFinOps/GetServiceFinOps.cs"
  # Backend — Handlers vazios catalogados (D-024 a D-029)
  "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/ExternalAI/Features/CaptureExternalAIResponse/CaptureExternalAIResponse.cs"
  "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/ExternalAI/Features/ConfigureExternalAIPolicy/ConfigureExternalAIPolicy.cs"
  "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/ExternalAI/Features/ApproveKnowledgeCapture/ApproveKnowledgeCapture.cs"
  "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/ExternalAI/Features/ReuseKnowledgeCapture/ReuseKnowledgeCapture.cs"
  "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/ExternalAI/Features/GetExternalAIUsage/GetExternalAIUsage.cs"
  "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/ExternalAI/Features/ListKnowledgeCaptures/ListKnowledgeCaptures.cs"
  # Backend — Handlers de Orchestration vazios catalogados (D-046)
  "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/GenerateRobotFrameworkDraft/GenerateRobotFrameworkDraft.cs"
  "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/ValidateKnowledgeCapture/ValidateKnowledgeCapture.cs"
  "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/GenerateTestScenarios/GenerateTestScenarios.cs"
  "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/GetAiConversationHistory/GetAiConversationHistory.cs"
  "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/SummarizeReleaseForApproval/SummarizeReleaseForApproval.cs"
  # Backend — TODO inline em handlers parcialmente implementados (D-039, D-040, D-047)
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetGovernancePack/GetGovernancePack.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/ListGovernancePacks/ListGovernancePacks.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/ListIntegrationConnectors/ListIntegrationConnectors.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/ListGovernanceWaivers/ListGovernanceWaivers.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetIntegrationConnector/GetIntegrationConnector.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/ListIngestionSources/ListIngestionSources.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/ListIngestionExecutions/ListIngestionExecutions.cs"
  "src/modules/governance/NexTraceOne.Governance.Application/Features/GetTeamDetail/GetTeamDetail.cs"
  # Frontend — App.tsx: ReactQueryDevtools corretamente protegido por import.meta.env.DEV (CORRIGIDO em Phase 0)
  "src/frontend/src/App.tsx"
  # Frontend — Mocks catalogados (D-030 a D-035)
  "src/frontend/src/features/operations/pages/TeamReliabilityPage.tsx"
  "src/frontend/src/features/operations/pages/ServiceReliabilityDetailPage.tsx"
  "src/frontend/src/features/operations/pages/PlatformOperationsPage.tsx"
  "src/frontend/src/features/product-analytics/pages/PersonaUsagePage.tsx"
  "src/frontend/src/features/product-analytics/pages/ValueTrackingPage.tsx"
  "src/frontend/src/features/product-analytics/pages/JourneyFunnelPage.tsx"
)

# ── Funções auxiliares ─────────────────────────────────────────────────────────

is_whitelisted() {
  local file="$1"
  local rel_file="${file#${REPO_ROOT}/}"
  for known in "${KNOWN_DEBT_FILES[@]}"; do
    if [[ "$rel_file" == "$known" ]]; then
      return 0
    fi
  done
  return 1
}

is_excluded() {
  local file="$1"
  local rel_file="${file#${REPO_ROOT}/}"
  # Verificar padrões de exclusão
  [[ "$rel_file" == *"/node_modules/"* ]] && return 0
  [[ "$rel_file" == *"/.git/"* ]] && return 0
  [[ "$rel_file" == *"/bin/"* ]] && return 0
  [[ "$rel_file" == *"/obj/"* ]] && return 0
  [[ "$rel_file" == *"/__tests__/"* ]] && return 0
  [[ "$rel_file" == *"/e2e/"* ]] && return 0
  [[ "$rel_file" == *"/e2e-real/"* ]] && return 0
  [[ "$rel_file" == *"/Tests/"* ]] && return 0
  [[ "$rel_file" == *"/tests/"* ]] && return 0
  # Match top-level tests/ directory (e.g. tests/modules/..., tests/platform/...)
  [[ "$rel_file" == tests/* ]] && return 0
  [[ "$rel_file" == *"/docs/"* ]] && return 0
  [[ "$rel_file" == docs/* ]] && return 0
  [[ "$rel_file" == *"scripts/quality/check-no-demo-artifacts.sh" ]] && return 0
  [[ "$rel_file" == *.test.ts ]] && return 0
  [[ "$rel_file" == *.test.tsx ]] && return 0
  [[ "$rel_file" == *.spec.ts ]] && return 0
  [[ "$rel_file" == *.spec.tsx ]] && return 0
  return 1
}

search_pattern() {
  local pattern="$1"
  local label="$2"
  local severity="$3"   # CRITICAL | WARNING | INFO
  local file_exts="$4"  # ex: "cs tsx ts"
  local description="$5"

  local findings=()
  local non_whitelisted=()

  # Construir argumentos de inclusão para grep
  local include_args=()
  for ext in $file_exts; do
    include_args+=("--include=*.${ext}")
  done

  # Executar grep
  while IFS= read -r line; do
    local file
    file=$(echo "$line" | cut -d':' -f1)
    if is_excluded "$file"; then
      continue
    fi
    findings+=("$line")
    if ! is_whitelisted "$file"; then
      non_whitelisted+=("$line")
    fi
  done < <(grep -r --with-filename -n "$pattern" "${include_args[@]}" "${REPO_ROOT}" 2>/dev/null || true)

  local total=${#findings[@]}
  local new_violations=${#non_whitelisted[@]}

  if [[ $total -eq 0 ]]; then
    return 0
  fi

  local color="$YELLOW"
  [[ "$severity" == "CRITICAL" ]] && color="$RED"
  [[ "$severity" == "INFO" ]] && color="$CYAN"

  echo -e "${color}${BOLD}[${severity}]${RESET} ${label}"
  echo -e "  ${CYAN}Padrão:${RESET} ${pattern}"
  echo -e "  ${CYAN}Descrição:${RESET} ${description}"
  echo -e "  ${CYAN}Total encontrado:${RESET} ${total} (${new_violations} NOVOS fora da whitelist)"

  if [[ $VERBOSE == true ]] || [[ $new_violations -gt 0 ]]; then
    if [[ ${#non_whitelisted[@]} -gt 0 ]]; then
      echo -e "  ${RED}Violações NOVAS (não catalogadas):${RESET}"
      for v in "${non_whitelisted[@]}"; do
        local rel="${v#${REPO_ROOT}/}"
        echo -e "    ${RED}→ ${rel}${RESET}"
      done
    fi
    if [[ $VERBOSE == true ]] && [[ ${#findings[@]} -gt 0 ]]; then
      echo -e "  ${YELLOW}Todos os achados (incluindo whitelist):${RESET}"
      for f in "${findings[@]}"; do
        local rel="${f#${REPO_ROOT}/}"
        echo -e "    ${YELLOW}~ ${rel}${RESET}"
      done
    fi
  fi

  echo ""

  case "$severity" in
    CRITICAL) TOTAL_CRITICAL=$((TOTAL_CRITICAL + new_violations)) ;;
    WARNING)  TOTAL_WARNING=$((TOTAL_WARNING + new_violations)) ;;
    INFO)     TOTAL_INFO=$((TOTAL_INFO + new_violations)) ;;
  esac
}

# ── Cabeçalho ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${BOLD}╔══════════════════════════════════════════════════════════════╗${RESET}"
echo -e "${BOLD}║     NexTraceOne — Phase 0 Anti-Demo Guardrail                ║${RESET}"
echo -e "${BOLD}║     check-no-demo-artifacts.sh                               ║${RESET}"
echo -e "${BOLD}╚══════════════════════════════════════════════════════════════╝${RESET}"
echo ""
echo -e "  Repositório: ${REPO_ROOT}"
echo -e "  Modo: $([ "$WARN_ONLY" == "true" ] && echo "WARN ONLY" || echo "STRICT (falha em violações críticas)")"
echo -e "  Verbose: ${VERBOSE}"
echo ""
echo -e "${BOLD}── Verificações em execução ─────────────────────────────────────${RESET}"
echo ""

# ── BLOCO 1: Backend fake — IsSimulated ──────────────────────────────────────
search_pattern \
  "IsSimulated.*=.*true" \
  "IsSimulated = true em handler" \
  "CRITICAL" \
  "cs" \
  "Handler retorna dados simulados. Deve ser implementação real ou ter DemoBanner no frontend correspondente."

# ── BLOCO 2: Backend fake — GenerateSimulated ────────────────────────────────
search_pattern \
  "GenerateSimulated\|GenerateDemo\|GenerateFake" \
  "GenerateSimulated* em handler de produção" \
  "CRITICAL" \
  "cs" \
  "Método que gera dados fictícios em handler de produção. Proibido após Fase 0."

# ── BLOCO 3: Handlers vazios — TODO: Implementar ─────────────────────────────
search_pattern \
  "TODO: Implementar\|TODO: implementar" \
  "Handler vazio com TODO: Implementar" \
  "CRITICAL" \
  "cs" \
  "Handler exposto sem implementação. Deve retornar Result.Failure com código NotImplemented."

# ── BLOCO 4: Frontend — const mock em páginas operacionais ───────────────────
search_pattern \
  "const mock[A-Z]\|const mockServices\|const mockJobs\|const mockQueues\|const mockEvents\|const mockSubsystems\|const mockPersonas\|const mockMilestones\|const mockJourneys\|const mockDetails" \
  "const mock* em página operacional" \
  "CRITICAL" \
  "tsx ts" \
  "Array de dados fictícios locais em página operacional. Proibido fora de arquivos de teste."

# ── BLOCO 5: Frontend — ReactQueryDevtools sem guard ─────────────────────────
search_pattern \
  "ReactQueryDevtools" \
  "ReactQueryDevtools sem guard de ambiente" \
  "WARNING" \
  "tsx ts" \
  "Verificar se ReactQueryDevtools está protegido por import.meta.env.DEV. CORRIGIDO em Phase 0."

# ── BLOCO 6: Segurança — Password=postgres ───────────────────────────────────
search_pattern \
  "Password=postgres\|password=postgres\|Password=admin\|Password=password" \
  "Credencial hardcoded (Password=postgres ou equivalente)" \
  "CRITICAL" \
  "cs json yaml yml" \
  "Credencial hardcoded fora de arquivo de desenvolvimento. Risco de segurança P0."

# ── BLOCO 7: Frontend — Demo Data / Preview Data hardcoded ───────────────────
search_pattern \
  '"Demo Data"\|"Preview Data"\|"Sample Data"\|"Fake Data"\|demo data\|preview data' \
  "Texto de Demo/Preview hardcoded em código de produto" \
  "WARNING" \
  "tsx ts" \
  "Texto de demonstração hardcoded visível ao utilizador. Deve usar i18n ou ser removido."

# ── BLOCO 8: DataSource = "demo" ─────────────────────────────────────────────
search_pattern \
  'DataSource.*=.*"demo"\|DataSource.*=.*"fake"\|DataSource.*=.*"simulated"' \
  'DataSource = "demo" em handler' \
  "WARNING" \
  "cs" \
  "Handler declara fonte de dados como demo. Catalogado? Ver inventário de dívida."

# ── BLOCO 9: Segurança — ReactQueryDevtools import não guardado ──────────────
search_pattern \
  "from '@tanstack/react-query-devtools'" \
  "Import de react-query-devtools" \
  "INFO" \
  "tsx ts" \
  "Verificar que este import está em componente protegido por import.meta.env.DEV."

# ── Sumário ───────────────────────────────────────────────────────────────────
echo ""
echo -e "${BOLD}── Sumário ──────────────────────────────────────────────────────${RESET}"
echo ""
echo -e "  Violações CRÍTICAS (novas, não catalogadas): ${TOTAL_CRITICAL}"
echo -e "  Avisos (novas, não catalogadas):             ${TOTAL_WARNING}"
echo -e "  Informações (verificar manualmente):         ${TOTAL_INFO}"
echo ""

if [[ $TOTAL_CRITICAL -gt 0 ]]; then
  echo -e "${RED}${BOLD}╔══════════════════════════════════════════════════════════════╗${RESET}"
  echo -e "${RED}${BOLD}║  ✗ FALHOU — ${TOTAL_CRITICAL} violação(ões) crítica(s) encontrada(s)           ${RESET}"
  echo -e "${RED}${BOLD}║  Consulte: docs/audits/PHASE-0-DEMO-DEBT-INVENTORY.md        ${RESET}"
  echo -e "${RED}${BOLD}║  Política: docs/engineering/PHASE-0-PRODUCT-FREEZE-POLICY.md ${RESET}"
  echo -e "${RED}${BOLD}╚══════════════════════════════════════════════════════════════╝${RESET}"
  echo ""
  if [[ $WARN_ONLY == false ]]; then
    exit 1
  fi
elif [[ $TOTAL_WARNING -gt 0 ]]; then
  echo -e "${YELLOW}${BOLD}╔══════════════════════════════════════════════════════════════╗${RESET}"
  echo -e "${YELLOW}${BOLD}║  ⚠ AVISO — ${TOTAL_WARNING} aviso(s) encontrado(s)                         ${RESET}"
  echo -e "${YELLOW}${BOLD}║  Reveja os achados acima antes de mergear.                   ${RESET}"
  echo -e "${YELLOW}${BOLD}╚══════════════════════════════════════════════════════════════╝${RESET}"
  echo ""
else
  echo -e "${GREEN}${BOLD}╔══════════════════════════════════════════════════════════════╗${RESET}"
  echo -e "${GREEN}${BOLD}║  ✓ PASSOU — Nenhuma violação nova encontrada                 ${RESET}"
  echo -e "${GREEN}${BOLD}║  Os itens da dívida catalogada continuam na whitelist.        ${RESET}"
  echo -e "${GREEN}${BOLD}╚══════════════════════════════════════════════════════════════╝${RESET}"
  echo ""
fi

exit 0

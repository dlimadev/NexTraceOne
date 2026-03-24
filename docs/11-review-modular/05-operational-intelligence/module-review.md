# Revisão Modular — Operational Intelligence

> **Data:** 2026-03-24  
> **Prioridade:** P2 (Pilar Core — Incidents e Reliability)  
> **Módulo Backend:** `src/modules/operationalintelligence/`  
> **Módulo Frontend:** `src/frontend/src/features/operations/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **Operational Intelligence** é o maior módulo do NexTraceOne em termos de escopo, cobrindo 5 subdomínios:

- **Incidents** — Gestão de incidentes, correlação, evidência, mitigação, runbooks
- **Automation** — Workflows de automação, ações, aprovação, execução, validação, auditoria
- **Reliability** — Snapshots de fiabilidade, scoring (0-100), trends, cobertura
- **Runtime** — Health monitoring, drift detection, baselines, observability debt
- **Cost** — Cost records, snapshots, atribuição, trends, anomalias

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento com a visão | ✅ Forte | Incidentes e fiabilidade são operacionais e visíveis |
| Completude | ✅ Alta | 5 subdomínios, 5 DbContexts, 40+ endpoints, 10 páginas frontend |
| Maturidade backend | ✅ Alta | Domínio rico com state transitions, scoring, correlação |
| Maturidade frontend | ✅ Funcional | 10 páginas completas, 5 API clients (784 linhas) |

---

## 3. Páginas e Ações do Frontend

| Página | Rota | Permissão | Estado | Funcionalidade |
|--------|------|-----------|--------|----------------|
| IncidentsPage | `/operations/incidents` | operations:incidents:read | ✅ Funcional | Lista incidentes com filtros (severity, status, type) |
| IncidentDetailPage | `/operations/incidents/:id` | operations:incidents:read | ✅ Funcional | Correlação, evidência, mitigação, timeline |
| RunbooksPage | `/operations/runbooks` | operations:runbooks:read | ✅ Funcional | Runbooks indexados por serviço/tipo |
| AutomationWorkflowsPage | `/operations/automation` | operations:automation:read | ✅ Funcional | Lista workflows com status/risk filter |
| AutomationWorkflowDetailPage | `/operations/automation/:id` | operations:automation:read | ✅ Funcional | Detalhe: preconditions, steps, validation, audit |
| AutomationAdminPage | `/operations/automation/admin` | operations:automation:read | ✅ Funcional | Admin: catálogo de ações, estatísticas |
| TeamReliabilityPage | `/operations/reliability` | operations:reliability:read | ✅ Funcional | Fiabilidade por equipa com health tracking |
| ServiceReliabilityDetailPage | `/operations/reliability/:id` | operations:reliability:read | ✅ Funcional | Trends e cobertura por serviço |
| EnvironmentComparisonPage | `/operations/runtime-comparison` | operations:runtime:read | ✅ Funcional | Comparação de ambientes (drift detection) |
| PlatformOperationsPage | `/platform/operations` | platform:admin:read | ✅ Funcional | Health dashboard, jobs, queues, eventos |

---

## 4. Backend — Domínio

### 4.1 Incidents — IncidentRecord

| Campo | Tipo | Propósito |
|-------|------|-----------|
| ExternalRef | string | Referência legível (INC-2026-0042) |
| Type | enum | ServiceDegradation, DependencyFailure, CapacityIssue... |
| Severity | enum | Critical, Major, Minor, Warning |
| Status | enum | Open → Investigating → Mitigating → Monitoring → Resolved → Closed |
| Correlation fields | JSON | CorrelatedChanges, CorrelatedDependencies, ImpactedContracts |
| Evidence fields | JSON | TelemetrySummary, BusinessImpact |
| Mitigation fields | JSON | Actions, Recommendations, RunbookLinks |

### 4.2 Automation — AutomationWorkflowRecord

| Estado | Transições |
|--------|-----------|
| Draft → PendingApproval → Approved → Executing → Completed/Failed/Cancelled |
| RiskLevel: Low, Medium, High, Critical |
| ActionTypes: RestartControlled, ScaleOut, ScaleIn, ToggleFeatureFlag, DrainInstance, RollbackDeployment, PurgeQueue, RunDiagnostics |

### 4.3 Reliability — ReliabilitySnapshot

Scoring ponderado (0-100):
- OverallScore = (RuntimeHealthScore × 0.50) + (IncidentImpactScore × 0.30) + (ObservabilityScore × 0.20)
- TrendDirection: Improving, Stable, Declining
- RuntimeHealthStatus: Healthy, Degraded, Unavailable, NeedsAttention

### 4.4 Runtime — RuntimeSnapshot

Health auto-classificada:
- Unhealthy: ErrorRate ≥ 10% OR P99Latency ≥ 3000ms
- Degraded: ErrorRate ≥ 5% OR P99Latency ≥ 1000ms
- Healthy: Abaixo dos thresholds

### 4.5 Cost — CostRecord, CostSnapshot

Tracking de custos por serviço com trends e alertas de anomalia.

---

## 5. Endpoints API (40+)

| Módulo | Endpoints | Total |
|--------|----------|-------|
| Incidents | CRUD, summary, correlation, evidence, mitigation, by-service, by-team | ~10 |
| Automation | CRUD, request-approval, approve, reject, execute, cancel, complete-step, evaluate-preconditions, validation, audit | ~15 |
| Reliability | services, service/:id, trend, coverage, teams/:id/summary, teams/:id/trend, domains/:id/summary | ~7 |
| Runtime | health, drift, baselines, observability | ~5 |
| Cost | report, delta, by-release, by-route, import, snapshot, trend, anomaly | ~8 |

---

## 6. Banco de Dados

| DbContext | Propósito |
|-----------|-----------|
| IncidentDbContext | Incidentes, runbooks, mitigação |
| AutomationDbContext | Workflows de automação, ações, auditoria |
| ReliabilityDbContext | Snapshots de fiabilidade |
| RuntimeIntelligenceDbContext | Snapshots de runtime, baselines, drift |
| CostIntelligenceDbContext | Registos de custo, snapshots |

---

## 7. Segurança

| Permissão | Operações |
|-----------|-----------|
| operations:incidents:read | Listar, ver incidentes |
| operations:incidents:write | Criar, atualizar incidentes |
| operations:automation:read | Listar workflows |
| operations:automation:write | Criar workflows |
| operations:automation:approve | Aprovar workflows |
| operations:automation:execute | Executar workflows |
| operations:reliability:read | Ver fiabilidade |
| operations:runtime:read | Ver runtime |
| platform:admin:read | Platform operations dashboard |

---

## 8. Resumo de Ações

### Ações de Validação (P1)

| # | Ação | Esforço |
|---|------|---------|
| 1 | Validar fluxo de incidentes: Create → Correlate → Evidence → Mitigate → Resolve | 3h |
| 2 | Validar automation: Create → Approve → Execute → Validate | 2h |
| 3 | Validar reliability scoring end-to-end | 2h |
| 4 | Validar runtime health auto-classification e drift detection | 2h |
| 5 | Validar cost tracking e anomaly detection | 2h |

### Ações de Documentação (P2)

| # | Ação | Esforço |
|---|------|---------|
| 6 | Criar documentação unificada de Operational Intelligence | 3h |
| 7 | Documentar modelo de scoring de reliability | 2h |
| 8 | Documentar thresholds de health classification | 1h |
| 9 | Documentar fluxo de automação com diagrama | 2h |

### Ações de Melhoria (P3)

| # | Ação | Esforço |
|---|------|---------|
| 10 | Verificar testes (164+ existentes, alguns ausentes) | 3h |
| 11 | Verificar integração IncidentCorrelationService com Change Intelligence | 2h |
| 12 | Avaliar se Cost Intelligence deve ser subdomínio separado | 1h |

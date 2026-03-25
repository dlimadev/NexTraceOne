# Operational Intelligence — Module Remediation Plan

> **Module:** Operational Intelligence (06)  
> **Table prefix:** `ops_` (current code: `oi_`)  
> **Date:** 2026-03-25 (reinforced N9-R)  
> **Status:** Consolidation Phase — B1  
> **Overall maturity:** ~74 % (target ≥ 85 %)  
> **Total remediation items:** 73  
> **Total estimated effort:** ~218 h (~27 sprints-days)

---

## 1. Resumo executivo

### Estado actual

O módulo Operational Intelligence é o **mais funcional** do NexTraceOne: 5 subdomínios (Incidents, Automation, Reliability, Runtime, Cost), 19 entidades, 56 endpoints, 10 páginas frontend, 5 DbContexts. O domínio é rico e coerente.

### Principais lacunas

| Lacuna | Severidade | Impacto |
|--------|-----------|---------|
| ❌ **Zero integração com Notifications** | 🔴 P0_BLOCKER | Incidentes críticos, anomalias e aprovações não geram alertas |
| ❌ **Zero integração com Audit & Compliance** | 🔴 P0_BLOCKER | Decisões de automação e transições de incidentes não são auditadas centralmente |
| ❌ **Prefixo de tabelas `oi_` em vez de `ops_`** | 🔴 P1_CRITICAL | 19 tabelas + outbox com prefixo errado; migrations precisam ser recriadas |
| ❌ **Zero frontend para Cost Intelligence** | 🔴 P1_CRITICAL | 9 endpoints backend sem UI; funcionalidade invisível |
| ❌ **Zero RowVersion/concurrency token** | 🔴 P1_CRITICAL | Atualizações concorrentes em entidades mutáveis sem proteção |
| ⚠️ **Scoring com seed data** | 🟠 P2_HIGH | Fórmula implementada, mas sub-scores vêm de dados seed |
| ⚠️ **Thresholds hardcoded** | 🟠 P2_HIGH | ErrorRate 5%/10%, P99 1000/3000ms sem configuração por serviço |
| ⚠️ **Four-eyes principle ausente** | 🟠 P2_HIGH | Quem aprova automação pode ser quem a pediu |
| ⚠️ **EnvironmentId inconsistente** | 🟠 P2_HIGH | `Guid?` em Incidents, `string` em Runtime/Cost/Automation |

### Risco actual

**MÉDIO-ALTO** — O módulo funciona localmente, mas as integrações críticas (Notifications, Audit) estão totalmente ausentes, e a persistência precisa de renomeação antes das migrations finais.

### Prioridade do módulo

**ALTA** — É um dos módulos com mais funcionalidade real e mais impacto no produto final. Os gaps são maioritariamente de integração e polish, não de arquitetura.

---

## 2. Quick wins (A)

Correções pequenas, concretas e de alto valor. Podem ser executadas em 1–2 sprints sem dependências externas.

| ID | Título | Problema que resolve | Camada | Esforço | Prioridade | Ficheiros |
|----|--------|---------------------|--------|---------|------------|-----------|
| QW-01 | Criar `README.md` do módulo | 211 ficheiros C# sem documentação de entrada | Docs | 2 h | P1_CRITICAL | `src/modules/operationalintelligence/README.md` |
| QW-02 | Documentar fórmula de scoring `OverallScore = 0.50×Runtime + 0.30×Incident + 0.20×Observability` | Fórmula hardcoded em `ReliabilitySnapshot.cs` sem documentação formal | Docs | 2 h | P1_CRITICAL | `ReliabilitySnapshot.cs`, novo `docs/scoring-formula.md` |
| QW-03 | Documentar thresholds de health classification (ErrorRate 5%/10%, P99 1000/3000ms) | Thresholds hardcoded em `RuntimeSnapshot.cs` sem referência | Docs | 1 h | P1_CRITICAL | `RuntimeSnapshot.cs` |
| QW-04 | Criar diagrama de state machine da automação (11 estados + 5 approval states) | Fluxo complexo sem representação visual | Docs | 2 h | P1_CRITICAL | `AutomationWorkflowRecord.cs` |
| QW-05 | Corrigir permissão da rota RunbooksPage: `operations:incidents:read` → `operations:runbooks:read` | Route permission errada — utilizadores com `runbooks:read` mas sem `incidents:read` não acedem | Frontend/Security | 15 min | P1_CRITICAL | `App.tsx` linha da rota `/operations/runbooks` |
| QW-06 | Resolver pasta frontend `operational-intelligence/` embrionária — mergir em `operations/` ou eliminar | Pasta fantasma sem conteúdo útil; confunde developers | Frontend | 1 h | P2_HIGH | `src/frontend/src/features/operational-intelligence/` |
| QW-07 | Registar permissão `operations:runbooks:write` em `RolePermissionCatalog.cs` | Permissão referida nos docs mas não registada; Runbook CRUD futuro fica bloqueado | Backend | 30 min | P2_HIGH | `RolePermissionCatalog.cs` |
| QW-08 | Registar permissão `operations:reliability:write` em `RolePermissionCatalog.cs` | Permissão ausente para futuro ajuste manual de scores | Backend | 30 min | P3_MEDIUM | `RolePermissionCatalog.cs` |
| QW-09 | Remover `invalidate+refetch` redundante nas mutations do `IncidentsPage` | Dual invalidation desnecessária; código duplicado | Frontend | 30 min | P3_MEDIUM | `IncidentsPage.tsx` |
| QW-10 | Padronizar `staleTime` em todas as queries operacionais para 30s | Inconsistência entre páginas; dados stale com duração variável | Frontend | 1 h | P3_MEDIUM | `features/operations/**/*.tsx` |
| QW-11 | Adicionar XML docs a todas as interfaces públicas do Application | 21 abstrações sem documentação; onboarding dificultado | Docs | 3 h | P2_HIGH | `Application/Abstractions/*.cs` |
| QW-12 | Criar `CostImportBatchStatus` como enum tipado (substituir string constants) | Strings mágicas `"Pending"`, `"Completed"`, `"Failed"` sem type safety | Domain | 1 h | P1_CRITICAL | `CostImportBatch.cs` |

**Total Quick Wins: 12 items, ~15 h**

---

## 3. Correções funcionais obrigatórias (B)

Tudo o que precisa ser corrigido para o módulo ficar realmente utilizável em produção.

### 3.1 Integrações cross-module (P0/P1)

| ID | Título | Descrição objectiva | Impacto | Prioridade | Dependências | Esforço |
|----|--------|---------------------|---------|------------|--------------|---------|
| FC-01 | **Integrar com Notifications — publicar eventos de domínio** | Publicar `IncidentCreatedEvent`, `IncidentEscalatedEvent`, `AutomationApprovalRequestedEvent`, `AutomationExecutionCompletedEvent`, `ReliabilityScoreDegradedEvent`, `CostAnomalyDetectedEvent` via outbox. Criar handlers no módulo Notifications para subscrever. | Sem esta integração, incidentes críticos não alertam on-call, aprovações pendentes ficam invisíveis, anomalias de custo passam despercebidas | P0_BLOCKER | Módulo Notifications operacional (N8-R concluído) | 8–12 h |
| FC-02 | **Integrar com Audit & Compliance — forward de eventos sensíveis** | Publicar audit events via outbox para: automation approve/reject/execute, incident create/escalate/close, cost batch import, baseline changes. Consumidos pelo Audit & Compliance. | Sem isto, automações em produção não têm rastreabilidade central. Compliance gap. | P0_BLOCKER | Módulo Audit operacional (N10-R concluído) | 6–8 h |
| FC-03 | **Publicar domain events para todas as state transitions** | Actualmente só existem `RuntimeAnomalyDetectedEvent` e `CostAnomalyDetectedEvent`. Faltam: `IncidentStatusChangedEvent`, `AutomationStatusChangedEvent`, `MitigationCompletedEvent`, `DriftDetectedEvent`, `ReliabilityScoreChangedEvent` (existe mas não é publicado) | Eventos são o contrato de integração; sem eles, Notifications e Audit não conseguem subscrever | P1_CRITICAL | Nenhuma | 6 h |

### 3.2 Frontend (P1/P2)

| ID | Título | Descrição objectiva | Impacto | Prioridade | Dependências | Esforço |
|----|--------|---------------------|---------|------------|--------------|---------|
| FC-04 | **Criar Cost Intelligence frontend page** | Criar `CostDashboardPage` com: report geral, trends, delta, anomalias, import batches. 9 endpoints backend (`CostIntelligenceEndpointModule`) sem UI. | Funcionalidade de FinOps operacional invisível para utilizadores | P1_CRITICAL | API client `cost.ts` (FC-06) | 16–20 h |
| FC-05 | **Adicionar Cost ao sidebar menu** | Adicionar entrada "Cost Intelligence" no menu lateral sob Operations | Página existirá mas sem acesso via navegação | P1_CRITICAL | FC-04 | 30 min |
| FC-06 | **Criar API client `cost.ts`** | Criar `cost.ts` cobrindo todos os 9 endpoints de cost: ingestion, reports, delta, trends, attribution, anomaly, batch import | Frontend não consegue chamar endpoints de cost | P1_CRITICAL | Nenhuma | 4 h |
| FC-07 | **Completar `reliability.ts` API client** | Adicionar métodos: `getTrend`, `getCoverage`, `getTeamTrend`, `getDomainSummary` — endpoints existem, client parcial | Funcionalidade de reliability parcialmente inacessível | P2_HIGH | Nenhuma | 2 h |
| FC-08 | **Completar `runtimeIntelligence.ts` API client** | Adicionar métodos: `getHealth`, `detectDrift`, `assessObservability` | Endpoints de runtime parcialmente inacessíveis via frontend | P2_HIGH | Nenhuma | 2 h |
| FC-09 | **Adicionar filtros avançados no IncidentsPage** | Expor filtros de severity, type, environment, date range — backend já suporta, frontend não expõe todos | Utilizadores não conseguem filtrar eficientemente | P2_HIGH | Nenhuma | 4 h |
| FC-10 | **Verificar cobertura i18n pt.json** | Validar que todos os namespaces operations/* têm tradução completa; 3 páginas grandes (>20KB) em risco | i18n incompleto para utilizadores PT | P2_HIGH | Nenhuma | 4 h |
| FC-11 | **Adicionar breadcrumb navigation** | Adicionar breadcrumbs a IncidentDetail, ReliabilityDetail, AutomationDetail | Navegação hierárquica ausente em detail pages | P3_MEDIUM | Nenhuma | 4 h |
| FC-12 | **Adicionar feedback toast/notification nas mutations** | Success/error feedback visual em todas as operações de escrita | Utilizadores sem confirmação visual de acções | P3_MEDIUM | Nenhuma | 2 h |

### 3.3 Backend funcional (P1/P2)

| ID | Título | Descrição objectiva | Impacto | Prioridade | Dependências | Esforço |
|----|--------|---------------------|---------|------------|--------------|---------|
| FC-13 | **Adicionar RowVersion (`xmin`) a todos os aggregates mutáveis** | IncidentRecord, MitigationWorkflowRecord, AutomationWorkflowRecord, DriftFinding, ServiceCostProfile, CostImportBatch — sem concurrency token | Actualizações concorrentes podem causar lost updates silenciosos | P1_CRITICAL | Nenhuma | 3 h |
| FC-14 | **Hardening: rejeitar execução de automação sem ApprovalStatus = Approved** | `UpdateAutomationWorkflowAction` deve verificar `ApprovalStatus` antes de executar | Bypass de aprovação possível; risco operacional grave | P1_CRITICAL | Nenhuma | 2 h |
| FC-15 | **Implementar four-eyes principle: approver ≠ requester** | Na operação `Approve()`, validar que `approverId != workflow.CreatedBy` | Quem pede automação pode aprovar a própria — sem segregação de funções | P1_CRITICAL | Nenhuma | 2 h |
| FC-16 | **Adicionar Runbook CRUD (POST/PUT/DELETE)** | Actualmente só existem GET endpoints; sem criação/edição de runbooks | Runbooks são read-only; equipa não consegue gerir | P2_HIGH | QW-07 (registar permissão) | 8 h |
| FC-17 | **Adicionar endpoint `PATCH /incidents/{id}/status`** | Transição explícita de status de incidente sem update completo | Status change misturado com update geral; sem auditoria isolada | P2_HIGH | Nenhuma | 4 h |
| FC-18 | **Adicionar idempotency control em signal ingestion** | `POST /runtime/snapshots` e `POST /cost/snapshots` sem controlo de idempotência | Duplicação de signals em retry/replay | P2_HIGH | Nenhuma | 3 h |
| FC-19 | **Validar que `IncidentCorrelationService` retorna dados reais** | Verificar se correlação com Change Governance produz dados reais vs seed/mock | Se correlação é cosmética, a funcionalidade é enganosa | P2_HIGH | Change Governance module | 3 h |
| FC-20 | **Adicionar FluentValidation a todos os command DTOs** | Verificar 20+ commands; alguns sem validação explícita | Input inválido pode chegar ao domínio | P2_HIGH | Nenhuma | 4 h |
| FC-21 | **Adicionar ServiceCostProfile management endpoint** | Endpoint para configurar budget e thresholds por serviço | Cost profiles não configuráveis via API | P2_HIGH | Nenhuma | 4 h |
| FC-22 | **Adicionar RuntimeBaseline management endpoint** | Endpoint para ajuste manual de baselines | Baselines só criados automaticamente; sem ajuste manual | P2_HIGH | Nenhuma | 4 h |
| FC-23 | **Adicionar workflow timeout para estados stuck** | PendingApproval e Executing sem timeout; workflows podem ficar stuck indefinidamente | Workflows nunca expiram | P3_MEDIUM | Nenhuma | 4 h |
| FC-24 | **Adicionar retention configuration e cleanup job** | Sem endpoints de retenção nem background job para limpeza de dados antigos | Tabelas crescem indefinidamente | P3_MEDIUM | Nenhuma | 8 h |

**Total Correções Funcionais: 24 items, ~98 h**

---

## 4. Ajustes estruturais (C)

### 4.1 Persistência (ops_ prefix e schema)

| ID | Título | Descrição | Porque é estrutural | Impacto | Dependências | Esforço |
|----|--------|-----------|--------------------:|---------|--------------|---------|
| SA-01 | **Renomear prefixo de 19 tabelas `oi_` → `ops_`** | Todas as 19 entity configurations usam `oi_` no código actual (e.g., `oi_incident_records`). Target oficial: `ops_`. | Prefixo de tabela é decisão arquitectural global (docs/architecture/database-table-prefixes.md) | Migrations ficam inconsistentes com padrão do produto | Nenhuma | 3–4 h |
| SA-02 | **Renomear outbox tables para `ops_*_outbox_messages`** | IncidentDbContext e AutomationDbContext usam `outbox_messages` genérico. Devem usar `ops_inc_outbox_messages`, `ops_auto_outbox_messages` etc. | Colisão de nomes com outbox de outros módulos | Outbox tables de módulos diferentes com mesmo nome | SA-01 | 1 h |
| SA-03 | **Padronizar `EnvironmentId` como `Guid` em todas as entidades** | IncidentRecord usa `Guid?`; RuntimeSnapshot, ReliabilitySnapshot, AutomationWorkflowRecord, CostRecord usam `string`. Inconsistência impede join/FK. | Tipo inconsistente impede queries cross-subdomain e FK com Environment Management | Queries cross-entity impossíveis por EnvironmentId | Nenhuma | 3 h |
| SA-04 | **Adicionar `TenantId` explícito em entidades que faltam** | RuntimeSnapshot, CostSnapshot, DriftFinding sem `TenantId` explícito (dependem apenas de RLS). Para ClickHouse migration readiness, precisa ser explícito. | ClickHouse não suporta RLS; queries precisam de TenantId como coluna | Migração para ClickHouse bloqueada | Nenhuma | 2 h |
| SA-05 | **Configurar outbox table name override em todos os 5 DbContexts** | Apenas IncidentDbContext e AutomationDbContext têm outbox; Reliability/Runtime/Cost não têm. Verificar se precisam. | Inconsistência de configuração entre DbContexts | Outbox potencialmente em falta em subdomínios que publicam eventos | SA-02 | 1 h |
| SA-06 | **Adicionar check constraints para ranges de score/rate/cost** | Nenhum check constraint em: `OverallScore [0–100]`, `ErrorRate [0–1]`, `P99LatencyMs >= 0`, `TotalCost >= 0` | Valores fora de range podem ser persistidos sem validação a nível de DB | Dados inválidos passam silenciosamente | Nenhuma | 2 h |
| SA-07 | **Converter enums de Automation de STRING para INTEGER** | AutomationDbContext guarda enums como STRING; outros subdomínios usam INTEGER | Inconsistência de serialização entre subdomínios do mesmo módulo | Queries cross-subdomain mais complexas | Nenhuma | 2 h |

### 4.2 Domínio e scoring

| ID | Título | Descrição | Porque é estrutural | Impacto | Dependências | Esforço |
|----|--------|-----------|--------------------:|---------|--------------|---------|
| SA-08 | **Tornar thresholds de health configuráveis via Configuration module** | ErrorRate (5%/10%), P99 (1000/3000ms) hardcoded em `RuntimeSnapshot.ClassifyHealth()`. Devem vir de `cfg_` entries. | Hardcoding impede customização per-service/per-tenant | Thresholds one-size-fits-all; sem SLO customization | Configuration module | 6 h |
| SA-09 | **Adicionar hysteresis/debounce para transições de health** | Estado pode flipar Degraded ↔ Healthy a cada snapshot por ruído em métricas | Sem debounce, dashboards e alertas ficam noisy | Alert fatigue e estados instáveis | SA-08 | 3 h |
| SA-10 | **Adicionar SLO-based threshold overrides** | Diferentes serviços têm diferentes SLOs; thresholds globais são insuficientes | Product requirement: cada serviço pode ter limites customizados | Serviços high-criticality tratados igual a low-criticality | SA-08 | 6 h |
| SA-11 | **Adicionar approval quorum para automações Critical** | Actualmente 1 aprovador basta. Para risk=Critical, deveria exigir 2-of-3. | Segregação de controlo para operações de alto risco | Operações críticas com aprovação insuficiente | FC-15 | 4 h |
| SA-12 | **Adicionar timeout/expiry para aprovações pendentes** | PendingApproval nunca expira. Deveria expirar após X horas/dias configuráveis. | Workflows stuck em pendente indefinidamente sem acção | Backlog de aprovações pendentes sem gestão | Nenhuma | 3 h |

### 4.3 ClickHouse e escalabilidade

| ID | Título | Descrição | Porque é estrutural | Impacto | Dependências | Esforço |
|----|--------|-----------|--------------------:|---------|--------------|---------|
| SA-13 | **Definir schema ClickHouse para `ch_ops_runtime_snapshots`** | Schema com MergeTree + TTL para time-series de runtime metrics. Primeira tabela candidata. | RuntimeSnapshot é o maior volume de dados; PostgreSQL não escala a longo prazo | Queries de timeline degradam com volume | SA-04 (TenantId explícito) | 8 h |
| SA-14 | **Implementar outbox → ClickHouse consumer para runtime** | Consumer que lê outbox messages e insere em ClickHouse | Pipeline dual-write necessário para migração gradual | Sem pipeline, dados ficam só em PostgreSQL | SA-13, infraestrutura ClickHouse | 16 h |
| SA-15 | **Migrar queries de timeline para dual-read pattern** | `GetReleaseHealthTimeline` e `CompareReleaseRuntime` devem ler de ClickHouse quando disponível, fallback para PostgreSQL | Queries precisam funcionar com e sem ClickHouse | Queries time-series limitadas a PostgreSQL | SA-14 | 8 h |
| SA-16 | **Definir retention/TTL policies** | Sem políticas de retenção para PostgreSQL ou ClickHouse | Tabelas crescem indefinidamente; custos de storage e performance | Storage crescente sem controlo | Nenhuma | 4 h |
| SA-17 | **Adicionar TenantId enforcement em ClickHouse query layer** | ClickHouse não tem RLS; queries precisam de WHERE TenantId = @tenantId explícito | Sem isto, tenant isolation quebrada em queries analíticas | **Risco de segurança: data leak cross-tenant** | SA-13 | 4 h |

### 4.4 Cost Intelligence pipeline

| ID | Título | Descrição | Porque é estrutural | Impacto | Dependências | Esforço |
|----|--------|-----------|--------------------:|---------|--------------|---------|
| SA-18 | **Completar Cost import pipeline end-to-end** | `ImportCostBatch` handler existe mas é parcial. Falta: validação de dados, error handling robusto, status tracking completo. | Import é o ponto de entrada de dados de custo; sem pipeline completo, Cost Intelligence é seed-only | Dados de custo reais não entram no sistema | Nenhuma | 8 h |
| SA-19 | **Adicionar precondição checks extensíveis para automação** | Actualmente `EvaluatePreconditions.cs` tem lógica fixa. Deveria suportar plugins/extensões. | Lógica de precondição hardcoded limita customização | Precondições one-size-fits-all | Nenhuma | 8 h |

**Total Ajustes Estruturais: 19 items, ~92 h**

---

## 5. Pré-condições para recriar migrations (D)

Antes de apagar as migrations existentes e gerar a nova baseline do módulo, **todos** os seguintes pontos devem estar concluídos:

| # | Pré-condição | Status actual | Itens bloqueadores |
|---|-------------|---------------|-------------------|
| 1 | Modelo de domínio finalizado (19 entidades revistas e confirmadas) | ✅ Concluído | `domain-model-finalization.md` |
| 2 | Todos os nomes de tabelas com prefixo `ops_` (19 tabelas) | ❌ **Pendente** — código usa `oi_` | SA-01 |
| 3 | Outbox tables com nome prefixado `ops_*_outbox_messages` | ❌ **Pendente** — usam `outbox_messages` genérico | SA-02, SA-05 |
| 4 | `RowVersion` (`xmin`) adicionado a todos os aggregates mutáveis | ❌ **Pendente** | FC-13 |
| 5 | `EnvironmentId` padronizado como `Guid` em todas as entidades | ❌ **Pendente** — mix de Guid e string | SA-03 |
| 6 | `TenantId` explícito em entidades que faltam (RuntimeSnapshot, CostSnapshot, DriftFinding) | ❌ **Pendente** | SA-04 |
| 7 | Check constraints definidos para colunas de score/rate/cost | ❌ **Pendente** | SA-06 |
| 8 | Enums de Automation convertidos de STRING para INTEGER | ❌ **Pendente** | SA-07 |
| 9 | `CostImportBatchStatus` tipado como enum (não string constants) | ❌ **Pendente** | QW-12 |
| 10 | FKs e indexes validados em todas as 16+ entity configurations | ✅ Concluído | Verificar apenas |
| 11 | UNIQUE constraints adicionados a RuntimeBaseline, ObservabilityProfile, ServiceCostProfile | ❌ **Pendente** | SA-06 |
| 12 | Estratégia de seed data definida (dev-only vs production seeds) | ❌ **Pendente** | Decisão necessária |

**Resultado:** 8 de 12 pré-condições em falta. Migrations NÃO devem ser recriadas até estas estarem resolvidas.

---

## 6. Critérios de aceite do módulo (E)

O módulo pode ser considerado **fechado** quando TODOS os seguintes critérios forem satisfeitos:

### Backend

| # | Critério | Status |
|---|----------|--------|
| 1 | Todos os 56 endpoints operacionais e testáveis | ✅ (56 endpoints registados) |
| 2 | RowVersion em todos os aggregates mutáveis | ❌ FC-13 |
| 3 | FluentValidation em todos os commands | ❌ FC-20 |
| 4 | Permissões correctas em todas as rotas | ⚠️ QW-05 (RunbooksPage errada) |
| 5 | Domain events publicados para todas as state transitions | ❌ FC-03 |
| 6 | Idempotency control em signal ingestion | ❌ FC-18 |

### Frontend

| # | Critério | Status |
|---|----------|--------|
| 7 | Todas as páginas operacionais (incluindo Cost) | ❌ FC-04 (Cost dashboard ausente) |
| 8 | Todos os API clients completos | ❌ FC-06, FC-07, FC-08 |
| 9 | i18n completo (en + pt) | ⚠️ FC-10 |
| 10 | Guards de acção em-page (write buttons condicionais) | ❌ (botões visíveis sem permissão) |

### Scoring

| # | Critério | Status |
|---|----------|--------|
| 11 | Fórmula documentada formalmente | ❌ QW-02 |
| 12 | Thresholds configuráveis (pelo menos via config) | ❌ SA-08 |
| 13 | Hysteresis para evitar flapping | ❌ SA-09 |

### Automações

| # | Critério | Status |
|---|----------|--------|
| 14 | Four-eyes principle implementado | ❌ FC-15 |
| 15 | Hardening de execution gate | ❌ FC-14 |
| 16 | Local audit trail completo | ✅ `AutomationAuditRecord` |

### Segurança

| # | Critério | Status |
|---|----------|--------|
| 17 | Todas as 16 permissões registadas em RolePermissionCatalog | ❌ QW-07, QW-08 |
| 18 | In-page action guards para write operations | ❌ |
| 19 | Tenant isolation validada em todos os 5 DbContexts | ✅ RLS via TenantRlsInterceptor |
| 20 | EnvironmentId consistente | ❌ SA-03 |

### Auditoria

| # | Critério | Status |
|---|----------|--------|
| 21 | Integração com Notifications operacional | ❌ FC-01 |
| 22 | Integração com Audit & Compliance operacional | ❌ FC-02 |

### Persistência

| # | Critério | Status |
|---|----------|--------|
| 23 | Prefixo `ops_` em todas as tabelas | ❌ SA-01 |
| 24 | Outbox tables com prefixo correcto | ❌ SA-02 |
| 25 | Migrations recriadas com schema final | ❌ (pré-condições em falta) |

### ClickHouse (quando aplicável)

| # | Critério | Status |
|---|----------|--------|
| 26 | Schema definido para runtime time-series | ❌ SA-13 |
| 27 | TenantId enforcement em query layer | ❌ SA-17 |

### Documentação mínima

| # | Critério | Status |
|---|----------|--------|
| 28 | Module README.md | ❌ QW-01 |
| 29 | Scoring formula reference | ❌ QW-02 |
| 30 | Automation workflow state machine diagram | ❌ QW-04 |
| 31 | XML docs em domain entities e interfaces públicas | ❌ QW-11 |

**Resultado:** 8/31 critérios satisfeitos (26%). O módulo NÃO está pronto para closure. Precisa de ~218h de trabalho.

---

## 7. Ordem recomendada de execução

### Sprint 1 — Quick wins e segurança (1 semana, ~20 h)

1. QW-01: Criar README.md do módulo
2. QW-02: Documentar fórmula de scoring
3. QW-03: Documentar thresholds de health
4. QW-04: Criar diagrama de automação
5. QW-05: **Corrigir permissão RunbooksPage** (security fix)
6. QW-07: Registar `operations:runbooks:write`
7. QW-12: Tipar `CostImportBatchStatus` como enum
8. FC-14: **Hardening execution gate** (security fix)
9. FC-15: **Implementar four-eyes principle** (security fix)

### Sprint 2 — Integração cross-module P0 (1 semana, ~28 h)

10. FC-03: Publicar domain events para todas as state transitions
11. FC-01: **Integrar com Notifications** (P0_BLOCKER)
12. FC-02: **Integrar com Audit & Compliance** (P0_BLOCKER)
13. FC-13: Adicionar RowVersion a aggregates

### Sprint 3 — Cost Intelligence frontend (1 semana, ~25 h)

14. FC-06: Criar API client `cost.ts`
15. FC-04: **Criar Cost dashboard page**
16. FC-05: Adicionar Cost ao sidebar menu
17. FC-07: Completar `reliability.ts` client
18. FC-08: Completar `runtimeIntelligence.ts` client

### Sprint 4 — Persistência ops_ (1 semana, ~15 h)

19. SA-01: **Renomear prefixo `oi_` → `ops_`** em 19 entity configurations
20. SA-02: Renomear outbox tables
21. SA-03: Padronizar EnvironmentId como Guid
22. SA-04: Adicionar TenantId explícito
23. SA-05: Configurar outbox em todos os DbContexts

### Sprint 5 — Scoring e thresholds (1 semana, ~15 h)

24. SA-08: Tornar thresholds configuráveis
25. SA-09: Adicionar hysteresis
26. SA-10: Adicionar SLO overrides per-service
27. SA-06: Adicionar check constraints
28. SA-07: Converter enums de STRING para INTEGER

### Sprint 6 — Backend completeness (1 semana, ~25 h)

29. FC-16: Runbook CRUD
30. FC-17: Endpoint de status transition
31. FC-18: Idempotency control em ingestion
32. FC-19: Validar IncidentCorrelationService
33. FC-20: FluentValidation em todos os commands
34. FC-21: ServiceCostProfile management endpoint
35. FC-22: RuntimeBaseline management endpoint

### Sprint 7 — Frontend polish (3 dias, ~12 h)

36. FC-09: Filtros avançados no IncidentsPage
37. FC-10: Verificar cobertura i18n pt.json
38. FC-11: Breadcrumb navigation
39. FC-12: Toast feedback em mutations
40. QW-06: Resolver pasta `operational-intelligence/`
41. QW-09: Remover dual invalidate
42. QW-10: Padronizar staleTime
43. QW-11: XML docs em interfaces

### Sprint 8 — Cost pipeline e automação avançada (1 semana, ~23 h)

44. SA-18: Completar Cost import pipeline
45. SA-11: Approval quorum para Critical
46. SA-12: Timeout para aprovações pendentes
47. SA-19: Preconditions extensíveis
48. FC-23: Workflow timeout handling
49. FC-24: Retention configuration

### Sprints 9-10 — ClickHouse (2 semanas, ~40 h)

50. SA-13: Schema ClickHouse para runtime
51. SA-14: Outbox → ClickHouse consumer
52. SA-15: Dual-read pattern em timeline queries
53. SA-16: Retention/TTL policies
54. SA-17: **TenantId enforcement em ClickHouse** (segurança)

### Post-ClickHouse — Recrear migrations

55. Validar todas as 12 pré-condições
56. Apagar migrations existentes
57. Gerar nova baseline migration
58. Testar migration up/down

---

## 8. Backlog priorizado

| # | Item | Camada | Prioridade | Tipo | Sprint | Depende de outro módulo? | Esforço |
|---|------|--------|-----------|------|--------|--------------------------|---------|
| 1 | FC-01 Integrar com Notifications | Backend | P0_BLOCKER | FUNCTIONAL_FIX | 2 | SIM (Notifications) | 10 h |
| 2 | FC-02 Integrar com Audit & Compliance | Backend | P0_BLOCKER | FUNCTIONAL_FIX | 2 | SIM (Audit) | 7 h |
| 3 | FC-03 Publicar domain events para state transitions | Backend | P1_CRITICAL | FUNCTIONAL_FIX | 2 | NÃO | 6 h |
| 4 | FC-13 RowVersion em aggregates mutáveis | Backend | P1_CRITICAL | FUNCTIONAL_FIX | 2 | NÃO | 3 h |
| 5 | FC-14 Hardening execution gate | Backend | P1_CRITICAL | FUNCTIONAL_FIX | 1 | NÃO | 2 h |
| 6 | FC-15 Four-eyes principle | Backend | P1_CRITICAL | FUNCTIONAL_FIX | 1 | NÃO | 2 h |
| 7 | FC-04 Cost dashboard frontend | Frontend | P1_CRITICAL | FUNCTIONAL_FIX | 3 | NÃO | 18 h |
| 8 | FC-06 API client cost.ts | Frontend | P1_CRITICAL | FUNCTIONAL_FIX | 3 | NÃO | 4 h |
| 9 | FC-05 Cost sidebar menu | Frontend | P1_CRITICAL | FUNCTIONAL_FIX | 3 | NÃO | 0.5 h |
| 10 | SA-01 Renomear tabelas oi_ → ops_ | Infra | P1_CRITICAL | STRUCTURAL_FIX | 4 | NÃO | 4 h |
| 11 | SA-02 Renomear outbox tables | Infra | P1_CRITICAL | STRUCTURAL_FIX | 4 | NÃO | 1 h |
| 12 | QW-01 Module README.md | Docs | P1_CRITICAL | QUICK_WIN | 1 | NÃO | 2 h |
| 13 | QW-02 Documentar scoring formula | Docs | P1_CRITICAL | QUICK_WIN | 1 | NÃO | 2 h |
| 14 | QW-03 Documentar thresholds | Docs | P1_CRITICAL | QUICK_WIN | 1 | NÃO | 1 h |
| 15 | QW-04 Diagrama automação | Docs | P1_CRITICAL | QUICK_WIN | 1 | NÃO | 2 h |
| 16 | QW-05 Corrigir RunbooksPage permission | Frontend | P1_CRITICAL | QUICK_WIN | 1 | NÃO | 0.25 h |
| 17 | QW-12 CostImportBatchStatus enum | Domain | P1_CRITICAL | QUICK_WIN | 1 | NÃO | 1 h |
| 18 | SA-03 Padronizar EnvironmentId Guid | Backend | P2_HIGH | STRUCTURAL_FIX | 4 | NÃO | 3 h |
| 19 | SA-04 TenantId explícito | Backend | P2_HIGH | STRUCTURAL_FIX | 4 | NÃO | 2 h |
| 20 | SA-08 Thresholds configuráveis | Backend | P2_HIGH | STRUCTURAL_FIX | 5 | SIM (Configuration) | 6 h |
| 21 | SA-09 Hysteresis | Backend | P2_HIGH | STRUCTURAL_FIX | 5 | NÃO | 3 h |
| 22 | SA-10 SLO overrides | Backend | P2_HIGH | STRUCTURAL_FIX | 5 | NÃO | 6 h |
| 23 | SA-17 TenantId em ClickHouse | Infra | P2_HIGH | STRUCTURAL_FIX | 10 | NÃO | 4 h |
| 24 | FC-07 reliability.ts client | Frontend | P2_HIGH | FUNCTIONAL_FIX | 3 | NÃO | 2 h |
| 25 | FC-08 runtimeIntelligence.ts client | Frontend | P2_HIGH | FUNCTIONAL_FIX | 3 | NÃO | 2 h |
| 26 | FC-09 Filtros IncidentsPage | Frontend | P2_HIGH | FUNCTIONAL_FIX | 7 | NÃO | 4 h |
| 27 | FC-10 i18n pt.json | Frontend | P2_HIGH | FUNCTIONAL_FIX | 7 | NÃO | 4 h |
| 28 | FC-16 Runbook CRUD | Backend | P2_HIGH | FUNCTIONAL_FIX | 6 | NÃO | 8 h |
| 29 | FC-17 Incident status endpoint | Backend | P2_HIGH | FUNCTIONAL_FIX | 6 | NÃO | 4 h |
| 30 | FC-18 Idempotency ingestion | Backend | P2_HIGH | FUNCTIONAL_FIX | 6 | NÃO | 3 h |
| 31 | FC-19 Validar correlation | Backend | P2_HIGH | FUNCTIONAL_FIX | 6 | SIM (Change Gov) | 3 h |
| 32 | FC-20 FluentValidation | Backend | P2_HIGH | FUNCTIONAL_FIX | 6 | NÃO | 4 h |
| 33 | FC-21 ServiceCostProfile endpoint | Backend | P2_HIGH | FUNCTIONAL_FIX | 6 | NÃO | 4 h |
| 34 | FC-22 RuntimeBaseline endpoint | Backend | P2_HIGH | FUNCTIONAL_FIX | 6 | NÃO | 4 h |
| 35 | QW-07 Registar runbooks:write | Backend | P2_HIGH | QUICK_WIN | 1 | NÃO | 0.5 h |
| 36 | QW-11 XML docs interfaces | Docs | P2_HIGH | QUICK_WIN | 7 | NÃO | 3 h |
| 37 | SA-05 Outbox em todos DbContexts | Infra | P2_HIGH | STRUCTURAL_FIX | 4 | NÃO | 1 h |
| 38 | SA-06 Check constraints | Infra | P2_HIGH | STRUCTURAL_FIX | 5 | NÃO | 2 h |
| 39 | SA-18 Cost import pipeline | Backend | P2_HIGH | STRUCTURAL_FIX | 8 | NÃO | 8 h |
| 40 | SA-13 ClickHouse schema runtime | Infra | P2_HIGH | STRUCTURAL_FIX | 9 | NÃO | 8 h |
| 41 | SA-14 ClickHouse consumer | Infra | P2_HIGH | STRUCTURAL_FIX | 9 | SIM (Infraestrutura) | 16 h |
| 42 | SA-15 Dual-read pattern | Backend | P2_HIGH | STRUCTURAL_FIX | 10 | NÃO | 8 h |
| 43 | SA-16 Retention/TTL policies | Infra | P2_HIGH | STRUCTURAL_FIX | 10 | NÃO | 4 h |
| 44 | FC-11 Breadcrumbs | Frontend | P3_MEDIUM | FUNCTIONAL_FIX | 7 | NÃO | 4 h |
| 45 | FC-12 Toast feedback | Frontend | P3_MEDIUM | FUNCTIONAL_FIX | 7 | NÃO | 2 h |
| 46 | FC-23 Workflow timeout | Backend | P3_MEDIUM | FUNCTIONAL_FIX | 8 | NÃO | 4 h |
| 47 | FC-24 Retention cleanup job | Backend | P3_MEDIUM | FUNCTIONAL_FIX | 8 | NÃO | 8 h |
| 48 | SA-07 Enum STRING→INTEGER | Infra | P3_MEDIUM | STRUCTURAL_FIX | 5 | NÃO | 2 h |
| 49 | SA-11 Approval quorum | Backend | P3_MEDIUM | STRUCTURAL_FIX | 8 | NÃO | 4 h |
| 50 | SA-12 Approval timeout | Backend | P3_MEDIUM | STRUCTURAL_FIX | 8 | NÃO | 3 h |
| 51 | SA-19 Preconditions extensíveis | Backend | P3_MEDIUM | STRUCTURAL_FIX | 8 | NÃO | 8 h |
| 52 | QW-06 Resolver pasta frontend | Frontend | P3_MEDIUM | QUICK_WIN | 7 | NÃO | 1 h |
| 53 | QW-08 Registar reliability:write | Backend | P3_MEDIUM | QUICK_WIN | 1 | NÃO | 0.5 h |
| 54 | QW-09 Remover dual invalidate | Frontend | P3_MEDIUM | QUICK_WIN | 7 | NÃO | 0.5 h |
| 55 | QW-10 Padronizar staleTime | Frontend | P3_MEDIUM | QUICK_WIN | 7 | NÃO | 1 h |

### Resumo por prioridade

| Prioridade | Items | Esforço |
|-----------|-------|---------|
| P0_BLOCKER | 2 | ~17 h |
| P1_CRITICAL | 15 | ~48 h |
| P2_HIGH | 22 | ~110 h |
| P3_MEDIUM | 16 | ~43 h |
| **Total** | **55** | **~218 h** |

### Resumo por tipo

| Tipo | Items | Esforço |
|------|-------|---------|
| QUICK_WIN | 12 | ~15 h |
| FUNCTIONAL_FIX | 24 | ~98 h |
| STRUCTURAL_FIX | 19 | ~92 h |
| PRE-MIGRATION | — | ~13 h (incluído acima) |
| **Total** | **55** | **~218 h** |

### Dependências externas

| Item | Módulo dependente | Impacto |
|------|------------------|---------|
| FC-01 | Notifications (11) | Módulo Notifications deve ter event handlers |
| FC-02 | Audit & Compliance (10) | Módulo Audit deve aceitar eventos de OI |
| FC-19 | Change Governance (05) | Change Gov deve publicar events consumidos por OI |
| SA-08 | Configuration (09) | Thresholds via Configuration module |
| SA-14 | Infraestrutura ClickHouse | Cluster ClickHouse provisionado |

---

## Fonte dos dados

Este plano consolida gaps de:

- `backend-functional-corrections.md` — 16 backend items
- `frontend-functional-corrections.md` — 16 frontend items
- `scoring-thresholds-automation-review.md` — 16 scoring/automation items
- `security-and-permissions-review.md` — 15 security items
- `persistence-model-finalization.md` — 13 divergences + 8 persistence items
- `module-dependency-map.md` — 10 dependency items
- `documentation-and-onboarding-upgrade.md` — 10 documentation items
- `clickhouse-data-placement-review.md` — 8 ClickHouse items
- `end-to-end-operational-flow-validation.md` — 5 flow gaps
- `domain-model-finalization.md` — 8 domain gaps

Items foram deduplicados, consolidados e re-priorizados para este plano unificado.

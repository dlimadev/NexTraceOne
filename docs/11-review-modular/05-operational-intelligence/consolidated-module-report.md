# Operational Intelligence — Consolidated Module Report

> Gerado a partir da consolidação de todos os relatórios de auditoria e revisão modular do NexTraceOne.
> Última atualização: 2026-03-24

---

## 1. Visão Geral do Módulo

O **Operational Intelligence** é o maior módulo do NexTraceOne em termos de escopo funcional, cobrindo cinco subdomínios operacionais:

| Subdomínio | Propósito | DbContext |
|------------|-----------|-----------|
| **Incidents** | Gestão de incidentes, correlação change↔incident, evidência, mitigação, runbooks | `IncidentDbContext` |
| **Automation** | Workflows de automação operacional, aprovação, execução, validação, auditoria | `AutomationDbContext` |
| **Reliability** | Snapshots de fiabilidade por serviço/equipa, scoring ponderado (0–100), trends | `ReliabilityDbContext` |
| **Runtime** | Health monitoring, drift detection entre ambientes, baselines, observability debt | `RuntimeIntelligenceDbContext` |
| **Cost** | Cost records e snapshots por serviço, atribuição, trends, anomalias (FinOps operacional) | `CostIntelligenceDbContext` |

**Localização no código:**

| Camada | Caminho |
|--------|---------|
| Backend | `src/modules/operationalintelligence/` |
| Frontend (principal) | `src/frontend/src/features/operations/` (16 ficheiros, 10 páginas) |
| Frontend (embrionário) | `src/frontend/src/features/operational-intelligence/` (1 ficheiro — FinOps config) |

**Classificação:** Prioridade **P2** · Criticidade **MÉDIA** · Maturidade global **74%** · Tipo de correção **LOCAL_FIX**

---

## 2. Estado Atual

### 2.1 Maturidade por Dimensão

| Dimensão | Maturidade | Nota |
|----------|-----------|------|
| Backend | 🟢 90% | Domínio DDD rico, CQRS completo, 5 bounded contexts bem isolados |
| Frontend | 🟢 85% | 10 páginas funcionais, 5 API clients (784 linhas), classificação COMPLETE_APPARENT |
| Documentação | 🟡 50% | Sem README de módulo, sem documentação do modelo de domínio |
| Testes | 🟡 70% | 164+ testes existentes, mas cobertura irregular entre subdomínios |
| **Overall** | **🟡 74%** | |

### 2.2 Domínio — 51 Entidades

Classificação DDD: **✅ COERENTE** — modelo rico com 51 entidades distribuídas por 5 bounded contexts bem separados.

| Entidade Principal | Tipo | DbContext |
|--------------------|------|-----------|
| `IncidentRecord` | AggregateRoot | IncidentDb |
| `MitigationAction` | Entity | IncidentDb |
| `AutomationWorkflowRecord` | AggregateRoot | AutomationDb |
| `AutomationExecution` | Entity | AutomationDb |
| `Runbook` | Entity | AutomationDb |
| `ReliabilitySnapshot` | AggregateRoot | ReliabilityDb |
| `RuntimeInsight` | Entity | RuntimeIntelDb |
| `CostReport` | AggregateRoot | CostIntelDb |

### 2.3 Endpoints API — 7 Endpoint Modules (~40+ endpoints)

| Endpoint Module | Rota base | Permissões | Rate Limit | DbContext |
|-----------------|-----------|------------|------------|-----------|
| `AutomationEndpointModule` | `/api/operations/automation` | `operations:automation:read/write/execute/approve` | operations / auth-sensitive | AutomationDb |
| `CostIntelligenceEndpointModule` | `/api/operations/costs` | `operations:cost:read/write` | data-intensive | CostIntelDb |
| `IncidentEndpointModule` | `/api/operations/incidents` | `operations:incidents:read/write` | operations | IncidentDb |
| `MitigationEndpointModule` | `/api/operations/mitigation` | `operations:mitigation:read/write` | operations | IncidentDb |
| `RunbookEndpointModule` | `/api/operations/runbooks` | `operations:runbooks:read` | Global | AutomationDb |
| `ReliabilityEndpointModule` | `/api/operations/reliability` | `operations:reliability:read` | data-intensive | ReliabilityDb |
| `RuntimeIntelligenceEndpointModule` | `/api/operations/runtime` | `operations:runtime:read/write` | operations | RuntimeIntelDb |

### 2.4 Rotas Frontend — 10 Páginas (todas ✅ FUNCIONAL)

| Página | Rota | Permissão | Menu |
|--------|------|-----------|------|
| IncidentsPage | `/operations/incidents` | `operations:incidents:read` | ✅ operations |
| IncidentDetailPage | `/operations/incidents/:incidentId` | `operations:incidents:read` | Navegação |
| RunbooksPage | `/operations/runbooks` | `operations:runbooks:read` | ✅ operations |
| AutomationWorkflowsPage | `/operations/automation` | `operations:automation:read` | ✅ operations |
| AutomationWorkflowDetailPage | `/operations/automation/:workflowId` | `operations:automation:read` | Navegação |
| AutomationAdminPage | `/operations/automation/admin` | `operations:automation:read` | Via admin |
| TeamReliabilityPage | `/operations/reliability` | `operations:reliability:read` | ✅ operations |
| ServiceReliabilityDetailPage | `/operations/reliability/:serviceId` | `operations:reliability:read` | Navegação |
| EnvironmentComparisonPage | `/operations/runtime-comparison` | `operations:runtime:read` | ✅ operations |
| PlatformOperationsPage | `/platform/operations` | `platform:admin:read` | ✅ admin |

---

## 3. Problemas Críticos e Bloqueadores

Não existem bloqueadores P0 neste módulo. Os problemas identificados são de maturidade e completude, não de funcionalidade partida.

| # | Problema | Severidade | Causa Raiz |
|---|---------|-----------|------------|
| 1 | `nextraceone_operations` aloja **12 DbContexts** numa única BD, sendo 5 deste módulo | 🟠 Alto | CR-7 (Dívida técnica BD) |
| 2 | Integração com dados de telemetria em tempo real incompleta — dashboards usam dados estáticos/seed | 🟠 Alto | Backend sem ingestão real |
| 3 | Integração `IncidentCorrelationService` com Change Intelligence por validar end-to-end | 🟡 Médio | Acoplamento cross-module |

---

## 4. Problemas por Camada

### 4.1 Frontend

| # | Problema | Severidade | Detalhe |
|---|---------|-----------|--------|
| F1 | Módulo `operational-intelligence/` embrionário com apenas 1 ficheiro (`OperationsFinOpsConfigurationPage`) | 🟡 Médio | Classificação PARTIAL no inventário frontend — sem integração API, sem API client próprio. Necessita expansão ou fusão com `operations/` |
| F2 | Página `AutomationAdminPage` não listada no inventário principal de rotas | 🟢 Baixo | Acessível via admin, mas potencialmente sub-documentada |

**Nota positiva:** As 10 páginas principais em `features/operations/` estão todas funcionais, com 5 ficheiros de API client (incidents, reliability, automation, runtimeIntelligence, platformOps) e padrão React Query consistente.

### 4.2 Backend

| # | Problema | Severidade | Detalhe |
|---|---------|-----------|--------|
| B1 | Ausência de dados reais de telemetria — dashboards dependem de seed/mock | 🟠 Alto | `RuntimeIntelligenceDbContext` e `ReliabilityDbContext` não têm ingestão automatizada |
| B2 | Correlação incidente↔mudança depende de dados em tempo real do módulo Change Governance | 🟡 Médio | `IncidentCorrelationService` por validar com integração real |
| B3 | Subdomínio Cost Intelligence como parte do OpIntel gera acoplamento conceptual com FinOps do módulo Governance | 🟡 Médio | Avaliar se deve ser subdomínio independente |
| B4 | Endpoint `RunbookEndpointModule` tem apenas GET — sem CRUD completo para runbooks | 🟡 Médio | Apenas leitura; criação/edição pode estar em falta |
| B5 | Endpoint `ReliabilityEndpointModule` listado apenas com GET — endpoints detalhados (trend, coverage, by-team) não confirmados na auditoria estrutural | 🟢 Baixo | Module review menciona ~7 endpoints, auditoria lista apenas rota base |

### 4.3 Database

| # | Problema | Severidade | Detalhe |
|---|---------|-----------|--------|
| D1 | 5 DbContexts deste módulo partilham `nextraceone_operations` com outros 7 DbContexts (12 no total) | 🟠 Alto | Risco de colisão de tabelas outbox, contenção em DDL, complexidade de migrations |
| D2 | Zero RowVersion/ConcurrencyToken em entidades do módulo | 🟡 Médio | Conflitos de concorrência resolvidos com last-write-wins via `UpdatedAt` — aceitável para cargas moderadas |
| D3 | Zero check constraints — validação apenas na camada aplicacional | 🟡 Médio | Dados inválidos podem entrar via acesso directo à BD |
| D4 | Sem filtered indexes `WHERE IsDeleted = false` | 🟡 Médio | Queries sobre dados soft-deleted podem ser ineficientes |

**Nota positiva:** Todas as boas práticas transversais estão aplicadas — RLS via `TenantRlsInterceptor`, `AuditInterceptor`, soft delete, outbox pattern, strongly-typed IDs, `EncryptedStringConverter`. O módulo tem **12 migrations** activas e C# seeder `IncidentSeedData` com 6 incidentes e 3 runbooks.

### 4.4 Segurança

| # | Problema | Severidade | Detalhe |
|---|---------|-----------|--------|
| S1 | Endpoint `/api/operations/automation/{id}/approve` é auth-sensitive mas usa rate limiting genérico `auth-sensitive` — sem MFA step-up verificável | 🟡 Médio | Aprovação de automação é operação crítica — deveria exigir confirmação adicional |
| S2 | Permissão `operations:automation:execute` permite execução de workflows sem approval gate obrigatório a nível de API | 🟢 Baixo | O domínio modela state transitions (Draft→PendingApproval→Approved→Executing), mas enforcement no endpoint por validar |

**Nota positiva:** O módulo define 9 permissões granulares cobrindo read/write/approve/execute, com JWT obrigatório em todos os endpoints e RLS de tenant activo em todos os DbContexts.

### 4.5 IA e Agentes

Não existem agentes de IA dedicados ao módulo Operational Intelligence na implementação actual. Contudo, o relatório `database-ai-agents-workflow-support-report.md` confirma que o pilar **Operational Reliability** tem suporte adequado na BD para integração futura com agentes (via entidades `Incident`, `Runbook`, `AutomationRule`).

| # | Problema | Severidade | Detalhe |
|---|---------|-----------|--------|
| A1 | Sem agente de IA para investigação de incidentes ou recomendação de mitigação | 🟡 Médio | Visão do produto inclui "AI-assisted Analysis and Recommendations" — pilar ainda não conectado |
| A2 | Sem integração com AI Knowledge Sources para contexto operacional nos incidentes | 🟢 Baixo | Dependente da maturidade do módulo AI Knowledge (actualmente 25% backend) |

### 4.6 Documentação

| # | Problema | Severidade | Detalhe |
|---|---------|-----------|--------|
| DC1 | Sem README do módulo `operationalintelligence` | 🟠 Alto | Nenhum dos 9 módulos backend tem README, mas este módulo — o maior — é especialmente afectado |
| DC2 | Modelo de scoring de reliability não documentado formalmente | 🟡 Médio | Fórmula existe no código (`OverallScore = RuntimeHealthScore×0.50 + IncidentImpactScore×0.30 + ObservabilityScore×0.20`) mas sem doc dedicada |
| DC3 | Thresholds de health classification não documentados | 🟡 Médio | Unhealthy: ErrorRate≥10% OR P99≥3000ms; Degraded: ErrorRate≥5% OR P99≥1000ms — apenas no código |
| DC4 | Fluxo de automação (Draft→Approved→Executing→Completed) sem diagrama | 🟡 Médio | State machine complexa (8 action types, 5 risk levels) sem representação visual |
| DC5 | Sem documentação das invariantes de negócio dos aggregates do módulo | 🟢 Baixo | Facilita review e manutenção futura |

---

## 5. Dependências

### 5.1 Dependências Ascendentes (módulos dos quais depende)

| Módulo | Tipo | Detalhe |
|--------|------|--------|
| **Identity & Access** | Fundacional | JWT, RLS, permissões `operations:*` |
| **Catalog** | Dados | `ServiceId` para correlação de incidentes e reliability por serviço |
| **Change Governance** | Dados | `ChangeCorrelation` referencia mudanças para correlação incidente↔change |

### 5.2 Dependências Descendentes (módulos que dependem deste)

Nenhum módulo depende directamente do Operational Intelligence. Não é bloqueador para outros módulos.

### 5.3 Dependência Partilhada de BD

A base `nextraceone_operations` é partilhada com Change Governance (4 DbContexts), Governance (1), Configuration (1) e Notifications (1). A separação lógica via DbContexts isolados permite migração futura para BDs separadas sem alteração de código, mas a concentração actual (12 DbContexts) é um risco operacional.

---

## 6. Quick Wins

| # | Acção | Esforço | Impacto | Referência |
|---|-------|---------|---------|------------|
| QW1 | Criar README do módulo `operationalintelligence` com visão geral dos 5 subdomínios, endpoints, e fluxos | 3h | Docs 50%→60% | DC1 |
| QW2 | Documentar fórmula de scoring de reliability e thresholds de health classification num ficheiro dedicado | 2h | Conhecimento operacional explícito | DC2, DC3 |
| QW3 | Adicionar diagrama do fluxo de automação (state machine) à documentação | 2h | Compreensão do domínio | DC4 |
| QW4 | Decidir e documentar destino do módulo frontend `operational-intelligence/` (fusão com `operations/` ou expansão) | 1h | Eliminar ambiguidade PARTIAL | F1 |
| QW5 | Verificar e documentar se endpoints de reliability detalhados (trend, coverage, by-team) existem ou são gaps | 2h | Clarificar inventário real | B5 |

**Esforço total estimado: ~10h (1–2 dias)**

---

## 7. Refactors Estruturais

| # | Refactor | Esforço | Impacto | Risco | Prioridade |
|---|---------|---------|---------|-------|-----------|
| SR1 | Implementar ingestão real de dados de telemetria para Runtime e Reliability (substituir seed/mock) | 2–3 semanas | Dashboard com dados reais — diferença entre demo e produção | Médio | P2 |
| SR2 | Avaliar e potencialmente executar split de `nextraceone_operations` em 2–3 bases separadas por bounded context | 3–4 semanas | Reduz risco de contenção, simplifica migrations | Alto | P4 |
| SR3 | Adicionar RowVersion/ConcurrencyToken nas entidades AggregateRoot do módulo (IncidentRecord, AutomationWorkflowRecord, CostReport, ReliabilitySnapshot) | 1 semana | Integridade sob concorrência elevada | Médio | P3 |
| SR4 | Conectar `IncidentCorrelationService` com Change Intelligence real e validar fluxo end-to-end | 1–2 semanas | Pilar Change-to-Incident Correlation funcional | Médio | P2 |
| SR5 | Avaliar extracção de Cost Intelligence como subdomínio independente (separado do OpIntel e do FinOps do Governance) | 1 semana (análise) + 2–3 semanas (execução) | Fronteiras de bounded context claras | Médio | P3 |
| SR6 | Completar CRUD de runbooks (actualmente apenas GET no endpoint) e elevar cobertura de testes para ≥80% | 1–2 semanas | Módulo completo para Operational Reliability | Baixo | P3 |

---

## 8. Critérios de Fecho

Baseado nos planos de fecho modular e critérios de produto:

| # | Critério | Estado Actual | Alvo |
|---|---------|--------------|------|
| 1 | Dashboard com dados reais (não seed/mock) | ❌ Não atingido | Telemetria real integrada |
| 2 | README do módulo criado | ❌ Não existe | README + sub-docs por subdomínio |
| 3 | Documentação do domínio (scoring, thresholds, state machines) | ❌ Não existe | Documentação formal |
| 4 | Fluxo de incidentes validado end-to-end: Create→Correlate→Evidence→Mitigate→Resolve | ⚠️ Não validado | Validação completa |
| 5 | Fluxo de automação validado: Create→Approve→Execute→Validate | ⚠️ Não validado | Validação completa |
| 6 | Correlação incidente↔mudança funcional com dados reais | ⚠️ Não validado | Integração real com Change Governance |
| 7 | Testes ≥80% | ❌ 70% actual | 80%+ |
| 8 | Maturidade ≥80% | ❌ 74% actual | 80%+ |

**Maturidade alvo:** 74% → **80%+**

---

## 9. Plano de Ação Priorizado

### Fase 1 — Quick Wins (1–2 dias)

| Ordem | Acção | Ref |
|-------|-------|-----|
| 1 | Criar README do módulo | QW1 |
| 2 | Documentar scoring, thresholds, e state machine de automação | QW2, QW3 |
| 3 | Resolver ambiguidade do módulo frontend `operational-intelligence/` | QW4 |
| 4 | Clarificar inventário real de endpoints de reliability | QW5 |

### Fase 2 — Validação e Completude (1–2 semanas) — Wave 3 do plano global

| Ordem | Acção | Ref |
|-------|-------|-----|
| 5 | Validar fluxo de incidentes end-to-end | Critério 4 |
| 6 | Validar fluxo de automação end-to-end | Critério 5 |
| 7 | Validar reliability scoring end-to-end | Critério 4 |
| 8 | Validar runtime health auto-classification e drift detection | — |
| 9 | Completar CRUD de runbooks | SR6 |
| 10 | Elevar cobertura de testes de 70% para 80%+ | Critério 7 |

### Fase 3 — Integrações e Dados Reais (2–3 semanas) — Wave 3–4 do plano global

| Ordem | Acção | Ref |
|-------|-------|-----|
| 11 | Conectar correlação incidente↔mudança com Change Intelligence | SR4 |
| 12 | Implementar ingestão de dados de telemetria real | SR1 |
| 13 | Avaliar posição de Cost Intelligence (subdomínio vs módulo independente) | SR5 |

### Fase 4 — Infra e Resiliência (longo prazo) — Waves 4–5 do plano global

| Ordem | Acção | Ref |
|-------|-------|-----|
| 14 | Adicionar RowVersion nas entidades AggregateRoot | SR3 |
| 15 | Participar na avaliação de split de `nextraceone_operations` | SR2 |

---

## 10. Inconsistências entre Relatórios

| # | Inconsistência | Relatórios Envolvidos | Impacto | Resolução Sugerida |
|---|---------------|----------------------|---------|-------------------|
| 1 | **Número de endpoints diverge**: module-review menciona "40+ endpoints" distribuídos por 5 subdomínios (~10+15+7+5+8), mas o `backend-endpoints-report.md` lista apenas ~15 endpoints explícitos nos 7 endpoint modules | `module-review.md` vs `backend-endpoints-report.md` | 🟡 Médio — incerteza sobre completude real da API | Realizar auditoria directa ao código dos endpoint modules para inventário definitivo |
| 2 | **Número de DbContexts**: o `backend-persistence-report.md` refere 5 DbContexts para OpIntel, mas o `database-structural-audit.md` conta 12 DbContexts em `nextraceone_operations` no total. Contudo, o module-review lista exactamente 5 DbContexts (Incident, Automation, Reliability, RuntimeIntelligence, CostIntelligence) — consistente | Consistente entre relatórios | 🟢 Sem impacto | Confirmado: 5 DbContexts deste módulo, 12 no total da BD |
| 3 | **Classificação frontend**: `frontend-module-inventory.md` classifica `operations/` como COMPLETE_APPARENT, mas classifica `operational-intelligence/` separadamente como PARTIAL (1 ficheiro). O `modular-review-summary.md` trata tudo como um único módulo funcional a 85% | `frontend-module-inventory.md` vs `modular-review-summary.md` | 🟡 Médio — dois módulos frontend para um módulo backend | Fusionar `operational-intelligence/` em `operations/` ou expandir com propósito claro |
| 4 | **Maturidade de testes**: module-review diz "164+ existentes, alguns ausentes", `modular-review-summary.md` atribui 70%, e o `module-consolidation-report.md` diz "Razoável". Não há contagem precisa de testes por subdomínio | Vários | 🟢 Baixo — todos concordam que está entre 70–75% | Executar contagem real de testes por subdomínio |
| 5 | **Criticidade diverge**: `module-consolidation-report.md` classifica criticidade como "MÉDIA" e diz "Não bloqueia outros", enquanto o `modular-review-summary.md` posiciona OpIntel como dependente de Catalog e Change Governance na cadeia de dependências. Isto sugere que, embora não bloqueie, é bloqueado por outros | `module-consolidation-report.md` vs `modular-review-summary.md` | 🟢 Baixo — ambos concordam que não é bloqueador | Manter classificação MÉDIA; priorizar dependências ascendentes primeiro |

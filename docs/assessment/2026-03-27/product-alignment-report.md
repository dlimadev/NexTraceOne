# Relatório de Alinhamento com a Visão do Produto

**Projeto:** NexTraceOne
**Data da Avaliação:** 2026-03-27
**Escopo:** Avaliação transversal de todos os módulos face à visão oficial do produto
**Avaliação Global:** ~65% alinhado

---

## 1. Resumo Executivo

O NexTraceOne define-se como **"a fonte de verdade para serviços, contratos, mudanças, operação e conhecimento operacional"**. Esta avaliação mede o grau em que cada módulo e pilar do produto contribui efetivamente para essa visão.

### Resultado Global: ~65% Alinhado

O produto apresenta uma base arquitectural sólida — 12 módulos com bounded contexts bem definidos, 23 DbContexts especializados, entidades de domínio com strongly-typed IDs, e uma separação Clean Architecture consistente (API → Application → Domain → Infrastructure). Os pilares de **Contract Governance** e **Service Catalog** são os mais maduros, com persistência real, migrações completas e fluxos funcionais.

No entanto, múltiplos pilares operam com dados simulados (`IsSimulated`), handlers vazios ou incompletos, e módulos recentes (Knowledge, Integrations, ProductAnalytics) sem migrações EF. O frontend utiliza React 19 com React Router v7 em vez da stack alvo documentada (React 18, TanStack Router, Zustand), o que representa um desvio técnico a monitorizar.

**Distribuição por estado:**

| Estado | Pilares | Percentagem |
|--------|---------|-------------|
| READY | 1 | 10% |
| PARTIAL | 8 | 80% |
| INCOMPLETE | 1 | 10% |

---

## 2. Avaliação por Pilar

### 2.1 Service Governance — PARTIAL (70%)

**O que funciona:**
- Service Catalog implementado no módulo `catalog` com entidades completas
- Tabelas `cat_service_assets` com migrações aplicadas
- `CatalogGraphDbContext` para topologia e dependências
- Endpoints de CRUD de serviços com DTOs claros
- Frontend com `ServiceCatalogListPage`

**O que falta ou está parcial:**
- Teams geridos no módulo `governance` (`gov_teams`) em vez de ter visibilidade forte no catálogo
- Service Reliability no módulo `operationalintelligence` usa dados simulados em 13 handlers
- `ReliabilityDbContext` existe mas snapshots dependem de `IsSimulated`

**Ficheiros de referência:**
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Persistence/CatalogGraphDbContext.cs`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Reliability/Persistence/ReliabilityDbContext.cs`

---

### 2.2 Contract Governance — READY (85%)

**O que funciona:**
- Suporte completo a 4 tipos de contrato: REST, SOAP, Event, Background Service
- `ContractsDbContext` com tabelas `ctr_contract_versions`, `ctr_contract_drafts`, `ctr_spectral_rulesets`
- Contract Studio com draft workflow funcional
- Versionamento semântico com diff
- Validação via Spectral rulesets geridos na base de dados
- Frontend com `SpectralRulesetManagerPage` e `ContractListPage`
- `DeveloperPortalDbContext` para portal de publicação

**O que falta ou está parcial:**
- Publication Center parcial — fluxo de approval workflow pode ser mais robusto
- Políticas de contrato (spectral rulesets) funcionais mas sem enforcement automático completo
- Exemplos e schemas poderiam ter melhor UX de gestão

**Ficheiros de referência:**
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/ContractsDbContext.cs`
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Portal/Persistence/DeveloperPortalDbContext.cs`
- `src/frontend/src/features/contracts/spectral/SpectralRulesetManagerPage.tsx`

---

### 2.3 Change Intelligence & Production Change Confidence — PARTIAL (60%)

**O que funciona:**
- 4 DbContexts dedicados: `ChangeIntelligenceDbContext`, `PromotionDbContext`, `RulesetGovernanceDbContext`, `WorkflowDbContext`
- Entidades para releases, promoções, gates, lint results (`chg_lint_results`, `chg_promotion_requests`)
- Workflow de promoção entre ambientes
- Evidence Pack como entidade de domínio
- Rollback Assessment como entidade de domínio

**O que falta ou está parcial:**
- Blast Radius existe como entidade mas é computado sem telemetria real
- Scoring de confiança precisa de ingestão real de dados de deploy
- Correlação change-to-incident não tem dados reais de runtime
- Release Calendar: freeze windows existem mas não há UI de calendário

**Ficheiros de referência:**
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/ChangeIntelligence/Persistence/ChangeIntelligenceDbContext.cs`
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/Promotion/Persistence/PromotionDbContext.cs`
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/Workflow/Persistence/WorkflowDbContext.cs`

---

### 2.4 Operational Reliability — PARTIAL (50%)

**O que funciona:**
- Entidades SLO/SLA com `ReliabilityDbContext`
- Incidentes com `IncidentDbContext` e tabelas `ops_incidents`
- Handlers para snapshots de fiabilidade, burn rates, error budgets
- Tabelas `ops_reliability_snapshots`, `ops_cost_records`

**O que está parcial ou problemático:**
- **13 handlers usam o padrão `IsSimulated`**, retornando dados fabricados em vez de dados reais
- Isto afeta: reliability snapshots, burn rates, SLO compliance, error budgets, service health
- A confiança que o produto oferece neste pilar é ilusória enquanto os dados forem simulados

**Ficheiros afetados (padrão IsSimulated):**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/` — múltiplos handlers em Features/

**Ficheiros de referência:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Reliability/Persistence/ReliabilityDbContext.cs`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Incidents/Persistence/IncidentDbContext.cs`

---

### 2.5 Operational Consistency — PARTIAL (45%)

**O que funciona:**
- Runbooks como entidade de domínio no módulo `operationalintelligence`
- `AutomationDbContext` com persistência de workflows de automação
- Frontend com páginas `RunbooksPage` e `AutomationWorkflowsPage`

**O que falta ou está parcial:**
- Handlers de automação têm lógica vazia ou simulada
- Audit trail de automação usa `IsSimulated`
- Não há execução real de runbooks automatizados
- Conexão entre runbooks e incidentes/mudanças é conceptual mas não operacional

**Ficheiros de referência:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Automation/Persistence/AutomationDbContext.cs`

---

### 2.6 AI-Assisted Operations & Engineering — PARTIAL (55%)

**O que funciona:**
- Streaming de respostas funcional (one-shot)
- 3 ferramentas reais de agente implementadas (tool execution)
- Grounding básico conectado
- `AiOrchestrationDbContext` para persistência de sessões e resultados
- Model registry funcional

**O que falta ou está parcial:**
- **11 handlers de orquestração vazios ou incompletos** (D-024 a D-029, D-046)
- Retrieval básico — sem vector search, sem cross-module entity lookup
- Agentes especializados (contract creation, change analysis, incident investigation) não implementados
- Grounding não consulta Knowledge, ChangeGovernance ou OperationalIntelligence

**Ficheiros de referência:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Orchestration/Persistence/AiOrchestrationDbContext.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/ExternalAI/Persistence/ExternalAiDbContext.cs`

---

### 2.7 Source of Truth & Operational Knowledge — PARTIAL (40%)

**O que funciona:**
- Global Search com Full-Text Search (PostgreSQL FTS) implementado
- Frontend `GlobalSearchPage` funcional
- Módulo Knowledge criado com `KnowledgeDbContext`

**O que falta ou está parcial:**
- **Módulo Knowledge sem migrações EF** — não pode persistir dados
- Sem operações de update/delete em artigos de conhecimento
- Sem relações cross-module (knowledge ↔ serviço, knowledge ↔ contrato, knowledge ↔ mudança)
- Sem frontend dedicado para Knowledge Hub
- Changelog, Operational Notes e Knowledge Relations não implementados

**Ficheiros de referência:**
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/Persistence/KnowledgeDbContext.cs`

---

### 2.8 AI Governance — PARTIAL (55%)

**O que funciona:**
- Model Registry com `AiGovernanceDbContext`
- Políticas de acesso a modelos (entidades de domínio)
- Budgets e quotas de tokens (entidades de domínio)
- Auditoria de uso de IA (entidades de domínio)

**O que falta ou está parcial:**
- **6 handlers ExternalAI vazios** (D-024 a D-029) — não há integração real com providers externos
- Enforcement de políticas não testado end-to-end
- Budget tracking sem dados reais de consumo
- Sem UI de gestão de políticas de IA

**Ficheiros de referência:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Persistence/AiGovernanceDbContext.cs`

---

### 2.9 Operational Intelligence & Optimization — PARTIAL (45%)

**O que funciona:**
- 5 DbContexts dedicados: Automation, Cost, Incidents, Reliability, Runtime
- Entidades para cost records, incidents, reliability snapshots
- `RuntimeIntelligenceDbContext` para telemetria de runtime
- Tabelas `ops_*` com migrações aplicadas

**O que falta ou está parcial:**
- Runtime Intelligence precisa de ingestão real de telemetria (traces, logs, métricas)
- Cost Intelligence com dados simulados
- Correlações entre telemetria e mudanças não operacionais
- Sem integração com ClickHouse (direcção arquitectural futura)

**Ficheiros de referência:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Runtime/Persistence/RuntimeIntelligenceDbContext.cs`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Cost/Persistence/CostIntelligenceDbContext.cs`

---

### 2.10 FinOps Contextual — PARTIAL (35%)

**O que funciona:**
- Handlers existem para: domain FinOps, service FinOps, team FinOps, summary, trends, benchmarking, waste signals, executive drill-down
- Frontend com 6+ páginas dedicadas: FinOpsPage, DomainFinOpsPage, ServiceFinOpsPage, TeamFinOpsPage, ExecutiveFinOpsPage, BenchmarkingPage

**O que está parcial ou problemático:**
- **Todos os handlers usam dados simulados/computados** (`IsSimulated = true`)
- 6 páginas frontend com `DemoBanner` sinalizando dados de demonstração
- Sem integração real com fontes de custo (cloud providers, infraestrutura)
- Sem atribuição real de custo por serviço, equipa ou mudança

**Ficheiros afetados:**
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetDomainFinOps/GetDomainFinOps.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetServiceFinOps/GetServiceFinOps.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetTeamFinOps/GetTeamFinOps.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetFinOpsSummary/GetFinOpsSummary.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetFinOpsTrends/GetFinOpsTrends.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetBenchmarking/GetBenchmarking.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetWasteSignals/GetWasteSignals.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetEfficiencyIndicators/GetEfficiencyIndicators.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetExecutiveTrends/GetExecutiveTrends.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetExecutiveDrillDown/GetExecutiveDrillDown.cs`
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetFrictionIndicators/GetFrictionIndicators.cs`

---

## 3. Anti-Padrões Encontrados

### 3.1 Padrão `IsSimulated` — Dados Fabricados em Handlers de Produção

**Gravidade:** ALTA
**Ocorrências:** 31 ocorrências em 11 ficheiros de handler

Este é o anti-padrão mais crítico encontrado. Handlers retornam dados fabricados com a flag `IsSimulated = true`, criando uma ilusão de funcionalidade completa quando na realidade não existe integração com dados reais.

**Impacto no produto:**
- Mina a credibilidade do NexTraceOne como "fonte de verdade"
- FinOps, Reliability e Cost Intelligence são essencialmente demonstrações
- Decisões operacionais baseadas em dados simulados são perigosas
- Clientes enterprise não aceitam dados fabricados em ambientes de produção

**Ficheiros identificados (amostra):**
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetDomainFinOps/GetDomainFinOps.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetEfficiencyIndicators/GetEfficiencyIndicators.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetBenchmarking/GetBenchmarking.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetServiceFinOps/GetServiceFinOps.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetFinOpsSummary/GetFinOpsSummary.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetFinOpsTrends/GetFinOpsTrends.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetTeamFinOps/GetTeamFinOps.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetWasteSignals/GetWasteSignals.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetExecutiveTrends/GetExecutiveTrends.cs`
- `src/modules/governance/NexTraceOne.Governance.Application/Features/GetExecutiveDrillDown/GetExecutiveDrillDown.cs`
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetFrictionIndicators/GetFrictionIndicators.cs`

---

### 3.2 Handlers Vazios na Orquestração de IA

**Gravidade:** ALTA
**Ocorrências:** 11 handlers de orquestração sem implementação real (D-024 a D-029, D-046)

Handlers de ExternalAI e Orchestration no módulo AIKnowledge não têm implementação funcional, impedindo integração real com providers de IA externos e limitando os agentes especializados.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/` — handlers em Features de ExternalAI e Orchestration

---

### 3.3 Handlers com TODO no Módulo Governance

**Gravidade:** MÉDIA
**Ocorrências:** 8 handlers com marcadores TODO (D-039, D-040, D-047)

Handlers no módulo Governance que têm implementação parcial com TODOs pendentes, afetando governance packs, compliance e reporting.

---

### 3.4 Páginas Frontend com Dados Mock

**Gravidade:** MÉDIA
**Ocorrências:** 6+ páginas de produção e 37+ ficheiros de teste

Páginas frontend que consomem dados do backend simulado e mostram `DemoBanner`:
- `FinOpsPage`
- `DomainFinOpsPage`
- `ServiceFinOpsPage`
- `TeamFinOpsPage`
- `ExecutiveFinOpsPage`
- `BenchmarkingPage`

---

### 3.5 Desvio da Stack Frontend Alvo

**Gravidade:** MÉDIA

| Aspecto | Documentado | Implementado |
|---------|-------------|--------------|
| Router | TanStack Router | React Router v7 (`^7.13.1`) |
| React | React 18 | React 19 (`^19.2.0`) |
| State Management | Zustand | Não utilizado (TanStack Query apenas) |

**Impacto:**
- React Router v7 é funcional e moderno, mas diverge da decisão arquitectural documentada
- React 19 introduz mudanças significativas face ao React 18 documentado
- Ausência de Zustand pode ser intencional se TanStack Query for suficiente, mas deve ser uma decisão explícita

---

### 3.6 Módulos sem Migrações EF

**Gravidade:** ALTA

Três módulos têm `DbContext` definido mas sem migrações criadas, impedindo persistência real:

| Módulo | DbContext | Tabelas Esperadas | Migrações |
|--------|-----------|-------------------|-----------|
| Knowledge | `KnowledgeDbContext` | `knw_*` | ❌ Ausentes |
| Integrations | `IntegrationsDbContext` | `int_*` | ❌ Ausentes |
| ProductAnalytics | `ProductAnalyticsDbContext` | `pan_*` | ❌ Ausentes |

**Nota:** Existem referências a tabelas `int_*` e `pan_*` em entity configurations, mas as migrações EF formais não foram geradas.

---

## 4. Recomendações Prioritárias

### Prioridade Imediata (Sprint 1-2)

1. **Criar migrações EF para Knowledge, Integrations e ProductAnalytics** — Sem migrações, estes módulos não podem persistir dados, o que invalida qualquer funcionalidade que dependa deles.

2. **Eliminar padrão `IsSimulated` nos handlers críticos** — Começar por OperationalIntelligence (13 handlers) e depois Governance FinOps (8+ handlers). Substituir por queries reais à base de dados, mesmo que retornem conjuntos vazios quando não há dados.

3. **Adicionar `CancellationToken` a métodos async** — Identificados ~237 métodos async sem `CancellationToken`, o que é um requisito de qualidade enterprise e está explícito nas regras do produto.

### Prioridade Estrutural (Sprint 2-4)

4. **Implementar handlers ExternalAI** — Os 6 handlers vazios impedem integração real com providers de IA, o que é central para o pilar de AI-Assisted Operations.

5. **Completar handlers de Orchestration** — Os 5 handlers vazios limitam a capacidade de agentes especializados.

6. **Substituir dados mock no frontend** — As 6 páginas com DemoBanner devem consumir dados reais do backend (mesmo que sejam conjuntos vazios com empty states adequados).

### Prioridade Estratégica (Sprint 4-8)

7. **Completar módulo Knowledge** — FTS search cross-module, relações knowledge ↔ serviço/contrato/mudança, UI dedicada.

8. **Completar módulo Integrations** — Adapters para GitLab, Jenkins, GitHub, Azure DevOps.

9. **Ingestão real de telemetria** — Sem dados reais de runtime, os pilares de Change Intelligence e Operational Reliability ficam limitados a demonstrações.

10. **Decisão explícita sobre stack frontend** — Documentar formalmente se o desvio para React 19 + React Router v7 é intencional, ou planear migração para a stack alvo.

### Prioridade de Produto (Sprint 5-10)

11. **Release Calendar UI** — Freeze windows existem mas sem visualização de calendário.
12. **Knowledge Hub frontend** — Páginas dedicadas para gestão de conhecimento operacional.
13. **Licensing & Entitlements** — Módulo não existe e é requisito estratégico para deployment enterprise.

---

## 5. Conclusão

O NexTraceOne tem uma base arquitectural sólida com 12 módulos bem organizados em bounded contexts, seguindo Clean Architecture e DDD. O pilar de **Contract Governance** é o mais maduro e demonstra o potencial da plataforma. No entanto, a presença extensiva de dados simulados, handlers vazios e módulos sem persistência impede que o produto cumpra a sua promessa de **"fonte de verdade"** de forma credível.

A eliminação do padrão `IsSimulated` e a criação de migrações para módulos pendentes são as ações com maior impacto no alinhamento imediato com a visão do produto. Estas correções não exigem redesenho arquitectural — a estrutura correcta já existe, precisa apenas de ser completada com implementação real.

---

*Documento gerado como parte da avaliação de estado do projecto NexTraceOne em 2026-03-27.*

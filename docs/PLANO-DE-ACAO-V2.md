# NexTraceOne — Plano de Ação Consolidado (Pós-v1.0.0)

> **Data:** Abril 2026
> **Estado base:** v1.0.0 — Waves A→BC (155 features) 100% concluídas. Zero gaps abertos.
> **Este documento substitui e consolida:** `NEXT-ACTION-PLAN.md` (mantido para referência histórica), bem como todos os planos de acção sectoriais já removidos.
> **Referências vivas:** [HONEST-GAPS.md](./HONEST-GAPS.md) · [FUTURE-ROADMAP.md](./FUTURE-ROADMAP.md) · [docs/analysis/](./analysis/) · [docs/adr/](./adr/)

---

## Legenda de Prioridade

| Símbolo | Significado |
|---------|-------------|
| 🔴 P1 | Infraestrutura crítica — bloqueia evolução de produto |
| 🟠 P2 | Evolução de produto — alto impacto operacional |
| 🟡 P3 | Evolução de produto — valor estratégico médio |
| 🟢 P4 | Inovação / Futuro — diferenciação competitiva |

---

## Prioridade 1 — Infraestrutura: PostgreSQL Hardening 🔴 P1

**Objectivo:** Escalar o produto para ambientes enterprise de alta carga.

**Documento:** [INFRA-PHASE-1-POSTGRES-HARDENING.md](./analysis/INFRA-PHASE-1-POSTGRES-HARDENING.md)

**Estimativa:** 2–3 semanas

### Items

| # | Item | Detalhes |
|---|------|---------|
| INF-01 | PgBouncer em transaction pooling | Substituir `Maximum Pool Size=20` em todos os `DbContext`; configurar pool separado por module-schema |
| INF-02 | Particionamento temporal | Tabelas `audit_events`, `otel_*`, `ai_usage_entries`, `pan_*` — particionamento por mês (pg_partman) |
| INF-03 | Read Replica | Separar queries pesadas de analytics e relatórios (Governance, ProductAnalytics, AI) para réplica |
| INF-04 | Redis para hot data | Catálogo de serviços, perfis de utilizador, sessões, token quota cache — remover DB round-trips redundantes |

---

## Prioridade 2 — Infraestrutura: ClickHouse como Provider Principal 🔴 P1

**Objectivo:** Substituir Elasticsearch por ClickHouse como provider padrão de observabilidade.

**Documento:** [INFRA-PHASE-2-CLICKHOUSE-MIGRATION.md](./analysis/INFRA-PHASE-2-CLICKHOUSE-MIGRATION.md)

**Estimativa:** 3–4 semanas

### Items

| # | Item | Detalhes |
|---|------|---------|
| INF-05 | Tornar ClickHouse o provider padrão | Inverter default: CH primeiro, ES como provider opcional enterprise |
| INF-06 | Migrar TelemetryStore snapshots | Mover `otel_*` do PostgreSQL Product Store para ClickHouse |
| INF-07 | Migrar ProductAnalytics events | `pan_events` → ClickHouse para performance analítica |
| INF-08 | Mover ES para override | `docker-compose.override.yml` — opcional para quem integra Elastic stack |

---

## Prioridade 3 — Pipeline de Ingestão Avançado 🟠 P2

**Objectivo:** Transformar pipeline de ingestão linear em motor configurável por tenant (equivalente ao Dynatrace OpenPipeline).

**Referências:** [INGESTION-PIPELINE-IMPLEMENTATION.md](./INGESTION-PIPELINE-IMPLEMENTATION.md) · [ADR-010](./adr/010-server-side-ingestion-pipeline.md) · [HONEST-GAPS.md PIP-01..06](./HONEST-GAPS.md)

**Estimativa:** 3–4 semanas

### Items

| ID | Item | Fase | Prioridade |
|----|------|------|-----------|
| PIP-01 | Dead Letter Queue — `ModuleOutboxProcessorJob` descarta silenciosamente após 5 retries | Fase 1 | 🔴 Alta |
| PIP-02 | Ingestion Observability — métricas de throughput/latência/falhas por tenant | Fase 1 | 🔴 Alta |
| PIP-03 | `TenantPipelineRule` — pipeline configurável por tenant (masking, filtering, enrichment) | Fase 2 | 🟠 Média |
| PIP-04 | `StorageBucket` — routing condicional e retenção configurável por tenant | Fase 3 | 🟠 Média |
| PIP-05 | `CatalogEnrichmentProcessor` — enriquecimento de spans/logs com contexto do Service Catalog | Fase 4 | 🟡 Baixa |
| PIP-06 | `LogToMetricProcessor` — transformação log → metric server-side | Fase 5 | 🟡 Baixa |

---

## Prioridade 4 — Host Infrastructure Module 🟠 P2

**Objectivo:** Adicionar visibilidade de hosts ao modelo de dados e ao grafo de topologia.

**Documento:** [INFRA-PHASE-3-HOST-INFRASTRUCTURE.md](./analysis/INFRA-PHASE-3-HOST-INFRASTRUCTURE.md)

**Estimativa:** 2–3 semanas

### Items

| # | Item |
|---|------|
| HI-01 | Novo bounded context `HostInfrastructure` (`src/modules/hostinfrastructure/`) |
| HI-02 | Entidades: `HostAsset`, `ServiceDeployment`, `HostHealthRecord` |
| HI-03 | Features: `RegisterHost`, `UpdateHostMetrics`, `GetHostDashboard`, `CorrelateServiceWithHost` |
| HI-04 | OTEL hostmetrics pipeline (extensão do Ingestion API) |
| HI-05 | Frontend: `HostDashboardPage` + overlay de hosts no grafo de topologia |

---

## Prioridade 5 — Topology UI Time-Travel e Discovery 🟠 P2

**Objectivo:** Completar a UI de topologia com time-travel, alertas real-time e discovery contínuo.

**Documento:** [INFRA-PHASE-4-TOPOLOGY-COMPLETIONS.md](./analysis/INFRA-PHASE-4-TOPOLOGY-COMPLETIONS.md)

**Estimativa:** 2–3 semanas

### Items

| # | Item |
|---|------|
| TOP-01 | `TopologyTimeTravel.tsx` — slider temporal sobre `GraphSnapshot` existente |
| TOP-02 | `PropagationRisk` via SignalR/WebSocket push (actualmente só on-demand) |
| TOP-03 | Quartz job de discovery contínuo (5 min) em background |
| TOP-04 | Pipeline de actualização automático do `NodeHealthRecord` |

---

## Prioridade 6 — SAML Level A — Quick Win 🟠 P2

**Objectivo:** Promover DEG-11 (SAML) de Nível A′ para Nível A completo — expor no `/admin/system-health`.

**Referência:** [HONEST-GAPS.md DEG-11](./HONEST-GAPS.md) — próximo passo concreto identificado na auditoria CFG-02.

**Estimativa:** 1–2 horas

### Items

| # | Item |
|---|------|
| SAM-01 | Adicionar `bool IsConfigured { get; }` em `ISamlConfigProvider` |
| SAM-02 | Expor como quinto provider em `OptionalProviderNames.Saml` |
| SAM-03 | Incluir em `GetOptionalProviders` para aparecer em `/admin/system-health` |

---

## Prioridade 7 — V3 Frontend: Operational Intelligence Surface 🟡 P3

**Objectivo:** Elevar o frontend de "app de módulos" para superfície de decisão operacional persona-aware com dashboards inteligentes, real-time e colaboração.

**Documento:** [V3-EVOLUTION-FRONTEND-DASHBOARDS.md](./V3-EVOLUTION-FRONTEND-DASHBOARDS.md)

**Estimativa:** 12 waves (total ~6 meses em paralelo com outras iniciativas)

### Waves V3

| Wave | Foco | Estimativa |
|------|------|-----------|
| V3.1 | Dashboard Intelligence — Governed Boards (query-driven, tokens, versioning) | 3–4 semanas |
| V3.2 | Live Streaming & Real-time Annotations (WebSocket/SSE) | 2–3 semanas |
| V3.3 | Template Marketplace & Parametrization | 2–3 semanas |
| V3.4 | Notebook Mode — Collaborative Analysis | 3–4 semanas |
| V3.5 | Deep-link & Context Preservation | 1–2 semanas |
| V3.6 | AI-assisted Dashboard Creation | 2–3 semanas |
| V3.7 | Collaborative Editing (CRDT/Yjs) | 3–4 semanas |
| V3.8 | Plugin/Extension System | 4–5 semanas |
| V3.9 | Mobile On-call UX | 2–3 semanas |
| V3.10 | Persona Intelligence Suites | 3–4 semanas |
| V3.11 | Source-of-Truth Centers | 2–3 semanas |
| V3.12 | Contract Studio V3 + AI Agents Console + IDE Extension Console | 3–4 semanas |

---

## Prioridade 8 — IDE Extensions 🟡 P3

**Objectivo:** Experiência estilo copiloto directamente no IDE com contexto governado do NexTraceOne.

**Dependências:** API pública estável + autenticação via `PlatformApiToken` (já implementado em Wave B.2).

**Estimativa:** 6–8 semanas

### Items

| Extension | Estado actual | Próximo passo |
|-----------|--------------|--------------|
| VS Code | Extensão básica implementada (`tools/ide-extensions/vscode/`) — Ask AI + Configure | Publicar no VS Code Marketplace; adicionar service lookup, contract preview, change risk widget |
| Visual Studio | Extensão básica implementada (`tools/ide-extensions/visualstudio/`) — AI Chat + Ask about selection | Publicar no Visual Studio Marketplace; maturar UI com contexto de solução |
| JetBrains (Rider/IntelliJ) | Planeada | Início dependente de feedback de adopção das primeiras duas |

---

## Prioridade 9 — AI Evaluation Harness (ADR-009) 🟡 P3

**Objectivo:** Sistema interno de avaliação de qualidade das respostas dos agentes AI, sem dependência de LLM externo para scoring.

**Referência:** [ADR-009](./adr/009-ai-evaluation-harness.md)

**Estimativa:** 3–4 semanas

### Items

| # | Item |
|---|------|
| AEH-01 | `AiEvaluationCase` + `AiEvaluationResult` — entidades de avaliação |
| AEH-02 | `EvaluationHarnessJob` (Quartz) — execução periódica de casos contra agentes |
| AEH-03 | Metrics: BLEU/ROUGE para tasks estruturadas; human-preference score para tasks abertas |
| AEH-04 | Dashboard de qualidade por agente no `/admin/ai/evaluation` |
| AEH-05 | Integração com `AiAgentPerformanceMetric` existente |

---

## Prioridade 10 — Change Confidence Score 2.0 (ADR-008) 🟡 P3

**Objectivo:** Evoluir o score de confiança de caixa-preta para sistema explicável com sub-scores citáveis.

**Referência:** [ADR-008](./adr/008-change-confidence-score-v2.md)

**Estimativa:** 3–4 semanas

### Sub-scores a implementar

| Sub-score | Fonte primária |
|-----------|---------------|
| `TestCoverage` | CI metadata via `Integrations` / `IngestionExecutions` |
| `ContractStability` | `Catalog.Contracts` (ComputeSemanticDiff, GetContractHealthTimeline) |
| `HistoricalRegression` | `OperationalIntelligence.Incidents` correlacionados |
| `BlastSurface` | `ChangeGovernance.BlastRadius` + `ContractConsumerInventory` |
| `DependencyHealth` | `OperationalIntelligence.Reliability` + `RuntimeIntelligence.DriftFinding` |
| `CanarySignal` | `ICanaryProvider` (opcional, degrada graciosamente) |
| `PreProdDelta` | `RuntimeIntelligence.RuntimeBaseline` |

### Items de implementação

| # | Item |
|---|------|
| CC2-01 | Novas entidades: `ChangeConfidenceSubScore`, `ChangeConfidenceCitation` |
| CC2-02 | Nova feature: `ComputeChangeConfidenceV2` (mantém compatibilidade com consumidores do V1) |
| CC2-03 | `ChangeConfidenceWeightConfig` no `ConfigurationDefinitionSeeder` (pesos por tenant) |
| CC2-04 | API: `GET /changegovernance/releases/{id}/confidence/v2` com breakdown por sub-score |
| CC2-05 | Frontend: `ChangeConfidenceBreakdownPanel.tsx` com citations e persona-aware rendering |

---

## Prioridade 11 — AI Evolution: Fases 2–4 🟡 P3

**Objectivo:** Completar a evolução do módulo AI conforme [AI-EVOLUTION-ROADMAP.md](./AI-EVOLUTION-ROADMAP.md).

### Fase 2 — Agent Lightning (RL Loop)

**Estimativa:** 10–20 semanas

| # | Item |
|---|------|
| AL-01 | Integração com Agent Lightning (Microsoft Research) para RL training |
| AL-02 | Feedback loop: `AiAgentTrajectoryFeedback` → RL Trainer → modelo melhorado |
| AL-03 | Algoritmos: GRPO (raciocínio), PPO (tool use), APO (modelos externos) |
| AL-04 | Métricas de melhoria por agente ao longo do tempo |

### Fase 3 — Capacidades Enterprise

**Estimativa:** 20–36 semanas

| # | Item |
|---|------|
| AIE-01 | `AiWorkflowBuilder` — workflows multi-agente via UI |
| AIE-02 | `AiTeamAgent` — agentes especializados por equipa com permissões segregadas |
| AIE-03 | `IntelligentOnboardingAgent` — onboarding de novo serviço assistido por IA |
| AIE-04 | `SecurityReviewAgent` completo com análise de código e contratos |

### Fase 4 — Inovações Sem Concorrência (AI-INNOVATION-BLUEPRINT.md)

**Estimativa:** 36–52 semanas

| # | Item |
|---|------|
| OME-01 | Organizational Memory Engine completo — grafo temporal vivo multi-fonte |
| ACC-01 | Adaptive Contract Contracts — contratos que evoluem baseados em usage real |
| PDP-01 | Predictive Deployment Planner — scheduling inteligente de deploys |
| IFF-01 | Intelligent Feature Flags — flags que mudam com base em comportamento observado |

---

## Prioridade 12 — Inovação: Features de Alto Valor 🟢 P4

**Referência:** [INOVACAO-ROADMAP.md](./analysis/INOVACAO-ROADMAP.md)

### Tier 1 — Alto Valor, Base Existente Aproveitável

| Feature | Dor que resolve | Estimativa |
|---------|----------------|-----------|
| Contract Drift Detection entre Ambientes | Promoção sem perceber divergência de contrato entre staging e prod | 2–3 semanas |
| Change-to-Contract Impact (Automático) | Consumidores descobrem breaking changes ao falhar | 3–4 semanas |
| Release Confidence Scorecard na UI | Score composto visível em Promotion UI (backend BC.3 já existe) | 1–2 semanas |
| Service Health Digest por Email | Resumo semanal por equipa sem acesso ao portal | 2–3 semanas |

---

## Prioridade 13 — Kubernetes Deployment 🟢 P4

**Referência:** [FUTURE-ROADMAP.md §11.1](./FUTURE-ROADMAP.md)

**Estimativa:** 3–4 semanas

### Items

| # | Item |
|---|------|
| K8S-01 | Helm charts para todos os serviços (apihost, workers, ingestion, frontend) |
| K8S-02 | Horizontal Pod Autoscaler configs |
| K8S-03 | Service mesh integration (Istio/Linkerd) para mTLS e traffic management |
| K8S-04 | Air-gapped deployment support |

---

## Providers Nível B → A (Dívida Técnica de Provider Pattern)

Baseado na auditoria CFG-02 em [HONEST-GAPS.md](./HONEST-GAPS.md). Estes providers funcionam em modo de degradação graciosa (Nível B — simulated in handler). Promover para Nível A apenas quando o cliente externo real for implementado.

| ID | Provider | Próximo passo para Nível A |
|----|----------|---------------------------|
| DEG-03 | Runtime Intelligence | `IRuntimeProvider` + agente CLR real |
| DEG-04 | Chaos Experiments | `IChaosProvider` ligado a Litmus/Chaos Mesh |
| DEG-05 | mTLS Certificate Manager | `ICertificateProvider` ligado a cert-manager/Vault PKI |
| DEG-06 | Multi-tenant Schema Planner | Executor IaC (Terraform/Pulumi) real |
| DEG-07 | Capacity Forecast | Pipeline de snapshots de runtime em `aik_*` |
| DEG-12 | External AI Models | Driver por vendor com governance (já partial via `ModelRegistry`) |
| DEG-13 | Elasticsearch queries | `IElasticQueryClient` com fallback gracioso completo |
| DEG-14 | ClickHouse analytics | Análogo a DEG-13 |

---

## Bugs e Gaps Conhecidos

| ID | Gap | Módulo | Severidade |
|----|-----|--------|-----------|
| GAP-01 | DEG-11 SAML: não aparece em `/admin/system-health` (é Nível A′ em vez de A) | IdentityAccess/Integrations | 🟠 Médio — Quick Win |
| GAP-02 | PIP-01 Dead Letter Queue: outbox descarta silenciosamente após 5 retries | BuildingBlocks.Infrastructure | 🔴 Alto — PIP Fase 1 |
| GAP-03 | PIP-02 Sem métricas de ingestão por tenant | IngestionApi | 🔴 Alto — PIP Fase 1 |
| GAP-04 | `ChangeConfidenceScore` é caixa-preta sem sub-scores explicáveis | ChangeGovernance | 🟠 Médio — ADR-008 |
| GAP-05 | `TopologyTimeTravel.tsx` não existe — grafo é estático sem slider temporal | Frontend/Catalog | 🟡 Baixo — TOP-01 |
| GAP-06 | Topology discovery é on-demand, sem job contínuo | Catalog/ChangeGovernance | 🟡 Baixo — TOP-03 |

---

## Dívida Técnica

| ID | Item | Módulo | Prioridade |
|----|------|--------|-----------|
| TEC-01 | Cache de token quota (Redis) — actualmente N checks/request em BD | AIKnowledge | 🟠 P2 (após Redis INF-04) |
| TEC-02 | `Maximum Pool Size=20` em todos os DbContexts — inadequado para prod | Infrastructure | 🔴 P1 — INF-01 |
| TEC-03 | `AiExternalInferenceRecord` — external inference audit sem UI de consulta | AIKnowledge | 🟡 P3 |
| TEC-04 | `WarRoomSession` — entity exists, sem workflow de UI completo | AIKnowledge | 🟡 P3 |
| TEC-05 | `SelfHealingAction` — entity exists, pipeline de execução real pending | AIKnowledge | 🟡 P3 |
| TEC-06 | `GuardianAlert` — entity exists, `ProactiveArchitectureGuardianJob` executa mas sem canal de notificação real | AIKnowledge | 🟡 P3 |
| TEC-07 | `JourneyDefinition` — configurável mas UI de análise de funil ainda limitada | ProductAnalytics | 🟡 P3 |
| TEC-08 | OpenAPI artefacto de build existe (`swagger.json`) mas sem portal de documentação público | ApiHost | 🟢 P4 |

---

## Mapa de Dependências

```
INF-01 (PgBouncer) + INF-04 (Redis)
  └── desbloqueiam: TEC-01 (token quota cache)

INF-05..08 (ClickHouse como default)
  └── desbloqueiam: PIP-04 (StorageBucket routing)

HI-01..05 (Host Infrastructure)
  └── dependem de: INF-01 (PgBouncer para novo DbContext)
  └── desbloqueiam: TOP-01..04 (Topology com overlay de hosts)

SAM-01..03 (SAML Level A) — sem dependências, começar imediatamente

V3 Frontend — independente de infra, pode começar em paralelo

IDE Extensions — requerem API estável (já existe)

CC2-01..05 (Change Confidence 2.0)
  └── dependem de: HI backend (para HostHealth no BlastSurface)

ADR-009 (Evaluation Harness)
  └── dependem de: AiSkill/AiAgentExecution já existentes ✅

Fase 2 AI (Agent Lightning RL)
  └── dependem de: AiAgentTrajectoryFeedback já existente ✅
```

---

## Ordem Recomendada de Execução

```
Semana 1–2:   SAM-01..03 (Quick Win) + PIP-01..02 (Dead Letter + Observability)
Semana 1–5:   INF-01..04 (PostgreSQL Hardening) em paralelo
Semana 3–7:   INF-05..08 (ClickHouse as default)
Semana 6–10:  PIP-03..06 (Pipeline completo)
Semana 8–12:  HI-01..05 (Host Infrastructure)
Semana 10–14: TOP-01..04 (Topology completions)
Semana 12–16: CC2-01..05 (Change Confidence 2.0) + AEH-01..05 (Evaluation Harness)
Semana 14–22: V3 Frontend waves (em paralelo com backend)
Semana 16–24: IDE Extensions publicação + maturação
Semana 20+:   AI Fase 2..4 (Agent Lightning, Enterprise, Inovação)
Semana 24+:   Kubernetes Helm charts
```

---

> **Nota de governança deste documento:** Qualquer item concluído deve ser assinalado com ✅ e a data de conclusão. Quando todos os items de uma Prioridade estiverem concluídos, actualizar o CHANGELOG.md e o HONEST-GAPS.md.

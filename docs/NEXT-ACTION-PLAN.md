# NexTraceOne — Próximas Evoluções (Pós-v1.0.0)

> **Data:** Abril 2026  
> **Contexto:** Waves A → BC (155 features) concluídas. Produto em v1.0.0. Este documento consolida as evoluções prioritárias não incluídas no roadmap A→BC.  
> **Referências:** [FUTURE-ROADMAP.md](./FUTURE-ROADMAP.md) · [HONEST-GAPS.md](./HONEST-GAPS.md) · [docs/analysis/](./analysis/) · [docs/V3-EVOLUTION-FRONTEND-DASHBOARDS.md](./V3-EVOLUTION-FRONTEND-DASHBOARDS.md)

---

## Prioridades por Impacto

| Prioridade | Área | Documento de referência | Estimativa |
|---|---|---|---|
| 🔴 P1 | Infraestrutura — PostgreSQL Hardening | [INFRA-PHASE-1-POSTGRES-HARDENING.md](./analysis/INFRA-PHASE-1-POSTGRES-HARDENING.md) | 2–3 semanas |
| 🔴 P1 | Infraestrutura — ClickHouse como provider padrão | [INFRA-PHASE-2-CLICKHOUSE-MIGRATION.md](./analysis/INFRA-PHASE-2-CLICKHOUSE-MIGRATION.md) | 3–4 semanas |
| 🟠 P2 | Ingestion Pipeline — Dead Letter Queue + Observability | [INGESTION-PIPELINE-IMPLEMENTATION.md](./INGESTION-PIPELINE-IMPLEMENTATION.md) | 3–4 semanas |
| 🟠 P2 | Infraestrutura — Host Infrastructure Module | [INFRA-PHASE-3-HOST-INFRASTRUCTURE.md](./analysis/INFRA-PHASE-3-HOST-INFRASTRUCTURE.md) | 2–3 semanas |
| 🟠 P2 | Infraestrutura — Topology UI Time-Travel | [INFRA-PHASE-4-TOPOLOGY-COMPLETIONS.md](./analysis/INFRA-PHASE-4-TOPOLOGY-COMPLETIONS.md) | 2–3 semanas |
| 🟡 P3 | V3 Frontend — Operational Intelligence Surface | [V3-EVOLUTION-FRONTEND-DASHBOARDS.md](./V3-EVOLUTION-FRONTEND-DASHBOARDS.md) | 12 waves |
| 🟡 P3 | IDE Extensions — VS Code + Visual Studio | [FUTURE-ROADMAP.md §2](./FUTURE-ROADMAP.md) | 6–8 semanas |
| 🟡 P3 | SAML Level A — Promoção ao admin dashboard | [HONEST-GAPS.md DEG-11](./HONEST-GAPS.md) | 1–2 dias |
| 🟢 P4 | Inovação — Contract Drift Detection + Change-to-Contract | [INOVACAO-ROADMAP.md](./analysis/INOVACAO-ROADMAP.md) | por feature |
| 🟢 P4 | Kubernetes Deployment — Helm charts | [FUTURE-ROADMAP.md §11.1](./FUTURE-ROADMAP.md) | 3–4 semanas |

---

## 1. Infraestrutura — P1

### 1.1 PostgreSQL Hardening

**Objetivo:** Escalar o produto para ambientes de alta carga sem degradação de performance.

**Items:**
- [ ] PgBouncer em transaction pooling (substituir `Maximum Pool Size=20`)
- [ ] Particionamento temporal em tabelas de alto volume (`audit_events`, `otel_*`)
- [ ] Read Replica para queries pesadas de analytics
- [ ] Redis para hot data (catálogo, perfis de serviço, sessões)

**Documento:** [INFRA-PHASE-1-POSTGRES-HARDENING.md](./analysis/INFRA-PHASE-1-POSTGRES-HARDENING.md)

---

### 1.2 ClickHouse como Provider Principal de Observabilidade

**Objetivo:** Substituir Elasticsearch por ClickHouse no docker-compose padrão para melhor performance analítica e custo operacional.

**Items:**
- [ ] Tornar ClickHouse o provider padrão (inverter default ES → CH)
- [ ] Migrar TelemetryStore snapshots do PostgreSQL para ClickHouse
- [ ] Migrar ProductAnalytics events para ClickHouse
- [ ] Mover Elasticsearch para `docker-compose.override.yml` (opcional para quem precisa)

**Documento:** [INFRA-PHASE-2-CLICKHOUSE-MIGRATION.md](./analysis/INFRA-PHASE-2-CLICKHOUSE-MIGRATION.md)

---

## 2. Ingestion Pipeline — P2

**Baseado em:** [HONEST-GAPS.md PIP-01..06](./HONEST-GAPS.md) e [INGESTION-PIPELINE-IMPLEMENTATION.md](./INGESTION-PIPELINE-IMPLEMENTATION.md)

| ID | Feature | Fase | Prioridade |
|---|---|---|---|
| PIP-01 | Dead Letter Queue — `ModuleOutboxProcessorJob` descarta silenciosamente após 5 retries | Fase 1 | Alta |
| PIP-02 | Ingestion Observability — métricas de throughput/latência/falhas por tenant | Fase 1 | Alta |
| PIP-03 | `TenantPipelineRule` — pipeline configurável por tenant (masking, filtering, enrichment) | Fase 2 | Média |
| PIP-04 | `StorageBucket` — routing condicional e retenção configurável por tenant | Fase 3 | Média |
| PIP-05 | `CatalogEnrichmentProcessor` — enriquecimento de spans/logs com contexto do Service Catalog | Fase 4 | Baixa |
| PIP-06 | `LogToMetricProcessor` — transformação log → metric server-side | Fase 5 | Baixa |

---

## 3. Host Infrastructure Module — P2

**Objetivo:** Adicionar visibilidade de hosts ao modelo de dados e ao grafo de topologia.

**Items:**
- [ ] Novo bounded context `HostInfrastructure` (src/modules/hostinfrastructure/)
- [ ] Entidades: `HostAsset`, `ServiceDeployment`, `HostHealthRecord`
- [ ] Features: `RegisterHost`, `UpdateHostMetrics`, `GetHostDashboard`, `CorrelateServiceWithHost`
- [ ] OTEL hostmetrics pipeline
- [ ] Frontend: `HostDashboardPage` + overlay de hosts no grafo de topologia

**Documento:** [INFRA-PHASE-3-HOST-INFRASTRUCTURE.md](./analysis/INFRA-PHASE-3-HOST-INFRASTRUCTURE.md)

---

## 4. Topology UI Completions — P2

**Objetivo:** Completar a UI de topologia com time-travel, alertas real-time e discovery contínuo.

**Items:**
- [ ] `TopologyTimeTravel.tsx` — slider temporal sobre `GraphSnapshot` existente
- [ ] `PropagationRisk` via SignalR/WebSocket push (actualmente só on-demand)
- [ ] Quartz job de discovery contínuo (5 min) em background
- [ ] Pipeline de actualização automático do `NodeHealthRecord`

**Documento:** [INFRA-PHASE-4-TOPOLOGY-COMPLETIONS.md](./analysis/INFRA-PHASE-4-TOPOLOGY-COMPLETIONS.md)

---

## 5. SAML Level A Promotion — Quick Win

**Objetivo:** Promover DEG-11 (SAML) de Nível A′ para Nível A completo.

**Items:**
- [ ] Adicionar `bool IsConfigured { get; }` em `ISamlConfigProvider`
- [ ] Expor como quinto provider em `OptionalProviderNames.Saml`
- [ ] Incluir em `GetOptionalProviders` para aparecer em `/admin/system-health`

**Referência:** [HONEST-GAPS.md DEG-11](./HONEST-GAPS.md) — próximo passo concreto identificado na auditoria CFG-02.

**Esforço estimado:** 1–2 horas.

---

## 6. V3 Frontend — Operational Intelligence Surface — P3

**Objetivo:** Elevar o frontend de "app de módulos" para superfície de decisão operacional persona-aware.

**Waves V3.1–V3.12 planeadas (ver documento completo):**

| Wave | Foco |
|---|---|
| V3.1 | Dashboard Intelligence — Governed Boards (query-driven, tokens, versioning) |
| V3.2 | Live Streaming & Real-time Annotations |
| V3.3 | Template Marketplace & Parametrization |
| V3.4 | Notebook Mode — Collaborative Analysis |
| V3.5 | Deep-link & Context Preservation |
| V3.6 | AI-assisted Dashboard Creation |
| V3.7 | Collaborative Editing (CRDT/Yjs) |
| V3.8 | Plugin/Extension System |
| V3.9 | Mobile On-call UX |
| V3.10 | Persona Intelligence Suites |
| V3.11 | Source-of-Truth Centers |
| V3.12 | Contract Studio V3 + AI Agents Console + IDE Extension Console |

**Documento:** [V3-EVOLUTION-FRONTEND-DASHBOARDS.md](./V3-EVOLUTION-FRONTEND-DASHBOARDS.md)

---

## 7. IDE Extensions — P3

**Objetivo:** Experiência estilo copiloto directamente no IDE com contexto governado do NexTraceOne.

| Extension | Estado | Capacidades alvo |
|---|---|---|
| VS Code | 📋 Planeada | Service lookup, contract preview, change risk, AI assistant |
| Visual Studio | 📋 Planeada | Mesmas capacidades para ecossistema .NET |
| JetBrains (Rider/IntelliJ) | 📋 Futura | Mesmas capacidades para Java/Kotlin/.NET |

**Dependências:** API pública estável + autenticação via `PlatformApiToken` (já implementado em Wave B.2).

---

## 8. Inovação — Features de Alto Valor — P4

Baseado em [INOVACAO-ROADMAP.md](./analysis/INOVACAO-ROADMAP.md):

### Tier 1 — Alto Valor, Base Existente Aproveitável

| Feature | Dor que resolve | Estimativa |
|---|---|---|
| Contract Drift Detection entre Ambientes | Promoção sem perceber divergência de contrato entre staging e prod | 2–3 semanas |
| Change-to-Contract Impact (Automático) | Consumidores descobrem breaking changes ao falhar | 3–4 semanas |
| Release Confidence Scorecard na UI | Score composto visível em Promotion UI (já existe backend BC.3) | 1–2 semanas |
| Service Health Digest por Email | Resumo semanal por equipa sem acesso ao portal | 2–3 semanas |

---

## 9. Kubernetes Deployment — P4

**Items:**
- [ ] Helm charts para todos os serviços (apihost, workers, ingestion, frontend)
- [ ] Horizontal Pod Autoscaler configs
- [ ] Service mesh integration (Istio/Linkerd) para mTLS e traffic management
- [ ] Air-gapped deployment support (Wave C.4 do roadmap)

---

## 10. Outros Providers Nível B → A

Baseado na auditoria CFG-02 em [HONEST-GAPS.md](./HONEST-GAPS.md):

| ID | Provider | Próximo passo para promover a Nível A |
|---|---|---|
| DEG-03 | Runtime Intelligence | `IRuntimeProvider` + agente CLR real |
| DEG-04 | Chaos Experiments | `IChaosProvider` ligado a Litmus/Chaos Mesh |
| DEG-05 | mTLS Certificate Manager | `ICertificateProvider` ligado a cert-manager/Vault PKI |
| DEG-06 | Multi-tenant Schema Planner | Executor IaC (Terraform/Pulumi) real |
| DEG-07 | Capacity Forecast | Pipeline de snapshots de runtime em `aik_*` |
| DEG-12 | External AI Models | Driver por vendor (OpenAI/Anthropic) com governance |
| DEG-13 | Elasticsearch queries | `IElasticQueryClient` completo com fallback gracioso |
| DEG-14 | ClickHouse analytics | Análogo a DEG-13 |

---

## Dependências Entre Evoluções

```
Infraestrutura P1 (PostgreSQL + ClickHouse)
  └── bloqueia: Host Infrastructure (Fase 3 requer PgBouncer)

Host Infrastructure (Fase 3)
  └── bloqueia: Topology UI Time-Travel (Fase 4 — overlay de hosts)

SAML Level A (Quick Win)
  └── sem dependências — pode começar imediatamente

V3 Frontend
  └── independente de infra — pode começar em paralelo

IDE Extensions
  └── requer: API pública estável (já existe)
```

---

> **Nota:** Este documento substitui o `docs/ACTION-PLAN.md` (removido após todas as ACTs serem concluídas). O `docs/FUTURE-ROADMAP.md` é o registo histórico completo das waves A→BC implementadas.

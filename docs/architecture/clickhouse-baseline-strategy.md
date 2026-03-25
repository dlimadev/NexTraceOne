# Estratégia Final do ClickHouse

> **Status:** DRAFT
> **Data:** 2026-03-25
> **Fase:** N15 — Estratégia de Transição de Persistência

---

## Objetivo

Fechar como o ClickHouse entra de forma complementar, segura e sem misturar responsabilidades com o PostgreSQL.

---

## Por Que o ClickHouse Existe no Produto

O NexTraceOne é uma plataforma de observabilidade, governança e inteligência operacional. Naturalmente gera grandes volumes de:

- **Eventos de uso** (product analytics, UI tracking)
- **Métricas de runtime** (latência, throughput, erro rate)
- **Dados de custo** (FinOps, cost per service)
- **Logs de integração** (execuções, health checks)
- **Telemetria de IA** (token usage, model latency)

Estes dados são **append-only, time-series, de alto volume e analíticos por natureza**. PostgreSQL pode armazená-los mas não é eficiente para:
- Queries analíticas sobre milhões/bilhões de linhas
- Agregações temporais (per-hour, per-day)
- Retenção de longo prazo com compressão
- Columnar scan de métricas

**ClickHouse é a escolha natural** como banco analítico complementar.

---

## Princípios Fundamentais

| Princípio | Detalhe |
|-----------|---------|
| PostgreSQL é o banco de domínio | Todas as entidades transacionais, CRUD, workflows, configurações |
| ClickHouse é o banco analítico | Apenas eventos, métricas, logs de alto volume e aggregações |
| Sem duplicação de responsabilidade | Um dado vive em PostgreSQL OU ClickHouse, não em ambos (excepto buffer temporário) |
| Correlação via IDs | ClickHouse armazena IDs que referenciam entidades do PostgreSQL |
| Sem FK no ClickHouse | ClickHouse não suporta constraints — integridade é responsabilidade da aplicação |
| Escrita append-only | ClickHouse é optimizado para inserts; updates são caros e devem ser evitados |

---

## Quais Módulos Usam ClickHouse

| Módulo | Nível ClickHouse | Tipo de Dados | Fase |
|--------|-----------------|--------------|------|
| Product Analytics | **REQUIRED** | Eventos de uso, adoption, funnels | Fase 1 |
| Operational Intelligence | **RECOMMENDED** | Runtime metrics, cost analytics | Fase 1/2 |
| Integrations | **RECOMMENDED** | Execution logs, health history | Fase 2 |
| Governance | **RECOMMENDED** | Compliance analytics, FinOps | Fase 2 |
| AI & Knowledge | OPTIONAL_LATER | Token usage, model performance | Fase 3 |
| Service Catalog | OPTIONAL_LATER | Health trend analytics | Fase 3+ |
| Change Governance | OPTIONAL_LATER | Change frequency analytics | Fase 3+ |
| Audit & Compliance | OPTIONAL_LATER | Long-term audit analytics | Fase 3+ |
| Identity & Access | NONE | — | — |
| Environment Management | NONE | — | — |
| Contracts | NONE | — | — |
| Configuration | NONE | — | — |
| Notifications | NONE | — | — |

---

## Que Dados Entram no ClickHouse

### Obrigatórios (Fase 1)

| Módulo | Tabela ClickHouse | Engine | Dados |
|--------|-----------------|----|-------|
| Product Analytics | `pan_events` | MergeTree | Eventos de uso (page views, clicks, actions) |
| Product Analytics | `pan_daily_module_stats` | SummingMergeTree | Adoption por módulo (agregação diária) |
| Product Analytics | `pan_daily_persona_stats` | SummingMergeTree | Uso por persona (agregação diária) |
| Product Analytics | `pan_daily_friction_stats` | SummingMergeTree | Indicadores de fricção (agregação diária) |
| Product Analytics | `pan_session_summaries` | AggregatingMergeTree | Resumos de sessão para funnels |

### Recomendados (Fase 2)

| Módulo | Tabela ClickHouse | Engine | Dados |
|--------|-----------------|----|-------|
| Operational Intelligence | `ops_runtime_metrics` | MergeTree | Latência, throughput, error rate por serviço |
| Operational Intelligence | `ops_cost_entries` | MergeTree | Cost per service/team/operation |
| Operational Intelligence | `ops_incident_trends` | SummingMergeTree | Trends de incidentes (agregação) |
| Integrations | `int_execution_logs` | MergeTree | Logs de execução de connectors |
| Integrations | `int_health_history` | MergeTree | Health status history por connector |
| Governance | `gov_compliance_trends` | SummingMergeTree | Compliance score trends |
| Governance | `gov_finops_aggregates` | SummingMergeTree | FinOps cost aggregations |

### Opcionais (Fase 3+)

| Módulo | Tabela ClickHouse | Dados |
|--------|-----------------|----|
| AI & Knowledge | `aik_token_usage_ledger` | Token consumption per model/user/agent |
| AI & Knowledge | `aik_model_performance` | Model latency, success rate |
| Catalog | `cat_health_trends` | Service health over time |
| Change Governance | `chg_change_frequency` | Change frequency analytics |
| Audit & Compliance | `aud_long_term_events` | Archived audit events (>1 year) |

---

## Que Dados NÃO Entram no ClickHouse

| Tipo de Dado | Banco | Justificação |
|-------------|-------|-------------|
| Entidades de domínio (users, tenants, roles, services) | PostgreSQL | CRUD transacional, ACID, FK constraints |
| Configurações | PostgreSQL | Low volume, needs consistency |
| Workflows (incidents, changes, promotions) | PostgreSQL | State machines, needs ACID |
| Contratos (APIs, events, schemas) | PostgreSQL | Versionados, needs referential integrity |
| Notificações (active) | PostgreSQL | State management, delivery tracking |
| Sessions (active) | PostgreSQL | Short-lived, needs consistency |
| Seeds e referência | PostgreSQL | Low volume, bootstrap data |

---

## Correlação PostgreSQL ↔ ClickHouse

**Padrão de correlação:**

```
ClickHouse event record:
  - event_id: UUID (unique)
  - tenant_id: UUID (matches iam_tenants.id)
  - user_id: UUID (matches iam_users.id)
  - service_id: UUID (matches cat_services.id)
  - environment_id: UUID (matches env_environments.id)
  - timestamp: DateTime64
  - ... metrics/payload
```

- IDs no ClickHouse referenciam logicamente entidades no PostgreSQL
- A aplicação faz joins via Application Layer (não via DB query)
- Dashboards podem fazer enrichment queries (ClickHouse → dados + PostgreSQL → names)

---

## Fase 1 — O Que é Obrigatório

| Item | Detalhe |
|------|---------|
| Schema ClickHouse para Product Analytics | 5 tabelas definidas em `clickhouse-data-placement-review.md` |
| Pipeline de ingestão | Application → ClickHouse writer service → ClickHouse |
| Retention policy | 90 dias raw events, 365 dias agregados |
| Backup strategy | ClickHouse backup schedule definido |
| Monitoring | ClickHouse health + query performance |

---

## O Que Pode Entrar Depois

| Item | Fase | Justificação |
|------|------|-------------|
| OpIntel runtime metrics | 2 | Depende de telemetry pipeline |
| Integration execution logs | 2 | Depende de volume real |
| Governance FinOps aggregates | 2 | Depende de cost pipeline |
| AI token usage | 3 | Depende de IA estar funcional |
| Catalog health trends | 3+ | Depende de monitoring pipeline |
| Audit long-term archive | 3+ | Depende de volume de audit events |

---

## Arquitectura de Ingestão

```
Application (backend)
    │
    ├─ Domain events → Outbox (PostgreSQL)
    │                    │
    │                    └─ Outbox processor → Event bus
    │                                            │
    │                                            └─ ClickHouse writer subscriber
    │                                                 │
    │                                                 └─ ClickHouse INSERT
    │
    └─ Direct metrics → ClickHouse writer service
                              │
                              └─ Batched ClickHouse INSERT
```

**Padrões de ingestão:**
1. **Via Outbox** — domain events que alimentam analytics (recomendado para consistency)
2. **Via Direct Write** — métricas de runtime/telemetria que não passam pelo domínio
3. **Via OpenTelemetry Collector** — métricas de infraestrutura (já configurado)

---

## Riscos ClickHouse

| Risco | Mitigação |
|-------|-----------|
| Over-engineering antes de volume real | Começar só com Product Analytics (REQUIRED), expandir com dados reais |
| Misturar dados transacionais | Regra: se o dado precisa de update/delete → PostgreSQL |
| Correlação complexa PG↔CH | Manter enrichment simples, evitar joins complexos |
| Schema drift | Versionar schema ClickHouse como código (migrations ou DDL scripts) |
| ClickHouse downtime | Analytics degrada gracefully; domínio transacional não é afectado |

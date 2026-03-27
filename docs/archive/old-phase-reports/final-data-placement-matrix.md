# Matriz Final PostgreSQL vs ClickHouse por Módulo

> **Status:** DRAFT
> **Data:** 2026-03-25
> **Fase:** N15 — Estratégia de Transição de Persistência

---

## Objetivo

Transformar a decisão de data placement numa referência operacional por módulo.

---

## Legenda

| Nível ClickHouse | Significado |
|-----------------|------------|
| **NONE** | Sem dados analíticos — tudo no PostgreSQL |
| **OPTIONAL_LATER** | Pode beneficiar no futuro, mas não necessário agora |
| **RECOMMENDED** | Deveria usar ClickHouse para workloads analíticos específicos |
| **REQUIRED** | Não consegue cumprir requisitos sem ClickHouse |

---

## Matriz por Módulo

### 01. Identity & Access

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | NONE |
| **PostgreSQL** | Todas as entidades: users, tenants, roles, permissions, sessions, delegations, JIT access, break glass, access reviews, external identities, SSO mappings, security events |
| **ClickHouse** | — |
| **Fora de escopo** | — |

---

### 02. Environment Management

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | NONE |
| **PostgreSQL** | Environments, environment access, environment policies, telemetry policies, integration bindings |
| **ClickHouse** | — |
| **Fora de escopo** | — |

---

### 03. Service Catalog

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | OPTIONAL_LATER |
| **PostgreSQL** | Services, dependencies, endpoints, team ownership, SLOs, developer portal content |
| **ClickHouse (futuro)** | Service health trends over time, dependency graph analytics |
| **Fora de escopo** | Real-time monitoring data (vem do OpenTelemetry) |

---

### 04. Contracts

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | NONE |
| **PostgreSQL** | API contracts, event contracts, SOAP contracts, versions, schemas, validation results, approval workflows |
| **ClickHouse** | — |
| **Fora de escopo** | — |

---

### 05. Change Governance

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | OPTIONAL_LATER |
| **PostgreSQL** | Change records, validations, approvals, promotions, rulesets, workflows, blast radius |
| **ClickHouse (futuro)** | Change frequency analytics, deployment velocity trends, blast radius trends |
| **Fora de escopo** | CI/CD pipeline data (external) |

---

### 06. Operational Intelligence

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | **RECOMMENDED** |
| **PostgreSQL** | Incidents, mitigation workflows, automations, runbooks, reliability snapshots, SLI/SLO definitions, cost budgets |
| **ClickHouse** | Runtime metrics time-series (`ops_runtime_metrics`), cost analytics entries (`ops_cost_entries`), incident trend aggregations (`ops_incident_trends`), SLA compliance metrics |
| **Fora de escopo** | Raw telemetry (OpenTelemetry Collector → ClickHouse directly) |

---

### 07. AI & Knowledge

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | OPTIONAL_LATER |
| **PostgreSQL** | Models, providers, agents, tools, policies, messages, conversations, knowledge captures, orchestration contexts, runtime configs |
| **ClickHouse (futuro)** | Token usage ledger (`aik_token_usage_ledger`), model performance metrics (`aik_model_performance`), provider latency |
| **Fora de escopo** | LLM training data, vector embeddings (external vector DB) |

---

### 08. Governance

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | **RECOMMENDED** |
| **PostgreSQL** | Compliance reports, risk assessments, policy frameworks, team scorecards, FinOps budgets, SLA definitions |
| **ClickHouse** | Compliance trend aggregations (`gov_compliance_trends`), FinOps cost aggregations (`gov_finops_aggregates`), risk score history |
| **Fora de escopo** | External compliance tool data |

---

### 09. Configuration

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | NONE |
| **PostgreSQL** | Configuration definitions, configuration entries, configuration audit entries |
| **ClickHouse** | — |
| **Fora de escopo** | — |

---

### 10. Audit & Compliance

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | OPTIONAL_LATER |
| **PostgreSQL** | Audit events (active, <1 year), compliance campaigns, retention policies, evidence items |
| **ClickHouse (futuro)** | Long-term audit archive (`aud_long_term_events`), audit trend analytics |
| **Fora de escopo** | External SIEM integration data |

---

### 11. Notifications

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | NONE |
| **PostgreSQL** | Notifications, notification deliveries, notification preferences |
| **ClickHouse** | — |
| **Fora de escopo** | — |

---

### 12. Integrations

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | **RECOMMENDED** |
| **PostgreSQL** | Integration connectors, ingestion sources, credentials, retry policies, operational state |
| **ClickHouse** | Execution logs (`int_execution_logs`), connector health history (`int_health_history`), freshness tracking, performance metrics |
| **Fora de escopo** | Data flowing through connectors (passthrough) |

---

### 13. Product Analytics

| Aspecto | Valor |
|---------|-------|
| **ClickHouse** | **REQUIRED** |
| **PostgreSQL** | Event definitions, tracking configs, funnel definitions (config tables only, 2-3 tables) |
| **ClickHouse** | `pan_events` (MergeTree), `pan_daily_module_stats` (SummingMergeTree), `pan_daily_persona_stats` (SummingMergeTree), `pan_daily_friction_stats` (SummingMergeTree), `pan_session_summaries` (AggregatingMergeTree) |
| **Fora de escopo** | External analytics (Google Analytics, Mixpanel) |

---

## Resumo Consolidado

| ClickHouse Level | Módulos | Quantidade |
|-----------------|---------|-----------|
| **NONE** | Identity, Environment, Contracts, Configuration, Notifications | 5 |
| **OPTIONAL_LATER** | Catalog, Change Governance, AI & Knowledge, Audit & Compliance | 4 |
| **RECOMMENDED** | Operational Intelligence, Governance, Integrations | 3 |
| **REQUIRED** | Product Analytics | 1 |
| **TOTAL** | — | **13** |

---

## Tabelas ClickHouse Planeadas

| Fase | Módulo | Tabelas | Engine |
|------|--------|---------|--------|
| 1 | Product Analytics | 5 | MergeTree, SummingMergeTree, AggregatingMergeTree |
| 2 | Operational Intelligence | 3 | MergeTree, SummingMergeTree |
| 2 | Integrations | 2 | MergeTree |
| 2 | Governance | 2 | SummingMergeTree |
| 3+ | AI & Knowledge | 2 | MergeTree |
| 3+ | Catalog | 1 | MergeTree |
| 3+ | Change Governance | 1 | SummingMergeTree |
| 3+ | Audit & Compliance | 1 | MergeTree |
| **TOTAL** | — | **17** | — |

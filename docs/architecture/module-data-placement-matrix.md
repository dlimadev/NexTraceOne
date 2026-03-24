# NexTraceOne — Module Data Placement Matrix (PostgreSQL vs ClickHouse)

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Phase:** A0 + A1 — Consolidation

---

## Legend

| ClickHouse Level | Meaning |
|------------------|---------|
| **NONE** | Module has no analytical data requirements for ClickHouse |
| **OPTIONAL_LATER** | Module may benefit from ClickHouse in the future but it is not needed now |
| **RECOMMENDED** | Module should use ClickHouse for specific analytical workloads |
| **REQUIRED** | Module cannot meet its analytical requirements without ClickHouse |

---

## 01 — Identity & Access

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Users, tenants, roles, permissions, sessions, security events, API keys, refresh tokens, JIT access, break glass, delegations |
| **ClickHouse data** | None |
| **Must NOT be in ClickHouse** | All identity/auth/session data (transactional, requires ACID, RLS) |
| **Must NOT be in PostgreSQL** | N/A |
| **ClickHouse level** | **NONE** |

---

## 02 — Environment Management

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Environments, environment policies, profiles, criticality levels, drift records |
| **ClickHouse data** | None |
| **Must NOT be in ClickHouse** | All environment configuration and policies (transactional, requires consistency) |
| **Must NOT be in PostgreSQL** | N/A |
| **ClickHouse level** | **NONE** |

---

## 03 — Service Catalog

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Services, APIs, consumers, dependencies, health records, snapshots, developer portal sessions |
| **ClickHouse data** | None |
| **Must NOT be in ClickHouse** | All service registry data (source of truth, requires ACID, referential integrity) |
| **Must NOT be in PostgreSQL** | N/A |
| **ClickHouse level** | **OPTIONAL_LATER** — May benefit from ClickHouse for service health trend analytics in the future |

---

## 04 — Contracts

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Contracts, versions, schemas, API endpoints, event contracts, SOAP services, Spectral rulesets, compliance scores, signatures, provenance |
| **ClickHouse data** | None |
| **Must NOT be in ClickHouse** | All contract definitions, versions, and governance data (source of truth, requires ACID) |
| **Must NOT be in PostgreSQL** | N/A |
| **ClickHouse level** | **NONE** |

---

## 05 — Change Governance

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Releases, change events, blast radius reports, workflow templates, workflow instances, approval decisions, promotion requests, rulesets, freeze windows, rollback assessments |
| **ClickHouse data** | None |
| **Must NOT be in ClickHouse** | All change tracking and workflow data (requires ACID, transactional integrity for approval flows) |
| **Must NOT be in PostgreSQL** | N/A |
| **ClickHouse level** | **OPTIONAL_LATER** — May benefit from ClickHouse for change frequency analytics and trend analysis |

---

## 06 — Operational Intelligence

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Incident records, mitigation actions, automation workflows, automation executions, runbooks, reliability snapshots, health classifications |
| **ClickHouse data** | Runtime metrics time-series, cost analytics aggregations, incident trend analysis, SLA compliance time-series, telemetry data |
| **Must NOT be in ClickHouse** | Active incident records, runbook definitions, automation workflow definitions (require ACID for state management) |
| **Must NOT be in PostgreSQL** | High-volume telemetry data, raw metrics streams (performance and storage concerns) |
| **ClickHouse level** | **RECOMMENDED** — Runtime metrics, cost analytics, and telemetry data are natural fits for columnar analytical storage |

---

## 07 — AI & Knowledge

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | AI models, providers, access policies, token quotas, agents, agent executions, orchestration sessions, knowledge entries, messages, routing decisions, IDE extensions |
| **ClickHouse data** | Token usage ledger aggregations, AI usage analytics, model performance metrics, provider latency tracking |
| **Must NOT be in ClickHouse** | AI model definitions, access policies, agent configurations, knowledge entries (require ACID, RLS) |
| **Must NOT be in PostgreSQL** | High-volume token usage event streams (if volume exceeds transactional DB capacity) |
| **ClickHouse level** | **OPTIONAL_LATER** — Token usage analytics may benefit from ClickHouse when volume grows, but PostgreSQL is sufficient initially |

---

## 08 — Governance

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Teams, domains, policies, governance packs, waivers, compliance reports, risk assessments, controls, evidence |
| **ClickHouse data** | Compliance trend analytics, risk scoring time-series, FinOps aggregated reporting, governance pack adoption metrics |
| **Must NOT be in ClickHouse** | Policy definitions, waiver records, team structures, evidence (require ACID, referential integrity, audit trail) |
| **Must NOT be in PostgreSQL** | Large-scale aggregated reporting data (if volume justifies offloading) |
| **ClickHouse level** | **RECOMMENDED** — Compliance analytics, risk trends, and FinOps aggregated reporting benefit from columnar storage |

---

## 09 — Configuration

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Configuration definitions, configuration entries, configuration audit entries |
| **ClickHouse data** | None |
| **Must NOT be in ClickHouse** | All configuration data (transactional, requires ACID, versioning, rollback support) |
| **Must NOT be in PostgreSQL** | N/A |
| **ClickHouse level** | **NONE** |

---

## 10 — Audit & Compliance

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Audit events, audit chain links, audit campaigns, compliance policies, compliance results, retention policies |
| **ClickHouse data** | None |
| **Must NOT be in ClickHouse** | All audit data (requires ACID, immutability, cryptographic hash chain integrity) |
| **Must NOT be in PostgreSQL** | N/A |
| **ClickHouse level** | **OPTIONAL_LATER** — May benefit from ClickHouse for long-term audit analytics and retention queries over large volumes |

---

## 11 — Notifications

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Notifications, notification deliveries, notification preferences |
| **ClickHouse data** | None |
| **Must NOT be in ClickHouse** | All notification data (transactional delivery tracking, user preferences require ACID) |
| **Must NOT be in PostgreSQL** | N/A |
| **ClickHouse level** | **NONE** |

---

## 12 — Integrations

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Integration connectors, ingestion sources, connector configurations |
| **ClickHouse data** | Ingestion execution logs, connector performance metrics, data freshness tracking time-series |
| **Must NOT be in ClickHouse** | Connector definitions, configuration (require ACID, referential integrity) |
| **Must NOT be in PostgreSQL** | High-volume ingestion execution logs (if volume grows significantly) |
| **ClickHouse level** | **RECOMMENDED** — Execution analytics and freshness monitoring are analytical workloads suited for ClickHouse |

---

## 13 — Product Analytics

| Attribute | Value |
|-----------|-------|
| **PostgreSQL data** | Analytics event definitions, tracking configurations, funnel definitions |
| **ClickHouse data** | Usage event streams, adoption metrics, persona usage aggregations, journey funnel data, engagement time-series, value tracking metrics |
| **Must NOT be in ClickHouse** | Tracking configuration definitions (require ACID) |
| **Must NOT be in PostgreSQL** | High-volume usage event streams, real-time analytics aggregations (volume and query patterns require columnar storage) |
| **ClickHouse level** | **REQUIRED** — Product analytics is fundamentally an analytical workload; ClickHouse is essential for event streams and aggregated metrics |

---

## Summary Table

| Module | PostgreSQL | ClickHouse Level | Primary ClickHouse Use Case |
|--------|-----------|-----------------|---------------------------|
| Identity & Access | ✅ All data | NONE | — |
| Environment Management | ✅ All data | NONE | — |
| Service Catalog | ✅ All data | OPTIONAL_LATER | Health trend analytics |
| Contracts | ✅ All data | NONE | — |
| Change Governance | ✅ All data | OPTIONAL_LATER | Change frequency analytics |
| Operational Intelligence | ✅ Domain data | **RECOMMENDED** | Runtime metrics, cost analytics, telemetry |
| AI & Knowledge | ✅ Domain data | OPTIONAL_LATER | Token usage analytics |
| Governance | ✅ Domain data | **RECOMMENDED** | Compliance analytics, FinOps reporting |
| Configuration | ✅ All data | NONE | — |
| Audit & Compliance | ✅ All data | OPTIONAL_LATER | Long-term audit analytics |
| Notifications | ✅ All data | NONE | — |
| Integrations | ✅ Domain data | **RECOMMENDED** | Execution analytics, freshness monitoring |
| Product Analytics | ✅ Config only | **REQUIRED** | Usage events, adoption metrics, funnels |

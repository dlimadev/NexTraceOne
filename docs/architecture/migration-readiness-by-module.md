# Migration Readiness por Módulo

> **Status:** DRAFT
> **Data:** 2026-03-25
> **Fase:** N15 — Estratégia de Transição de Persistência
> **Autor:** Consolidação automática com base nos artefactos N1–N14

---

## Objetivo

Avaliar quais módulos estão prontos para a nova baseline PostgreSQL e quais ainda bloqueiam a recriação das migrations.

---

## Legenda

| Estado | Significado |
|--------|-------------|
| ✅ SIM | Pronto para nova baseline |
| ⚠️ PARCIAL | Necessita correções menores antes da baseline |
| ❌ NÃO | Bloqueadores críticos impedem a baseline |

---

## Matriz de Readiness

| # | Módulo | Domínio Final | Persistência Final | Prefixo Definido | Seeds Definidos | Fronteiras Claras | Backlog Crítico Aberto | Dependências Bloqueantes | Readiness |
|---|--------|--------------|-------------------|-----------------|----------------|------------------|----------------------|------------------------|-----------|
| 01 | Identity & Access | ✅ 21 entidades, 7 aggregates | ✅ 17 tabelas `iam_` | ✅ `iam_` | ⚠️ HasData (roles/permissions/tenant) — necessita formalização | ⚠️ Environment entities acopladas (OI-04) | ⚠️ MFA não enforced (P0), API Key ausente (P1), 17 permissões licensing | OI-04: Environment entities embedded | ⚠️ PARCIAL |
| 02 | Environment Management | ✅ 5 entidades finalizadas | ✅ tabelas `env_` desenhadas | ✅ `env_` | ❌ Sem seeds definidos | ❌ Sem backend dedicado (dentro de Identity) | ⚠️ Extração para módulo próprio pendente (OI-04) | OI-04: Extração bloqueante | ❌ NÃO |
| 03 | Service Catalog | ✅ 9+5 tabelas cat_/dp_ | ✅ `cat_` + `dp_` | ✅ `cat_`, `dp_` | ❌ Sem seeds definidos | ⚠️ Contracts backend dentro deste módulo (OI-01) | ⚠️ OI-01 pendente | OI-01: Contracts dentro de Catalog | ⚠️ PARCIAL |
| 04 | Contracts | ✅ 8 tabelas, 5 faltam | ⚠️ Prefixo `ct_` → `ctr_` pendente | ✅ `ctr_` (target) | ❌ Sem seeds definidos | ❌ Backend dentro de Catalog (OI-01) | ⚠️ 5 tabelas ausentes, prefixo errado | OI-01: Extração bloqueante | ❌ NÃO |
| 05 | Change Governance | ✅ 4 subdomínios, 26 tabelas | ✅ `chg_` | ✅ `chg_` | ❌ Sem seeds definidos | ✅ 4 DbContexts bem separados | ⚠️ 35 itens remediação (~168h) | Nenhuma | ⚠️ PARCIAL |
| 06 | Operational Intelligence | ✅ 5 subdomínios, 19 tabelas | ⚠️ Prefixo `oi_` → `ops_` pendente | ✅ `ops_` (target) | ⚠️ IncidentSeedData existe (parcial) | ✅ 5 DbContexts bem separados | ⚠️ 55 itens remediação (~218h) | Nenhuma | ⚠️ PARCIAL |
| 07 | AI & Knowledge | ✅ 40+ entidades, 4 subdomínios | ⚠️ Prefixos mistos → `aik_` pendente | ✅ `aik_` (target) | ❌ Sem seeds definidos | ✅ 3 DbContexts separados | 🔴 55 itens (~325h), tools não executam (CR-2) | Nenhuma | ❌ NÃO |
| 08 | Governance | ✅ 12 tabelas `gov_` | ✅ `gov_` | ✅ `gov_` | ❌ Sem seeds definidos | ⚠️ Integrations + Product Analytics dentro (OI-02, OI-03) | ⚠️ Extrações pendentes | OI-02, OI-03 | ⚠️ PARCIAL |
| 09 | Configuration | ✅ 4 tabelas `cfg_` | ✅ `cfg_` (já aplicado) | ✅ `cfg_` | ✅ ConfigurationDefinitionSeeder (~345 defs) | ✅ Fronteiras claras | ⚠️ 22 itens (~33h), 0 migrations | Nenhuma | ✅ SIM |
| 10 | Audit & Compliance | ✅ 6 tabelas `aud_` | ✅ `aud_` | ✅ `aud_` | ❌ Sem seeds definidos | ✅ 1 DbContext isolado | ⚠️ 43 itens (~197h) | Nenhuma | ⚠️ PARCIAL |
| 11 | Notifications | ✅ 3 aggregates, 6 enums | ⚠️ 0 migrations, `ntf_` target | ✅ `ntf_` (target) | ❌ Sem seeds definidos | ✅ 1 DbContext isolado | 🔴 Permissões notifications:* ausentes do RolePermissionCatalog | Nenhuma | ❌ NÃO |
| 12 | Integrations | ✅ 3 entidades | ⚠️ Dentro de Governance (`gov_` → `int_`) | ✅ `int_` (target) | ❌ Sem seeds definidos | ❌ Backend dentro de Governance (OI-02) | ⚠️ 46 itens (~160h), zero domain events | OI-02: Extração bloqueante | ❌ NÃO |
| 13 | Product Analytics | ✅ 1 entidade | ⚠️ Dentro de Governance (`gov_` → `pan_`) | ✅ `pan_` (target) | ❌ Sem seeds definidos | ❌ Backend dentro de Governance (OI-03) | 🔴 52 itens (~195h), mock data, ClickHouse REQUIRED | OI-03: Extração bloqueante | ❌ NÃO |

---

## Resumo de Readiness

| Estado | Módulos | Quantidade |
|--------|---------|-----------|
| ✅ SIM | Configuration | 1 |
| ⚠️ PARCIAL | Identity & Access, Service Catalog, Change Governance, Operational Intelligence, Governance, Audit & Compliance | 6 |
| ❌ NÃO | Environment Management, Contracts, AI & Knowledge, Notifications, Integrations, Product Analytics | 6 |

---

## Bloqueadores Transversais

| ID | Bloqueador | Módulos Afetados | Prioridade |
|----|-----------|-----------------|-----------|
| OI-01 | Contracts backend dentro de Catalog | Catalog, Contracts | HIGH |
| OI-02 | Integrations backend dentro de Governance | Governance, Integrations | HIGH |
| OI-03 | Product Analytics backend dentro de Governance | Governance, Product Analytics | HIGH |
| OI-04 | Environment Management disperso em Identity | Identity & Access, Environment Management | MEDIUM |
| OI-05 | Resíduos de Licensing em código e seeds | Identity & Access | LOW |
| OI-07 | Configuration e Notifications com 0 migrations | Configuration, Notifications | MEDIUM |
| PREFIX | Prefixos errados em 3+ módulos (`oi_`→`ops_`, `ct_`→`ctr_`, entidades em `gov_`) | OpIntel, Contracts, Integrations, Product Analytics | HIGH |

---

## Conclusão

**Apenas 1 módulo (Configuration) está totalmente pronto para baseline.**
6 módulos estão parcialmente prontos e necessitam correções menores.
6 módulos possuem bloqueadores críticos que impedem a baseline, principalmente extração de backends e criação de DbContexts dedicados.

A resolução dos bloqueadores OI-01 a OI-04 é pré-condição obrigatória para a maioria dos módulos avançarem para a nova baseline.

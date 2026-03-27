# N9-R — Remediation Plan Reinforcement Report

> **Module:** Operational Intelligence (06)  
> **Prompt:** N9-R  
> **Date:** 2026-03-25  
> **Status:** ✅ CONCLUÍDO

---

## 1. O que estava fraco no remediation plan anterior

O ficheiro `module-remediation-plan.md` anterior tinha **23 items em 101h** organizados em 5 secções (A–E), mas apresentava os seguintes problemas:

| Problema | Detalhe |
|----------|---------|
| 🔴 **Sem resumo executivo** | Não explicava estado actual, risco, ou prioridade do módulo |
| 🔴 **Quick wins insuficientes** | Apenas 7 items documentais; não incluía security fixes imediatos (QW-05 RunbooksPage permission, FC-14/FC-15 execution gate/four-eyes) |
| 🔴 **Correções funcionais incompletas** | Apenas 8 items; faltavam 16+ correções identificadas nos artefactos de backend, frontend e scoring |
| 🔴 **Sem integração Notifications/Audit como P0** | FC-01/FC-02 classificados como P1, mas são P0_BLOCKER — o módulo não pode ir para produção sem eles |
| 🔴 **Sem publicação de domain events** | Não incluía FC-03 (publicar events para todas as state transitions) — pré-condição para FC-01/FC-02 |
| 🟠 **Ajustes estruturais genéricos** | SA-01 dizia "confirm all 19 tables use ops_" — as tabelas usam `oi_`, não `ops_`, e isto precisa de acção real |
| 🟠 **Sem backlog priorizado em tabela** | Nenhuma tabela consolidada com todos os items, prioridade, tipo e dependências |
| 🟠 **Sem ordem de execução detalhada** | Apenas 3 waves genéricas; sem sprint-by-sprint breakdown |
| 🟠 **Critérios de aceite superficiais** | Listava ficheiros de review como critérios, não critérios reais de implementação |
| 🟠 **Sem ClickHouse backlog** | Mencionava "define schema" mas não incluía pipeline completo (consumer, dual-read, TenantId enforcement) |
| 🟠 **Sem scoring/threshold backlog detalhado** | Não incluía hysteresis, SLO overrides, configuração via Configuration module |
| 🟡 **Sem pré-condições de migration detalhadas** | 8 pré-condições, mas sem status real (7 diziam "not yet" sem mapear para items) |

---

## 2. O que foi reforçado

### Métricas de comparação

| Métrica | Antes | Depois | Delta |
|---------|-------|--------|-------|
| Total de items | 23 | 55 | +139% |
| Esforço estimado | ~101 h | ~218 h | +116% |
| Quick wins | 7 | 12 | +71% |
| Correções funcionais | 8 | 24 | +200% |
| Ajustes estruturais | 8 | 19 | +138% |
| Pré-condições migration | 8 | 12 | +50% |
| Critérios de aceite | 12 (superficiais) | 31 (implementação) | +158% |
| Sprints planeados | 3 waves | 10 sprints | +233% |
| Tamanho do ficheiro | 113 linhas | ~500 linhas | +342% |

### Mudanças estruturais

| Secção | O que mudou |
|--------|-------------|
| **Resumo executivo** (NOVO) | Adicionado com: estado, lacunas, risco, prioridade |
| **Quick wins** | Expandido de 7→12; incluindo security fixes imediatos (QW-05, QW-12) |
| **Correções funcionais** | Expandido de 8→24; separado em: cross-module (3), frontend (9), backend (12) |
| **Ajustes estruturais** | Expandido de 8→19; separado em: persistência (7), domínio/scoring (5), ClickHouse (5), cost pipeline (2) |
| **Pré-condições migration** | Expandido de 8→12; cada uma mapeada para item do backlog; status real |
| **Critérios de aceite** | Totalmente reescrito com 31 critérios em 8 categorias (backend, frontend, scoring, automações, segurança, auditoria, persistência, ClickHouse, documentação) |
| **Ordem de execução** (REESCRITO) | 10 sprints detalhados com sequência técnica real |
| **Backlog priorizado** (NOVO) | Tabela consolidada com 55 items: ID, camada, prioridade (P0–P3), tipo, sprint, dependências, esforço |

### Principais acções adicionadas

| Acção | Prioridade | Porque faltava |
|-------|-----------|----------------|
| FC-01/FC-02 reclassificados como **P0_BLOCKER** | P0 | Eram P1; são bloqueadores reais de produção |
| FC-03 Publicar domain events para state transitions | P1 | Pré-condição para Notifications/Audit |
| FC-15 Four-eyes principle | P1 | Security gap crítico — aprovador = requester possível |
| FC-14 Hardening execution gate | P1 | Security gap — execução sem verificação de approval |
| FC-06/FC-07/FC-08 API clients completos | P2 | Frontend não consegue usar endpoints backend |
| SA-08/SA-09/SA-10 Scoring configurável + hysteresis + SLO | P2 | Thresholds hardcoded; sem customização |
| SA-13/SA-14/SA-15/SA-17 ClickHouse pipeline completo | P2 | Faltava consumer, dual-read, TenantId enforcement |
| SA-18 Cost import pipeline | P2 | ImportCostBatch parcial; cost data não entra |
| SA-11/SA-12 Approval quorum + timeout | P3 | Gaps de automação avançada não cobertos |

---

## 3. Resumo das principais acções adicionadas

### P0_BLOCKER (2 items, ~17 h)
- **FC-01**: Integrar com Notifications — publicar 6 tipos de eventos de domínio via outbox
- **FC-02**: Integrar com Audit & Compliance — forward de 6+ acções sensíveis

### P1_CRITICAL (15 items, ~48 h)
- **FC-03**: Publicar domain events para todas as state transitions (6 novos events)
- **FC-13**: RowVersion em 6 aggregates mutáveis
- **FC-14/FC-15**: Security hardening (execution gate + four-eyes)
- **FC-04/FC-05/FC-06**: Cost Intelligence frontend completo (page + menu + API client)
- **SA-01/SA-02**: Renomear 19 tabelas `oi_` → `ops_` + outbox tables
- **QW-01–05, QW-12**: Documentation + security quick wins

### P2_HIGH (22 items, ~110 h)
- Scoring configurável (thresholds, hysteresis, SLO overrides)
- API clients completos (reliability, runtime)
- ClickHouse schema + consumer + dual-read + retention
- Backend CRUD endpoints (Runbooks, status transition, idempotency)
- Cost import pipeline
- EnvironmentId/TenantId padronização

### P3_MEDIUM (16 items, ~43 h)
- Frontend polish (breadcrumbs, toast, staleTime, i18n)
- Automation avançada (quorum, timeout, preconditions extensíveis)
- Enum storage, check constraints

---

## 4. Confirmação de conclusão do N9

| Critério | Status |
|----------|--------|
| `module-remediation-plan.md` significativamente mais forte | ✅ De 23→55 items, 101→218h, 113→500+ linhas |
| Plano priorizado e acionável | ✅ P0→P3 com 10 sprints detalhados |
| Quick wins, correções, estruturais e pré-conditions separados | ✅ 4 secções distintas + backlog consolidado |
| `n9-remediation-plan-reinforcement-report.md` existe | ✅ Este ficheiro |
| Backlog priorizado em tabela com dependências | ✅ Secção 8 com 55 items |
| Ordem de execução com sequência técnica | ✅ Secção 7 com 10 sprints |
| Critérios de aceite objectivos | ✅ 31 critérios em 8 categorias |

**✅ O N9 pode agora ser considerado COMPLETO.**

---

## 5. Indicação: módulo pronto para execução real?

### O que está pronto
- ✅ Domínio modelado (19 entidades, 23 enums, 5 subdomínios)
- ✅ 56 endpoints backend operacionais
- ✅ 10 páginas frontend funcionais
- ✅ 5 DbContexts com RLS, audit, encryption
- ✅ Scoring formula implementada
- ✅ Automation state machine completa
- ✅ Backlog de remediação priorizado e acionável

### O que bloqueia execução real
- ❌ **P0_BLOCKER**: Integrações Notifications e Audit ausentes (FC-01, FC-02)
- ❌ **P1_CRITICAL**: Prefixo `oi_` → `ops_` (SA-01)
- ❌ **P1_CRITICAL**: RowVersion ausente (FC-13)
- ❌ **P1_CRITICAL**: Four-eyes principle ausente (FC-15)
- ❌ **P1_CRITICAL**: Cost frontend ausente (FC-04)

### Dependências externas
- **Notifications (11)**: Event handlers para subscrever domain events de OI — módulo N8-R concluído
- **Audit & Compliance (10)**: Aceitar audit events de OI — módulo N10-R concluído
- **Change Governance (05)**: Publicar events para correlação — módulo N7-R concluído
- **Configuration (09)**: Thresholds via config entries — módulo N2 concluído
- **Infraestrutura ClickHouse**: Cluster provisionado — fora do scope modular

### Estimativa de esforço para closure

| Até | Esforço | Resultado |
|-----|---------|-----------|
| **MVP funcional** (P0+P1 blocker fixes) | ~65 h | Módulo utilizável em staging |
| **Production-ready** (P0+P1+P2 critical) | ~175 h | Módulo pronto para produção |
| **Feature-complete** (todos os items) | ~218 h | Módulo totalmente consolidado |

### Maturity actual vs target

| Área | Actual | Target | Gap |
|------|--------|--------|-----|
| Backend | 80% | 90% | 10% |
| Frontend | 65% | 85% | 20% |
| Scoring | 70% | 85% | 15% |
| Automação | 75% | 90% | 15% |
| Segurança | 65% | 90% | 25% |
| Auditoria | 30% | 85% | 55% |
| Persistência | 70% | 90% | 20% |
| Documentação | 40% | 75% | 35% |
| ClickHouse | 0% | 50% | 50% |
| **Overall** | **~55%** | **~85%** | **~30%** |

**O módulo está pronto para iniciar execução real**, começando pelo Sprint 1 (quick wins + security fixes) e Sprint 2 (integração cross-module P0).

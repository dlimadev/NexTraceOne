# NexTraceOne — Master Action Plan

> **Data:** Maio 2026 (última revisão) | Criado: Abril 2026  
> **Estado do produto:** Backend v1.0.0 READY (12 módulos, 155 waves analytics concluídas). Gaps pós-v1.0.0 documentados em [HONEST-GAPS.md](./HONEST-GAPS.md).  
> **Propósito:** Plano único e autoritativo de tudo o que falta implementar para fechar 100% do escopo e evoluir o produto.

---

## Resumo Executivo

| Grupo | Ficheiro de Plano | Prioridade | Esforço estimado | Estado |
|-------|-------------------|------------|-----------------|--------|
| **0 — Forensic Fixes (gaps reais)** | ~~PLAN-08-FORENSIC-FIXES.md~~ | — | — | ✅ Concluído (removido) |
| **1 — Ingestion Pipeline** | [PLAN-01-INGESTION-PIPELINE.md](./plans/PLAN-01-INGESTION-PIPELINE.md) | — | — | ✅ PIP-01..06 Concluídos |
| **2 — Core Completions** | [PLAN-02-CORE-COMPLETIONS.md](./plans/PLAN-02-CORE-COMPLETIONS.md) | 🔴 Alta | 4–6 semanas | CC-01 ✅; CC-02..08 pendentes |
| **3 — Frontend V3 (Wave AA)** | [PLAN-03-FRONTEND-V3.md](./plans/PLAN-03-FRONTEND-V3.md) | 🟡 Média | 16–24 semanas | Não iniciado |
| **4 — Infrastructure Evolution** | [PLAN-04-INFRASTRUCTURE.md](./plans/PLAN-04-INFRASTRUCTURE.md) | 🟡 Média | 9–13 semanas | Não iniciado |
| **5 — AI Evolution** | [PLAN-05-AI-EVOLUTION.md](./plans/PLAN-05-AI-EVOLUTION.md) | 🟡 Média | 20–30 semanas | Não iniciado |
| **6 — SaaS Evolution** | [PLAN-06-SAAS-EVOLUTION.md](./plans/PLAN-06-SAAS-EVOLUTION.md) | 🟠 Roadmap | 12–20 semanas | Não iniciado |
| **7 — Legacy/Mainframe** | [PLAN-07-LEGACY.md](./plans/PLAN-07-LEGACY.md) | 🔵 Futuro | 24+ semanas | Não iniciado |

---

## Histórico de Remoções

| Ficheiro Removido | Motivo | Data |
|-------------------|--------|------|
| `docs/ACTION-PLAN.md` | Todos os 25 ACTs (ACT-001..025) marcados ✅ RESOLVIDO | Abril 2026 |
| `docs/analysis/PRODUCT-ANALYTICS-IMPROVEMENT-PLAN.md` | Todos os itens implementados | Abril 2026 |
| `docs/FORENSIC-ANALYSIS-2026-04.md` | Todos os 4 problemas críticos resolvidos; supersedido por CHANGELOG.md | Maio 2026 |
| `docs/PLAN-ACTION-2026-04.md` | Todos os tasks ✅; supersedido por CHANGELOG.md | Maio 2026 |
| `docs/plans/PLAN-08-FORENSIC-FIXES.md` | Todos os 15 items ✅ Concluído; supersedido por CHANGELOG.md | Maio 2026 |

## Histórico de Actualizações

| Ficheiro | Alteração | Data |
|----------|-----------|------|
| `docs/FUTURE-ROADMAP.md` | Cabeçalho corrigido: waves S–Z e AB–BC eram falsamente listadas como "planeadas" | Abril 2026 |
| `docs/HONEST-GAPS.md` | DES-02 removido (ActivateAccount/ResetPassword implementados); GAP-M01..06 adicionados | Maio 2026 |
| `PRODUCTION-ACTION-PLAN.md` | Tasks 1.1, 1.2, 4.4 marcadas ✅; diagnóstico Selenium corrigido | Maio 2026 |
| `PRODUCTION-READINESS-REPORT.md` | Selenium diagnosis corrigido; status Contract Pipeline actualizado | Maio 2026 |

---

## Gaps Honestos Remanescentes (HONEST-GAPS.md)

Os seguintes itens do `HONEST-GAPS.md` continuam abertos após v1.0.0:

| ID | Descrição | Plano | Estado |
|----|-----------|-------|--------|
| PIP-01..06 | Pipeline de ingestão (Dead Letter Queue, observabilidade, regras por tenant, routing, enrichment, log→metric) | PLAN-01 | ✅ Concluído (Abril 2026) |
| DEG-11 (A′→A) | SAML: expor `ISamlConfigProvider.IsConfigured` no `/admin/system-health` dashboard | PLAN-02 CC-01 | ✅ Concluído (Abril 2026) |
| DEG-03..07 (Nível B) | Runtime, Chaos, mTLS, Schema Planner, Capacity Forecast — promover para Nível A quando clientes exigirem | PLAN-02 | 🔄 Pendente |
| GAP-M01 | `GetDashboardAnnotations` hardcoded (4 anotações fictícias) | PLAN-02 CC-09 (a criar) | 🔴 Aberto |
| GAP-M02 | Startup `ValidateOnStart` para JWT/ConnectionStrings não implementado | PRODUCTION-ACTION-PLAN Task 2.1 | 🔴 Aberto |
| GAP-M03 | Contract Pipeline: 3 features ainda usam ContractJson do request | PRODUCTION-ACTION-PLAN Task 3.1 | 🔴 Parcial |
| GAP-M06 | `IIdentityNotifier` não ligado a email real | PLAN-02 ou Notifications sprint | 🟡 Aberto |

---

## Referências

- [HONEST-GAPS.md](./HONEST-GAPS.md) — fonte da verdade para gaps e degradações graciosas
- [IMPLEMENTATION-STATUS.md](./IMPLEMENTATION-STATUS.md) — estado de cada módulo
- [FUTURE-ROADMAP.md](./FUTURE-ROADMAP.md) — roadmap completo (155 waves backend concluídas + Wave AA planeada)
- [INGESTION-PIPELINE-IMPLEMENTATION.md](./INGESTION-PIPELINE-IMPLEMENTATION.md) — spec técnica detalhada do pipeline
- [V3-EVOLUTION-FRONTEND-DASHBOARDS.md](./V3-EVOLUTION-FRONTEND-DASHBOARDS.md) — spec técnica Wave AA
- [AI-EVOLUTION-ROADMAP.md](./AI-EVOLUTION-ROADMAP.md) — roadmap IA fases 0–5
- [AI-AGENT-LIGHTNING.md](./AI-AGENT-LIGHTNING.md) — framework RL para agentes
- [SAAS-ROADMAP.md](./SAAS-ROADMAP.md) — roadmap SaaS
- [NEXTTRACE-AGENT.md](./NEXTTRACE-AGENT.md) — spec do NexTrace Agent binário
- [analysis/INFRA-EVOLUTION-OVERVIEW.md](./analysis/INFRA-EVOLUTION-OVERVIEW.md) — plano infra 4 fases

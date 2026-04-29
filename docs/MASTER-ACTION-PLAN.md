# NexTraceOne — Master Action Plan

> **Data:** Abril 2026  
> **Estado do produto:** Backend v1.0.0 READY (12 módulos, 155 waves analytics concluídas).  
> **Propósito:** Plano único e autoritativo de tudo o que falta implementar para fechar 100% do escopo e evoluir o produto.

---

## Resumo Executivo

| Grupo | Ficheiro de Plano | Prioridade | Esforço estimado |
|-------|-------------------|------------|-----------------|
| **0 — Forensic Fixes (gaps reais)** | [PLAN-08-FORENSIC-FIXES.md](./plans/PLAN-08-FORENSIC-FIXES.md) | 🔴 Urgente | 3–6 semanas |
| **1 — Ingestion Pipeline** | [PLAN-01-INGESTION-PIPELINE.md](./plans/PLAN-01-INGESTION-PIPELINE.md) | 🔴 Alta | 6–10 semanas |
| **2 — Core Completions** | [PLAN-02-CORE-COMPLETIONS.md](./plans/PLAN-02-CORE-COMPLETIONS.md) | 🔴 Alta | 4–6 semanas |
| **3 — Frontend V3 (Wave AA)** | [PLAN-03-FRONTEND-V3.md](./plans/PLAN-03-FRONTEND-V3.md) | 🟡 Média | 16–24 semanas |
| **4 — Infrastructure Evolution** | [PLAN-04-INFRASTRUCTURE.md](./plans/PLAN-04-INFRASTRUCTURE.md) | 🟡 Média | 9–13 semanas |
| **5 — AI Evolution** | [PLAN-05-AI-EVOLUTION.md](./plans/PLAN-05-AI-EVOLUTION.md) | 🟡 Média | 20–30 semanas |
| **6 — SaaS Evolution** | [PLAN-06-SAAS-EVOLUTION.md](./plans/PLAN-06-SAAS-EVOLUTION.md) | 🟠 Roadmap | 12–20 semanas |
| **7 — Legacy/Mainframe** | [PLAN-07-LEGACY.md](./plans/PLAN-07-LEGACY.md) | 🔵 Futuro | 24+ semanas |

**Total estimado:** 94–139 semanas de esforço (paralelizável em múltiplas frentes)

> **Análise forense completa:** [FORENSIC-ANALYSIS-2026-04.md](./FORENSIC-ANALYSIS-2026-04.md) — estado real do projeto com problemas concretos identificados.  
> **Plano de ação executivo:** [PLAN-ACTION-2026-04.md](./PLAN-ACTION-2026-04.md) — tasks detalhadas com critérios de aceite.

---

## O Que Foi Removido Nesta Auditoria

| Ficheiro Removido | Motivo |
|-------------------|--------|
| `docs/ACTION-PLAN.md` | Todos os 25 ACTs (ACT-001..025) marcados ✅ RESOLVIDO |
| `docs/analysis/PRODUCT-ANALYTICS-IMPROVEMENT-PLAN.md` | Todos os itens implementados; pendências residuais (Redis cache, integration tests) ficam em PLAN-02 |

## O Que Foi Atualizado

| Ficheiro | Alteração |
|----------|-----------|
| `docs/FUTURE-ROADMAP.md` | Cabeçalho corrigido: waves S–Z e AB–BC eram falsamente listadas como "planeadas" — todas estão ✅ CONCLUÍDAS. Tabela de priorização atualizada. |

---

## Gaps Honestos Remanescentes (HONEST-GAPS.md)

Os seguintes itens do `HONEST-GAPS.md` continuam abertos após v1.0.0:

| ID | Descrição | Plano |
|----|-----------|-------|
| PIP-01..06 | Pipeline de ingestão (Dead Letter Queue, observabilidade, regras por tenant, routing, enrichment, log→metric) | PLAN-01 |
| DEG-11 (A′→A) | SAML: expor `ISamlConfigProvider.IsConfigured` no `/admin/system-health` dashboard | PLAN-02 |
| DEG-03..07 (Nível B) | Runtime, Chaos, mTLS, Schema Planner, Capacity Forecast — promover para Nível A quando clientes exigirem | PLAN-02 |

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

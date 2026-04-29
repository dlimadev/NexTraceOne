# PLAN-08 — Forensic Fixes: Gaps do Estado Real (Abril 2026)

> **Análise base:** [FORENSIC-ANALYSIS-2026-04.md](../FORENSIC-ANALYSIS-2026-04.md)  
> **Plano executivo:** [PLAN-ACTION-2026-04.md](../PLAN-ACTION-2026-04.md)  
> **Prioridade:** 🔴 Alta (dados falsos visíveis ao utilizador)

---

## Grupo F — Frontend (Dados Falsos/Sem API)

| ID | Task | Ficheiro | Estado | Esforço |
|----|------|---------|--------|---------|
| F-01 | PersonaHomePage: conectar `GET /api/v1/governance/persona-home` e remover arrays hardcoded | `src/frontend/src/features/governance/pages/PersonaHomePage.tsx` | ⬜ Pendente | 3–5h |
| F-02 | DashboardReportsPage: remover `select: () => SIMULATED_REPORTS` | `src/frontend/src/features/governance/pages/DashboardReportsPage.tsx` | ⬜ Pendente | 1–2h |
| F-03 | DashboardTemplatesPage: criar endpoint backend + ligar `useQuery` | `src/frontend/src/features/governance/pages/DashboardTemplatesPage.tsx` | ⬜ Pendente | 8–16h |
| F-04 | SetupWizardPage: adicionar persistência via API (estado do wizard) | `src/frontend/src/features/platform-admin/pages/SetupWizardPage.tsx` | ⬜ Pendente | 16–24h |

---

## Grupo B — Backend (IsSimulated=true Estrutural)

| ID | Task | Ficheiro | Estado | Esforço |
|----|------|---------|--------|---------|
| B-01 | GetPersonaHome: injetar cross-module e retornar dados reais | `src/modules/governance/…/Features/GetPersonaHome/GetPersonaHome.cs` | ⬜ Pendente | 4–8h |
| B-02 | GetWidgetDelta: criar WidgetSnapshot entity + delta real | `src/modules/governance/…/Features/GetWidgetDelta/GetWidgetDelta.cs` | ⬜ Pendente | 16–24h |
| B-03 | ComposeAiDashboard: integrar IChatCompletionProvider | `src/modules/governance/…/Features/ComposeAiDashboard/ComposeAiDashboard.cs` | ⬜ Pendente | 8–12h |
| B-04 | GetDashboardLiveStream: criar IDashboardDataBridge + eventos reais | `src/modules/governance/…/Features/GetDashboardLiveStream/GetDashboardLiveStream.cs` | ⬜ Pendente | 16–24h |
| B-05 | NQL Native Execution: implementar query EF Core para GovernanceTeams/Domains | `src/modules/governance/…/Persistence/QueryGovernanceService.cs:80` | ⬜ Pendente | 4–8h |
| B-06 | GetDemoSeedStatus: verificação real do estado | `src/modules/governance/…/Features/GetDemoSeedStatus/GetDemoSeedStatus.cs` | ⬜ Pendente | 2–3h |
| B-07 | InstantiateTemplate: criar dashboard real (não IsSimulated) | `src/modules/governance/…/Features/InstantiateTemplate/InstantiateTemplate.cs` | ⬜ Pendente | 8–16h |

---

## Grupo I — Infraestrutura (Providers Nível B → A)

| ID | Task | Estado | Esforço |
|----|------|--------|---------|
| I-01 | DEG-03: IRuntimeProvider + NullRuntimeProvider + system-health | ⬜ Pendente | 16h |
| I-02 | DEG-04: IChaosProvider + NullChaosProvider + system-health | ⬜ Pendente | 12h |
| I-03 | DEG-05: ICertificateProvider + NullCertificateProvider + system-health | ⬜ Pendente | 16h |
| I-04 | DEG-06: ISchemaPlanner + NullSchemaPlanner + system-health | ⬜ Pendente | 12h |

---

## Legenda de Estado

- ⬜ Pendente
- 🔄 Em progresso
- ✅ Concluído
- ❌ Cancelado / Out-of-scope

---

## Dependências

```
F-01 depende de: B-01 (para remover IsSimulated=true do PersonaHome)
F-03 depende de: DashboardTemplate backend (criar se não existir)
F-04 é independente
B-03 depende de: IChatCompletionProvider (já existe — injetar apenas)
B-04 depende de: IEventBus (já existe) ou nova IDashboardDataBridge
B-05 é independente (injetar repositórios existentes)
```

---

## Ordem de Implementação Recomendada

```
Sprint 1 (1 semana):
  F-02 (1–2h) → rápido win
  B-06 (2–3h) → rápido win
  B-05 (4–8h) → NQL fix
  F-01 (3–5h) → PersonaHome

Sprint 2 (1 semana):
  B-01 (4–8h) → PersonaHome backend real
  F-03 (8–16h) → Dashboard Templates

Sprint 3 (2 semanas):
  F-04 (16–24h) → SetupWizard
  B-03 (8–12h) → AI Compose
  B-07 (8–16h) → Template instantiation

Sprint 4 (2 semanas):
  B-02 (16–24h) → Widget Delta
  B-04 (16–24h) → Live Stream

Sprint 5+ (ongoing, demand-driven):
  I-01..04 → Providers Nível B → A
```

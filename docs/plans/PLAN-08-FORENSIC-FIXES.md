# PLAN-08 — Forensic Fixes: Gaps do Estado Real (Abril 2026)

> **Análise base:** [FORENSIC-ANALYSIS-2026-04.md](../FORENSIC-ANALYSIS-2026-04.md)  
> **Plano executivo:** [PLAN-ACTION-2026-04.md](../PLAN-ACTION-2026-04.md)  
> **Prioridade:** 🔴 Alta (dados falsos visíveis ao utilizador)

---

## Grupo F — Frontend (Dados Falsos/Sem API)

| ID | Task | Ficheiro | Estado | Esforço |
|----|------|---------|--------|---------|
| F-01 | PersonaHomePage: conectar `GET /api/v1/governance/persona-home` e remover arrays hardcoded | `src/frontend/src/features/governance/pages/PersonaHomePage.tsx` | ✅ Concluído | 3–5h |
| F-02 | DashboardReportsPage: remover `select: () => SIMULATED_REPORTS`; criar endpoint + API client | `src/frontend/src/features/governance/pages/DashboardReportsPage.tsx` | ✅ Concluído | 1–2h |
| F-03 | DashboardTemplatesPage: criar API client + ligar `useQuery` ao endpoint existente | `src/frontend/src/features/governance/pages/DashboardTemplatesPage.tsx` | ✅ Concluído | 4–6h |
| F-04 | SetupWizardPage: adicionar persistência via API (estado do wizard) | `src/frontend/src/features/platform-admin/pages/SetupWizardPage.tsx` | ⬜ Pendente | 16–24h |

---

## Grupo B — Backend (IsSimulated=true Estrutural)

| ID | Task | Ficheiro | Estado | Esforço |
|----|------|---------|--------|---------|
| B-01 | GetPersonaHome: injetar IIncidentModule + ICatalogGraphModule; retornar dados reais | `src/modules/governance/…/Features/GetPersonaHome/GetPersonaHome.cs` | ✅ Concluído | 4–8h |
| B-02 | GetWidgetDelta: criar WidgetSnapshot entity + delta real | `src/modules/governance/…/Features/GetWidgetDelta/GetWidgetDelta.cs` | ⬜ Pendente | 16–24h |
| B-03 | ComposeAiDashboard: integrar IAiDashboardComposerService (wrapper IChatCompletionProvider) | `src/modules/governance/…/Features/ComposeAiDashboard/ComposeAiDashboard.cs` | ✅ Concluído | 8–12h |
| B-04 | GetDashboardLiveStream: criar IDashboardDataBridge + eventos reais | `src/modules/governance/…/Features/GetDashboardLiveStream/GetDashboardLiveStream.cs` | ⬜ Pendente | 16–24h |
| B-05 | NQL Native Execution: implementar query EF Core para GovernanceTeams/Domains | `src/modules/governance/…/Persistence/QueryGovernanceService.cs` | ✅ Concluído | 4–8h |
| B-06 | GetDemoSeedStatus: verificação real do estado (já usa IDemoSeedStateRepository — não era bug) | `src/modules/governance/…/Features/GetDemoSeedStatus/GetDemoSeedStatus.cs` | ✅ Já real | — |
| B-07 | InstantiateTemplate: corrigir IsSimulated: true → false após criação real | `src/modules/governance/…/Features/InstantiateTemplate/InstantiateTemplate.cs` | ✅ Concluído | 1h |

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

## O que foi implementado neste sprint (Abril 2026)

### F-01 — PersonaHomePage
- Criado `src/frontend/src/features/governance/api/personaHome.ts` com `personaHomeApi.getPersonaHome()`
- Substituídos arrays `ENGINEER_STATS`, `TECHLEAD_STATS`, `EXECUTIVE_STATS`, `DEFAULT_STATS` por `useQuery`
- Banner de simulação só aparece quando `data.isSimulated === true`
- Adicionados estados de loading e erro

### F-02 — DashboardReportsPage
- Criado feature backend `ListScheduledDashboardReports` com método `ListByTenantAsync`
- Adicionado `ListByTenantAsync` a `IScheduledDashboardReportRepository` e `ScheduledDashboardReportRepository`
- Endpoint `GET /api/v1/governance/dashboards/scheduled-reports` registado em `DashboardsAndDebtEndpointModule`
- Adicionado `reportsApi.listScheduledReports()` ao API client frontend
- Removida variável `SIMULATED_REPORTS` e `select: () => SIMULATED_REPORTS`

### F-03 — DashboardTemplatesPage
- Criado `src/frontend/src/features/governance/api/dashboardTemplates.ts`
- Substituídos `TEMPLATES` hardcoded por `useQuery` ao endpoint `/api/v1/governance/dashboard-templates` (já existia)
- Adicionada mutação `instantiate` para criar dashboard real ao clicar "Use Template"

### B-01 — GetPersonaHome
- Injectados `IIncidentModule` e `ICatalogGraphModule` no handler
- Para persona `engineer`: conta serviços reais via `ListAllServicesAsync` e incidentes via `CountOpenIncidentsAsync`
- Para persona `executive`: agrega incidentes + trend + portfolio de serviços
- `IsSimulated: false` quando cross-module responde; fallback gracioso com `IsSimulated: true` em caso de erro

### B-03 — ComposeAiDashboard
- Criada interface `IAiDashboardComposerService` em `Governance.Application.Abstractions`
- Implementada `AiDashboardComposerService` em `Governance.Infrastructure.AI` usando `IChatCompletionProvider`
- Handler usa LLM real quando `IsConfigured = true`; fallback keyword-based honesto quando não configurado
- `IsSimulated: false` quando LLM respondeu; `IsSimulated: true` no fallback

### B-05 — NQL Native Execution
- Injectados `ITeamRepository` e `IGovernanceDomainRepository` em `DefaultQueryGovernanceService`
- Implementada `ExecuteNativeAsync` real para `GovernanceTeams` e `GovernanceDomains`
- `FROM governance.teams LIMIT 10` retorna equipas reais da BD

### B-07 — InstantiateTemplate
- Corrigido `IsSimulated: true → false` pois o dashboard já era criado realmente

---

## Dependências

```
F-01 depende de: B-01 (para remover IsSimulated=true do PersonaHome) ✅
F-03 depende de: DashboardTemplate backend (já existia) ✅
B-03 depende de: IChatCompletionProvider (já existia — injectado via IAiDashboardComposerService) ✅
B-05 é independente (repositórios já existiam) ✅
```

---

## Ordem de Implementação Recomendada (Sprint 2)

```
Sprint 2 (próximo):
  F-04 (16–24h) → SetupWizard persistência
  B-02 (16–24h) → Widget Delta snapshots reais
  B-04 (16–24h) → Dashboard Live Stream bridge real

Sprint 3+ (demand-driven):
  I-01..04 → Providers Nível B → A
```

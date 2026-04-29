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
| F-04 | SetupWizardPage: adicionar persistência via API (estado do wizard) | `src/frontend/src/features/platform-admin/pages/SetupWizardPage.tsx` | ✅ Concluído | 16–24h |

---

## Grupo B — Backend (IsSimulated=true Estrutural)

| ID | Task | Ficheiro | Estado | Esforço |
|----|------|---------|--------|---------|
| B-01 | GetPersonaHome: injetar IIncidentModule + ICatalogGraphModule; retornar dados reais | `src/modules/governance/…/Features/GetPersonaHome/GetPersonaHome.cs` | ✅ Concluído | 4–8h |
| B-02 | GetWidgetDelta: criar WidgetSnapshot entity + delta real | `src/modules/governance/…/Features/GetWidgetDelta/GetWidgetDelta.cs` | ✅ Concluído | 16–24h |
| B-03 | ComposeAiDashboard: integrar IAiDashboardComposerService (wrapper IChatCompletionProvider) | `src/modules/governance/…/Features/ComposeAiDashboard/ComposeAiDashboard.cs` | ✅ Concluído | 8–12h |
| B-04 | GetDashboardLiveStream: criar IDashboardDataBridge + eventos reais | `src/modules/governance/…/Features/GetDashboardLiveStream/GetDashboardLiveStream.cs` | ✅ Concluído | 16–24h |
| B-05 | NQL Native Execution: implementar query EF Core para GovernanceTeams/Domains | `src/modules/governance/…/Persistence/QueryGovernanceService.cs` | ✅ Concluído | 4–8h |
| B-06 | GetDemoSeedStatus: verificação real do estado (já usa IDemoSeedStateRepository — não era bug) | `src/modules/governance/…/Features/GetDemoSeedStatus/GetDemoSeedStatus.cs` | ✅ Já real | — |
| B-07 | InstantiateTemplate: corrigir IsSimulated: true → false após criação real | `src/modules/governance/…/Features/InstantiateTemplate/InstantiateTemplate.cs` | ✅ Concluído | 1h |

---

## Grupo I — Infraestrutura (Providers Nível B → A)

| ID | Task | Estado | Esforço |
|----|------|--------|---------|
| I-01 | DEG-03: IRuntimeProvider + NullRuntimeProvider + system-health | ✅ Concluído | 16h |
| I-02 | DEG-04: IChaosProvider + NullChaosProvider + system-health | ✅ Concluído | 12h |
| I-03 | DEG-05: ICertificateProvider + NullCertificateProvider + GetMtlsManager integrado | ✅ Concluído | 16h |
| I-04 | DEG-06: ISchemaPlanner + NullSchemaPlanner + system-health | ✅ Concluído | 12h |

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

### F-04 — SetupWizardPage
- Criado domain entity `SetupWizardStep` com `TenantId`, `StepId`, `DataJson`, `CompletedAt`
- Criada interface `ISetupWizardRepository` + implementação EF Core `SetupWizardRepository`
- Registado `DbSet<SetupWizardStep>` em `GovernanceDbContext`
- Criadas features `GetSetupWizardStatus` e `SaveSetupWizardStep`
- Endpoints `GET /api/v1/admin/setup/status` e `POST /api/v1/admin/setup/steps/{stepId}`
- Criado `src/frontend/src/features/platform-admin/api/setupWizard.ts`
- `SetupWizardPage.tsx` reescrita: `useQuery` para carregar estado, `useEffect` para restaurar `formData`, `useMutation` para persistir cada passo

### B-01 — GetPersonaHome
- Injectados `IIncidentModule` e `ICatalogGraphModule` no handler
- Para persona `engineer`: conta serviços reais via `ListAllServicesAsync` e incidentes via `CountOpenIncidentsAsync`
- Para persona `executive`: agrega incidentes + trend + portfolio de serviços
- `IsSimulated: false` quando cross-module responde; fallback gracioso com `IsSimulated: true` em caso de erro

### B-02 — GetWidgetDelta
- Criado domain entity `WidgetSnapshot` (`TenantId`, `DashboardId`, `WidgetId`, `DataHash`, `DataJson`, `CapturedAt`)
- Criada interface `IWidgetSnapshotRepository` + implementação EF Core `WidgetSnapshotRepository`
- Registado `DbSet<WidgetSnapshot>` em `GovernanceDbContext`; registado repositório em DI
- Handler `GetWidgetDelta` reescrito: consulta snapshots reais, compara campos, retorna `IsSimulated: false` quando snapshots existem

### B-03 — ComposeAiDashboard
- Criada interface `IAiDashboardComposerService` em `Governance.Application.Abstractions`
- Implementada `AiDashboardComposerService` em `Governance.Infrastructure.AI` usando `IChatCompletionProvider`
- Handler usa LLM real quando `IsConfigured = true`; fallback keyword-based honesto quando não configurado
- `IsSimulated: false` quando LLM respondeu; `IsSimulated: true` no fallback

### B-04 — GetDashboardLiveStream
- Criada interface `IDashboardDataBridge` em `Governance.Application.Abstractions`
- Criada `NullDashboardDataBridge` (retorna lista vazia — heartbeat honest-gap)
- Criada `SnapshotDashboardDataBridge` em `Governance.Infrastructure.Persistence` que detecta novos snapshots e emite `widget.refresh` com `IsSimulated: false`
- Registada `SnapshotDashboardDataBridge` em DI; endpoint SSE injecta `IDashboardDataBridge` e passa ao `GenerateEventsAsync`
- Heartbeats com `IsSimulated: true` apenas quando sem bridge real (NullDashboardDataBridge)

### B-05 — NQL Native Execution
- Injectados `ITeamRepository` e `IGovernanceDomainRepository` em `DefaultQueryGovernanceService`
- Implementada `ExecuteNativeAsync` real para `GovernanceTeams` e `GovernanceDomains`
- `FROM governance.teams LIMIT 10` retorna equipas reais da BD

### B-07 — InstantiateTemplate
- Corrigido `IsSimulated: true → false` pois o dashboard já era criado realmente

### I-01 — IRuntimeProvider (DEG-03)
- Criada `IRuntimeProvider` com `IsConfigured`, `GetModuleMatrixAsync()`
- Criada `NullRuntimeProvider` (retorna matriz vazia)
- Adicionado `OptionalProviderNames.Runtime = "runtime"`
- Registado em `Integrations.Infrastructure/DependencyInjection.cs`
- Exposto em `GetOptionalProviders.Handler`

### I-02 — IChaosProvider (DEG-04)
- Criada `IChaosProvider` com `IsConfigured`, `SubmitExperimentAsync()`, `ListRunningExperimentsAsync()`
- Criada `NullChaosProvider` (retorna `IsSimulated: true` com nota honesta)
- Adicionado `OptionalProviderNames.Chaos = "chaos"`
- Registado em DI; exposto em `GetOptionalProviders`

### I-03 — ICertificateProvider (DEG-05)
- Criada `ICertificateProvider` com `IsConfigured`, `ListCertificatesAsync()`, `RevokeCertificateAsync()`
- Criada `NullCertificateProvider` (lê `Mtls:CertManagerEndpoint` / `Mtls:VaultPkiEndpoint` para determinar `IsConfigured`)
- `GetMtlsManager.Handler` reescrito para usar `ICertificateProvider`: lista certificados reais quando configurado
- Adicionado `OptionalProviderNames.Certificate = "certificate"`

### I-04 — ISchemaPlanner (DEG-06)
- Criada `ISchemaPlanner` com `IsConfigured`, `PlanSchemaChangesAsync()`, `ApplySchemaChangesAsync()`
- Criada `NullSchemaPlanner` (retorna planos simulados com `IsSimulated: true`)
- Adicionado `OptionalProviderNames.SchemaPlanner = "schemaPlanner"`
- Registado em DI; exposto em `GetOptionalProviders`

---

## Dependências

```
F-01 depende de: B-01 (para remover IsSimulated=true do PersonaHome) ✅
F-03 depende de: DashboardTemplate backend (já existia) ✅
F-04 depende de: novo domain entity + repositório + endpoints ✅
B-02 depende de: WidgetSnapshot entity + repositório ✅
B-03 depende de: IChatCompletionProvider (já existia — injectado via IAiDashboardComposerService) ✅
B-04 depende de: IDashboardDataBridge + SnapshotDashboardDataBridge ✅
B-05 é independente (repositórios já existiam) ✅
I-01..04 dependem de: Integrations.Domain + Integrations.Infrastructure ✅
```

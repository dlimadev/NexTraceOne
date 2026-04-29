# NexTraceOne — Plano de Ação: Resolução de Gaps Identificados

> **Data:** Abril 2026  
> **Baseado em:** [FORENSIC-ANALYSIS-2026-04.md](./FORENSIC-ANALYSIS-2026-04.md)  
> **Contexto:** Os itens abaixo são os gaps concretos encontrados na análise forense. Ordenados por impacto/esforço.

---

## Fase 1 — Correções Críticas Frontend (Semana 1)

Estas correções removem dados falsos que os utilizadores estão a ver. São as mais urgentes.

### TASK-F-01: Corrigir PersonaHomePage — Conectar à API Real

**Ficheiro:** `src/frontend/src/features/governance/pages/PersonaHomePage.tsx`

**O que fazer:**
1. Remover arrays `ENGINEER_STATS`, `TECHLEAD_STATS`, `EXECUTIVE_STATS`, `DEFAULT_STATS`
2. Criar chamada `useQuery` para `GET /api/v1/governance/persona-home?userId=...&persona=...&tenantId=...`
3. Renderizar `data.cards` em vez dos arrays estáticos
4. Mostrar banner `SimulatedNote` quando `data.isSimulated === true`
5. Adicionar estados de loading e erro

**API backend:** `GET /api/v1/governance/persona-home` (endpoint já existe)  
**Contrato de resposta:** `{ persona, userId, cards: HomeCardDto[], quickActions: QuickActionDto[], isSimulated, simulatedNote }`

**Esforço estimado:** 3–5h  
**Critério de aceite:** Stats da página de home vêm da API; banner de simulação visível quando `isSimulated=true`

---

### TASK-F-02: Corrigir DashboardReportsPage — Remover Select Override

**Ficheiro:** `src/frontend/src/features/governance/pages/DashboardReportsPage.tsx`

**O que fazer:**
1. Remover `const SIMULATED_REPORTS: ScheduledReport[]` (linhas ~27–73)
2. Remover `select: () => SIMULATED_REPORTS` da chamada `useQuery`
3. Usar diretamente o resultado da API `reportsApi.getUsageAnalytics()`
4. Verificar que os tipos TypeScript do retorno da API correspondem ao tipo `ScheduledReport`

**Esforço estimado:** 1–2h  
**Critério de aceite:** Relatórios criados pelo utilizador aparecem na UI

---

### TASK-F-03: Corrigir DashboardTemplatesPage — Adicionar Chamada API

**Ficheiro:** `src/frontend/src/features/governance/pages/DashboardTemplatesPage.tsx`

**O que fazer:**
1. Identificar ou criar endpoint backend para listar templates (verificar `GET /api/v1/governance/dashboards/templates`)
2. Se endpoint não existir: criar handler `ListDashboardTemplates` no backend com EF Core
3. Substituir `TEMPLATES` hardcoded por `useQuery` com dados da API
4. Manter `TEMPLATES` como fallback/seed se API retornar vazio

**Nota:** Se o endpoint backend não existir ainda, criar também:
- `DashboardTemplate` entity + repositório + migração
- Handler `ListDashboardTemplates` + endpoint
- Seeds iniciais com os templates actuais

**Esforço estimado:** 4–8h (frontend apenas) ou 12–20h (com criação de backend)  
**Critério de aceite:** Templates vêm da API; admins podem adicionar novos templates

---

## Fase 2 — Correções Backend: Remover `IsSimulated=true` Estrutural (Semana 2–3)

### TASK-B-01: GetPersonaHome — Agregar Dados Cross-Module Reais

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetPersonaHome/GetPersonaHome.cs`

**O que fazer:**
1. Injetar `IIncidentModule`, `ICatalogGraphModule`, `IReliabilityModule` no handler
2. Para persona `engineer`: buscar serviços reais (via `ICatalogGraphModule.GetServiceCountForUser`), incidentes abertos (via `IIncidentModule.GetOpenIncidentCount`), SLO status (via `IReliabilityModule.GetSloSummary`)
3. Para persona `executive`: buscar métricas executivas reais
4. Alterar `IsSimulated: false` quando dados reais estiverem disponíveis
5. Manter `IsSimulated: true` apenas como fallback quando cross-module falha

**Critério de aceite:** `IsSimulated: false` para personas com dados; valores reais nos cards

---

### TASK-B-05: NQL Native Execution — Implementar Query Real para GovernanceTeams/Domains

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/QueryGovernanceService.cs`

**O que fazer:**
1. Injetar `ITeamRepository` e `IDomainRepository` no `DefaultQueryGovernanceService`
2. Implementar `ExecuteNativeAsync` para:
   - `NqlEntity.GovernanceTeams`: query EF Core em `gov_teams` com suporte a WHERE/ORDER BY/LIMIT
   - `NqlEntity.GovernanceDomains`: query EF Core em `gov_domains` com mesmo suporte
3. Mapear resultado para `IReadOnlyList<IReadOnlyList<object?>>` com colunas correctas
4. Remover comentário "real DB execution will be wired in a follow-up"

**Esforço estimado:** 4–8h  
**Critério de aceite:** `FROM governance.teams LIMIT 10` retorna equipas reais da BD

---

### TASK-B-03: ComposeAiDashboard — Integrar com IChatCompletionProvider

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/ComposeAiDashboard/ComposeAiDashboard.cs`

**O que fazer:**
1. Injetar `IChatCompletionProvider` no handler
2. Construir prompt com contexto (persona, description, available widgets)
3. Chamar `CompleteAsync()` para gerar proposta estruturada
4. Parsear resposta LLM para `DashboardProposalDto`
5. Manter fallback keyword-based quando LLM não configurado (`IsSimulated: true` nesse caso)
6. `IsSimulated: false` quando LLM real foi usado

**Pré-requisito:** `IChatCompletionProvider` já existe e está funcional  
**Esforço estimado:** 8–12h  
**Critério de aceite:** Composição por IA real quando provider configurado

---

### TASK-B-06: GetDemoSeedStatus — Verificação Real do Estado

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetDemoSeedStatus/GetDemoSeedStatus.cs`

**O que fazer:**
1. Verificar estado real da seed via contagem de registos em tabelas chave (teams, services, contracts)
2. `IsSeeded: true` quando há dados relevantes presentes
3. Adicionar data da última seed se disponível

**Esforço estimado:** 2–3h

---

## Fase 3 — SetupWizard com Persistência Real (Semana 3–4)

### TASK-F-04: SetupWizardPage — Persistência de Configuração

**Ficheiro:** `src/frontend/src/features/platform-admin/pages/SetupWizardPage.tsx`

**O que fazer:**
1. Criar endpoint `GET /api/v1/platform/setup/status` — retorna estado actual do wizard (steps completados)
2. Criar endpoint `POST /api/v1/platform/setup/steps/{stepId}` — persiste configuração de cada step
3. No frontend, carregar estado inicial com `useQuery`
4. A cada step "Next" fazer `useMutation` para persistir
5. Indicar visualmente steps já completados quando admin volta ao wizard

**Backend necessário:**
- `SetupWizardStep` entity (opcional) ou usar `IConfigurationResolutionService` para persistir
- Handler `GetSetupWizardStatus` + `SaveSetupWizardStep`
- Endpoint registado em `PlatformStatusEndpointModule`

**Esforço estimado:** 16–24h (frontend + backend)  
**Critério de aceite:** Estado do wizard persiste entre sessões; admin pode retomar onde parou

---

## Fase 4 — Dashboard Live Stream (Semana 4–6)

### TASK-B-04: GetDashboardLiveStream — Conectar a Fonte de Dados Real

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetDashboardLiveStream/GetDashboardLiveStream.cs`

**O que fazer:**
1. Criar abstração `IDashboardDataBridge` com métodos para subscrever a updates de widgets
2. Implementar `NullDashboardDataBridge` (comportamento atual — simulated events)
3. Implementar `InternalEventBusDashboardBridge` que escuta eventos do `IEventBus` interno
4. Quando bridge real disponível: emitir `IsSimulated: false` nos eventos
5. Registar no `/admin/system-health` como provider opcional

**Esforço estimado:** 16–24h  
**Critério de aceite:** Eventos reais de mudança/incidente aparecem no SSE stream

---

### TASK-B-02: GetWidgetDelta — Snapshots Persistidos

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetWidgetDelta/GetWidgetDelta.cs`

**O que fazer:**
1. Criar entity `WidgetSnapshot` com `WidgetId`, `DashboardId`, `TenantId`, `CapturedAt`, `DataHash`, `DataJson`
2. Migração para tabela `gov_widget_snapshots`
3. Endpoint/job que captura snapshots periodicamente
4. Handler `GetWidgetDelta` compara snapshots temporais reais
5. `IsSimulated: false` quando snapshots existem

**Esforço estimado:** 16–24h  
**Critério de aceite:** Delta real entre snapshots de widget

---

## Fase 5 — Providers Opcionais: Nível B → A (Semana 6–12, Prioritizar por Demand)

Promover apenas quando houver clientes a pedir explicitamente. Custo alto, impacto baixo sem uso.

### TASK-I-01: DEG-03 — IRuntimeProvider

**O que fazer:**
1. Criar interface `IRuntimeProvider` com `IsConfigured`, `GetModuleMatrixAsync()`
2. `NullRuntimeProvider` (comportamento atual)
3. Registar em `OptionalProviderNames.Runtime`
4. Exposição em `/admin/system-health`

### TASK-I-02: DEG-04 — IChaosProvider

**O que fazer:**
1. Criar interface `IChaosProvider` com `IsConfigured`, `SubmitExperimentAsync()`
2. `NullChaosProvider` (comportamento atual)
3. Registar em `OptionalProviderNames.Chaos`

### TASK-I-03: DEG-05 — ICertificateProvider (mTLS)

**O que fazer:**
1. Criar interface `ICertificateProvider` com `IsConfigured`, `ListCertificatesAsync()`, `RevokeCertificateAsync()`
2. `NullCertificateProvider` (lê de `IConfiguration` — comportamento atual)
3. `CertManagerCertificateProvider` para integração com cert-manager Kubernetes

---

## Fase 6 — InstantiateTemplate: Criação Real de Dashboard (Semana 4–6)

### TASK-B-07: InstantiateTemplate — Criar Dashboard Real

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/InstantiateTemplate/InstantiateTemplate.cs`

**O que fazer:**
1. Verificar se existe `DashboardDefinition` entity para persistir dashboards criados
2. Se não existir: criar entity + repositório + migração
3. Handler cria instância real do dashboard a partir do template
4. `IsSimulated: false` no resultado
5. Retornar ID do dashboard criado para redirect na UI

**Esforço estimado:** 8–16h

---

## Resumo de Prioridades

```
Prioridade 1 (Semana 1) — Remove dados falsos imediatamente visíveis:
  ✅ TASK-F-01: PersonaHomePage → conectar API
  ✅ TASK-F-02: DashboardReportsPage → remover select override
  ✅ TASK-B-06: GetDemoSeedStatus → verificação real

Prioridade 2 (Semana 2–3) — Backend simulation removal:
  ✅ TASK-B-05: NQL Native Execution → implementar query real
  ✅ TASK-B-01: GetPersonaHome → cross-module data bridges
  ✅ TASK-F-03: DashboardTemplatesPage → API real

Prioridade 3 (Semana 3–5) — Funcionalidades incompletas:
  ✅ TASK-F-04: SetupWizard → persistência
  ✅ TASK-B-03: ComposeAiDashboard → LLM real
  ✅ TASK-B-07: InstantiateTemplate → criação real

Prioridade 4 (Semana 6+) — Infraestrutura avançada:
  ✅ TASK-B-04: Dashboard Live Stream → data bridge
  ✅ TASK-B-02: Widget Delta → snapshots reais
  ✅ TASK-I-01..03: Providers Nível B → A
```

---

## Critérios de Aceite Globais

Quando todas as tarefas estiverem concluídas:

1. Nenhuma página frontend com dados `hardcoded` que ignorem a API
2. Zero `select: () => MOCK_DATA` em chamadas `useQuery` de produção
3. `grep -r "IsSimulated.*true" src/modules --include="*.cs"` → apenas handlers com `IsConfigured` false (providers opcionais) e handlers de SSE/delta com nota honesta
4. `FROM governance.teams LIMIT 10` via NQL retorna dados reais
5. PersonaHomePage mostra contagens reais por persona
6. SetupWizard persiste estado entre sessões

---

*Ver análise detalhada: [FORENSIC-ANALYSIS-2026-04.md](./FORENSIC-ANALYSIS-2026-04.md)*  
*Ver roadmap de evolução futura: [FUTURE-ROADMAP.md](./FUTURE-ROADMAP.md)*  
*Ver gaps declarados: [HONEST-GAPS.md](./HONEST-GAPS.md)*

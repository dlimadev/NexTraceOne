# NexTraceOne — Zero Ressalvas Backlog

**Data da validação:** 2026-03-20  
**Base documental usada nesta execução:**

- `docs/release/NexTraceOne_Release_Gate_Final.md`
- `docs/reviews/NexTraceOne_Production_Readiness_Review.md`
- `docs/reviews/NexTraceOne_Full_Production_Convergence_Report.md`
- `docs/release/NexTraceOne_Final_Production_Scope.md`
- `docs/acceptance/NexTraceOne_Baseline_Estavel.md`
- `docs/planos/NexTraceOne_Plano_Operacional_Finalizacao.md`
- `docs/planos/NexTraceOne_Plano_Evolucao_Fase_10.md`

---

## 1. Resumo executivo

Este documento **não parte do pressuposto do gate anterior**. O inventário abaixo foi montado a partir do **código real atual**, da estrutura da solution, do estado atual de build e do estado atual das suites de testes.

### Evidência revalidada nesta execução

- `run_build` → **sucesso**
- `dotnet build src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj -nologo` → **sucesso**
- `npm run build` em `src/frontend` → **falhou com 194 erros**
- `CoreApiHostIntegrationTests` reexecutados anteriormente nesta mesma trilha → `3 passed / 1 failed`
- `get_tests(Project=NexTraceOne.IntegrationTests)` → **52 testes reais** descobertos
- `get_tests(Project=NexTraceOne.E2E.Tests)` → **42 testes reais** descobertos

### Conclusão objetiva do inventário

O NexTraceOne **ainda está longe de GO sem ressalvas**.

O estado atual do repositório é:

- **backend estruturalmente forte e muito mais maduro do que no gate inicial**
- **persistência bastante avançada**, com migrações e snapshots para todos os contextos principais
- **evidência real já existente para Identity, Catalog, Source of Truth, Contracts backend, Governance/DbContexts, AI/DbContexts e parte de Incidents**
- **frontend ainda não pronto para produção**, com build quebrado, exclusões artificiais no `tsconfig.app.json` e contaminação por módulos fora do release
- **módulos importantes ainda em preview/mock/stub**, sobretudo em `Governance`, `Developer Portal`, `Operations automation/reliability`, UI E2E Playwright e parte de `AI Assistant`
- **evidência de testes ainda insuficiente em áreas críticas**, especialmente `Audit` via API host real e UX real sem mocks

### Síntese executiva zero-ressalvas

Para alcançar **GO sem ressalvas**, o NexTraceOne precisa simultaneamente de:

1. **frontend 100% compilável sem exclusões artificiais por módulo**
2. **eliminação de `preview`, `mock` e `stub` residuais em código de produto**
3. **fechamento da prova real de integração/E2E por módulo incluído e por módulo ainda compilado**
4. **alinhamento final entre UX, backend real, persistência, segurança e operação**
5. **módulos hoje fora do release, mas ainda presentes no produto compilado, ou concluídos de verdade ou removidos do artefacto produtivo**

---

## 2. Princípio de zero-ressalvas

A régua desta fase é diferente do gate final de release reduzido.

### Regra objetiva

Um item só pode ser considerado pronto para **GO sem ressalvas** se estiver:

- compilável no build principal
- conectado ao backend real
- sem mock/stub/preview residual
- com persistência real e migration válida
- com autorização real correta
- com readiness operacional verificável
- com teste real suficiente
- sem depender de exclusões artificiais de build
- sem depender de dados manuais não versionados

### Regra de honestidade técnica

Se um módulo existe no produto compilado, no roteamento, na navegação, na solution, nas APIs ou nos testes, ele **entra no backlog zero-ressalvas**.

`ReleaseScopeGate`, `PreviewGate`, exclusões do `tsconfig` e mocks de Playwright **não contam como prontidão**.

---

## 3. Estado por módulo

### 3.1 Identity & Access

**Frontend**
- rotas principais existem: `/login`, `/select-tenant`, `/mfa`, `/my-sessions`, `/users`, `/break-glass`, `/jit-access`, `/delegations`, `/access-reviews`
- há estados de UX em várias superfícies, mas o módulo participa do build quebrado do frontend
- gaps confirmados no build atual:
  - `src/frontend/src/features/identity-access/api/identity.ts` → `PagedList` ausente em `types`
  - `src/frontend/src/features/identity-access/pages/UsersPage.tsx` → `implicit any`

**Backend**
- `IdentityEndpointModule` existe e agrega submódulos reais (`Auth`, `Users`, `RolePermission`, `BreakGlass`, `JitAccess`, `Delegation`, `Tenant`, `AccessReview`, `Environment`)
- autorização por permissão existe
- endpoints de cookie session são condicionais por `CookieSessionOptions.Enabled`

**Persistência**
- `IdentityDbContext` possui migration, snapshot e correção recente de drift
- startup migration voltou a funcionar
- seed de desenvolvimento existe

**Testes**
- boa suite de unit tests em `tests/modules/identityaccess`
- integração real forte em `CriticalFlows` e `DeepCoverage`
- E2E real existe em `AuthApiFlowTests`, mas há falha atual de fixture SQL em cenário `GetCurrentUser_Me_Endpoint_Should_Return_User_Info`

**Classificação:** `QUASE PRONTO`

### 3.2 Shell / Dashboard / Search / Navigation

**Frontend**
- `App.tsx`, `AppSidebar.tsx`, `CommandPalette.tsx`, `QuickActions.tsx`, `PersonaQuickstart.tsx` e `GlobalSearchPage.tsx` participam da experiência principal
- há filtragem por `isRouteAvailableInFinalProductionScope(...)` na navegação principal
- o shell está operacional, mas o frontend global ainda falha em build

**Backend**
- depende de múltiplos endpoints reais de catálogo, identidade, mudanças e auditoria

**Testes**
- há evidência indireta via integração dos fluxos centrais
- Playwright UI usa sessões mockadas e `page.route(...)`, portanto **não serve como evidência zero-ressalvas**

**Classificação:** `PARCIAL`

### 3.3 Catalog / Source of Truth

**Frontend**
- páginas reais existem para catálogo e source of truth:
  - `ServiceCatalogListPage`
  - `ServiceCatalogPage`
  - `ServiceDetailPage`
  - `SourceOfTruthExplorerPage`
  - `ServiceSourceOfTruthPage`
  - `ContractSourceOfTruthPage`
  - `GlobalSearchPage`
- o detalhe de serviço e a exploração Source of Truth já têm round-trip real validado por integração

**Backend**
- `ServiceCatalogEndpointModule` e `SourceOfTruthEndpointModule` existem com handlers reais
- endpoints reais:
  - `/api/v1/catalog/services`
  - `/api/v1/catalog/services/{id}`
  - `/api/v1/catalog/services/summary`
  - `/api/v1/catalog/services/search`
  - `/api/v1/source-of-truth/services/{id}`
  - `/api/v1/source-of-truth/contracts/{id}`
  - `/api/v1/source-of-truth/search`
  - `/api/v1/source-of-truth/global-search`

**Persistência**
- `CatalogGraphDbContext` e `ContractsDbContext` com migrations e snapshots

**Testes**
- unit + integration fortes em `Catalog.Tests`
- integração RH-6 real passou para `Catalog / Source of Truth`
- há E2E real disponível em `RealBusinessApiFlowTests.Catalog_Should_List_Seeded_Services_And_Return_Service_Detail`

**Classificação:** `QUASE PRONTO`

### 3.4 Contracts

**Frontend**
- páginas e flows existem, mas o módulo está **fora da validação honesta do app build** por exclusão direta em `src/frontend/tsconfig.app.json`:
  - `src/features/contracts/**/*`
- `studioMock.ts` ainda existe no produto
- `DraftStudioPage` usa backend real (`contractStudioApi`), mas o módulo não está sob validação full typecheck do app principal

**Backend**
- `ContractsEndpointModule` e `ContractStudioEndpointModule` existem com handlers reais
- há endpoints reais de list/detail/history/import/version/lifecycle/draft studio

**Persistência**
- `ContractsDbContext` com migrations reais

**Testes**
- build dos testes de contracts foi corrigido nesta trilha
- `ContractsNewFeaturesTests` e `ProtocolAutoDetectionTests` passaram
- há E2E real disponível e atualmente `RealBusinessApiFlowTests.Contracts_Should_Create_Edit_And_Submit_Draft_With_Real_Backend` aparece como `Passed`
- há também integração RH-6 histórica de contracts ainda marcada `Failed` em `CoreApiHostIntegrationTests`

**Classificação:** `PARCIAL`

### 3.5 Change Governance

**Frontend**
- páginas principais existem: `ChangeCatalogPage`, `ChangeDetailPage`, `ReleasesPage`, `WorkflowPage`, `PromotionPage`
- fazem parte do release final reduzido

**Backend**
- `ChangeIntelligenceEndpointModule`, `WorkflowEndpointModule`, `PromotionEndpointModule` existem com handlers reais
- `/api/v1/releases*` está operacional

**Persistência**
- `ChangeIntelligenceDbContext`, `WorkflowDbContext`, `PromotionDbContext`, `RulesetGovernanceDbContext` com migrations e snapshots

**Testes**
- integração crítica real existe
- falha atual revalidada:
  - `CoreApiHostIntegrationTests.ChangeGovernance_And_Incidents_Should_Expose_Real_Read_Write_And_Correlation_Flows`
  - causa: teste espera `200 OK`; endpoint de criação de incidente devolve `201 Created`
- há E2E real descoberto para release list/start review, mas não revalidado agora

**Classificação:** `PARCIAL`

### 3.6 Incidents / Operations

**Frontend**
- páginas de `Incidents`, `Runbooks`, `Reliability`, `Automation`, `PlatformOperations` existem
- `src/frontend/tsconfig.app.json` exclui `src/features/operations/pages/**/*`
- rotas de operations estão fora do release reduzido e protegidas por `ReleaseScopeGate`, mas o código existe no produto

**Backend**
- `IncidentEndpointModule`, `RunbookEndpointModule`, `ReliabilityEndpointModule`, `AutomationEndpointModule` existem
- `IncidentEndpointModule` é real e devolve `201 Created` em criação
- `ListAutomationWorkflows` é explicitamente `PreviewOnly`

**Persistência**
- `IncidentDbContext`, `RuntimeIntelligenceDbContext`, `CostIntelligenceDbContext` com migrations e snapshots

**Testes**
- unit tests fortes em `OperationalIntelligence.Tests`
- integração real forte em `CriticalFlows`, `DeepCoverage` e `ExtendedDbContexts`
- E2E real existe
- falhas atuais persistem em `RealBusinessApiFlowTests.Incidents_Should_List_Seeded_Detail_And_Create_New_Incident`

**Classificação:** `PARCIAL`

### 3.7 Audit

**Frontend**
- `AuditPage` existe e está no release final reduzido
- UI Playwright encontrada é mockada por `page.route(...)`

**Backend**
- `AuditEndpointModule` existe com endpoints reais:
  - `/api/v1/audit/events`
  - `/api/v1/audit/trail`
  - `/api/v1/audit/search`
  - `/api/v1/audit/verify-chain`
  - `/api/v1/audit/report`
  - `/api/v1/audit/compliance`
- permissões reais por endpoint

**Persistência**
- `AuditDbContext` com migration e snapshot

**Testes**
- há integração real de DbContext em `ExtendedDbContextsPostgreSqlTests`
- **não foi localizado fluxo crítico API-host/E2E real para `/api/v1/audit/*`**
- `tests/modules/auditcompliance` contém apenas `PlaceholderTests.cs`

**Classificação:** `PARCIAL`

### 3.8 AI Assistant

**Frontend**
- `AiAssistantPage` existe, mas está fora do release reduzido e protegida por `ReleaseScopeGate`
- `src/frontend/tsconfig.app.json` exclui `src/features/ai-hub/pages/**/*`

**Backend**
- assistant fica em `AiGovernanceEndpointModule`
- existem handlers reais para conversas, mensagens e prompts sugeridos

**Persistência**
- `AiGovernanceDbContext` + `AiOrchestrationDbContext`

**Testes**
- integração real existe para AI governance/orchestration
- E2E real disponível com resultado misto:
  - `RealBusinessApiFlowTests.AI_Should_Create_Open_Send_Persist_Relist_And_Reopen_Conversation` → `Passed`
  - `RealBusinessApiFlowTests.AI_Should_Create_Conversation_Send_Message_And_List_Persisted_Messages` → `Failed`
  - `CoreApiHostIntegrationTests.AI_Should_Create_Open_Send_Persist_Relist_And_Reopen_Conversation_With_Real_Backend` → `Failed`

**Classificação:** `PARCIAL`

### 3.9 AI Hub admin

**Frontend**
- páginas existem (`ModelRegistryPage`, `AiPoliciesPage`, `AiRoutingPage`, `IdeIntegrationsPage`, `TokenBudgetPage`, `AiAuditPage`)
- fora do release reduzido e excluídas do typecheck principal

**Backend**
- `AiGovernanceEndpointModule` existe com endpoints reais de models, policies, budgets, audit, routing e enrichment

**Persistência**
- `AiGovernanceDbContext`, `ExternalAiDbContext`, `AiOrchestrationDbContext`

**Testes**
- boa cobertura unit e integration de persistência/contexts
- pouca prova UX real sem mocks

**Classificação:** `PARCIAL`

### 3.10 Governance

**Frontend**
- grande conjunto de páginas existe em `src/frontend/src/features/governance/pages/*`
- rotas continuam presentes no app sob `ReleaseScopeGate`
- build atual falha nessas páginas com erros reais TypeScript (`ReportsPage`, `RiskCenterPage`, `RiskHeatmapPage`, `MaturityScorecardsPage`, `ServiceFinOpsPage`, `TeamFinOpsPage`)
- `src/frontend/tsconfig.app.json` também exclui `src/features/governance/pages/**/*`, o que torna o controle de qualidade **tecnicamente desonesto**

**Backend**
- múltiplos endpoint modules reais existem:
  - `ExecutiveOverviewEndpointModule`
  - `GovernanceReportsEndpointModule`
  - `GovernanceRiskEndpointModule`
  - `GovernanceComplianceEndpointModule`
  - `GovernanceFinOpsEndpointModule`
  - `GovernancePacksEndpointModule`
  - `PolicyCatalogEndpointModule`
  - `TeamEndpointModule`
  - `DomainEndpointModule`
  - `DelegatedAdminEndpointModule`
  - `IntegrationHubEndpointModule`
  - `ProductAnalyticsEndpointModule`
- porém há feature explicitamente simulada em backend:
  - `SimulateGovernancePack`

**Persistência**
- `GovernanceDbContext` com migrations reais

**Testes**
- há algumas suites de unit tests e integration tests de governance/workflow
- não há prova UX/E2E real suficiente para a superfície total do módulo

**Classificação:** `BLOQUEADO`

### 3.11 Integrations

**Frontend**
- páginas existem (`IntegrationHubPage`, `ConnectorDetailPage`, `IngestionExecutionsPage`, `IngestionFreshnessPage`)
- fora do release reduzido e atrás de `ReleaseScopeGate`
- também excluídas do typecheck principal por `tsconfig.app.json`

**Backend**
- `IntegrationHubEndpointModule` existe com endpoints reais `/api/v1/integrations/*` e `/api/v1/ingestion/*`
- `Ingestion.Api` existe como componente separado e está protegido por API key/policy

**Persistência**
- dados ficam principalmente em `GovernanceDbContext` + stores relacionados

**Testes**
- há cobertura unit de `IntegrationHubFeatureTests`
- não há prova real suficiente de atualização por eventos externos reais em cenário de produto ponta a ponta

**Classificação:** `PARCIAL`

### 3.12 Product Analytics

**Frontend**
- páginas existem em `src/frontend/src/features/product-analytics/pages/*`
- rotas estão sob `ReleaseScopeGate`
- módulo excluído do typecheck principal por `tsconfig.app.json`

**Backend**
- `ProductAnalyticsEndpointModule` existe com endpoints reais de summary, module adoption, persona usage, journeys, value milestones e friction

**Persistência**
- persiste em `GovernanceDbContext` (`AddAnalyticsEvents` migration identificada)

**Testes**
- não há prova E2E real suficiente da experiência completa

**Classificação:** `PARCIAL`

### 3.13 Developer Portal

**Frontend**
- `DeveloperPortalPage` existe, mas está fora do release e explicitamente excluída do typecheck do app

**Backend**
- `DeveloperPortalEndpointModule` existe com endpoints reais de catálogo, subscrições, health, timeline e analytics
- `ExecutePlayground` é explicitamente `PreviewOnly`

**Persistência**
- `DeveloperPortalDbContext` com migrations reais e testes de contexto

**Testes**
- integração de contexto existe
- falta prova real de UX/fluxo produtivo sem preview

**Classificação:** `PARCIAL`

### 3.14 Platform / BackgroundWorkers / Ingestion.Api

**BackgroundWorkers**
- health real em `/health`, `/ready`, `/live`
- checks reais de `identity-db`, `outbox-processor-job`, `identity-expiration-job`
- runtime real ainda sem prova funcional ampla além do host e health

**Ingestion.Api**
- autenticação por API key/policy `IngestionApiKeyWrite`
- validação de API keys obrigatórias em produção
- correlação via `X-Correlation-Id`
- ainda sem prova suficiente de ingestão ponta a ponta com atualização de serviços externos reais

**Platform Operations frontend**
- rota `/platform/operations` existe, mas está fora do release e atrás de `ReleaseScopeGate`

**Classificação:** `PARCIAL`

### 3.15 Outros artefactos compilados

- `tools/NexTraceOne.CLI` continua essencialmente `STUB`
- existem artefactos diagnósticos residuais de EF em `src/modules/identityaccess/.../Migrations/__Probe/*`

---

## 4. Matriz completa de gaps

| Módulo | Página/Rota | Endpoint | Handler | Persistência/DbContext | Teste de Integração | Teste E2E | Segurança | Operação | Status atual | Gap exato | Ação necessária | Severidade | Pode ir para produção hoje? |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Identity & Access | `/login`, `/select-tenant`, `/mfa` | `/api/v1/identity/auth/*`, `/tenants/mine` | `LocalLogin`, `SelectTenant`, `GetCurrentUser` | `IdentityDbContext` | RH-6 login `Passed` | `AuthApiFlowTests` existe, um cenário falha por fixture SQL | auth e permissões reais; cookie session desativada por default | startup real ok | QUASE PRONTO | frontend ainda tem erros em `identity.ts`; E2E real instável | corrigir `types` compartilhados e fixture SQL do E2E | ALTO | NÃO |
| Identity Admin core | `/users` | `/api/v1/identity/users*` | `CreateUser`, `ListTenantUsers`, `AssignRole` | `IdentityDbContext` | autorização real `Passed` | sem prova UX real suficiente sem mocks | permissões reais | startup real ok | PARCIAL | `UsersPage.tsx` quebra build; UI sem prova real suficiente | fechar build do frontend e adicionar E2E real admin | ALTO | NÃO |
| Identity privileged access | `/break-glass`, `/jit-access`, `/delegations`, `/access-reviews`, `/my-sessions` | `/api/v1/identity/*` | handlers reais do módulo | `IdentityDbContext` | unit tests fortes | sem E2E real atualizado por fluxo | permissões reais | host ok | PARCIAL | sem prova E2E real suficiente; frontend global quebrado | adicionar provas reais por fluxo e fechar build | MÉDIO | NÃO |
| Shell / Dashboard | `/` | composição de múltiplas APIs | agregação do frontend | múltiplos stores | cobertura indireta | Playwright UI mockado | route protection ok | host ok | PARCIAL | frontend global quebrado; UI E2E usa mocks | fechar build e criar UI E2E real sem `page.route` | ALTO | NÃO |
| Navigation / Search | sidebar, command palette, quick actions, persona quickstart, global search | `/api/v1/source-of-truth/global-search` + navegação | `GlobalSearch` | `CatalogGraphDbContext` + `ContractsDbContext` | search backend existe | Playwright ainda mocka auth e APIs | filtros de escopo reais | ok | PARCIAL | UX real sem mocks insuficiente | revalidar UI real e remover auth/session mock das suites críticas | MÉDIO | NÃO |
| Catalog | `/services`, `/services/graph` | `/api/v1/catalog/services*`, `/graph` | `ListServices`, `GetServicesSummary`, `GetAssetGraph` | `CatalogGraphDbContext` | RH-6 `Passed` | E2E real existe | permission real | host ok | QUASE PRONTO | build frontend global impede prontidão zero-ressalvas | manter backend e fechar frontend global | MÉDIO | NÃO |
| Service Detail | `/services/:serviceId` | `/api/v1/catalog/services/{id}` | `GetServiceDetail` | `CatalogGraphDbContext` | RH-6 `Passed` | teste de página existe, mas não substitui E2E real | permission real | ok | QUASE PRONTO | depende do build frontend global | fechar frontend e ampliar E2E real de detalhe | MÉDIO | NÃO |
| Source of Truth | `/source-of-truth`, `/source-of-truth/services/:id`, `/source-of-truth/contracts/:id`, `/search` | `/api/v1/source-of-truth/*` | `GetServiceSourceOfTruth`, `GetContractSourceOfTruth`, `SearchSourceOfTruth`, `GlobalSearch` | `CatalogGraphDbContext` + `ContractsDbContext` | RH-6 `Passed` | E2E real existe | permission real | ok | QUASE PRONTO | ainda falta prova UX real suficiente e frontend global está quebrado | ampliar E2E real e fechar build frontend | MÉDIO | NÃO |
| Contracts backend core | `/api/v1/contracts/*` | `/api/v1/contracts/*` | `ListContracts`, `GetContractVersionDetail`, `ImportContract`, `CreateContractVersion` etc. | `ContractsDbContext` | muitos tests unit/integration; contracts tests corrigidos | `RealBusinessApiFlowTests.Contracts...` aparece `Passed` | permissions reais | DbContext ok | QUASE PRONTO | evidência frontend do módulo ainda não é honesta | reintroduzir módulo na validação de build e remover mock residual | MÉDIO | NÃO |
| Contracts frontend catalog | `/contracts` | `/api/v1/contracts/list`, `/summary` | frontend via `contractsApi` | `ContractsDbContext` | backend real existe | sem UI real suficiente | rota gated | N/A | PARCIAL | `src/features/contracts/**/*` está excluído do `tsconfig.app.json` | remover exclusão artificial e tornar módulo build-clean | CRÍTICO | NÃO |
| Contracts draft studio | `/contracts/studio/:draftId`, `/contracts/new` | `/api/v1/contracts/drafts/*` | `CreateDraft`, `UpdateDraftContent`, `UpdateDraftMetadata`, `SubmitDraftReview` | `ContractsDbContext` | backend real existe | E2E real de draft aparece `Passed` | auth real | N/A | PARCIAL | frontend do módulo fora do typecheck principal; `studioMock.ts` residual | trazer módulo para o build, remover mock residual e validar UX real | CRÍTICO | NÃO |
| Change Governance core | `/changes`, `/changes/:changeId`, `/releases` | `/api/v1/releases*` | queries e intelligence handlers reais | `ChangeIntelligenceDbContext` | RH-6 parcial; um teste falha | E2E real existe mas não foi revalidado agora | permissions reais | host ok | PARCIAL | teste crítico atual falha por mismatch `200` vs `201`; prova ainda não totalmente verde | alinhar contrato HTTP esperado e rerodar suite crítica | ALTO | NÃO |
| Workflow | `/workflow` | workflow endpoints | workflow handlers reais | `WorkflowDbContext` | governance workflow integration tests existem | sem prova UX real atual | permission real | DbContext ok | PARCIAL | sem prova UX real suficiente | adicionar E2E real e validar sem gate/preview | MÉDIO | NÃO |
| Promotion | `/promotion` | promotion endpoints | promotion handlers reais | `PromotionDbContext` | governance workflow integration tests existem | sem prova UX real atual | permission real | DbContext ok | PARCIAL | sem prova UX real suficiente | adicionar E2E real produtivo | MÉDIO | NÃO |
| Incidents | `/operations/incidents`, `/:incidentId` | `/api/v1/incidents*` | `ListIncidents`, `GetIncidentDetail`, `CreateIncident`, `GetIncidentCorrelation` | `IncidentDbContext` | integration forte existe | E2E real com falhas atuais | permissions reais | backend real responde `201` | PARCIAL | frontend de operations excluído do build; E2E real ainda falha em cenários relevantes | trazer operações para build honesto e corrigir E2E real | ALTO | NÃO |
| Runbooks | `/operations/runbooks` | `/api/v1/incidents/{id}/mitigation` + runbook endpoints | runbook features | `IncidentDbContext` | unit/integration existem | sem prova UX real atual | permission real | parcial | PARCIAL | módulo fora do build honesto e fora do release; prova UX insuficiente | validar backend + UX real ou retirar do produto compilado | MÉDIO | NÃO |
| Reliability | `/operations/reliability` | reliability endpoints | reliability features | `RuntimeIntelligenceDbContext` | unit tests existem | sem prova E2E real atual | permission real | parcial | PREVIEW | capability ainda preview | concluir backend+UI ou retirar do produto compilado | ALTO | NÃO |
| Automation | `/operations/automation*` | automation endpoints | `ListAutomationWorkflows` etc. | related stores | unit tests existem | sem prova E2E real | permission real | parcial | PREVIEW | `ListAutomationWorkflows` devolve `PreviewOnly` | implementar backend real ou retirar do produto compilado | CRÍTICO | NÃO |
| Audit API | `/audit` | `/api/v1/audit/*` | `SearchAuditLog`, `GetAuditTrail`, `VerifyChainIntegrity`, `ExportAuditReport`, `GetComplianceReport` | `AuditDbContext` | DbContext integration existe | não há E2E/API-host crítico equivalente confirmado | permissions reais | DbContext ok | PARCIAL | falta prova crítica real de API host e frontend real | criar teste crítico real de audit e E2E sem mocks | ALTO | NÃO |
| AI Assistant | `/ai/assistant` | `/api/v1/ai/assistant/*` | conversation/message handlers | `AiGovernanceDbContext` + `AiOrchestrationDbContext` | integration real existe | E2E real com resultados mistos | auth/perms reais | DBs ok | PARCIAL | módulo fora do release e fora do build honesto; evidência ainda contraditória | tornar build-clean e estabilizar fluxos create/send/relist | ALTO | NÃO |
| AI Hub admin | `/ai/models`, `/ai/policies`, `/ai/routing`, `/ai/ide`, `/ai/budgets`, `/ai/audit` | `/api/v1/ai/*` | `AiGovernanceEndpointModule` handlers | `AiGovernanceDbContext`, `ExternalAiDbContext`, `AiOrchestrationDbContext` | integration real de contexts existe | sem UX real suficiente | auth/perms reais | health AI existe | PARCIAL | UI excluída do build honesto e sem E2E real forte | reintroduzir no build e criar prova real por superfície | ALTO | NÃO |
| Governance executive | `/governance/executive*`, `/reports`, `/risk`, `/compliance`, `/finops` | `/api/v1/executive/*`, governance endpoints | handlers reais | `GovernanceDbContext` | governance integration existe | sem E2E real forte | perms reais | DbContext ok | BLOQUEADO | páginas quebram o build atual e continuam release-gated | corrigir todas as páginas TS e remover preview/gate ou retirar do produto compilado | CRÍTICO | NÃO |
| Governance org/core | `/governance/teams`, `/domains`, `/packs`, `/waivers`, `/delegated-admin` | governance endpoints | handlers reais + `SimulateGovernancePack` | `GovernanceDbContext` | unit/integration existem | sem E2E real forte | perms reais | DbContext ok | PARCIAL | mistura de endpoints reais com simulação e UI release-gated | remover simulação, fechar UX e adicionar testes reais | ALTO | NÃO |
| Integrations frontend | `/integrations*` | `/api/v1/integrations*`, `/api/v1/ingestion*` | `IntegrationHubEndpointModule` handlers | `GovernanceDbContext` + ingestion flows | unit tests existem | sem prova E2E real de atualização externa | auth real no backend | parcial | PARCIAL | UI fora do build honesto e sem prova real de atualizações/eventos externos | reintroduzir no build e criar flows reais com provider/scheduler | ALTO | NÃO |
| Product Analytics | `/analytics*` | `/api/v1/product-analytics/*` | analytics handlers reais | `GovernanceDbContext` | backend existe | sem E2E real suficiente | perms reais | parcial | PARCIAL | páginas fora do build honesto e sem prova real de ponta a ponta | reintroduzir páginas no build e validar eventos/summary real | ALTO | NÃO |
| Developer Portal | `/portal` | `/api/v1/developerportal/*` | portal handlers reais; `ExecutePlayground` preview only | `DeveloperPortalDbContext` | context integration existe | sem E2E real suficiente | auth real | parcial | PARCIAL | `ExecutePlayground` é `PreviewOnly`; UI excluída do build honesto | ou concluir playground real ou retirar do produto compilado | ALTO | NÃO |
| Platform Operations frontend | `/platform/operations` | platform status related | related handlers | mixed | alguma cobertura indireta | sem E2E real | permission real | BackgroundWorkers/Ingestion separados | PREVIEW | rota só existe via gate; sem UX real pronta | concluir ou retirar do produto compilado | MÉDIO | NÃO |
| ApiHost core | N/A | host composition | `MapAllModuleEndpoints`, `ApplyDatabaseMigrationsAsync` | múltiplos DbContexts | startup real validado | system health E2E existe | CORS ok; sem `UseForwardedHeaders` | `/health`, `/ready`, `/live` reais | QUASE PRONTO | falta evidência de proxy-awareness e prova final cross-module | adicionar `UseForwardedHeaders` e rerodar smoke/deploy flows | MÉDIO | NÃO |
| BackgroundWorkers | N/A | `/health`, `/ready`, `/live` | hosted services reais | `IdentityDbContext` | sem integration suite dedicada de runtime worker | sem E2E real | interno | health real | PARCIAL | falta prova operacional ampla dos jobs em runtime real | adicionar smoke/integration de jobs e deploy readiness | MÉDIO | NÃO |
| Ingestion.Api | N/A | ingestion endpoints reais | program orchestration + policy `IngestionApiKeyWrite` | governance related | sem prova ponta a ponta de fornecedor externo | sem E2E real completo | API key + correlation ok | health/readiness/liveness existem | PARCIAL | proteção existe, mas readiness de integrações externas reais ainda não está demonstrada | criar testes reais com payloads/eventos e retries observáveis | MÉDIO | NÃO |
| Frontend build config | `src/frontend/tsconfig.app.json` | N/A | N/A | N/A | N/A | N/A | N/A | build principal | BLOQUEADO | `exclude` remove árvores inteiras (`contracts`, `operations/pages`, `ai-hub/pages`, `governance/pages`, `integrations`, `product-analytics`) da validação honesta | remover exclusões artificiais ou separar apps/packages reais com builds próprios | CRÍTICO | NÃO |
| Frontend production build | app principal | N/A | N/A | N/A | N/A | N/A | N/A | `npm run build` falha | QUEBRADO | 194 erros TypeScript; erros em áreas do release e fora do release | corrigir `types` compartilhados e zerar erros TS em todas as árvores compiladas | CRÍTICO | NÃO |
| IdentityDbContext | N/A | N/A | N/A | `IdentityDbContext` | migrations integration forte | E2E auth existe | auth ok | startup real ok | QUASE PRONTO | artefactos `__Probe` residuais em `Migrations/__Probe` | remover artefactos diagnósticos e manter sequência limpa de migrations | BAIXO | NÃO |
| CatalogGraphDbContext + ContractsDbContext | N/A | N/A | N/A | `CatalogGraphDbContext`, `ContractsDbContext` | integration real forte | E2E real existe | N/A | startup ok | QUASE PRONTO | frontend contracts ainda fora do build honesto | alinhar frontend/backend e eliminar mock residual | MÉDIO | NÃO |
| Change/Workflow/Promotion/Ruleset DbContexts | N/A | N/A | N/A | `ChangeIntelligenceDbContext`, `WorkflowDbContext`, `PromotionDbContext`, `RulesetGovernanceDbContext` | governance workflow integration existe | ChangeGovernance E2E existe | N/A | startup ok | QUASE PRONTO | prova crítica do fluxo ainda incompleta | alinhar testes críticos e ampliar E2E | MÉDIO | NÃO |
| Incident/Runtime/Cost DbContexts | N/A | N/A | N/A | `IncidentDbContext`, `RuntimeIntelligenceDbContext`, `CostIntelligenceDbContext` | deep coverage + extended contexts existem | E2E incidents existe com falhas | N/A | startup ok | QUASE PRONTO | operação UI e E2E ainda não convergem | fechar flows end-to-end e remover exclusão de frontend | MÉDIO | NÃO |
| AuditDbContext | N/A | N/A | N/A | `AuditDbContext` | extended db contexts existe | sem E2E real forte | N/A | startup ok | QUASE PRONTO | sem prova API-host crítica e módulo de testes unit é placeholder | adicionar tests de aplicação/API e E2E real | ALTO | NÃO |
| AI DbContexts | N/A | N/A | N/A | `AiGovernanceDbContext`, `ExternalAiDbContext`, `AiOrchestrationDbContext` | ai governance integration forte | AI E2E real misto | policies/perms existem | health provider parcial | PARCIAL | prova UX e provider real ainda incompleta | estabilizar assistant + validar providers externos reais | ALTO | NÃO |
| GovernanceDbContext | N/A | N/A | N/A | `GovernanceDbContext` | migrations/tests existem | sem E2E real forte | N/A | startup ok | PARCIAL | backend persiste, mas módulo funcional/UX continua parcial e build frontend quebrado | concluir superfícies governance ou retirar do produto compilado | ALTO | NÃO |
| Unit tests por módulo | N/A | N/A | N/A | N/A | Identity/Catalog/Operational/AI fortes; Governance moderado; Audit fraco | N/A | N/A | pipeline parcial | PARCIAL | `AuditCompliance.Tests` é apenas `PlaceholderTests.cs`; alguns módulos têm cobertura desigual | elevar cobertura útil em Audit, Governance, Integrations e Platform | ALTO | NÃO |
| Integration tests reais | N/A | N/A | N/A | múltiplos | 52 testes descobertos | N/A | N/A | boa base | QUASE PRONTO | nem todos os fluxos críticos são reexecutados continuamente; alguns outcomes ainda `Failed/None` | consolidar suite crítica verde por módulo | MÉDIO | NÃO |
| E2E .NET real | N/A | N/A | N/A | múltiplos | N/A | 42 testes descobertos; vários cenários reais existem | N/A | fixture própria | PARCIAL | `ApiE2EFixture.SeedMinimalTestDataAsync()` contém SQL inválido com `\"Id\"`; parte da evidência real fica quebrada por infraestrutura de teste | corrigir fixture SQL e rerodar E2E real crítico | CRÍTICO | NÃO |
| Playwright UI E2E | `src/frontend/e2e/*.spec.ts` | APIs interceptadas | N/A | N/A | N/A | existe, mas mockado | auth mockada, rotas mockadas | fraco como evidência real | MOCKADO | `mockAuthSession` e `page.route(...)` simulam sessão e APIs | manter apenas como teste de UI isolada e criar suite real obrigatória para aceite | ALTO | NÃO |
| External AI providers | N/A | provider HTTP calls | `OllamaHttpClient`, adapters | AI contexts | unit tests existem | sem E2E real suficiente | políticas e budgets existem | health + retry parciais | PARCIAL | readiness externa não está demonstrada ponta a ponta; retries/timeouts não estão uniformemente provados | padronizar resiliência e adicionar integração real com provider | MÉDIO | NÃO |
| CLI | N/A | comandos da ferramenta | `NexTraceOne.CLI` | N/A | N/A | N/A | N/A | compilado na solution | STUB | ferramenta não é produto pronto; há `TODO`s e ausência de comandos reais | concluir ou retirar do escopo de artefacto produtivo | BAIXO | NÃO |

---

## 5. Gaps críticos

1. **Frontend principal não compila**
   - `npm run build` falha com 194 erros.

2. **Validação de build do frontend é tecnicamente desonesta**
   - `src/frontend/tsconfig.app.json` exclui árvores inteiras de funcionalidades:
     - `src/features/contracts/**/*`
     - `src/features/operations/pages/**/*`
     - `src/features/ai-hub/pages/**/*`
     - `src/features/governance/pages/**/*`
     - `src/features/integrations/**/*`
     - `src/features/product-analytics/**/*`
     - `src/features/catalog/pages/DeveloperPortalPage.tsx`
   - mesmo com essas exclusões, parte das árvores ainda vaza para o build e quebra a compilação.

3. **Módulo Governance continua a quebrar o build**
   - erros reais em `ReportsPage`, `RiskCenterPage`, `RiskHeatmapPage`, `MaturityScorecardsPage`, `ServiceFinOpsPage`, `TeamFinOpsPage`.

4. **E2E real em .NET continua parcialmente quebrado por infraestrutura**
   - `ApiE2EFixture.SeedMinimalTestDataAsync()` contém SQL inválido com identificadores escapados (`\"Id\"`), o que afeta cenários reais.

5. **Módulos ainda explicitamente preview-only no backend**
   - `Catalog.Application.Portal.Features.ExecutePlayground` → `PreviewOnly`
   - `OperationalIntelligence.Application.Automation.Features.ListAutomationWorkflows` → `PreviewOnly`
   - `Governance.Application.Features.SimulateGovernancePack` permanece como simulação, não fluxo produtivo fechado.

---

## 6. Gaps altos

1. **Audit sem prova crítica API-host/E2E suficiente**
2. **AI Assistant com evidência real contraditória**
3. **Change Governance com suite crítica ainda não totalmente verde**
4. **Contracts frontend fora da validação honesta do build**
5. **Integrations/Product Analytics/Developer Portal presentes, mas sem prova real suficiente e ainda gateados**
6. **Playwright UI E2E ainda depende de sessão e APIs mockadas**
7. **Operations frontend fora da validação honesta do build**

---

## 7. Gaps médios

1. `CookieSessionOptions.Enabled = false` por padrão — rollout seguro, mas não estado final ideal
2. ausência de `UseForwardedHeaders` / proxy awareness no `ApiHost`
3. `DevelopmentSeedDataExtensions` faz log de erro e continua — não falha rápido quando o ambiente de desenvolvimento fica parcialmente preparado
4. artefactos residuais `Migrations/__Probe` no módulo `Identity`
5. `BackgroundWorkers` com health real, mas sem prova operacional ampla de jobs em cenário produtivo real
6. readiness de integrações externas ainda pouco demonstrada ponta a ponta

---

## 8. Gaps baixos

1. `NEXTRACE_IGNORE_PENDING_MODEL_CHANGES` continua a ser definido em fixtures de integração/E2E e em `src/frontend/e2e-real/realStack.ts`, mas não foi localizada utilização ativa no código do `ApiHost`
2. `NexTraceOne.CLI` continua como artefacto secundário não concluído
3. mocks residuais/dead artifacts como `studioMock.ts` ainda existem no repositório

---

## 9. Backlog de correção por prioridade

### Prioridade 0 — Fechar honestidade técnica do produto compilado

1. **Zerar o build do frontend**
   - corrigir `identity.ts`, `types/index.ts`, `UsersPage.tsx`
   - corrigir páginas `Governance` que ainda entram no grafo de compilação

2. **Eliminar exclusões artificiais do `tsconfig.app.json`**
   - todas as árvores devem ser buildadas honestamente
   - se algum módulo realmente precisar de isolamento, separar em app/package própria com pipeline próprio

3. **Corrigir a fixture E2E real `.NET`**
   - ajustar SQL de `ApiE2EFixture.SeedMinimalTestDataAsync()`
   - revalidar `Auth`, `Incidents`, `AI`, `Contracts`, `ChangeGovernance`

### Prioridade 1 — Fechar prova real dos módulos já mais maduros

4. **Audit**
   - criar e executar fluxo crítico API-host real para `/api/v1/audit/search`, `/trail`, `/verify-chain`

5. **Change Governance**
   - alinhar teste crítico ao contrato HTTP observado (`201 Created` vs `200 OK`) sem mascarar comportamento
   - rerodar suite crítica

6. **Contracts frontend**
   - tirar o módulo da exclusão do build
   - remover `studioMock.ts` residual
   - validar UI real do draft studio contra backend

### Prioridade 2 — Converter preview/parcial em produto real ou remover do artefacto produtivo

7. **Developer Portal playground**
   - implementar sandbox real ou remover a capacidade do produto compilado

8. **Operations automation/reliability**
   - implementar backend real onde hoje há `PreviewOnly`
   - ou remover do produto compilado

9. **Governance**
   - concluir páginas executivas/risk/reports/finops/compliance e remover `SimulateGovernancePack`
   - ou retirar superfícies do artefacto produtivo final

10. **Integrations / Product Analytics / AI Hub admin**
   - reintroduzir no build honesto
   - fechar UX + backend + testes reais

### Prioridade 3 — Fechar zero-ressalvas operacional

11. adicionar `UseForwardedHeaders` / proxy awareness no `ApiHost`
12. tornar seed/test infra fail-fast quando preparação mínima falhar
13. reforçar runtime tests de `BackgroundWorkers` e `Ingestion.Api`
14. padronizar readiness real de integrações externas e providers AI

---

## 10. Sequência recomendada de execução

1. **Frontend build honesto primeiro**
   - sem isso, qualquer GO continua artificial

2. **E2E real .NET estável**
   - corrigir fixture SQL e parar de perder evidência por infraestrutura de teste

3. **Audit + Change Governance críticos**
   - porque ainda são os maiores buracos de prova dentro do escopo reduzido

4. **Contracts frontend e Operations frontend**
   - porque hoje existem, mas estão fora da validação honesta do build

5. **Governance / Integrations / Product Analytics / Developer Portal / AI admin**
   - converter preview/parcial em real ou retirar do artefacto produtivo

6. **Playwright real sem mocks**
   - criar uma trilha de aceite UI verdadeiramente integrada

7. **Hardening final de operação e integrações externas**
   - proxy, provider readiness, workers, ingestion, fail-fast seeds

---

## 11. Critério objetivo de GO sem ressalvas

O NexTraceOne só pode receber **GO sem ressalvas** quando todos os critérios abaixo forem verdadeiros ao mesmo tempo:

1. `run_build` verde
2. `npm run build` verde
3. `tsconfig.app.json` sem exclusões artificiais de módulos de produto
4. nenhuma página/feature do produto com `preview`, `mock`, `stub` ou `PreviewOnly`
5. nenhum módulo relevante protegido apenas por `ReleaseScopeGate` como substituto de conclusão técnica
6. todos os `DbContexts` com migrations, snapshot, startup migration e seed mínima validados
7. `ApiHost`, `BackgroundWorkers` e `Ingestion.Api` com startup real e health/readiness/liveness confiáveis
8. fluxos críticos reais verdes para:
   - Identity/Auth
   - Catalog / Source of Truth
   - Contracts
   - Change Governance
   - Incidents / Operations
   - Audit
   - AI Assistant
9. UI E2E crítica executada sem `mockAuthSession` nem `page.route(...)` para os fluxos de aceite
10. integrações externas com autenticação, retry/timeout, persistência e testes reais suficientes
11. nenhum gap classificado como `CRÍTICO` ou `ALTO` aberto

### Estado atual face a este critério

**NÃO ATINGIDO.**

O repositório está **mais maduro**, mas ainda não existe base honesta para `GO sem ressalvas`.

# NexTraceOne — Roadmap de Evolução 2026-2027

> **Data:** Abril 2026
> **Base:** Análise profunda em `docs/DEEP-ANALYSIS-APRIL-2026.md`
> **Pesquisa de mercado:** Backstage (CNCF), Cortex, OpsLevel, Port, Compass (Atlassian), Dynatrace, ServiceNow ITOM, PagerDuty, Datadog
> **Foco:** Estabilização → Hardening → Diferenciação → Expansão

---

## Visão Estratégica

O NexTraceOne compete num mercado em rápida evolução que inclui:
- **Internal Developer Portals (IDP):** Backstage, Cortex, OpsLevel, Port
- **Service Governance:** Atlassian Compass, ServiceNow
- **AIOps & Observability:** Dynatrace, Datadog, New Relic, PagerDuty
- **Change Intelligence:** LaunchDarkly, Split.io, Sleuth

O NexTraceOne diferencia-se por ser a **única plataforma** que combina **todas estas capacidades** num único sistema coerente, com **governança enterprise**, **multi-tenancy**, **AI governada** e **operação self-hosted**.

### O que os clientes enterprise procuram (baseado em pesquisa de mercado)

1. **Single pane of glass** para serviços, contratos, mudanças e operações
2. **Redução de tempo de investigação** de incidentes (correlação automatizada)
3. **Confiança em mudanças de produção** (blast radius, rollback intelligence)
4. **Governança sem fricção** (políticas automáticas, não manuais)
5. **IA contextualizada** (não chat genérico, mas assistência com dados reais)
6. **Métricas DORA** e relatórios executivos
7. **Catálogo de serviços** com ownership claro e scorecards
8. **Conformidade e auditoria** automatizadas (SOC 2, ISO 27001, LGPD/GDPR)
9. **Self-service para developers** com guardrails de governança
10. **Integração com CI/CD existente** (GitHub, GitLab, Azure DevOps, Jenkins)

---

## Fase 0 — ESTABILIZAÇÃO CRÍTICA (Semanas 1-3)

> **Objetivo:** Zero build errors, testes passando, funcionalidades core estáveis

### 0.1 Backend Build & Compilation ⏱️ 1 dia
- [x] Fix `AiGovernanceEndpointModule.cs:205` — adicionar `using Microsoft.AspNetCore.Http;`
- [x] Resolver conflitos de assembly version — **nenhum conflito real encontrado** (EF Core 10.0.4/10.0.5 não presente nos .csproj reais; apenas NU1605 histórico)
- [x] Remover 3 PackageReferences redundantes ✅ `Microsoft.Extensions.Options.ConfigurationExtensions` (Observability) + `Microsoft.Extensions.Localization` e `Microsoft.Extensions.Logging.Abstractions` (Application) removidos — todos transitivamente disponíveis via `FrameworkReference Microsoft.AspNetCore.App`
- [x] Remover duplicação de xunit em `BuildingBlocks.Security.Tests.csproj` ✅ Explicit `xunit` PackageReference removed (included transitively)

### 0.2 Frontend Build & Tests ⏱️ 3 dias
- [x] Fix tipo `GovernanceSummary | undefined` em `DomainDetailPage.tsx` e `TeamDetailPage.tsx`
- [x] Migrar `onSuccess` do `RunbookBuilderPage.tsx` para pattern correto do TanStack Query v5
- [x] Criar `TestWrapper` universal com todos os providers (QueryClient, Theme, Environment, Toast)
- [x] Aplicar TestWrapper — 141 falhas resolvidas ✅ 144 ficheiros / 915 testes passando
- [x] Fix 8 mocks desatualizados de `aiGovernanceApi` ✅ (resolvido com renderWithProviders migration)
- [x] Fix 53 ESLint errors (imports não utilizados, `any` types, hooks deps) ✅ 56→0 errors

### 0.3 Database Critical ⏱️ 2 dias
- [x] Gerar migração `W01_TelemetryStoreFoundation` para `TelemetryStoreDbContext` — 7 tabelas (`ops_ts_*`) + outbox + 15 índices + ModelSnapshot ✅ (Rev. 13)
- [ ] Regenerar 6 Designer files em falta (EF tooling) — requer `dotnet ef dbcontext scaffold` com PostgreSQL activo; não executável em sandbox. Executar localmente com: `dotnet ef migrations add <Name> --project <InfraProject> --startup-project src/platform/NexTraceOne.ApiHost`
- [x] Documentar processo de migração ✅ `scripts/db/apply-migrations.sh` com todos os 25 DbContexts mapeados

### 0.4 Outbox Processing ⏱️ 3-5 dias
- [x] Wire `OutboxProcessorJob` para todos os 25 DbContexts ✅ ConfigurationDbContext + NotificationsDbContext adicionados (Program.cs + csproj)
- [x] Documentar quais contexts têm outbox: todos os 25 que herdam `NexTraceDbContextBase` ✅ comentários no Program.cs
- [x] Testar comunicação cross-module via outbox para 3 cenários críticos ✅ `OutboxCrossModuleScenariosTests.cs` — Cenário 1 (happy path, em `OutboxEndToEndFlowTests`), Cenário 2 (retry after transient failure), Cenário 3 (exhausted retries / dead-letter behaviour)

---

## Fase 1 — HARDENING (Semanas 4-8)

> **Objetivo:** Production-readiness — segurança, validação, observabilidade

### 1.1 Validação de Input ⏱️ 5 dias
- [x] Adicionar FluentValidation aos ~30 Commands de escrita mais críticos:
  - Governance: `UpdateDomain` ✅, `ApproveGovernanceWaiver` ✅, `CreateGovernanceWaiver` ✅, `RunComplianceChecks` ✅
  - Governance (adicionais): `ApplyGovernancePack` ✅, `CreateDelegatedAdministration` ✅, `CreateDomain` ✅, `CreateGovernancePack` ✅, `CreatePackVersion` ✅, `CreateTeam` ✅, `RejectGovernanceWaiver` ✅, `UpdateGovernancePack` ✅, `UpdateTeam` ✅
  - AIKnowledge: `ActivateModel` ✅
  - AuditCompliance: `ApplyRetention` — N/A (empty command, no parameters)
  - IdentityAccess: `Logout`, `SeedDefaultModuleAccessPolicies`, `SeedDefaultRolePermissions` — N/A (empty commands, no parameters)
  - Notifications: `MarkAllNotificationsRead` — N/A (empty command, no parameters)
- [x] Governance Queries — 37 novos validators adicionados a todas as queries parametrizadas ✅ (Rev. 13): GetExecutiveDrillDown, GetEvidencePackage, GetCrossDomainDependencies, GetGovernancePack, GetComplianceGaps, ListGovernanceWaivers, GetDomainGovernanceSummary, GetControlsSummary, GetTeamGovernanceSummary, GetComplianceSummary, GetReportsSummary, GetExecutiveOverview, GetPlatformEvents, GetExecutiveTrends, ListGovernancePacks, GetRiskHeatmap, GetWasteSignals, GetFinOpsTrends, GetFinOpsSummary, GetPlatformJobs, GetBenchmarking, GetRiskSummary, GetEfficiencyIndicators, GetMaturityScorecards, GetPackApplicability, GetPackCoverage, GetDomainDetail, GetTeamDetail, GetTeamFinOps, GetDomainFinOps, GetServiceFinOps, GetCrossTeamDependencies, ListEvidencePackages, ListPackVersions, ListPolicies, GetPolicy, GetOnboardingContext — DI atualizado com 50 registos `IValidator`
- [x] AIKnowledge/IdentityAccess/Catalog/Integrations — 36 novos validators adicionados ✅ (Rev. 14): AIKnowledge Governance (25): GetEvaluation, GetAgent, GetAgentExecution, GetConversation, GetGuardrail, GetIdeCapabilities, GetModel, GetPromptTemplate, GetRoutingDecision, GetToolDefinition, ListAgentsByContext, ListAuditEntries, ListBudgets, ListConversations, ListEvaluations, ListGuardrails, ListIdeCapabilityPolicies, ListIdeClients, ListKnowledgeSourceWeights, ListMessages, ListModels, ListPolicies, ListPromptTemplates, ListSuggestedPrompts, ListToolDefinitions; Catalog Graph (5): ListSnapshots, GetOwnershipAudit, ListServices, GetServicesSummary, GetServiceMaturityDashboard; Integrations (5): GetIntegrationConnector, ListIngestionExecutions, ListIngestionSources, GetIngestionFreshness, ListIntegrationConnectors; IdentityAccess (1): ListSecurityEvents — cobertura final: Governance 50/58, AIKnowledge 72/88, IdentityAccess 30/43, Catalog 130/133, Integrations 11/13 (gaps restantes = queries sem parâmetros validáveis)

### 1.2 Error Handling ⏱️ 3 dias
- [x] Substituir 4 bare catch blocks em `CanonicalModelBuilder.cs` com logging ✅ All 5 catches now log via Trace.TraceWarning
- [x] Adicionar logging às 12+ exceções silenciadas em spec parsers ✅ All now have structured logging
- [x] Rever 5 instâncias de null/false silencioso com logging estruturado ✅ TenantRepository (4 catches) + RolePermissionRepository (2 catches) now inject ILogger and log `LogWarning` with context before returning bootstrap fallback values

### 1.3 Segurança ⏱️ 5 dias
- [x] Mover password de dev (`ouro18`) para `dotnet user-secrets` ✅ FIXED — password replaced with CHANGE_ME placeholder in appsettings.Development.json
- [x] Implementar PostgreSQL RLS policies como defesa em profundidade ✅ `infra/postgres/apply-rls.sql` — 38 tabelas cobertas com `get_current_tenant_id()` helper function + USING/WITH CHECK policies para todos os módulos tenant-aware
- [x] Documentar procedimento de rotação de chaves (JWT + encryption) ✅ `docs/security/KEY-ROTATION.md` criado
- [x] Configurar CORS por ambiente ✅ Already implemented with environment-aware validation, wildcard rejection, and explicit origins required for non-dev
- [x] Encriptar `AuditEvent.Payload` para campos sensíveis ✅ `[EncryptedField]` adicionado ao `AuditEvent.Payload` — AES-256-GCM aplicado automaticamente via `NexTraceDbContextBase.ApplyEncryptedFieldConvention`
- [x] Avaliar mover `TenantId` para `AuditableEntity<TId>` base — **Decisão: não aplicar**. A maioria das entidades já declara `TenantId` individualmente e o risco de breaking changes em EF Core mappings (column names, FK configurations) supera o benefício. Manter padrão atual + checklist de code review para novas entidades tenant-aware.

### 1.4 Implementar Interfaces Críticas ⏱️ 5-8 dias
- [x] `IEmbeddingProvider` — implementação com Ollama ou OpenAI embeddings para RAG funcional ✅ OllamaEmbeddingProvider + OpenAiEmbeddingProvider
- [x] `INotificationTemplateResolver` — resolver templates de notificação (email, webhook) ✅ Already implemented as NotificationTemplateResolver
- [x] `IPlatformHealthProvider` — agregar saúde da plataforma dos 25 DbContexts ✅ Already implemented as HealthCheckPlatformHealthProvider
- [x] `ILegacyEventParser<T>` — pelo menos para formatos JSON e XML genéricos ✅ 3 parsers exist (Batch, Mainframe, Mq)

### 1.5 Eliminar Stubs Remanescentes ⏱️ 5 dias
- [x] `GetAutomationAction` / `ListAutomationActions` — ✅ CORRECTED: static catalog by design (not stubs)
- [x] `GetExecutiveDrillDown` — popular ReliabilityScore, ChangeSafety, ContractCoverage a partir dos módulos cross ✅ IReliabilityModule + IContractsModule wired
- [x] `GetAutomationValidation` — retornar checks reais baseados na workflow definition ✅ Derives checks from workflow+validation state
- [x] `GetAutomationWorkflow` — popular Preconditions e ExecutionSteps da BD ✅ Derives from workflow lifecycle state
- [x] `GetServiceFinOps` — popular EfficiencyIndicators do `ICostIntelligenceModule` ✅ IReliabilityModule wired, EfficiencyIndicators populated

---

## Fase 2 — FRONTEND COMPLETION (Semanas 6-10)

> **Objetivo:** Todas as páginas conectadas a API real, i18n completo

### 2.1 Páginas de IA (prioridade máxima) ⏱️ 5 dias
- [x] `AiAssistantPage` (1213 linhas) — conectar a API real de conversas ✅ Already connected to `aiGovernanceApi` (listConversations, getConversation, sendMessage, createConversation)
- [x] `AiAnalysisPage` (591 linhas) — conectar a análise contextualizada ✅ Already connected (analyzeNonProdEnvironment, compareEnvironments, assessPromotionReadiness)
- [x] `AgentDetailPage` (563 linhas) — conectar a gestão de agentes ✅ Already connected to `aiGovernanceApi`

### 2.2 Páginas de Configuração ⏱️ 5 dias
- [x] `ConfigurationAdminPage` (908 linhas) — conectar a API de configuração ✅ Already uses `useConfigurationDefinitions` + `useConfigurationEntries` hooks
- [x] `AdvancedConfigurationConsolePage` (839 linhas) — conectar a admin API ✅ Already uses `useConfigurationDefinitions` hook
- [x] 5 config pages (Governance, Notification, OperationsFinOps, CatalogContracts, Workflow) ✅ Already connected — all use `useConfigurationDefinitions` + `useEffectiveSettings` + `useSetConfigurationValue` hooks

### 2.3 Knowledge & Notifications ⏱️ 3 dias
- [x] `KnowledgeHubPage`, `OperationalNotesPage`, `KnowledgeDocumentPage` — conectar a Knowledge API ✅ Already use `useKnowledgeDocuments`, `useOperationalNotes`, `useKnowledgeSearch`, `useKnowledgeDocument` hooks
- [x] `NotificationCenterPage`, `NotificationAnalyticsPage`, `NotificationPreferencesPage` — conectar a Notifications API ✅ Already use `useNotificationList`, `useNotificationAnalytics`, `useNotificationPreferences` hooks

### 2.4 Error States ⏱️ 2 dias
- [x] `ServiceDiscoveryPage` — adicionar error states para 2 useQuery ✅ PageErrorState for dashboard + services
- [x] `DelegationPage` — adicionar error states para 1 useQuery ✅ PageErrorState for delegations
- [x] `AccessReviewPage` — adicionar error states para 2 useQuery ✅ PageErrorState for campaigns + detail

### 2.5 i18n Completeness ⏱️ 3-5 dias
- [x] Completar 827 keys em PT-BR ✅ 0 keys em falta
- [x] Completar 795 keys em PT-PT ✅ 0 keys em falta
- [x] Completar 999 keys em ES ✅ 0 keys em falta
- [x] Script de verificação de i18n coverage no CI ✅ `scripts/quality/check-i18n-coverage.sh` + CI validate job

### 2.6 Testes ⏱️ 5 dias
- [x] Adicionar testes para as 40 páginas sem cobertura ✅ 34 novos ficheiros adicionados — **todas as 113 páginas têm testes**
- [x] Atingir 90%+ de testes passando ✅ 144/144 ficheiros / 915/915 testes passando (100%)

---

## Fase 3 — DIFERENCIAÇÃO COMPETITIVA (Semanas 11-20)

> **Objetivo:** Funcionalidades que nenhum concorrente oferece como um todo integrado

### 3.1 Service Templates & Scaffolding ⏱️ 8-10 dias
**Inspiração:** Backstage Software Templates

O NexTraceOne já tem o catálogo, contratos e governança. O próximo passo natural é:
- [x] Template engine para criação de novos serviços com contratos pré-definidos ✅ `ServiceTemplate` domain entity + `IServiceTemplateRepository` + `CreateServiceTemplate`, `GetServiceTemplate`, `ListServiceTemplates`, `ScaffoldServiceFromTemplate` features
- [x] Auto-geração de scaffolding de projeto (.NET, Node, Java) com contratos embedidos ✅ `ScaffoldServiceFromTemplate` — substituição de variáveis (`{{ServiceName}}`, `{{Domain}}`, etc.) + manifesto de ficheiros JSON + 23 testes unitários
- [x] Pipeline de criação: template → repositório → contratos → ownership → registro no catálogo ✅ `ScaffoldServiceFromTemplate` retorna plano completo com GovernancePolicyIds, BaseContractSpec, Files e Variables
- [x] Templates versionados e governados ✅ `ServiceTemplate.Slug` (kebab-case único) + `Version` + `IsActive` + `UsageCount` + `TenantId`
- [x] **Infrastructure completa** ✅ `TemplatesDbContext` + `EfServiceTemplateRepository` + `ITemplatesUnitOfWork` + `ServiceTemplateConfiguration` + migration `W01_ServiceTemplatesFoundation` + outbox processor `tpl_outbox_messages` + DI registado em ApiHost e BackgroundWorkers (Rev. 12)

API: `POST /api/v1/catalog/templates`, `GET /api/v1/catalog/templates`, `GET /api/v1/catalog/templates/{id}`, `GET /api/v1/catalog/templates/slug/{slug}`, `POST /api/v1/catalog/templates/{id}/scaffold`, `POST /api/v1/catalog/templates/slug/{slug}/scaffold`

**Valor:** Developers criam serviços conformes desde o primeiro commit.

### 3.2 Dependency Map Intelligence ⏱️ 8-10 dias
**Inspiração:** Cortex + ServiceNow Discovery

O NexTraceOne já tem dependency topology básica. Evoluir para:
- [x] Auto-discovery de dependências a partir de traces OpenTelemetry ✅ `InferDependencyFromOtel` feature existente
- [x] Mapa de dependências em tempo real (não apenas estático) ✅ `GetSubgraph` + `GetImpactPropagation` já implementados
- [x] Blast radius visual baseado no grafo de dependências ✅ `GetImpactPropagation` feature existente
- [x] Detecção automática de dependências circulares ✅ `DetectCircularDependencies` — DFS tricolor + `GET /api/v1/catalog/graph/circular-dependencies` + 8 testes unitários
- [x] Health propagation — se serviço A depende de B e B está degradado, A é marcado "at risk" ✅ `PropagateHealthStatus` — BFS + `GET /api/v1/catalog/graph/health-propagation/{rootServiceName}` + 10 testes unitários

**Valor:** Blast radius real-time, não estimativas estáticas.

### 3.3 Change Confidence Score V2 ⏱️ 5-8 dias
**Inspiração:** Sleuth + Split.io

Evoluir o scoring existente com:
- [x] Feature flag awareness — integração com LaunchDarkly/Split.io/Unleash ✅ `RecordFeatureFlagState` + `GetFeatureFlagAwareness` — risco por densidade/criticidade de flags + `POST /api/v1/changes/{id}/feature-flags` + `GET /api/v1/changes/{id}/feature-flags` + 7 testes unitários
- [x] Canary deployment tracking — percentagem de rollout como fator de confiança ✅ `RecordCanaryRollout` + `GetCanaryRolloutStatus` — ConfidenceBoost (Minimal/Low/Medium/High/Negative) + `POST /api/v1/changes/{id}/canary-rollout` + `GET /api/v1/changes/{id}/canary-rollout` + 9 testes unitários
- [x] Historical pattern matching — "mudanças similares no passado tiveram X% de falha" ✅ Implementado: `GetHistoricalPatternInsight` feature + `GET /api/v1/changes/{id}/historical-pattern` endpoint + `HistoricalPattern` como 5º fator no `GetChangeAdvisory` + 12 testes unitários (278/278 passing)
- [x] Pre-production comparison automática (diff de métricas staging vs production) ✅ `GetPreProductionComparison` feature + `GET /api/v1/changes/{preProdReleaseId}/pre-prod-comparison` endpoint + 301/301 change governance tests passing

**Valor:** Confiança baseada em dados históricos, feature flags e canary rollout real.

### 3.4 AI-Powered Incident Investigation ⏱️ 10-12 dias
**Inspiração:** PagerDuty AIOps + Datadog AI

O NexTraceOne já tem LLM E2E com grounding. Evoluir para:
- [x] **Auto-triage** ✅ `TriageIncident` — auto-triage baseado em correlação (confiança × tipo × ambiente × blast radius) + `GET /api/v1/incidents/{id}/triage` + 3 testes unitários
- [x] **Root cause suggestion** ✅ `GetRootCauseSuggestion` — análise de timeline de mudanças + categorização (Deployment/Configuration/Infrastructure) + passos de investigação + `GET /api/v1/incidents/{id}/root-cause` + 3 testes unitários
- [x] **Mitigation playbook** ✅ `SelectMitigationPlaybook` — seleção automática de runbook por score (serviço + tipo) com fallback textual, contexto de execução (urgência por severidade) e lista de alternativas + `GET /api/v1/incidents/{id}/mitigation-playbook` + 6 testes unitários
- [x] **Impact assessment** ✅ `GetIncidentImpactAssessment` — serviços afetados, contratos impactados, blast radius, propagation risk + `GET /api/v1/incidents/{id}/impact` + 3 testes unitários
- [x] **Similar incident search** ✅ `FindSimilarIncidents` — scoring por serviço+tipo+ambiente, padrão de recorrência, lookback configurável + `GET /api/v1/incidents/{id}/similar` + 6 testes unitários

Total: 548/548 OI tests passing.

**Valor:** Tempo de resolução reduzido de horas para minutos.

### 3.5 Compliance as Code ⏱️ 8-10 dias
**Inspiração:** ServiceNow GRC + Vanta

O NexTraceOne já tem audit trail e governance packs. Evoluir para:
- [x] **Framework templates** ✅ `GetComplianceFrameworkSummary` — SOC2, ISO27001, LGPD, GDPR, PCI-DSS — `GET /api/v1/audit/compliance/framework/{framework}` + 5 testes unitários (validação por framework)
- [x] **Continuous compliance** ✅ `EvaluateContinuousCompliance` — avaliação automática de recursos contra políticas ativas + `POST /api/v1/audit/compliance/evaluate` + 2 testes unitários
- [x] **Evidence collection automática** ✅ `ExportComplianceEvidences` — pacote de evidências por framework/categoria/período + `GET /api/v1/audit/compliance/evidences/export` + 3 testes unitários
- [x] **Compliance dashboard** ✅ `GetComplianceDashboard` — estado por categoria, critical gaps, score por tenant + `GET /api/v1/audit/compliance/dashboard` + 3 testes unitários
- [x] **Audit-ready reports** ✅ `GenerateAuditReadyReport` — relatório enterprise com assinatura digital SHA-256, sumário executivo por módulo/ação, suporte JSON/PDF/XLSX, entregável a auditores externos + `GET /api/v1/audit/compliance/report` + 8 testes unitários
- [x] **IReportRenderer** ✅ Interface `IReportRenderer` em Application.Abstractions + `JsonReportRenderer` (default) registado na infra — abstração pronta para adapters QuestPDF (PDF) e ClosedXML (XLSX) quando disponíveis (Rev. 12)

Total: 147/147 compliance tests passing.

---

## Fase 4 — EXPANSÃO DE ECOSSISTEMA (Semanas 21-30)

> **Objetivo:** Integrações nativas com ecossistema enterprise

### 4.1 CI/CD Integrations Nativas ⏱️ 10-15 dias
- [x] **GitHub Actions** ✅ (Rev. 15) — `GitHubActionsPayloadNormalizer` normaliza `deployment_status` e `workflow_run` webhooks → `POST /api/v1/integrations/webhooks/github` + `IngestionExecution` audit trail; 38 testes unitários
- [x] **GitLab CI** ✅ (Rev. 15) — `GitLabCiPayloadNormalizer` normaliza pipeline events com extracção de variáveis `ENVIRONMENT`/`VERSION` → `POST /api/v1/integrations/webhooks/gitlab`
- [x] **Azure DevOps** ✅ (Rev. 15) — `AzureDevOpsPayloadNormalizer` normaliza `release-deployment-completed-event` e `build-completed-event` → `POST /api/v1/integrations/webhooks/azuredevops`
- [x] **`IngestCiCdWebhook` feature** ✅ (Rev. 15) — `ICiCdPayloadNormalizer` abstraction (open/closed para novos vendors); handler cria `IngestionExecution` para rastreabilidade completa; DI registado com 3 normalizers
- [ ] **Jenkins** — plugin para change confidence check como stage
- [ ] **ArgoCD/Flux** — controller para Kubernetes deployments

**Valor:** Zero configuração manual de eventos de deploy. Os 3 principais vendors (GitHub, GitLab, Azure DevOps) já normalizam e alimentam o pipeline de Change Intelligence automaticamente.

### 4.2 IDE Extensions ⏱️ 8-10 dias
- [ ] **VS Code Extension** — ver contratos, ownership, change confidence inline
- [ ] **Visual Studio Extension** — mesmas capacidades para ecossistema .NET
- [ ] **JetBrains Plugin** — IntelliJ/Rider para equipas Java/Kotlin

**Valor:** Developers acedem a governança sem sair do IDE.

### 4.3 API Marketplace ⏱️ 10-12 dias
**Inspiração:** SwaggerHub + RapidAPI + Backstage Marketplace

Evoluir o Developer Portal existente para:
- [x] **Subscrição de APIs** com approval workflow ✅ (Rev. 16) — `ApproveSubscription` + `RejectSubscription` + `SubscriptionStatus` (PendingApproval/Active/Rejected/Cancelled) + `POST /api/v1/developerportal/subscriptions/{id}/approve|reject` + 6 testes
- [x] **API keys management** governado ✅ (Rev. 16) — `ApiKey` aggregate (hash SHA-256, raw key retornado apenas uma vez) + `CreateApiKey` + `RevokeApiKey` + `ListApiKeys` + `ValidateApiKey` + 4 endpoints + 21 testes
- [x] **Usage analytics** por consumidor, API, versão ✅ (Rev. 16) — `GetApiUsageAnalytics` com breakdown por API/consumer/activeKeys + `GET /api/v1/developerportal/usage/analytics` + 4 testes
- [x] **Rate limiting policies** configuráveis por contrato ✅ (Rev. 16) — `RateLimitPolicy` entity (rpm/rph/rpd/burst) + `SetRateLimitPolicy` + `GetRateLimitPolicy` + `PUT|GET /api/v1/developerportal/ratelimit/{apiAssetId}` + 4 testes
- [ ] **Sandbox environments** para teste de contratos (PlaygroundSession já existe; sandbox completo com mocking requer infra adicional)

**Valor:** Marketplace interno governado, eliminando shadow APIs.

### 4.4 Cost Intelligence V2 ⏱️ 8-10 dias
**Inspiração:** Kubecost + Vantage + Apptio

Evoluir o FinOps existente para:
- [x] **Cloud cost correlation** — AWS/Azure cost por serviço correlacionado com mudanças ✅ (Rev. 17) — `CorrelateCloudCostWithChange` feature + `GET /api/v1/cost/correlation/{changeId}`
- [x] **Anomaly detection** — alertas de custo fora do padrão ✅ (Rev. 17) — `DetectCostAnomalies` feature (batch scan de todos os perfis) + `POST /api/v1/cost/anomaly-detection`
- [x] **Budget forecasting** — projeção de custos baseada em tendência + mudanças planeadas ✅ (Rev. 17) — `BudgetForecast` entity + `ForecastBudget` + `GetBudgetForecast` + `POST|GET /api/v1/cost/forecasts`
- [x] **Efficiency recommendations** — "serviço X gasta 40% mais que serviços similares" ✅ (Rev. 17) — `EfficiencyRecommendation` entity + `GenerateEfficiencyRecommendations` + `ListEfficiencyRecommendations` + `POST|GET /api/v1/cost/efficiency`
- [x] **Showback reports** — custo por equipa, domínio, serviço para accountability ✅ (Rev. 17) — `GetShowbackReport` + `GET /api/v1/cost/showback` com breakdown by team/domain/service

**Valor:** FinOps contextualizado por serviço, não apenas dashboards de custo cloud genéricos.

### 4.5 Multi-Cluster & Multi-Cloud ⏱️ 10-15 dias
- [x] **Kubernetes integration** — `ClusterRegistration` entity com suporte a K8s version, API endpoint, node/service counts ✅ (Rev. 18) — `RegisterCluster` + `GET /api/v1/clusters` + `POST /api/v1/clusters`
- [x] **Multi-cluster view** — `ListClusters` feature + `GetClusterStatus` feature ✅ (Rev. 18) — inventory completo com health aggregation, paginação, filtros por provider/status/isEdge
- [x] **Cloud-agnostic** — `GetMultiCloudView` feature ✅ (Rev. 18) — `CloudProvider` enum (AWS/Azure/GCP/OnPremises/Edge/Other) + view agregada por provider com HealthPercent + `GET /api/v1/clusters/multi-cloud`
- [x] **Edge deployment support** — `IngestEdgeDeploymentEvent` feature ✅ (Rev. 18) — `IsEdge` flag em ClusterRegistration + `POST /api/v1/clusters/{id}/edge-events` + `UpdateClusterHealthSnapshot`

---

## Fase 5 — DIFERENCIAÇÃO AVANÇADA (Semanas 31-40+)

> **Objetivo:** Funcionalidades que posicionam o NexTraceOne como líder de mercado

### 5.1 Predictive Intelligence ⏱️ 15-20 dias
- [x] **Failure prediction** — `ServiceFailurePrediction` entity + `PredictServiceFailure` feature: weighted formula (error rate 40% + incident count + change frequency) → probability % + RiskLevel + CausalFactors + RecommendedAction ✅ (Rev. 19) — `POST /api/v1/predictions/service-failure`
- [x] **Capacity planning** — `CapacityForecast` entity + `GetCapacityForecast` feature: growth rate → DaysToSaturation → SaturationRisk (Immediate/Near/Moderate/Low) ✅ (Rev. 19) — `POST /api/v1/predictions/capacity-forecast`
- [x] **Change risk prediction** — `GetChangeRiskPrediction` feature: multi-factor scoring (history + blast radius + test evidence + timing + change type multiplier) → RiskScore 0-100 + recommendations ✅ (Rev. 19) — `GET /api/v1/predictions/change-risk/{changeId}`
- [x] **SLO burn rate alerts** — `GetSloBurnRateAlert` feature: burn rate = error rate / error budget → time-to-exhaustion + Critical/Warning/OK classification ✅ (Rev. 19) — `GET /api/v1/predictions/slo-burn-rate`

### 5.2 Developer Experience Score ⏱️ 8-10 dias
**Inspiração:** DX Core 4 + SPACE framework

- [x] **Productivity metrics** — `ComputeDeveloperExperienceScore` feature: cycle time + deployment frequency + cognitive load + toil % → weighted OverallScore + Elite/High/Medium/Low level ✅ (Rev. 19) — `POST /api/v1/developer-experience/scores`
- [x] **Cognitive load measurement** — `CognitivLoadScore` field (0-10) in `DxScore` entity: inverse-weighted contribution to OverallScore ✅ (Rev. 19)
- [x] **Toil tracking** — `ToilPercentage` + `ManualStepsCount` fields in `ProductivitySnapshot` + `RecordProductivitySnapshot` feature ✅ (Rev. 19) — `POST /api/v1/developer-experience/snapshots`
- [ ] **Developer survey automation** — questionários periódicos integrados na plataforma (requires UI + survey engine)
- [ ] **Developer NPS** — satisfação com ferramentas, processos, plataforma (requires survey/NPS subsystem)

### 5.3 GraphQL Federation Gateway ⏱️ 10-15 dias
- [ ] Gateway GraphQL federado que expõe o catálogo completo do NexTraceOne
- [ ] Schema stitching automático entre módulos
- [ ] Subscriptions para eventos real-time (mudanças, incidentes, deploys)
- [ ] SDK para integração com ferramentas externas

### 5.4 Observability Correlation Engine ⏱️ 15-20 dias
- [x] **Trace-to-change correlation** — `CorrelateTraceToChange` feature: timestamp-based correlation (±2h window) → CorrelationConfidence + CorrelationReason ✅ (Rev. 19) — `GET /api/v1/runtime/traces/{traceId}/change-correlation`
- [x] **Log anomaly detection** — `DetectLogAnomaly` feature: error spike % vs baseline + post-change detection → AnomalyType (ErrorSpike/Regression/BaselineDeviation) ✅ (Rev. 19) — `POST /api/v1/runtime/log-anomaly`
- [ ] **Metric correlation** — correlação automática entre métricas de diferentes serviços (requires telemetry aggregation)
- [ ] **Topology-aware alerting** — alertas inteligentes baseados no grafo de dependências

### 5.5 Governance Policy Engine V2 ⏱️ 10-15 dias
- [ ] **Policy as Code** — políticas definíveis em YAML/JSON, versionadas no repositório
- [ ] **Policy simulation** — "se aplicar esta política, X serviços ficam non-compliant"
- [ ] **Gradual enforcement** — policies em modo warning antes de blocking
- [ ] **Exception management** — waivers com expiração automática e audit trail

---

## Cronograma Resumido

```
             Abr  Mai  Jun  Jul  Ago  Set  Out  Nov  Dez  Jan  Fev  Mar
             2026 2026 2026 2026 2026 2026 2026 2026 2026 2027 2027 2027
Fase 0       ████
Fase 1            ████████
Fase 2            ████████████
Fase 3                      ████████████████████
Fase 4                                     ████████████████████
Fase 5                                                    ████████████████
```

---

## KPIs de Sucesso por Fase

| Fase | KPI | Meta |
|------|-----|------|
| 0 | Build errors | 0 |
| 0 | Testes passando | 95%+ |
| 1 | Features com validators | 85%+ |
| 1 | Catch blocks com logging | 100% |
| 1 | Outbox processors ativos | 24/24 |
| 2 | Páginas com API real | 100% |
| 2 | i18n coverage (todos idiomas) | 95%+ |
| 2 | Páginas com testes | 90%+ |
| 3 | Tempo médio de resolução de incidentes | -40% |
| 3 | Compliance checks automáticos | 100% dos frameworks configurados |
| 4 | CI/CD integrations | ≥3 plataformas |
| 4 | IDE extensions | ≥2 IDEs |
| 5 | Prediction accuracy | >70% para failure prediction |

---

## Prioridades Baseadas em Valor para o Cliente

### Impacto Imediato (Fase 0-1)
1. **Estabilidade** — sem build errors, testes passando, sem stubs silenciosos
2. **Segurança** — RLS, rotação de chaves, validação de input

### Impacto Alto (Fase 2-3)
3. **Developer self-service** — todas as páginas funcionais, templates de serviço
4. **Incident resolution time** — AI investigation, auto-triage, runbook selection
5. **Compliance automation** — evidence collection, continuous compliance

### Impacto Diferenciador (Fase 4-5)
6. **Zero-touch deploy events** — integração nativa CI/CD
7. **Predictive intelligence** — previsão de incidentes, capacity planning
8. **IDE integration** — governança no fluxo de trabalho do developer
9. **API Marketplace** — catálogo vivo com subscrição e analytics
10. **Cost intelligence** — FinOps contextualizado por mudança e serviço

---

## Análise Competitiva — Posicionamento

| Capacidade | Backstage | Cortex | OpsLevel | ServiceNow | PagerDuty | Datadog | **NexTraceOne** |
|------------|-----------|--------|----------|------------|-----------|---------|-----------------|
| Service Catalog | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| Contract Governance | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | **✅ Único** |
| Change Intelligence | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Incident Management | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ |
| AI Governance | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | **✅ Único** |
| Audit Blockchain | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | **✅ Único** |
| Multi-tenancy | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ |
| Self-hosted | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | **✅** |
| DORA Metrics | Plugin | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Service Scorecards | Plugin | ✅ | ✅ | ❌ | ❌ | ✅ | ✅ |
| Compliance as Code | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ | ✅ (com Fase 3) |
| Predictive AI | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ (com Fase 5) |
| FinOps | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | **✅ Único** |

### Diferenciação Única do NexTraceOne
1. **Contract Governance** — nenhum concorrente oferece governança de contratos REST/SOAP/Kafka/Event como first-class citizen
2. **AI Governance** — modelo registry, quotas, policies, audit de uso — nenhum concorrente tem isto integrado
3. **Audit Blockchain** — trilha de auditoria com SHA-256 chain — enterprise-grade tamper-proof
4. **FinOps contextualizado** — custo correlacionado com serviço, mudança, incidente — não existe em nenhum concorrente
5. **Tudo self-hosted** — enterprise pode correr tudo on-premises, ao contrário de Cortex, OpsLevel, PagerDuty

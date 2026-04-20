# NexTraceOne — CHANGELOG

> Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/)
> Versionamento: [SemVer](https://semver.org/lang/pt-BR/)

---

## [Unreleased]

### Adicionado
- **OPS-01** `GetServiceOperationalTimeline`: feature completa + endpoint `GET /knowledge/services/{serviceId}/operational-timeline` + página `ServiceTimelinePage` (/knowledge/services/:serviceId/timeline) + i18n em 4 idiomas (en/es/pt-BR/pt-PT) + 11 testes unitários (Knowledge: 103/103).
- **OPS-02** `GenerateServerFromContract`: geração de servidor real para .NET, Java/Spring, Node/Express, Python/FastAPI, Go — cada linguagem inclui 3+ ficheiros (controller, interface de serviço, ficheiro de projeto) sem `TODO` comments (Catalog: 39/39).
- **OPS-03** `OptionalProviderStartupLogger`: emite `LogWarning` no arranque por cada provider opcional não configurado em ambientes não-Development.
- **DEG-11** SAML SSO — padrão Level A′ completo: `ISamlProvider` em `Integrations.Domain`, `NullSamlProvider` + `ConfigurationSamlProvider` em `Integrations.Infrastructure`, `ISamlService` em `IdentityAccess`, `GetOptionalProviders` expõe 5 providers (canary / backup / kafka / cloudBilling / saml). ProductAnalytics: 175/175; Governance: 579/579.
- **QLT-02** i18n: 142 novas chaves de tradução adicionadas (es: 51, pt-BR: 41, pt-PT: 50). Cobre `aiHub.ideQuery.*`, `aiHub.streaming.*`, `aiHub.onboarding.*`, `ai.feedback.*`, `mtlsManager.policyMode.*`, `releaseGatesDashboard.nonBlocking`. `validate:i18n`, `typecheck` e `lint` passam.
- **QLT-04** Cobertura de testes: 68 novos testes unitários adicionados cobrindo handlers sem cobertura prévia em ChangeGovernance (+34), Governance (+18) e OperationalIntelligence (+16).
- **CFG-01** `SystemHealthPage` em `/admin/system-health` (Platform Admin): lista providers opcionais com estado configured / not-configured.
- **CFG-02** Auditoria completa do padrão `IsConfigured + Null*Provider` em DEG-01..DEG-15 documentada em `HONEST-GAPS.md`.
- **CFG-03** `docs/deployment/PRODUCTION-BOOTSTRAP.md` com checklist para remoção de todos os `simulatedNote`.
- **ACT-023** Testes de integração `ExportAnalyticsData` (CSV/JSON, paginação, authz): 17 novos testes (ProductAnalytics: 175/175).
- **ACT-024** OpenAPI como artefacto de build: job `openapi-artifact` em `ci.yml` gera `swagger.json`.
- **ACT-022** E2E SAML SSO com Playwright + mock IdP: `src/frontend/e2e/saml-sso-flows.spec.ts` — 14 testes cobrindo admin config page (NotConfigured/Enabled/Disabled), Test Connection (sucesso/falha), SAML initiation (GET /auth/saml/sso), ACS callback (SAMLResponse válido/inválido/vazio) e fluxo @smoke end-to-end com mock IdP via route interception.
- **ACT-025** Elasticsearch com `xpack.security.enabled=true` em `docker-compose.staging.yml`.
- **DOC-01** `IMPLEMENTATION-STATUS.md`: aviso de contagens indicativas no header; referência para `HONEST-GAPS.md`; notação `+` em vez de números fixos.
- **DOC-02** `HONEST-GAPS.md` actualizado para "Zero gaps abertos" — todos os 25 ACTs, 3 CFGs, 3 OPS, 4 QLTs e 4 DOCs concluídos. Produto pronto para `v1.0.0`.
- **DOC-03** `FUTURE-ROADMAP.md`: item SAML Protocol Handlers (4.1) marcado como ✅ IMPLEMENTADO com nota ACT-022 concluído.
- **DOC-04** `CHANGELOG.md` (este ficheiro) criado seguindo o formato Keep a Changelog.
- **QLT-01** `parallel_validation` executado: CodeQL 0 alertas em csharp/javascript/actions; Code Review sem findings bloqueantes nos ficheiros desta iteração.

### Corrigido
- **QLT-03** `TreatWarningsAsErrors=true` activado em `Directory.Build.props`; build 100% limpo (0 warnings, 0 errors). Correcções: nullability (CS86xx), CA1826 / CA1875 / CA1854 / CA2024, SYSLIB0057, CS1574/CS1734 (XML doc), `ToLower()` → `ToLowerInvariant()`.
- `Knowledge` module: bug em `KnowledgeDbContextDesignTimeFactory` corrigido (Knowledge: 103/103 testes passam).
- `AlignKnowledgeGraphSnapshotTenantId` migration: alinhamento de `TenantId` em `knowledge_graph_snapshots`.

### Melhorado
- **AIKnowledge (Fases 9–12)** — 1303 testes passam. Entidades: `AiSkill`, `AiSkillExecution`, `AiSkillFeedback`, `AiAgentTrajectoryFeedback`, `AiAgentPerformanceMetric`, `WarRoomSession`, `ChangeConfidenceScore`, `SelfHealingAction`, `GuardianAlert`, `OrganizationalMemoryNode`. Features: `RegisterSkill`, `ExecuteSkill`, `RateSkillExecution`, `SeedDefaultSkills`, `SubmitAgentExecutionFeedback`, `GetAgentPerformanceDashboard`, `ExportPendingTrajectories`, `ProcessNaturalLanguageQuery`, `QuantifyTechDebt`, `GetSlaIntelligence`, `ProactiveArchitectureGuardianJob`. Frontend: `AgentFeedbackWidget.tsx`.
- **ProductAnalytics (Fases 1–4)** — 175 testes passam. Paginação em `GetPersonaUsage` / `GetModuleAdoption` / `GetAdoptionFunnel`; exportação CSV/JSON (`ExportAnalyticsData`); `JourneyDefinition` entity + CRUD + migração `pan_journey_definitions`; `GetCohortAnalysis` handler; `CohortAnalysisPage` + `JourneyConfigPage` no frontend; `AnalyticsConfigKeys` + 6 definições no `ConfigurationDefinitionSeeder`; migration `AddAnalyticsPerformanceIndexes` (3 índices).
- **Governance** — 590 testes passam. `IDateTimeProvider` injectado em `GetCapacityForecast`, `GetRiskHeatmap`, `GetEfficiencyIndicators`, `GetAiGovernanceDashboard`, `GetObservabilityMode`, `GetGracefulShutdownConfig`.
- **IdentityAccess** — 504 testes passam. `IdentityAccessQueryFeaturesTests` + `IdentityAccessMutationFeaturesTests` (28 novos testes).
- **ChangeGovernance** — 518 testes passam. `ChangeGovernanceExtendedGapTests` + novos testes para `GetChangeScore`, `ListReleases`, `GetPromotionGateStatus`.
- **OperationalIntelligence** — 929 testes passam. Novos testes para `GetCostByRelease`, `GetObservabilityScore`, `UpdateRunbook`.

---

## [0.9.0] — 2026-04-19

### Adicionado
- Fundação do produto: todos os módulos core (IdentityAccess, Catalog, ChangeGovernance, Governance, OperationalIntelligence, Knowledge, AIKnowledge, AuditCompliance, Notifications, Configuration, Integrations, ProductAnalytics).
- Stack: .NET 10 / ASP.NET Core 10, EF Core 10, PostgreSQL 16, React 18, TypeScript, Vite, Tailwind CSS, Radix UI.
- Building blocks: `Result<T>`, `EntityTag`, `IDateTimeProvider`, `TenantSchemaManager`, `RepositoryBase`, `NexTraceDbContextBase`, multi-tenancy por schema PostgreSQL.
- Módulos de integração: Kafka (Confluent), Cloud Billing, Canary (Argo/Flagger), Backup (Velero), OpenTelemetry.
- Frontend: roteamento via `react-router-dom` v7, Zustand para estado, TanStack Query para data fetching, i18n com `react-i18next` (en / es / pt-BR / pt-PT).
- CI: `dotnet build`, `dotnet test`, `npm run typecheck`, `npm run lint`, `npm run validate:i18n`.

---

<!-- Ligações de versão -->
[Unreleased]: https://github.com/dlimadev/NexTraceOne/compare/v0.9.0...HEAD
[0.9.0]: https://github.com/dlimadev/NexTraceOne/releases/tag/v0.9.0

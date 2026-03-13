# NexTraceOne — Roadmap MVP1 Expandido

## Visão Geral

Metodologia: **1 módulo por vez · 1 camada por vez · 1 aggregate por vez**.
Cada fase termina com aprovação antes de iniciar a próxima.

> **Última atualização:** Março 2026 — Revisão completa pós módulos 1-4 (Identity, Licensing, EngineeringGraph, DeveloperPortal).
> Ver `docs/SOLUTION-REVIEW-MODULES-1-4.md` para revisão detalhada dos módulos 1 a 4.
> Ver `docs/MVP1-EXPANDED-PLAN.md` para análise detalhada de valor vs. esforço por módulo.

---

## Estado Atual do Projeto (Março 2026 — Revisão Real)

### Build e Testes
- **Build:** ✅ Compila sem erros (149 warnings)
- **Testes:** ✅ 370 testes passando, 0 falhas (22 projetos de teste)

### O que está concluído (100%):

- ✅ **Building Blocks (6/6)** — Domain, Application, Infrastructure, EventBus, Observability, Security — 100% funcionais (49 testes)
- ✅ **Identity (95%)** — 35 features, 111 testes, RBAC + multi-tenancy + OIDC + enterprise features. Sem migrations EF Core (usa auto-migration)
- ✅ **Licensing (80%)** — Domain + Application + Infrastructure + API completos. Modelo rico (trial, capabilities, quotas, hardware binding). Apenas 8 testes — precisa expandir. Sem frontend
- ✅ **EngineeringGraph (98%)** — 21 features, 37 testes, blast radius, integração inbound (SyncConsumers), temporalidade, overlays. Frontend com i18n. Módulo de referência
- ✅ **Contracts (100%)** — 9 features, 42 testes, diff semântico, classificação de breaking changes. Frontend com i18n
- ✅ **ChangeIntelligence (100%)** — Release, ChangeEvent, BlastRadius, ChangeScore, NotifyDeployment. 18 testes. Frontend com i18n
- ✅ **RulesetGovernance (100%)** — Ruleset, LintResult, UploadRuleset, ExecuteLint, Spectral-compatible. 26 testes
- ✅ **Workflow (100%)** — 12 features, 40 testes. Evidence pack, SLA, aprovação completa. Frontend com i18n
- ✅ **Promotion (100%)** — 5 features, 29 testes. CreatePromotionRequest, EvaluatePromotionGates, ApprovePromotion, BlockPromotion, GetPromotionStatus
- ✅ **Audit (100%)** — RecordAuditEvent, GetAuditTrail, SearchAuditLog, VerifyChainIntegrity, ExportAuditReport, GetComplianceReport
- ✅ **Scaffold completo** — 14 módulos × 5 camadas criados

### O que está parcial:

- 🟡 **DeveloperPortal (30%)** — Domain completo (5 aggregates), Application parcial (16 features — 8 implementados, 8 stubs). Infrastructure, API, testes e frontend em scaffold. **Não registrado no ApiHost**
- 🟡 **i18n (50%)** — en e pt-BR completos (175+ chaves). pt-PT e es ausentes
- 🟡 **API Integration Readiness (60%)** — APIs sob /api/v1/, SyncConsumers como referência inbound. Sem Swagger UI, sem API key, sem rate limiting

### O que está pendente:

- 🔲 **DeveloperPortal Infrastructure** — DbContext, repositórios, configurations, migrations
- 🔲 **DeveloperPortal API endpoints** — 16 endpoints não mapeados
- 🔲 **DeveloperPortal DI wiring** — MediatR e validators não registrados
- 🔲 **DeveloperPortal registro no ApiHost** — Não está em Program.cs
- 🔲 **i18n pt-PT e es** — Locales por criar
- 🔲 **RuntimeIntelligence** (parcial) — IngestRuntimeSnapshot, GetRuntimeDriftFindings
- 🔲 **CostIntelligence** (parcial) — IngestCostSnapshot, GetCostByRelease, AttributeCostToService
- ❌ **AiOrchestration** — excluído do MVP1, vai para MVP2
- ❌ **ExternalAi** — excluído do MVP1, vai para MVP2

---

## FASE 1 — Fundação ✅ CONCLUÍDA (Semanas 1–4)

### Semana 1–2: Building Blocks ✅

| Artefato | Status |
|----------|--------|
| `ITypedId` + `TypedIdBase` | ✅ |
| `Entity<TId>` + `AggregateRoot<TId>` + `ValueObject` + `AuditableEntity<TId>` | ✅ |
| `IDomainEvent` + `DomainEventBase` + `IIntegrationEvent` + `IntegrationEventBase` | ✅ |
| `Result<T>` + `Error` + `ErrorType` | ✅ |
| `ICommand` + `IQuery` + `IPagedQuery` | ✅ |
| `ICurrentUser` + `ICurrentTenant` + `IUnitOfWork` + `IDateTimeProvider` + `IEventBus` | ✅ |
| Pipeline Behaviors (Validation → Logging → Performance → TenantIsolation → Transaction) | ✅ |
| `NexTraceDbContextBase` + `RepositoryBase<T,TId>` + `OutboxMessage` | ✅ |
| `NexTraceGuards` (extensões de domínio) | ✅ |
| `PagedList<T>` + `ResultExtensions.ToHttpResult()` | ✅ |
| Interceptors (`TenantRlsInterceptor`, `AuditInterceptor`) | ✅ |
| `EncryptedStringConverter` (AES-256-GCM) | ✅ |
| `AssemblyIntegrityChecker` + `HardwareFingerprint` | ✅ |
| `TenantResolutionMiddleware` | ✅ |
| `SerilogConfiguration` + `NexTraceActivitySources` + `NexTraceMeters` | ✅ |
| `InProcessEventBus` + `OutboxEventBus` | ✅ |
| `DependencyInjection.cs` de cada Building Block | ✅ |
| i18n: `SharedMessages.resx` + `SharedMessages.pt-BR.resx` (Identity + base) | ✅ |
| Testes unitários (Result, ValueObject, PagedList) | ✅ |

**Entregável:** ✅ Building Blocks 100% funcionais.

### Semana 3–4: Módulo Identity ✅

| Feature / Aggregate | Status |
|---------------------|--------|
| User (Aggregate Root) | ✅ |
| Role, Permission (Entities) | ✅ |
| TenantMembership | ✅ |
| Session | ✅ |
| FederatedLogin (OIDC) | ✅ |
| LocalLogin | ✅ |
| RefreshToken | ✅ |
| CreateUser | ✅ |
| AssignRole | ✅ |
| ListTenantUsers | ✅ |
| GetUserProfile | ✅ |
| RevokeSession | ✅ |
| IdentityDbContext + Migrations | ✅ |
| Testes (AssignRole, RevokeSession) | ✅ |

**Entregável:** ✅ Identity 100% funcional com RBAC e multi-tenancy.

---

## FASE 2 — Licenciamento e Grafo (Semanas 5–7) ✅ CONCLUÍDA

### Semana 5: Licensing — Infrastructure ✅

**Pré-requisito antes de avançar para EngineeringGraph.**

| Feature / Artefato | Status |
|---------------------|--------|
| License (Aggregate Root) | ✅ |
| LicenseCapability, HardwareBinding | ✅ |
| ActivateLicense | ✅ |
| VerifyLicenseOnStartup | ✅ |
| CheckCapability | ✅ |
| TrackUsageMetric | ✅ |
| `LicensingDbContext` com IUnitOfWork | ✅ |
| `LicenseRepository` + `HardwareBindingRepository` | ✅ |
| Migrations EF Core | ✅ |
| `AddLicensingInfrastructure` DI | ✅ |
| Testes | ✅ |

### Semana 6: EngineeringGraph — Core ✅

| Feature | Status |
|---------|--------|
| ApiAsset (Aggregate Root) | ✅ |
| ConsumerRelationship (Entity) | ✅ |
| RegisterApiAsset | ✅ |
| RegisterServiceAsset | ✅ |
| MapConsumerRelationship | ✅ |
| GetAssetGraph | ✅ |
| GetAssetDetail | ✅ |
| SearchAssets | ✅ |

### Semana 7: EngineeringGraph — Discovery ✅

| Feature | Status |
|---------|--------|
| InferDependencyFromOtel (receptor passivo) | ✅ |
| ValidateDiscoveredDependency | ✅ |
| GetSubgraph (mini-grafos contextuais) | ✅ |
| GetImpactPropagation (blast radius direto + transitivo) | ✅ |
| CreateGraphSnapshot / ListSnapshots / GetTemporalDiff | ✅ |
| GetNodeHealth (overlays explicáveis) | ✅ |
| CreateSavedView / ListSavedViews | ✅ |
| SyncConsumers (integração inbound externa com upsert em lote) | ✅ |
| EngineeringGraphDbContext + Migrations | ✅ |
| Frontend: EngineeringGraphPage.tsx com 5 abas e i18n completo | ✅ |
| API client frontend com 15 funções | ✅ |
| Testes (37 testes unitários — domínio + aplicação + sync) | ✅ |
| Scripts de seed e massa de teste para frontend | ✅ |
| Documentação: API de integração externa, roadmap do módulo | ✅ |

**Entregável:** ✅ Grafo de dependências funcional com APIs, consumidores, temporalidade, impacto, overlays, integração inbound e massa de teste.

---

## FASE 3 — Contratos e Portal (Semanas 8–10) 🟡 EM ANDAMENTO

### Semana 8: Contracts — Import e Histórico ✅

| Feature | Status |
|---------|--------|
| ContractVersion (Aggregate Root) | ✅ |
| ContractDiff (Entity) | ✅ |
| SemanticVersion (Value Object) | ✅ |
| ChangeEntry (Value Object) | ✅ |
| ImportContract | ✅ |
| CreateContractVersion | ✅ |
| GetContractHistory | ✅ |
| ExportContract | ✅ |
| ValidateContractIntegrity | ✅ |

### Semana 9: Contracts — Diff Semântico ✅

| Feature | Status |
|---------|--------|
| ComputeSemanticDiff | ✅ |
| ClassifyBreakingChange | ✅ |
| SuggestSemanticVersion | ✅ |
| LockContractVersion | ✅ |
| ContractsDbContext + Migrations | ✅ |
| Testes (42 testes unitários) | ✅ |

### Semana 10: DeveloperPortal — Estado Real (Revisão Março 2026)

> **Nota:** Análise baseada no código real. O módulo tem Domain completo mas Infrastructure, API e testes em scaffold.

| Camada / Feature | Status | Detalhe |
|---------|--------|---------|
| **Domain** (5 aggregates, 4 enums, 11 errors) | ✅ | CodeGenerationRecord, PlaygroundSession, PortalAnalyticsEvent, SavedSearch, Subscription |
| **Application** — 8 features implementados | ✅ | CreateSubscription, DeleteSubscription, GenerateCode, GetSubscriptions, GetApiConsumers, GetPlaygroundHistory, GetPortalAnalytics, RecordAnalyticsEvent |
| **Application** — 8 features stubs | 🟡 | ExecutePlayground (mock), SearchCatalog (vazio), GetApiDetail (stub), GetApiHealth (stub), GetMyApis (stub), GetApisIConsume (stub), GetAssetTimeline (stub), RenderOpenApiContract (stub) |
| **Application DI** — MediatR + validators | ❌ | TODO não implementado — handlers não são registrados |
| **Infrastructure** — DbContext | ❌ | Existe mas sem DbSets, sem configs, sem repos |
| **Infrastructure** — Repositories | ❌ | 5 interfaces definidas mas sem implementação |
| **Infrastructure** — Migrations | ❌ | Sem migrations EF Core |
| **Infrastructure DI** — DbContext + repos | ❌ | TODO não implementado |
| **API** — Endpoints | ❌ | EndpointModule existe mas sem rotas mapeadas |
| **Contracts** — IDeveloperPortalModule | ❌ | Interface vazia (sem métodos) |
| **Registro no ApiHost** | ❌ | Módulo não está em Program.cs |
| **Testes** | ❌ | 1 placeholder (Assert.True(true)) |
| **Frontend** | ❌ | Sem página dedicada |

**Entregável Contracts:** ✅ Catálogo de APIs com diff semântico e classificação de breaking changes.
**Entregável DeveloperPortal:** ❌ Incompleto — precisa de Infrastructure, API, DI e testes para ser funcional.

---

## FASE 4 — Change Intelligence (Semanas 11–14) — ✅ CONCLUÍDA

### Semana 11: ChangeIntelligence — Pipeline Inicial ✅

| Feature | Status |
|---------|--------|
| Release (Aggregate Root) | ✅ |
| ChangeEvent (Entity) | ✅ |
| NotifyDeployment | ✅ |
| ClassifyChangeLevel | ✅ |
| UpdateDeploymentState | ✅ |

### Semana 12: ChangeIntelligence — Blast Radius ✅

| Feature | Status |
|---------|--------|
| BlastRadiusReport (Entity) | ✅ |
| CalculateBlastRadius (consome EngineeringGraph via Contracts) | ✅ |
| ComputeChangeScore | ✅ |

### Semana 13: ChangeIntelligence — Queries e Rollback ✅

| Feature | Status |
|---------|--------|
| GetRelease | ✅ |
| ListReleases | ✅ |
| GetBlastRadiusReport | ✅ |
| RegisterRollback | ✅ |
| ChangeIntelligenceDbContext + Migrations | ✅ |

### Semana 14: RulesetGovernance ✅

| Feature | Status |
|---------|--------|
| Ruleset (Aggregate Root) | ✅ |
| LintResult (Entity) | ✅ |
| UploadRuleset | ✅ |
| ExecuteLintForRelease | ✅ |
| GetRulesetFindings | ✅ |
| ComputeRulesetScore | ✅ |
| InstallDefaultRulesets | ✅ |
| RulesetGovernanceDbContext + Migrations | ✅ |

**Entregável:** ✅ Pipeline completo de classificação de mudança com blast radius e score.

---

## FASE 5 — Governança e Aprovação (Semanas 15–18) — ✅ CONCLUÍDA

### Semana 15: Workflow — Template e Iniciação ✅

| Feature | Status |
|---------|--------|
| WorkflowTemplate (Aggregate Root) | ✅ |
| WorkflowInstance (Aggregate Root) | ✅ |
| WorkflowStage (Entity) | ✅ |
| ApprovalDecision (Entity) | ✅ |
| EvidencePack (Aggregate Root) | ✅ |
| SlaPolicy (Entity) | ✅ |
| CreateWorkflowTemplate | ✅ |
| InitiateWorkflow | ✅ |

### Semana 16: Workflow — Aprovação e Evidence Pack ✅

| Feature | Status |
|---------|--------|
| ApproveStage | ✅ |
| RejectWorkflow | ✅ |
| RequestChanges | ✅ |
| AddObservation | ✅ |
| GetWorkflowStatus | ✅ |
| ListPendingApprovals | ✅ |
| GenerateEvidencePack | ✅ |
| GetEvidencePack | ✅ |
| ExportEvidencePackPdf | ✅ |
| EscalateSlaViolation | ✅ |
| WorkflowDbContext + Migrations | ✅ |
| IWorkflowModule (WorkflowModuleService) | ✅ |
| Testes unitários (domínio + aplicação) | ✅ |

### Semana 17: Promotion — Request e Gates ✅

| Feature | Status |
|---------|--------|
| PromotionRequest (Aggregate Root) | ✅ |
| PromotionGate (Entity) | ✅ |
| CreatePromotionRequest | ✅ |
| EvaluatePromotionGates | ✅ |

### Semana 18: Promotion — Status e Rollback ✅

| Feature | Status |
|---------|--------|
| GetPromotionStatus | ✅ |
| ApprovePromotion | ✅ |
| BlockPromotion | ✅ |
| PromotionDbContext + Migrations | ✅ |
| IPromotionModule (Contracts) | ✅ |
| Registo no ApiHost | ✅ |

**Entregável:** ✅ Fluxo de aprovação completo com evidence pack em PDF.

---

## FASE 6 — Auditoria e Compliance (Semanas 19–20) — ✅ CONCLUÍDA

### Semana 19: Audit — Registro ✅

| Feature | Status |
|---------|--------|
| AuditEvent (Aggregate Root) | ✅ |
| AuditChainLink (Entity / hash chain SHA-256) | ✅ |
| RecordAuditEvent | ✅ |
| GetAuditTrail | ✅ |
| SearchAuditLog | ✅ |

### Semana 20: Audit — Integridade e Relatórios ✅

| Feature | Status |
|---------|--------|
| VerifyChainIntegrity | ✅ |
| ExportAuditReport | ✅ |
| GetComplianceReport | ✅ |
| AuditDbContext + IUnitOfWork | ✅ |
| IAuditModule (Contracts) | ✅ |
| Registo no ApiHost | ✅ |

**Entregável:** ✅ Hash chain auditável SHA-256 com relatórios de compliance.

---

## FASE 7 — Intelligence Parcial (Semanas 21–22)

> **Nota:** Incluídos parcialmente no MVP1. Análise de tendências e IA ficam para MVP2.

### Semana 21: RuntimeIntelligence (parcial)

| Feature | Status |
|---------|--------|
| RuntimeSnapshot (Aggregate Root) | 🔲 |
| IngestRuntimeSnapshot | 🔲 |
| GetRuntimeDriftFindings (básico) | 🔲 |
| RuntimeIntelligenceDbContext + Migrations | 🔲 |

### Semana 22: CostIntelligence (parcial)

| Feature | Status |
|---------|--------|
| CostSnapshot (Aggregate Root) | 🔲 |
| IngestCostSnapshot | 🔲 |
| GetCostByRelease | 🔲 |
| AttributeCostToService | 🔲 |
| CostIntelligenceDbContext + Migrations | 🔲 |

**Entregável:** Correlação de custo e runtime com releases.

---

## FASE 8 — Hardening e Entrega (Semanas 23–26)

| Semana | Objetivo | Status |
|--------|----------|--------|
| 23 | CLI `nex` completo (validate, release, notify, approval, impact) | 🔲 |
| 24 | Testes de integração E2E completos + performance testing | 🔲 |
| 25 | Docker Compose completo + documentação de deploy + secrets management | 🔲 |
| 26 | Onboarding Accelerator + documentação de API + treinamento | 🔲 |

---

## Próximos Passos Imediatos (Revisão Março 2026)

### P0 — Blockers / Críticos
1. **DeveloperPortal Infrastructure:** Implementar DbContext (DbSets, configs), 5 repositories, gerar migrations EF Core
2. **DeveloperPortal DI wiring:** Registrar MediatR handlers, FluentValidation validators, repositories, DbContext
3. **DeveloperPortal API endpoints:** Mapear 16 endpoints no EndpointModule
4. **DeveloperPortal registro no ApiHost:** Adicionar `AddDeveloperPortalModule()` em Program.cs
5. **Identity Migrations:** Gerar migrations EF Core explícitas (substituir auto-migration)

### P1 — Importante
1. **DeveloperPortal testes:** Criar testes unitários para domain + features implementadas (mínimo 20 testes)
2. **Licensing testes:** Expandir de 8 para ≥30 testes (trial lifecycle, quotas, hardware binding)
3. **i18n pt-PT:** Criar locale pt-PT.json com adaptações para português europeu
4. **i18n es:** Criar locale es.json com traduções para espanhol
5. **i18n config:** Atualizar i18n.ts para registrar pt-PT e es
6. **Swagger UI:** Habilitar interface interativa para exploração das APIs
7. **API key / Client credentials:** Implementar autenticação sistema-a-sistema para integrações externas
8. **Rate limiting:** Implementar throttling nas APIs públicas
9. **DeveloperPortal features integração:** Integrar stubs com EngineeringGraph e Contracts
10. **Frontend Licensing page:** Criar dashboard de licenciamento
11. **Frontend DeveloperPortal page:** Criar UI do portal

### P2 — Evolução Recomendada
1. **API versioning middleware:** Implementar versionamento formal (Asp.Versioning)
2. **Idempotency keys:** Generalizar padrão do SyncConsumers para outros endpoints
3. **Webhook outbound dispatcher:** Implementar envio de notificações via webhook
4. **Documentação de integração:** Criar EXTERNAL-INTEGRATION-API.md para cada módulo
5. **Frontend graph visualization:** Apache ECharts para visualização de grafos
6. **Licensing enforcement behavior:** MediatR behavior para verificar capabilities
7. **Seletor de 4 idiomas:** Atualizar AppHeader com dropdown para en, pt-BR, pt-PT, es
8. **Testes E2E:** Expandir para fluxos completos de ponta a ponta

### P3 — Melhorias Futuras
1. **OTel receptor real:** Implementar discovery automático via OpenTelemetry
2. **Offline licensing:** Implementar cache local para validação offline
3. **ImportFromBackstage/Kong real:** Integração com APIs reais do Backstage e Kong
4. **CORS para parceiros:** Configuração dinâmica de origens para integrações

---

## Roadmap de i18n

### Estado Atual
| Idioma | Status | Ficheiro |
|--------|--------|----------|
| English (en) | ✅ Completo | src/frontend/src/locales/en.json (175+ chaves) |
| Português Brasil (pt-BR) | ✅ Completo | src/frontend/src/locales/pt-BR.json (175+ chaves) |
| Português Portugal (pt-PT) | 🟡 Criado (base) | src/frontend/src/locales/pt-PT.json |
| Espanhol (es) | 🟡 Criado (base) | src/frontend/src/locales/es.json |

### Tarefas Pendentes
1. ✅ Infraestrutura i18next + react-i18next configurada
2. ✅ Detecção automática de idioma do browser
3. 🟡 Criar ficheiro pt-PT.json com adaptações lexicais
4. 🟡 Criar ficheiro es.json com traduções completas
5. 🟡 Atualizar i18n.ts para registrar 4 idiomas
6. 🔲 Atualizar AppHeader com seletor de 4 idiomas (dropdown em vez de toggle)
7. 🔲 Adicionar namespace developerPortal.* nas 4 locales
8. 🔲 Adicionar namespace licensing.* nas 4 locales
9. 🔲 Backend: Criar SharedMessages.pt-PT.resx e SharedMessages.es.resx

---

## Roadmap de Integração Externa das APIs

### Estado Atual
| Capacidade | Status |
|-----------|--------|
| APIs sob /api/v1/ (versioning manual) | ✅ |
| SyncConsumers (referência inbound) | ✅ |
| OpenAPI JSON (.NET 10 nativo) | ✅ (Development only) |
| Result<T>.ToHttpResult() padronizado | ✅ |
| Error codes i18n | ✅ |
| Multi-tenancy via header/JWT | ✅ |
| Postman collection | ✅ |
| Swagger UI | ❌ |
| API key / Client credentials | ❌ |
| Rate limiting | ❌ |
| Formal API versioning middleware | ❌ |
| Idempotency keys (além de SyncConsumers) | ❌ |
| Webhook outbound | ❌ |
| Documentação por módulo | 🟡 (apenas EngineeringGraph) |

---

## Legenda

| Ícone | Significado |
|-------|------------|
| 🔲 | Pendente |
| 🟡 | Em desenvolvimento |
| ✅ | Concluído e testado |
| 🚫 | Bloqueado |

---

## Regras Absolutas de Desenvolvimento

1. **Um módulo por vez** — nunca dois módulos em paralelo
2. **Uma camada por vez** — Domain → Application → Infrastructure → API
3. **Um aggregate por vez** — completo antes de passar ao próximo
4. **Aprovação entre fases** — revisão de arquitetura e testes antes de avançar
5. **Código em inglês, comentários em português** — sem exceção
6. **XML documentation em PT-BR** — toda classe e método público
7. **Testes unitários adjacentes** — ao lado da feature implementada

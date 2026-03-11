# NexTraceOne — Roadmap MVP1 Expandido

## Visão Geral

Metodologia: **1 módulo por vez · 1 camada por vez · 1 aggregate por vez**.
Cada fase termina com aprovação antes de iniciar a próxima.

> **Última atualização:** Março 2026 — Pós-auditoria completa de código.
> Ver `docs/MVP1-EXPANDED-PLAN.md` para análise detalhada de valor vs. esforço por módulo.

---

## Estado Atual do Projeto (Março 2026)

### O que está concluído:

- ✅ **Building Blocks (6/6)** — Domain, Application, Infrastructure, EventBus, Observability, Security — 100% funcionais
- ✅ **Identity** — Login, FederatedLogin, RefreshToken, CreateUser, AssignRole, Sessions, RBAC — 100% funcional
- ✅ **Licensing (estrutura)** — Domain + Application features implementadas (90%) — Infrastructure/Repos pendente
- ✅ **Scaffold completo** — 14 módulos × 5 camadas criados (estrutura física e entities de domínio definidas)
- ✅ **Auditoria de código** — SOLID, DDD, CQRS, i18n, segurança, convenções — revisados e corrigidos
- ✅ **IntegrationEventBase** — base abstrata adicionada ao BuildingBlocks.Domain para todos os Integration Events
- ✅ **Testes unitários** — Building Blocks (Result, ValueObject, PagedList) e Identity (AssignRole, RevokeSession)

### O que está pendente (todos os outros módulos):

- 🟡 **Licensing** — completar Infrastructure (LicensingDbContext, Repositories, Migrations)
- 🔲 **EngineeringGraph** — features são stubs com TODO
- 🔲 **Contracts** — features são stubs com TODO
- 🔲 **ChangeIntelligence** — features são stubs com TODO (CORE DO PRODUTO)
- 🔲 **RulesetGovernance** — features são stubs com TODO
- 🔲 **Workflow** — features são stubs com TODO
- 🔲 **Promotion** — features são stubs com TODO
- 🔲 **Audit** — features são stubs com TODO
- 🔲 **DeveloperPortal** — features são stubs com TODO
- 🔲 **RuntimeIntelligence** (parcial) — features são stubs com TODO
- 🔲 **CostIntelligence** (parcial) — features são stubs com TODO
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

## FASE 2 — Licenciamento e Grafo (Semanas 5–7) ← PRÓXIMA

### Semana 5: Licensing — Completar Infrastructure

**Pré-requisito antes de avançar para EngineeringGraph.**

| Feature / Artefato | Status |
|---------------------|--------|
| License (Aggregate Root) | ✅ domain |
| LicenseCapability, HardwareBinding | ✅ domain |
| ActivateLicense | ✅ application |
| VerifyLicenseOnStartup | ✅ application |
| CheckCapability | ✅ application |
| TrackUsageMetric | ✅ application |
| `LicensingDbContext` com IUnitOfWork | 🔲 |
| `LicenseRepository` + `HardwareBindingRepository` | 🔲 |
| Migrations EF Core | 🔲 |
| `AddLicensingInfrastructure` DI | 🔲 |
| Testes de repositório | 🔲 |

### Semana 6: EngineeringGraph — Core

| Feature | Status |
|---------|--------|
| ApiAsset (Aggregate Root) | 🔲 |
| ConsumerRelationship (Entity) | 🔲 |
| RegisterApiAsset | 🔲 |
| RegisterServiceAsset | 🔲 |
| MapConsumerRelationship | 🔲 |
| GetAssetGraph | 🔲 |

### Semana 7: EngineeringGraph — Discovery

| Feature | Status |
|---------|--------|
| InferDependencyFromOtel (receptor passivo) | 🔲 |
| ValidateDiscoveredDependency | 🔲 |
| EngineeringGraphDbContext + Migrations | 🔲 |

**Entregável:** Grafo de dependências funcional com APIs e consumidores mapeados.

---

## FASE 3 — Contratos e Portal (Semanas 8–10)

### Semana 8: Contracts — Import e Histórico

| Feature | Status |
|---------|--------|
| ContractVersion (Aggregate Root) | 🔲 |
| ContractDiff (Entity) | 🔲 |
| ImportContract | 🔲 |
| GetContractHistory | 🔲 |
| ValidateContractIntegrity | 🔲 |

### Semana 9: Contracts — Diff Semântico

| Feature | Status |
|---------|--------|
| ComputeSemanticDiff | 🔲 |
| ClassifyBreakingChange | 🔲 |
| SuggestSemanticVersion | 🔲 |
| ContractsDbContext + Migrations | 🔲 |

### Semana 10: DeveloperPortal — Read Models

| Feature | Status |
|---------|--------|
| SearchCatalog | 🔲 |
| GetApiDetail | 🔲 |
| GetMyApis | 🔲 |
| GetApiConsumers | 🔲 |

**Entregável:** Catálogo de APIs com diff semântico e classificação de breaking changes.

---

## FASE 4 — Change Intelligence (Semanas 11–14) — CORE DO PRODUTO

### Semana 11: ChangeIntelligence — Pipeline Inicial

| Feature | Status |
|---------|--------|
| Release (Aggregate Root) | 🔲 |
| ChangeEvent (Entity) | 🔲 |
| NotifyDeployment | 🔲 |
| ClassifyChangeLevel | 🔲 |
| UpdateDeploymentState | 🔲 |

### Semana 12: ChangeIntelligence — Blast Radius

| Feature | Status |
|---------|--------|
| BlastRadiusReport (Entity) | 🔲 |
| CalculateBlastRadius (consome EngineeringGraph via Contracts) | 🔲 |
| ComputeChangeScore | 🔲 |

### Semana 13: ChangeIntelligence — Queries e Rollback

| Feature | Status |
|---------|--------|
| GetRelease | 🔲 |
| ListReleases | 🔲 |
| GetBlastRadiusReport | 🔲 |
| RegisterRollback | 🔲 |
| ChangeIntelligenceDbContext + Migrations | 🔲 |

### Semana 14: RulesetGovernance

| Feature | Status |
|---------|--------|
| Ruleset (Aggregate Root) | 🔲 |
| LintExecution (Entity) | 🔲 |
| UploadRuleset | 🔲 |
| ExecuteLintForRelease | 🔲 |
| GetLintResult | 🔲 |
| RulesetGovernanceDbContext + Migrations | 🔲 |

**Entregável:** Pipeline completo de classificação de mudança com blast radius e score.

---

## FASE 5 — Governança e Aprovação (Semanas 15–18)

### Semana 15: Workflow — Template e Iniciação

| Feature | Status |
|---------|--------|
| WorkflowTemplate (Aggregate Root) | 🔲 |
| WorkflowInstance (Entity) | 🔲 |
| CreateWorkflowTemplate | 🔲 |
| InitiateWorkflow | 🔲 |

### Semana 16: Workflow — Aprovação e Evidence Pack

| Feature | Status |
|---------|--------|
| ApproveStage | 🔲 |
| RejectWorkflow | 🔲 |
| GenerateEvidencePack | 🔲 |
| ExportEvidencePackPdf | 🔲 |
| WorkflowDbContext + Migrations | 🔲 |

### Semana 17: Promotion — Request e Gates

| Feature | Status |
|---------|--------|
| PromotionRequest (Aggregate Root) | 🔲 |
| PromotionGate (Entity) | 🔲 |
| CreatePromotionRequest | 🔲 |
| EvaluatePromotionGates | 🔲 |

### Semana 18: Promotion — Status e Rollback

| Feature | Status |
|---------|--------|
| GetPromotionStatus | 🔲 |
| ApprovePromotion | 🔲 |
| RollbackPromotion | 🔲 |
| PromotionDbContext + Migrations | 🔲 |

**Entregável:** Fluxo de aprovação completo com evidence pack em PDF.

---

## FASE 6 — Auditoria e Compliance (Semanas 19–20)

### Semana 19: Audit — Registro

| Feature | Status |
|---------|--------|
| AuditEvent (Aggregate Root) | 🔲 |
| AuditChainLink (Entity / hash chain) | 🔲 |
| RecordAuditEvent | 🔲 |
| GetAuditTrail | 🔲 |
| SearchAuditLog | 🔲 |

### Semana 20: Audit — Integridade e Relatórios

| Feature | Status |
|---------|--------|
| VerifyChainIntegrity | 🔲 |
| ExportAuditReport | 🔲 |
| GetComplianceReport | 🔲 |
| AuditDbContext + Migrations | 🔲 |

**Entregável:** Hash chain auditável SHA-256 com relatórios de compliance.

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

## Próximos Passos Imediatos

1. **Completar `LicensingDbContext`, `LicenseRepository` e migrations** (Fase 2, Semana 5)
2. **Implementar `EngineeringGraph`** — `RegisterApiAsset` + `MapConsumerRelationship` (Fase 2, Semana 6)
3. **Implementar `Contracts`** — `ImportContract` + `ComputeSemanticDiff` (Fase 3, Semana 8)
4. **Implementar `ChangeIntelligence`** — `NotifyDeployment` + `CalculateBlastRadius` (Fase 4, Semana 11)
5. **Registro no ApiHost** de todos os módulos conforme implementados

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

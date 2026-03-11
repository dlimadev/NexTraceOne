# NexTraceOne — Roadmap MVP1

## Visão Geral

Metodologia: **1 módulo por vez · 1 camada por vez · 1 aggregate por vez**.
Cada fase termina com aprovação antes de iniciar a próxima.

---

## FASE 1 — Fundação (Semanas 1–4)

### Semana 1–2: Building Blocks

**Objetivo:** Primitivos DDD e abstrações que sustentam toda a plataforma.

**Ordem de implementação:**

1. `ITypedId` + `TypedIdBase`
2. `Entity<TId>` + `AggregateRoot<TId>` + `ValueObject` + `AuditableEntity<TId>`
3. `IDomainEvent` + `DomainEventBase` + `IIntegrationEvent`
4. `Result<T>` + `Error` + `ErrorType`
5. `ICommand` + `IQuery` + `IPagedQuery`
6. `ICurrentUser` + `ICurrentTenant` + `IUnitOfWork` + `IDateTimeProvider` + `IEventBus`
7. Pipeline Behaviors (Validation → Logging → Performance → TenantIsolation → Transaction)
8. `NexTraceDbContextBase` + `RepositoryBase<T,TId>` + `OutboxMessage`
9. `NexTraceGuards` (extensões de domínio)
10. `PagedList<T>` + `ResultExtensions.ToHttpResult()`
11. Interceptors (`TenantRlsInterceptor`, `AuditInterceptor`)
12. `EncryptedStringConverter` (AES-256-GCM)
13. `AssemblyIntegrityChecker` + `HardwareFingerprint` (stub detalhado)
14. `TenantResolutionMiddleware`
15. `SerilogConfiguration` + `NexTraceActivitySources` + `NexTraceMeters`
16. `InProcessEventBus` + `OutboxEventBus`
17. `DependencyInjection.cs` de cada Building Block
18. Testes unitários completos de cada primitivo

**Entregável:** Building Blocks compilando com 100% de testes unitários.

### Semana 3–4: Módulo Identity

| Feature / Aggregate | Status |
|---------------------|--------|
| User (Aggregate Root) | 🔲 |
| Role, Permission (Entities) | 🔲 |
| TenantMembership | 🔲 |
| Session | 🔲 |
| FederatedLogin (OIDC) | 🔲 |
| LocalLogin | 🔲 |
| RefreshToken | 🔲 |
| CreateUser | 🔲 |
| AssignRole | 🔲 |
| ListTenantUsers | 🔲 |
| GetUserProfile | 🔲 |
| RevokeSession | 🔲 |
| IdentityDbContext + migrations | 🔲 |

---

## FASE 2 — Catálogo e Contratos (Semanas 5–8)

### Semana 5–6: Licensing

| Feature / Aggregate | Status |
|---------------------|--------|
| License (Aggregate Root) | 🔲 |
| LicenseCapability, HardwareBinding | 🔲 |
| ActivateLicense | 🔲 |
| VerifyLicenseOnStartup | 🔲 |
| CheckCapability | 🔲 |
| TrackUsageMetric | 🔲 |
| AlertLicenseThreshold | 🔲 |

### Semana 7–8: EngineeringGraph + DeveloperPortal + Contracts

| Módulo | Features Principais | Status |
|--------|-------------------|--------|
| EngineeringGraph | RegisterApiAsset, MapConsumerRelationship, GetAssetGraph, InferDependencyFromOtel | 🔲 |
| DeveloperPortal | SearchCatalog, GetApiDetail, GetMyApis, RenderOpenApiContract | 🔲 |
| Contracts | ImportContract, ComputeSemanticDiff, ClassifyBreakingChange, SuggestSemanticVersion | 🔲 |

---

## FASE 3 — Change Intelligence — CORE (Semanas 9–12)

### ChangeIntelligence + RulesetGovernance

| Feature | Status |
|---------|--------|
| Release (Aggregate Root) | 🔲 |
| ChangeEvent, BlastRadiusReport, ChangeIntelligenceScore | 🔲 |
| NotifyDeployment | 🔲 |
| ClassifyChangeLevel | 🔲 |
| CalculateBlastRadius | 🔲 |
| ComputeChangeScore | 🔲 |
| RegisterRollback | 🔲 |
| Ruleset, LintExecution | 🔲 |
| UploadRuleset, ExecuteLintForRelease | 🔲 |

---

## FASE 4 — Workflow e Promoção (Semanas 13–16)

### Workflow + Promotion

| Feature | Status |
|---------|--------|
| WorkflowTemplate, WorkflowInstance | 🔲 |
| InitiateWorkflow, ApproveStage, RejectWorkflow | 🔲 |
| GenerateEvidencePack, ExportEvidencePackPdf | 🔲 |
| PromotionRequest, PromotionGate | 🔲 |
| CreatePromotionRequest, EvaluatePromotionGates | 🔲 |

---

## FASE 5 — Audit e IA (Semanas 17–20)

### Audit + AiOrchestration + ExternalAi

| Feature | Status |
|---------|--------|
| AuditEvent, AuditChainLink (hash chain) | 🔲 |
| RecordAuditEvent, VerifyChainIntegrity | 🔲 |
| ClassifyChangeWithAI, SummarizeReleaseForApproval | 🔲 |
| GenerateTestScenarios (Robot Framework) | 🔲 |
| CLI `nex` completo | 🔲 |

---

## FASE 6 — Hardening e Entrega (Semanas 21–24)

| Semana | Objetivo |
|--------|----------|
| 21 | Testes de integração E2E completos |
| 22 | Performance testing e otimização de queries |
| 23 | Docker Compose completo + documentação de deploy |
| 24 | Onboarding Accelerator + CLI completo |

---

## Estado Atual do Projeto

### O que já existe:

- ✅ Prompt Master de contexto completo
- ✅ Documentos de arquitetura (ARCHITECTURE.md, CONVENTIONS.md, DOMAIN.md, SECURITY.md)
- ✅ Script PowerShell de scaffold v2 (Archon Pattern) — 2844 linhas
- ✅ CLAUDE.md para Claude Code com contexto completo

### O que NÃO foi implementado ainda:

- 🔲 Execução do scaffold (criar projetos físicos)
- 🔲 Lógica real de nenhum Building Block
- 🔲 Nenhum handler implementado
- 🔲 Nenhuma migration do EF Core
- 🔲 Nenhum endpoint funcional
- 🔲 Nenhum teste com assertion real

### Próximo passo imediato:

**Executar o scaffold PowerShell → Implementar BuildingBlocks.Domain (Fase 1, Semana 1)**

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

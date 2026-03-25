# Relatório de Estado do Backend — NexTraceOne

**Data:** 25 de março de 2026

---

## 1. Objectivo

Auditar o estado real do backend — camada por camada, módulo por módulo — com evidência de ficheiro concreto. Avaliar qualidade arquitectural, cobertura de regras de negócio, padrões de código e alinhamento com a visão do produto.

---

## 2. Building Blocks — Estado

### 2.1 BuildingBlocks.Core

**Estado: READY**

**Evidência em `src/building-blocks/NexTraceOne.BuildingBlocks.Core/`:**

- `Primitives/AuditableEntity.cs` — base com `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`, método `SoftDelete()`
- `Primitives/Entity.cs` — base com strongly-typed ID via `ITypedId`
- `Primitives/AggregateRoot.cs` — base com domain events via `IDomainEvent`
- `Primitives/ValueObject.cs` — value object com igualdade por valor
- `Results/Result<T>` — pattern de retorno para falhas controladas sem exceptions
- `Abstractions/ITypedId.cs` — contrato para IDs fortemente tipados
- `Guards/NexTraceGuards.cs` — guard clauses via Ardalis.GuardClauses
- `Enums/ChangeLevel.cs`, `DiscoveryConfidence.cs` — enumerações de domínio
- `Attributes/EncryptedFieldAttribute.cs` — marcação de campos encriptados

**Verificação de regras obrigatórias:**
- ✅ `sealed` aplicado em classes finais
- ✅ `Result<T>` para falhas controladas
- ✅ Guard clauses implementadas
- ✅ Strongly-typed IDs
- ✅ `DateTimeOffset` (não `DateTime.Now`)

---

### 2.2 BuildingBlocks.Application

**Estado: READY**

**Evidência em `src/building-blocks/NexTraceOne.BuildingBlocks.Application/`:**

- `Abstractions/ICommand.cs`, `IQuery.cs`, `IPagedQuery.cs` — contratos CQRS
- `Behaviors/ValidationBehavior.cs` — FluentValidation automático no pipeline
- `Behaviors/TenantIsolationBehavior.cs` — isolamento multi-tenant no pipeline
- `Behaviors/LoggingBehavior.cs`, `PerformanceBehavior.cs`, `TransactionBehavior.cs`
- `Context/DistributedExecutionContext.cs` — propagação de contexto (tenant, user, env)
- `Abstractions/ICurrentUser.cs`, `ICurrentTenant.cs`, `ICurrentEnvironment.cs`
- `Abstractions/IEventBus.cs`, `IUnitOfWork.cs`
- `Pagination/PagedList<T>` — lista paginada
- `Localization/ErrorLocalizer.cs` — i18n de erros

---

### 2.3 BuildingBlocks.Infrastructure

**Estado: READY**

**Evidência:**

- `Persistence/NexTraceDbContextBase.cs` — base para todos os DbContexts com:
  - `TenantRlsInterceptor` — Row-Level Security
  - `AuditInterceptor` — preenchimento automático de campos de auditoria
  - `EncryptionInterceptor` — encriptação de campos marcados
  - Domain Events → Outbox pattern
  - Global soft-delete filter (`HasQueryFilter(e => !e.IsDeleted)`)
  - `CommitAsync()` como `IUnitOfWork`

---

### 2.4 BuildingBlocks.Security

**Estado: PARTIAL (BROKEN em configuração)**

**Implementação técnica sólida:**
- `Authentication/JwtTokenService.cs` — geração e validação JWT com PBKDF2
- `Encryption/AesGcmEncryptor.cs` — AES-256-GCM com nonce aleatório
- `MultiTenancy/TenantResolutionMiddleware.cs` — resolução de tenant do JWT / header
- `Authorization/PermissionAuthorizationHandler.cs` — RBAC granular
- `RateLimiting/` — políticas por endpoint (auth: 20/min, AI: 30/min, global: 100/min)
- `Csrf/CsrfTokenService.cs` — double-submit cookie pattern
- `Integrity/AssemblyIntegrityChecker.cs` — verificação de integridade de assemblies
- `Audit/SecurityAuditService.cs` — eventos de segurança auditados

**Problemas CRÍTICOS de configuração (não de implementação):**

| Problema | Localização | Severity |
|----------|-------------|----------|
| Fallback JWT key hardcoded | `Authentication/JwtTokenService.cs:48` | CRITICAL |
| Fallback AES key hardcoded | `Encryption/AesGcmEncryptor.cs:113` | HIGH |
| Senha BD hardcoded em appsettings | `appsettings.json` (21 ocorrências) | CRITICAL |
| JWT Secret vazio em produção | `appsettings.json:34` | CRITICAL |

---

## 3. Módulos — Auditoria Detalhada

### 3.1 IdentityAccess

**Estado: READY**
**Ficheiros C#:** 178
**Bounded context:** `identityaccess`

**Domain (entidades verificadas):**
- v1.0: `Tenant`, `User`, `Role`, `Permission`, `Session`, `TenantMembership`
- v1.1: `ExternalIdentity`, `SsoGroupMapping`, `BreakGlassRequest`, `JitAccessRequest`, `Delegation`, `AccessReviewCampaign`, `AccessReviewItem`, `SecurityEvent`
- v1.2: `Environment`, `EnvironmentAccess`

**Application (handlers verificados):**
- `LoginWithPasswordCommand` — autenticação local com PBKDF2
- `RefreshTokenCommand` — rotação de refresh token
- `CreateUserCommand`, `UpdateUserCommand`, `DeactivateUserCommand`
- `CreateTenantCommand`, `SelectTenantCommand`
- `RequestBreakGlassCommand`, `ApproveBreakGlassCommand`
- `RequestJitAccessCommand`, `ApproveJitAccessCommand`
- `CreateDelegationCommand`, `StartAccessReviewCampaignCommand`
- `CreateEnvironmentCommand`, `UpdateEnvironmentCommand`

**Infrastructure:**
- `IdentityDbContext` com 17 DbSets e 43 índices
- Migration `20260325210113_InitialCreate` criada e verificada
- Repositórios para todas as entidades

**API:**
- Endpoints para auth, users, roles, sessions, environments, break-glass, JIT, delegations

**Verificação de regras:**
- ✅ `CancellationToken` em todos os handlers
- ✅ `Result<T>` para falhas controladas
- ✅ Strongly-typed IDs (`UserId`, `TenantId`, `EnvironmentId`)
- ✅ Guard clauses
- ✅ Sem `DateTime.Now` — usa `DateTimeOffset`

---

### 3.2 Catalog

**Estado: PARTIAL**
**Ficheiros C#:** 260
**Bounded context:** `catalog`

**Subdomains:**
1. **Contracts** — `ContractsDbContext` com 11 entidades
2. **Graph** — `CatalogGraphDbContext` com 9 entidades
3. **DeveloperPortal** — `DeveloperPortalDbContext` com 5 entidades

**Pontos fortes:**
- Contract lifecycle completo (Draft→InReview→Approved→Locked→Deprecated)
- `SpectralRuleset` para validação de qualidade de contratos
- `CatalogGraphDbContext` com relacionamentos de dependência entre serviços
- `ContractDiff`, `ContractScorecard`, `ContractEvidencePack` como entidades reais
- Check constraint em `protocol` para tipos de contrato

**Lacunas:**
- SOAP/WSDL: declarado no constraint mas sem handlers específicos WSDL/SOAP
- AsyncAPI/Event Contracts: declarado no constraint mas sem workflow específico
- Background Service Contracts: não encontrado
- Developer Portal fluxo de subscrição não verificado end-to-end

---

### 3.3 ChangeGovernance

**Estado: PARTIAL**
**Ficheiros C#:** 227
**Bounded context:** `changegovernance`

**Subdomains:**
1. **ChangeIntelligence** — 10 entidades (Release, BlastRadius, ChangeScore, ChangeEvent, etc.)
2. **Promotion** — 4 entidades (DeploymentEnvironment, PromotionRequest, PromotionGate, GateEvaluation)
3. **RulesetGovernance** — 3 entidades (Ruleset, RulesetBinding, LintResult)
4. **Workflow** — 6 entidades (WorkflowTemplate, WorkflowInstance, WorkflowStage, EvidencePack, SlaPolicy, ApprovalDecision)

**Pontos fortes:**
- `BlastRadiusReport` com modelagem real de impacto
- `FreezeWindow` para gestão de janelas de mudança
- `RollbackAssessment` para inteligência de rollback
- `PostReleaseReview` para verificação pós-release
- EvidencePack como entidade própria no Workflow

**Lacunas:**
- Workflow multi-stage: `WorkflowInstance` com stages existe; fluxo de aprovação completo não verificado
- Correlação automática deploy → release → telemetria não verificada end-to-end
- Release Calendar: `FreezeWindow` existe mas UI de calendário não verificada

---

### 3.4 OperationalIntelligence

**Estado: PARTIAL**
**Ficheiros C#:** 209
**Bounded context:** `operationalintelligence`

**Subdomains:**
1. **Incidents** — 5 entidades (real)
2. **Automation** — 3 entidades (real)
3. **Reliability** — 1 entidade (`ReliabilitySnapshot` — mínima)
4. **Runtime** — 4 entidades (real para comparação de ambientes)
5. **Cost** — 6 entidades (schema real; pipeline analítico não verificado)

**Pontos fortes:**
- `IncidentRecord` com correlação a mudanças
- `MitigationWorkflowRecord` com log de acções
- `AutomationWorkflowRecord` com validação e auditoria
- `RuntimeSnapshot` e `DriftFinding` para comparação entre ambientes
- `CostRecord` e `CostAttribution` para alocação de custos

**Lacunas:**
- `ReliabilityDbContext` com apenas 1 entidade — SLO tracking insuficiente
- Cost analytics pipeline (importação, correlação, tendências) não verificado como funcional
- Anomaly detection: configuração UI existe; lógica real não verificada

**Nota arquitectural:** GovernanceDbContext contém 4 entidades de outros módulos pending extracção: `IntegrationConnector`, `IngestionSource`, `IngestionExecution`, `AnalyticsEvent`

---

### 3.5 AIKnowledge

**Estado: PARTIAL**
**Ficheiros C#:** 267
**Bounded context:** `aiknowledge`

**Subdomain Governance — REAL (95%):**
- 14 entidades de domínio verificadas
- 24/28 handlers reais com lógica real
- 11 repositórios EF Core
- 27+ endpoints REST com permission checks
- Runtime providers: `OllamaProvider` (185 linhas, completo), `OpenAiProvider` (132 linhas, completo)
- `AiAgentRuntimeService` com pipeline 12-step

**Subdomain ExternalAI — STUB:**
- 4 entidades de domínio
- 1/8 handlers com lógica (`QueryExternalAISimple` — sem endpoint)
- `ExternalAiDbContext` com **0 DbSets** (TODO)
- DI.cs com comentários TODO
- 7/8 features 100% TODO

**Subdomain Orchestration — STUB:**
- 4 entidades no DbContext sem configurações
- 0/8 handlers implementados (todos TODO)
- `AiOrchestrationDbContext` sem repositórios
- DI.cs com comentários TODO

**Handlers mock (2):**
- `ListKnowledgeSourceWeights` — retorna 11 pesos hardcoded em memória
- `ListSuggestedPrompts` — retorna 21 prompts hardcoded

**Não implementado:**
- Streaming (Stream=false hardcoded em ambos os providers)
- Tool execution (AllowedTools campo existe; executor não wired)
- RAG/retrieval: interfaces registadas, implementações não verificadas
- Vector database: não existe no docker-compose ou codebase

---

### 3.6 Governance

**Estado: PARTIAL**
**Ficheiros C#:** 175
**Bounded context:** `governance`

**Entidades reais (8):**
`Team`, `GovernanceDomain`, `GovernancePack`, `GovernancePackVersion`, `GovernanceWaiver`, `DelegatedAdministration`, `TeamDomainLink`, `GovernanceRolloutRecord`

**Entidades temporárias (4) — candidatas a extracção:**
`IntegrationConnector`, `IngestionSource`, `IngestionExecution`, `AnalyticsEvent`

**Estado:** Governance core funcional. Entidades de integração/analytics no contexto errado.

---

### 3.7 AuditCompliance

**Estado: PARTIAL**
**Ficheiros C#:** 50
**Bounded context:** `auditcompliance`

**Entidades:** `AuditEvent`, `AuditChainLink`, `RetentionPolicy`, `CompliancePolicy`, `AuditCampaign`, `ComplianceResult`

**Ponto forte:** Hash chain (SHA-256) para integridade do audit trail — cada evento liga ao anterior

**Lacunas:**
- Cobertura mínima — 6 entidades para compliance enterprise
- Sem entidade de evidência ligada a contratos/mudanças
- Fluxos de compliance audit campaign não verificados

---

### 3.8 Notifications

**Estado: INCOMPLETE**
**Ficheiros C#:** 92
**Bounded context:** `notifications`

**Entidades:** `Notification`, `NotificationDelivery`, `NotificationPreference`

**Lacunas:**
- Apenas 3 entidades — muito mínimo para notificações enterprise
- Sem templates de conteúdo reais
- Sem entidade de canal (email, Slack, webhook)
- Integração com SMTP verificada? Não confirmada.

---

### 3.9 Configuration

**Estado: INCOMPLETE**
**Ficheiros C#:** 43
**Bounded context:** `configuration`

**Entidades:** `ConfigurationDefinition`, `ConfigurationEntry`, `ConfigurationAuditEntry`

**Lacunas:**
- Apenas 3 entidades — insuficiente para parametrização enterprise
- Sem hierarquia tenant/environment/module
- Sem validação de schema para valores de configuração
- Sem feature flags como entidade explícita

---

## 4. Plataforma

### 4.1 ApiHost

**Estado: PARTIAL**

**Program.cs verificado:**
- Middleware pipeline: HTTPS, security headers, rate limiting, CORS, auth, RLS, tenant resolution
- Registro de todos os 9 módulos
- Health checks: /health, /ready, /live
- OpenAPI apenas em Development
- ProblemDetails sem stack traces

**Configuração problemática:**
- 21 connection strings com senha hardcoded "ouro18"
- JWT Secret vazio

### 4.2 BackgroundWorkers

**Estado:** Existe como projecto; Quartz configurado; jobs específicos não auditados em detalhe.

### 4.3 Ingestion.Api

**Estado:** Existe como projecto separado; integração com OpenTelemetry não auditada em detalhe.

---

## 5. Regras de Código — Conformidade

| Regra | Estado | Evidência |
|-------|--------|-----------|
| `sealed` para classes finais | PARCIAL | Verificado em alguns; não enforçado globalmente |
| `CancellationToken` em async | CUMPRIDO | Verificado nos handlers auditados |
| `Result<T>` para falhas | CUMPRIDO | BuildingBlocks.Core implementado e usado |
| Guard clauses | CUMPRIDO | NexTraceGuards + Ardalis |
| Strongly-typed IDs | CUMPRIDO | UserId, TenantId, EnvironmentId verificados |
| Nunca `DateTime.Now` | CUMPRIDO | `DateTimeOffset` e `IDateTimeProvider` |
| Logging estruturado | CUMPRIDO | Serilog configurado no BuildingBlocks.Observability |
| DbContext isolado por módulo | CUMPRIDO | Comunicação por contracts/events |
| Domínio separado de infra | CUMPRIDO | 5 camadas por módulo |
| API fina | CUMPRIDO | Controllers sem lógica — CQRS via MediatR |
| Sem services gigantes | VERIFICADO EM PARTE | AiAgentRuntimeService (268 linhas) — aceitável |

---

## 6. Resumo de Recomendações

| Prioridade | Acção |
|-----------|-------|
| P0 | Remover credenciais hardcoded dos ficheiros de config |
| P0 | Tornar JWT_SECRET obrigatório via env var |
| P1 | Completar ExternalAI domain (DbContext + 7 features) |
| P1 | Wiring de tool execution no AiAgentRuntimeService |
| P1 | Completar Workflow multi-stage no ChangeGovernance |
| P2 | Extrair 4 entidades temporárias do GovernanceDbContext |
| P2 | Expandir Notifications com templates e canais |
| P2 | Expandir Configuration com hierarquia e feature flags |
| P2 | Implementar streaming nos providers de IA |
| P3 | Completar Orchestration domain (8 features TODO) |
| P3 | Melhorar ReliabilityDbContext com SLO tracking |

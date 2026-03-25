# Relatório de Estado do Banco de Dados — NexTraceOne

**Data:** 25 de março de 2026

---

## 1. Objectivo

Auditar o estado real da camada de persistência: DbContexts, entidades, migrações, schema, padrões de isolamento e adequação ao domínio do produto.

---

## 2. Arquitectura de Persistência

### 2.1 Base de Dados Física

- **PostgreSQL 16** — base de dados relacional principal
- **ClickHouse 24.8** — base de dados analítica para observabilidade
- **Estratégia de isolamento:** prefixos de tabela por módulo dentro de uma única base de dados `nextraceone`

**Evidência:** `infra/postgres/init-databases.sql`, `docker-compose.yml`

### 2.2 Prefixos de Tabela por Módulo

| Prefixo | Módulo | Exemplo |
|---------|--------|---------|
| `iam_` | IdentityAccess | `iam_users`, `iam_sessions` |
| `env_` | Environment Management | `env_environments` |
| `cfg_` | Configuration | `cfg_definitions` |
| `cat_` | Catalog Graph | `cat_api_assets` |
| `dp_` | Developer Portal | `dp_subscriptions` |
| `ctr_` | Contracts | `ctr_contract_versions` |
| `chg_` | Change Governance | `chg_releases`, `chg_prm_*` |
| `ops_` | Operational Intelligence | `ops_inc_*`, `ops_cost_*` |
| `aud_` | Audit & Compliance | `aud_audit_events` |
| `gov_` | Governance | `gov_teams` |
| `ntf_` | Notifications | `ntf_notifications` |
| `aik_` | AI Knowledge | `aik_gov_*`, `aik_ext_*` |

---

## 3. DbContexts — Inventário Completo (20)

### 3.1 NexTraceDbContextBase

**Ficheiro:** `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Persistence/NexTraceDbContextBase.cs`

Base para todos os DbContexts com:
- `TenantRlsInterceptor` — Row-Level Security PostgreSQL
- `AuditInterceptor` — preenchimento automático de campos de auditoria
- `EncryptionInterceptor` — AES-256-GCM para campos `[EncryptedField]`
- Domain Events → OutboxMessage por commit
- Global soft-delete filter (`HasQueryFilter(e => !e.IsDeleted)`)
- `CommitAsync()` como `IUnitOfWork`

---

### 3.2 Módulo IdentityAccess

| DbContext | Tabelas | Outbox | Migration |
|-----------|---------|--------|-----------|
| IdentityDbContext | 17 | iam_outbox_messages | 20260325210113 |

**17 entidades:** Tenant, User, Role, Permission, Session, TenantMembership, ExternalIdentity, SsoGroupMapping, BreakGlassRequest, JitAccessRequest, Delegation, AccessReviewCampaign, AccessReviewItem, SecurityEvent, Environment, EnvironmentAccess

**Verificação migration:**
- 43 índices criados
- Unique index em `(TenantId, Slug)` para ambientes
- Filtered index para ambiente de produção primário
- Check constraints para status enumerações

**Estado:** READY

---

### 3.3 Módulo Catalog

| DbContext | Tabelas | Outbox | Migration |
|-----------|---------|--------|-----------|
| ContractsDbContext | 11 | ctr_outbox_messages | 20260325* |
| CatalogGraphDbContext | 9 | cat_outbox_messages | 20260325* |
| DeveloperPortalDbContext | 5 | cat_portal_outbox_messages | 20260325* |

**ContractsDbContext (11):** ContractVersion, ContractDraft, ContractReview, ContractDiff, ContractRuleViolation, ContractArtifact, ContractScorecard, ContractEvidencePack, ContractExample, CanonicalEntity, SpectralRuleset

**Check constraint verificado:**
```sql
protocol IN ('OpenApi', 'Swagger', 'Wsdl', 'AsyncApi', 'Protobuf', 'GraphQL')
```

**CatalogGraphDbContext (9):** ApiAsset, ServiceAsset, ConsumerRelationship, ConsumerAsset, DiscoverySource, GraphSnapshot, NodeHealthRecord, SavedGraphView, LinkedReference

**DeveloperPortalDbContext (5):** Subscription, PlaygroundSession, CodeGenerationRecord, PortalAnalyticsEvent, SavedSearch

**Estado:** READY

---

### 3.4 Módulo ChangeGovernance

| DbContext | Tabelas | Outbox | Migration |
|-----------|---------|--------|-----------|
| ChangeIntelligenceDbContext | 10 | chg_outbox_messages | 20260325* |
| PromotionDbContext | 4 | chg_prm_outbox_messages | 20260325* |
| RulesetGovernanceDbContext | 3 | chg_rg_outbox_messages | 20260325* |
| WorkflowDbContext | 6 | chg_wf_outbox_messages | 20260325* |

**ChangeIntelligenceDbContext (10):** Release, BlastRadiusReport, ChangeIntelligenceScore, ChangeEvent, ExternalMarker, FreezeWindow, ReleaseBaseline, ObservationWindow, PostReleaseReview, RollbackAssessment

**WorkflowDbContext (6):** WorkflowTemplate, WorkflowInstance, WorkflowStage, EvidencePack, SlaPolicy, ApprovalDecision

**Estado:** READY

---

### 3.5 Módulo OperationalIntelligence

| DbContext | Tabelas | Outbox | Migration |
|-----------|---------|--------|-----------|
| IncidentDbContext | 5 | ops_inc_outbox_messages | 20260325* |
| AutomationDbContext | 3 | ops_auto_outbox_messages | 20260325* |
| ReliabilityDbContext | 1 | ops_rel_outbox_messages | 20260325* |
| RuntimeIntelligenceDbContext | 4 | ops_rt_outbox_messages | 20260325* |
| CostIntelligenceDbContext | 6 | ops_cost_outbox_messages | 20260325* |

**IncidentDbContext (5):** IncidentRecord, MitigationWorkflowRecord, MitigationWorkflowActionLog, MitigationValidationLog, RunbookRecord

**CostIntelligenceDbContext (6):** CostSnapshot, CostAttribution, CostTrend, ServiceCostProfile, CostImportBatch, CostRecord

**Nota:** `ReliabilityDbContext` com apenas 1 entidade (`ReliabilitySnapshot`) — insuficiente para SLO tracking completo

**Estado:** PARTIAL

---

### 3.6 Módulo AIKnowledge

| DbContext | Tabelas | Outbox | Migration |
|-----------|---------|--------|-----------|
| AiGovernanceDbContext | 19 | aik_gov_outbox_messages | 20260325* |
| AiOrchestrationDbContext | 4 | aik_orch_outbox_messages | 20260325* |
| ExternalAiDbContext | 0 (TODO) | aik_ext_outbox_messages | 20260325* |

**AiGovernanceDbContext (19):** AIAccessPolicy, AIModel, AIBudget, AiAssistantConversation, AiMessage, AIUsageEntry, AIKnowledgeSource, AIIDEClientRegistration, AIIDECapabilityPolicy, AIRoutingDecision, AIRoutingStrategy, AiProvider, AiSource, AiTokenQuotaPolicy, AiTokenUsageLedger, AiExternalInferenceRecord, AiAgent, AiAgentExecution, AiAgentArtifact

**ExternalAiDbContext:** 0 DbSets definidos — TODO no ficheiro de contexto

**Estado:** PARTIAL (Governance 100%, ExternalAI 0%, Orchestration parcial)

---

### 3.7 Módulo Governance

| DbContext | Tabelas | Outbox | Migration |
|-----------|---------|--------|-----------|
| GovernanceDbContext | 12* | gov_outbox_messages | 20260325* |

**Entidades core (8):** Team, GovernanceDomain, GovernancePack, GovernancePackVersion, GovernanceWaiver, DelegatedAdministration, TeamDomainLink, GovernanceRolloutRecord

**Entidades temporárias (4 — candidatas a extracção):** IntegrationConnector, IngestionSource, IngestionExecution, AnalyticsEvent

**Estado:** PARTIAL — 4 entidades no contexto errado

---

### 3.8 Módulo AuditCompliance

| DbContext | Tabelas | Outbox | Migration |
|-----------|---------|--------|-----------|
| AuditDbContext | 6 | aud_outbox_messages | 20260325* |

**6 entidades:** AuditEvent, AuditChainLink, RetentionPolicy, CompliancePolicy, AuditCampaign, ComplianceResult

**Estado:** PARTIAL

---

### 3.9 Módulo Notifications

| DbContext | Tabelas | Outbox | Migration |
|-----------|---------|--------|-----------|
| NotificationsDbContext | 3 | ntf_outbox_messages | 20260325* |

**3 entidades:** Notification, NotificationDelivery, NotificationPreference

**Estado:** INCOMPLETE — demasiado mínimo para notificações enterprise

---

### 3.10 Módulo Configuration

| DbContext | Tabelas | Outbox | Migration |
|-----------|---------|--------|-----------|
| ConfigurationDbContext | 3 | cfg_outbox_messages | 20260325* |

**3 entidades:** ConfigurationDefinition, ConfigurationEntry, ConfigurationAuditEntry

**Estado:** INCOMPLETE — sem hierarquia tenant/env/module

---

## 4. Padrões de Dados — Conformidade

### 4.1 Campos de Auditoria

Verificados em `src/building-blocks/NexTraceOne.BuildingBlocks.Core/Primitives/AuditableEntity.cs`:

```csharp
public DateTimeOffset CreatedAt { get; private set; }
public string CreatedBy { get; private set; }
public DateTimeOffset? UpdatedAt { get; private set; }
public string? UpdatedBy { get; private set; }
public bool IsDeleted { get; private set; }
```

**Estado:** CUMPRIDO — todos os DbContexts herdam de `NexTraceDbContextBase`

### 4.2 Tenant Isolation

- `TenantId` presente em todas as entidades multi-tenant
- `TenantRlsInterceptor` aplica RLS a todos os queries
- `ICurrentTenant` injectado em todos os DbContexts

**Estado:** CUMPRIDO

### 4.3 Strongly-typed IDs

Verificado em `src/modules/identityaccess/.../UserConfiguration.cs`:
```csharp
builder.Property(x => x.Id)
    .HasConversion(id => id.Value, value => UserId.From(value));
```

**Estado:** CUMPRIDO

### 4.4 Soft Delete

- `IsDeleted = false` como default no schema
- Global query filter `HasQueryFilter(e => !e.IsDeleted)` aplicado automaticamente
- `SoftDelete()` method na base class

**Estado:** CUMPRIDO

### 4.5 Controlo de Concorrência

```sql
xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
```

**Estado:** CUMPRIDO — optimistic locking via PostgreSQL xmin

### 4.6 Outbox Pattern

Cada DbContext tem tabela `*_outbox_messages` para domain events.
Domain events são convertidos em OutboxMessages no `CommitAsync()`.

**Estado:** CUMPRIDO

### 4.7 Encriptação de Campos

`[EncryptedField]` attribute em campos sensíveis.
`EncryptionInterceptor` aplica AES-256-GCM transparentemente.

**Estado:** CUMPRIDO (excepto key hardcoded em dev — ver security report)

---

## 5. Migrações

### 5.1 Estado

Todos os 20 DbContexts têm migração `InitialCreate` datada de 25/03/2026.

**Formato:** EF Core code-first, namespace por módulo
**Localização:** `src/modules/[module]/[Module].Infrastructure/[Subdomain]/Persistence/Migrations/`

**Exemplo verificado:**
```
src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Migrations/20260325210113_InitialCreate.cs
```

### 5.2 Qualidade das Migrações

**Positivo:**
- Migrações `Up()` e `Down()` presentes
- Check constraints definidos inline
- Índices estratégicos criados
- Row version (xmin) configurado

**Lacunas:**
- Apenas 1 migração por DbContext (InitialCreate) — produto ainda sem histórico de evolução de schema

### 5.3 Seeds Legados

**Localização:** `docs/architecture/legacy-seeds/`

**Problema:** 7 ficheiros SQL usam prefixos de tabela antigos:
- `seed-identity.sql` → usa `identity_*` (correcto: `iam_*`)
- `seed-catalog.sql` → usa `eg_*`, `ct_*` (correcto: `cat_*`, `ctr_*`)
- `seed-incidents.sql` → usa `oi_*` (correcto: `ops_inc_*`)

**Estes ficheiros NÃO devem ser executados** — os prefixos estão errados e causariam falhas.

**Recomendação:** ARCHIVE_CANDIDATE — mover para `/docs/archive/legacy-seeds/`

---

## 6. ClickHouse Schema

**Ficheiro:** `build/clickhouse/init-schema.sql`

**Base de dados:** `nextraceone_obs` (separada do PostgreSQL)

**Tabelas:**

| Tabela | Motor | Partição | TTL | Propósito |
|--------|-------|----------|-----|-----------|
| `otel_logs` | MergeTree | Mensal | 30 dias | Logs OpenTelemetry |
| `otel_traces` | MergeTree | Mensal | 30 dias | Spans e traces |
| `otel_metrics` | MergeTree | Mensal | 90 dias | Métricas de tempo |

**Qualidade do schema:**
- Compressão Zstandard
- `LowCardinality` para colunas de alta cardinalidade
- Nested arrays para eventos/links nos traces
- TTL automático para purga de dados antigos

**Lacuna:** Sem tabelas específicas para correlação trace↔release↔service — depende de campos de label

**Estado:** READY (schema observabilidade), PARTIAL (correlação com negócio)

---

## 7. Problemas Identificados

| Problema | Tipo | Ficheiro | Severity |
|---------|------|----------|----------|
| Seeds legados com prefixos antigos | DEPRECATED | `docs/architecture/legacy-seeds/` | HIGH |
| ExternalAiDbContext com 0 DbSets | INCOMPLETE | `AIKnowledge.Infrastructure/ExternalAI/` | HIGH |
| GovernanceDbContext com 4 entidades de outros módulos | STRUCTURAL | `Governance.Infrastructure/Persistence/GovernanceDbContext.cs` | MEDIUM |
| ReliabilityDbContext com apenas 1 entidade | INCOMPLETE | `OperationalIntelligence.Infrastructure/Reliability/` | MEDIUM |
| NotificationsDbContext com 3 entidades mínimas | INCOMPLETE | `Notifications.Infrastructure/Persistence/` | MEDIUM |
| ConfigurationDbContext sem hierarquia | INCOMPLETE | `Configuration.Infrastructure/Persistence/` | MEDIUM |
| Sem tabela de correlação telemetria-release no ClickHouse | MISSING | `build/clickhouse/init-schema.sql` | HIGH |

---

## 8. Recomendações

| Prioridade | Acção |
|-----------|-------|
| P1 | Arquivar seeds legados em `docs/archive/` |
| P1 | Completar ExternalAiDbContext com DbSets e configurações |
| P2 | Extrair entidades temporárias do GovernanceDbContext |
| P2 | Expandir ReliabilityDbContext com SLO, SLA, error budget |
| P2 | Expandir NotificationsDbContext com template e canal entities |
| P2 | Expandir ConfigurationDbContext com hierarquia e feature flags |
| P2 | Adicionar tabela de correlação trace↔release no ClickHouse |
| P3 | Criar seeders programáticos em substituição dos SQL legados |

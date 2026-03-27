# Relatório de Estado do Banco de Dados — NexTraceOne
**Auditoria Forense | Março 2026**

---

## 1. Estratégia de Persistência

### Arquitetura
- **PostgreSQL 16** como único banco de dados central
- **1 banco físico:** `nextraceone` (UTF8, en_US.utf8)
- **Isolamento por prefixo de tabela** por módulo
- **EF Core** como ORM com migrações por DbContext
- **Multi-tenancy** via RLS (Row-Level Security) + isolamento na camada de aplicação

### Prefixos por Módulo
| Prefixo | Módulo |
|---|---|
| `iam_` | Identity & Access |
| `cat_` | Catalog (Graph, Contracts, Portal) |
| `ctr_` | Contracts |
| `aud_` | Audit & Compliance |
| `gov_` | Governance |
| `chg_` | Change Governance |
| `ops_` | Operational Intelligence |
| `aik_` | AI Knowledge |
| `ntf_` | Notifications |
| `cfg_` | Configuration |
| `int_` | Integrations |
| `knw_` | Knowledge |

---

## 2. Inventário de DbContexts

### Total: 24 DbContexts

| DbContext | Módulo | Migrações | Estado |
|---|---|---|---|
| IdentityDbContext | identityaccess | Sim (1+) | READY |
| ContractsDbContext | catalog | Sim (com snapshot) | READY |
| CatalogGraphDbContext | catalog | Sim (com snapshot) | READY |
| DeveloperPortalDbContext | catalog | Sim (com snapshot) | READY |
| AuditDbContext | auditcompliance | Sim (2: InitialCreate + P7_4_AuditCorrelationId) | READY |
| GovernanceDbContext | governance | Sim (com snapshot) | READY* |
| ChangeIntelligenceDbContext | changegovernance | Sim (com snapshot) | READY |
| WorkflowDbContext | changegovernance | Sim (com snapshot) | READY |
| PromotionDbContext | changegovernance | Sim (com snapshot) | READY |
| RulesetGovernanceDbContext | changegovernance | Sim (com snapshot) | READY |
| IncidentDbContext | operationalintelligence | Sim (com snapshot) | READY |
| AutomationDbContext | operationalintelligence | Sim (com snapshot) | READY |
| ReliabilityDbContext | operationalintelligence | Sim (com snapshot) | READY |
| RuntimeIntelligenceDbContext | operationalintelligence | Snapshot existe; migração confirmada? | PARTIAL |
| CostIntelligenceDbContext | operationalintelligence | Snapshot existe; migração confirmada? | PARTIAL |
| AiGovernanceDbContext | aiknowledge | Sim (com snapshot) | READY |
| AiOrchestrationDbContext | aiknowledge | Snapshot existe; migração confirmada? | PARTIAL |
| ExternalAiDbContext | aiknowledge | Snapshot existe; migração confirmada? | PARTIAL |
| ConfigurationDbContext | configuration | Sim (com snapshot) | READY |
| NotificationsDbContext | notifications | Sim (2+ migrações: 20260327082159, 20260327092812) | READY |
| IntegrationsDbContext | integrations | Snapshot existe; migração não confirmada | PARTIAL |
| ProductAnalyticsDbContext | productanalytics | Sem migrações confirmadas | INCOMPLETE |
| KnowledgeDbContext | knowledge | Sem migrações confirmadas | INCOMPLETE |
| NexTraceDbContextBase | building-blocks | N/A (base class) | BASE |

*GovernanceDbContext existe mas módulo Governance tem 74 handlers que retornam dados mock; persistência própria é design intencional vazio.

---

## 3. Qualidade do Schema

### Pontos Fortes

**Isolamento de tenant:**
- Todos os DbContexts herdam de `NexTraceDbContextBase`
- `ICurrentTenant` injetado em todos os contextos
- `TenantRlsInterceptor`: `SELECT set_config('app.current_tenant_id', @__tenantId, false)` — parametrizado (sem SQL injection)
- `TenantIsolationBehavior` na camada de aplicação como segunda linha de defesa

**Audit fields:**
- `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` em todas as entidades auditáveis
- Soft-delete (`IsDeleted`) via `AuditableEntity<T>` base class
- `AuditInterceptor` para gestão automática de timestamps
- Row version com coluna `xmin` (PostgreSQL nativo) para concorrência otimista

**Qualidade estrutural:**
- UUID (Guid) como PKs — nativo PostgreSQL
- Check constraints em enums: e.g., `"Status" IN ('Planned','InProgress','Completed','Cancelled')`
- Índices adequados em colunas de alta consulta: `TenantId`, `ActionType`, `OccurredAt`, `PerformedBy`
- Outbox pattern com `idempotency_key` (índice único) em todos os módulos

**Evidência:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Infrastructure/Persistence/AuditDbContext.cs`, `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Persistence/NexTraceDbContextBase.cs`

---

## 4. Problemas Identificados

### Gap Crítico 1: Outbox não processado em 23 DbContexts

O outbox pattern está implementado em todos os DbContexts (tabela `*_outbox_messages`). Porém, apenas o IdentityDbContext tem processamento ativo de outbox. Os outros 23 DbContexts produzem eventos de domínio que ficam na tabela de outbox sem serem consumidos.

**Impacto:** Eventos de integração entre módulos não propagam. A arquitetura event-driven é estruturalmente presente mas funcionalmente inoperante para a maioria dos módulos.

**Evidência:** `docs/IMPLEMENTATION-STATUS.md` §Infrastructure — "Outbox pattern: PARTIAL — Only IdentityDbContext processed; 15 other contexts unprocessed"

### Gap Crítico 2: DbContexts sem migrações confirmadas

Os seguintes DbContexts têm `ModelSnapshot` mas não têm migrações confirmadas como executáveis:
- RuntimeIntelligenceDbContext
- CostIntelligenceDbContext
- AiOrchestrationDbContext
- ExternalAiDbContext
- IntegrationsDbContext
- ProductAnalyticsDbContext
- KnowledgeDbContext

**Impacto:** Estes schemas não são deployáveis sem geração explícita de migrações (`dotnet ef migrations add`).

**Evidência:** `docs/REBASELINE.md` §Dívidas de Arquitetura — A1, A2

### Gap 3: Cross-module interfaces como PLAN

8 interfaces cross-module estão definidas mas sem implementação:
- `IContractsModule`
- `IChangeIntelligenceModule`
- `IPromotionModule`
- `IRulesetGovernanceModule`
- `ICostIntelligenceModule`
- `IRuntimeIntelligenceModule`
- `IAiOrchestrationModule`
- `IExternalAiModule`

**Impacto:** Módulos como Governance não conseguem consultar dados reais de outros módulos. FinOps não pode correlacionar custos com serviços reais.

**Evidência:** `docs/IMPLEMENTATION-STATUS.md` §Cross-Module Contract Health

---

## 5. Seed Data

| Módulo | Seed | Ambiente |
|---|---|---|
| Configuration | ConfigurationDefinitionSeeder | Development/Staging |
| OperationalIntelligence | IncidentSeedData (SQL) | Development/Staging |
| ApiHost | DevelopmentSeedDataExtensions | Development only |

**Regra de segurança respeitada:** Seeds restritos a ambientes não-produção via verificação de ambiente.

**Evidência:** `src/platform/NexTraceOne.ApiHost/DevelopmentSeedDataExtensions.cs`

---

## 6. ClickHouse — Estado

**Status: ESQUEMA DEFINIDO, INTEGRAÇÃO INCOMPLETA**

Schema SQL para analytics em `build/clickhouse/`:
- `analytics-schema.sql` — tabelas analíticas
- `init-schema.sql` — inicialização

O docker-compose inclui ClickHouse. Porém, não há evidência de pipeline completo de ingestão de dados do PostgreSQL para ClickHouse em funcionamento.

**Recomendação:** ClickHouse é candidato estratégico para dados analíticos (observabilidade, FinOps, change history). Deve ser ativado quando o pipeline de ingestão de telemetria estiver pronto.

**Evidência:** `build/clickhouse/`, `docs/architecture/clickhouse-baseline-strategy.md`

---

## 7. Recomendações de Banco de Dados

| Ação | Prioridade | Impacto |
|---|---|---|
| Ativar processamento de outbox para todos os DbContexts críticos | Alta | Habilita event-driven entre módulos |
| Gerar migrações para RuntimeIntelligence e CostIntelligence | Alta | Schemas deployáveis |
| Gerar migrações para AiOrchestration, ExternalAI, Integrations | Média | Schemas deployáveis |
| Gerar migrações para ProductAnalytics e Knowledge | Média | Schemas deployáveis |
| Implementar cross-module interfaces | Alta | Desbloqueia Governance e FinOps real |
| Ativar pipeline ClickHouse para dados analíticos | Média | FinOps e observabilidade analítica |
| Senha ClickHouse vazia em appsettings.Development | Baixa | Configurar para produção |

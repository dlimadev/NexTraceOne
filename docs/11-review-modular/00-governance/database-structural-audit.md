# Auditoria Estrutural de Base de Dados — NexTraceOne

> **Data:** 2025-01-XX  
> **Escopo:** Toda a camada de persistência do NexTraceOne  
> **Método:** Análise estática de código (EF Core configurations, migrations, seeds, interceptors)  
> **Referência:** Visão oficial do produto — Source of Truth para serviços, contratos, mudanças e conhecimento operacional

---

## 1. Resumo Executivo

| Dimensão | Valor |
|---|---|
| DbContexts distintos | 19–20 (confirmados 20, ver inventário) |
| Bases de dados lógicas | 4 (`nextraceone_identity`, `nextraceone_catalog`, `nextraceone_operations`, `nextraceone_ai`) |
| Entity type configurations | 132 ficheiros |
| Definições de índice (HasIndex) | 353 (42 unique, 46 composite, 2 filtered/partial) |
| Conversões enum→string | 91 |
| Conversões enum→int | 12 |
| Conversões de strongly-typed IDs | 100+ |
| Conversões de value objects | 50+ |
| Migrations ativas | 29 |
| Model snapshots | 19 |
| SQL seed scripts | 7 (2.258 linhas totais) |
| C# seeders | 2 (ConfigurationDefinitionSeeder, IncidentSeedData) |
| Encriptação | AES-256-GCM via atributo `[EncryptedField]` |
| Multi-tenancy | RLS via `TenantRlsInterceptor` (PostgreSQL Row-Level Security) |
| Outbox pattern | Sim — tabelas customizadas por DbContext |
| Soft delete | Sim — `AuditableEntity.IsDeleted` + global query filter |
| Concurrency tokens (RowVersion) | **Nenhum** — conflito detetado via `UpdatedAt` app-level |
| Check constraints | **0** |

### Classificação Global

| Área | Estado |
|---|---|
| Cobertura de domínio | ✅ Forte — 8 módulos com persistência real |
| Integridade referencial | ⚠️ Parcial — 12 cascade, 1 restrict, 1 SetNull; sem check constraints |
| Performance estrutural | ⚠️ Parcial — 353 índices mas apenas 2 filtered; sem RowVersion |
| Multi-tenancy | ✅ Forte — RLS + TenantId em todas as entidades |
| Auditoria | ✅ Forte — interceptor automático CreatedAt/By, UpdatedAt/By |
| Migrations | ⚠️ Atenção — 2 módulos sem migrations; AiGovernance com dívida técnica |
| Seeds | ⚠️ Parcial — apenas dev; sem seed de produção para roles/permissions |
| Manutenibilidade | ✅ Boa — padrões consistentes; 132 configs bem organizadas |

---

## 2. DbContexts e Persistência

### 2.1 Distribuição por Base de Dados Lógica

| Base de dados | DbContexts | Total DbSets |
|---|---|---|
| `nextraceone_identity` | IdentityDbContext (16), AuditDbContext (6) | 22 |
| `nextraceone_catalog` | ContractsDbContext (7), CatalogGraphDbContext (8), DeveloperPortalDbContext (5) | 20 |
| `nextraceone_operations` | ChangeIntelligenceDbContext (10), PromotionDbContext (4), WorkflowDbContext (6), RulesetGovernanceDbContext (3), AutomationDbContext (3), CostIntelligenceDbContext (6), IncidentDbContext (5), ReliabilityDbContext (1), RuntimeIntelligenceDbContext (4), GovernanceDbContext (12), ConfigurationDbContext (3), NotificationsDbContext (3) | 60 |
| `nextraceone_ai` | AiGovernanceDbContext (19+), AiOrchestrationDbContext (4), ExternalAiDbContext (4) | 27+ |

**Risco identificado:** `nextraceone_operations` aloja 12 DbContexts numa única base de dados. Isto cria:
- Risco de colisão de tabelas outbox (se nomes não forem únicos)
- Complexidade de migrations partilhadas
- Potencial contenção em operações DDL

### 2.2 Classe Base

Todos os DbContexts estendem `NexTraceDbContextBase`, que fornece:

| Capacidade | Mecanismo |
|---|---|
| Multi-tenant RLS | `TenantRlsInterceptor` (PostgreSQL) |
| Auditoria automática | `AuditInterceptor` (CreatedAt/By, UpdatedAt/By) |
| Encriptação de campos | `EncryptionInterceptor` (AES-256-GCM) |
| Outbox pattern | `OutboxInterceptor` (domain events → messages) |
| Soft delete | Global query filter `IsDeleted == false` em `AuditableEntity` |
| Carregamento de configs | Filtro por `ConfigurationsNamespace` |

**Referência:** `src/platform/NexTraceOne.SharedKernel/` (base class e interceptors)

---

## 3. Entidades e Aderência ao Domínio

### 3.1 Padrões de Entidade

| Padrão | Utilização |
|---|---|
| Strongly-typed IDs | 100+ (UserId, TenantId, ServiceId, ContractId, etc.) |
| Value objects | 50+ (Email, FullName, HashedPassword, etc.) |
| Owned entities | ContractVersion.Signature, ContractVersion.Provenance |
| AuditableEntity base | CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, TenantId, IsDeleted |
| AggregateRoot base | DomainEvents collection para outbox |

### 3.2 Cobertura por Pilar do Produto

| Pilar NexTraceOne | Entidades DB | Suporte |
|---|---|---|
| Service Governance | ServiceDefinition, ServiceDependency, TeamService | ✅ Forte |
| Contract Governance | ContractVersion, ApiContract, EventContract, SOAPContract | ✅ Forte |
| Change Confidence | Release, BlastRadius, ChangeScore, PromotionRequest | ✅ Forte |
| Operational Reliability | Incident, Runbook, AutomationRule | ✅ Adequado |
| AI-assisted Operations | AiAgent, AiModel, Conversation, AiAccessPolicy | ✅ Forte (19+ entidades) |
| Source of Truth | GovernancePolicy, Standard, ComplianceReport | ✅ Adequado |
| AI Governance | ModelRegistry, TokenBudget, AuditEntry | ✅ Forte |
| FinOps | CostAllocation, CostImportPipeline, BudgetAlert | ⚠️ Parcial |

### 3.3 Lacunas Identificadas

| Área | Gap |
|---|---|
| IDE Extensions | Sem persistência para gestão de extensões IDE |
| Publication Center | Sem modelo dedicado para publicação de contratos |
| Risk Center | Parcialmente coberto por GovernanceDb; sem modelo de risco dedicado |
| Operational Notes | Sem entidade específica para notas operacionais |

---

## 4. Integridade e Performance Estrutural

### 4.1 Índices

| Tipo | Quantidade |
|---|---|
| Total de definições HasIndex | 353 |
| Unique constraints | 42 |
| Composite indexes | 46 |
| Filtered/partial indexes | 2 |
| **Check constraints** | **0** |

**Análise:** A cobertura de índices é extensa (353), mas a quase ausência de filtered indexes (apenas 2) e a total ausência de check constraints representam riscos:
- Queries sobre dados soft-deleted podem ser ineficientes sem filtered indexes `WHERE IsDeleted = false`
- Sem check constraints, validações de domínio dependem exclusivamente da camada aplicacional

### 4.2 Relacionamentos

| Tipo | Quantidade |
|---|---|
| Cascade delete | 12 |
| Restrict | 1 |
| SetNull | 1 |

**Risco:** Baixo número de `Restrict` sugere que a maioria das relações usa cascade ou não tem delete behavior explícito. Isto pode levar a exclusões em cadeia não intencionais.

### 4.3 Concorrência

**Não existe RowVersion/ConcurrencyToken em nenhuma entidade.** O controlo de conflitos é feito via `UpdatedAt` a nível aplicacional. Isto é aceitável para cargas moderadas mas pode causar lost updates sob concorrência elevada.

---

## 5. Migrations

### 5.1 Estado Atual

| DbContext | Nº Migrations | Estado |
|---|---|---|
| AiGovernanceDbContext | 7 | ⚠️ Dívida técnica (TenantId fixes) |
| GovernanceDbContext | 3 | ✅ Estável |
| IdentityDbContext | 2 | ✅ Estável |
| AuditDbContext | 2 | ✅ Estável |
| CostIntelligenceDbContext | 2 | ✅ Estável |
| Restantes 12 contextos | 1 cada | ✅ InitialCreate |
| ConfigurationDbContext | **0** | ❌ Usa `EnsureCreated` |
| NotificationsDbContext | **0** | ❌ Usa `EnsureCreated` |

**Total:** 29 migrations ativas + 19 model snapshots

### 5.2 Riscos

1. **AiGovernanceDbContext** tem 7 migrations incluindo correções de tipo TenantId (Guid→UUID). Candidato prioritário a consolidação.
2. **ConfigurationDbContext** e **NotificationsDbContext** sem migrations formais — `EnsureCreated` não suporta evolução incremental de schema.
3. **Sem DesignTimeDbContextFactory** — pode complicar tooling de migrations fora do contexto da aplicação.

---

## 6. Seeds

### 6.1 SQL Seeds (Development Only)

| Script | Linhas | Conteúdo |
|---|---|---|
| `seed-identity.sql` | 83 | 2 tenants, 10 users, environments |
| `seed-audit.sql` | 66 | 35 audit events, chain links |
| `seed-catalog.sql` | 172 | 9 services, 6 APIs, contracts |
| `seed-changegovernance.sql` | 266 | Workflows, promotions, rulesets |
| `seed-governance.sql` | 420 | Policies, standards |
| `seed-incidents.sql` | 224 | 6 incidents, 3 runbooks |
| `seed-aiknowledge.sql` | 1.027 | 10 AI agents, models, conversations |
| **Total** | **2.258** | |

**Localização:** `src/platform/NexTraceOne.ApiHost/SeedData/`

### 6.2 C# Seeders

| Seeder | Detalhe |
|---|---|
| `ConfigurationDefinitionSeeder` | 345+ definitions across 8 phases |
| `IncidentSeedData` | 6 incidents, 3 runbooks (usada em migrations) |

### 6.3 Características

- Todos os SQL scripts usam `INSERT...ON CONFLICT DO NOTHING` (idempotente)
- Orquestração via `DevelopmentSeedDataExtensions` — apenas em ambiente Development
- **Sem seed de produção** para roles, permissions ou governance packs

### 6.4 Lacunas

| Área | Seed Existente |
|---|---|
| Roles/Permissions de produção | ❌ Não existe |
| Governance packs iniciais | ❌ Não existe |
| Configurações base de IA | ⚠️ Apenas dev |
| Templates de workflow | ⚠️ Apenas dev |

---

## 7. Capacidades Transversais (Tenant / Ambiente / Auditoria)

### 7.1 Multi-Tenancy

| Aspeto | Estado |
|---|---|
| TenantId em todas as entidades | ✅ Via `AuditableEntity` |
| PostgreSQL Row-Level Security | ✅ Via `TenantRlsInterceptor` |
| Isolamento de dados | ✅ RLS ao nível da BD |
| Filtro global em queries | ✅ Via base class |

### 7.2 Ambientes

| Aspeto | Estado |
|---|---|
| Modelo de ambiente | ✅ `EnvironmentProfile` com `IsPrimaryProduction` |
| Suporte multi-ambiente | ✅ Via IdentityDbContext |
| Promoção entre ambientes | ✅ Via PromotionDbContext |

### 7.3 Auditoria

| Aspeto | Estado |
|---|---|
| CreatedAt/UpdatedAt automático | ✅ Via `AuditInterceptor` |
| CreatedBy/UpdatedBy automático | ✅ Via `AuditInterceptor` |
| Soft delete | ✅ `IsDeleted` + global query filter |
| Audit trail dedicado | ✅ `AuditDbContext` (6 DbSets) |
| Encriptação de campos sensíveis | ✅ AES-256-GCM via `[EncryptedField]` |

---

## 8. Suporte ao Produto Real (IA / Agents / Workflow / Change)

### 8.1 IA e Agents

O módulo de IA é o mais extenso em termos de persistência:

| DbContext | DbSets | Foco |
|---|---|---|
| AiGovernanceDbContext | 19+ | Providers, models, agents, policies, budgets, audit |
| AiOrchestrationDbContext | 4 | Conversations, messages, context |
| ExternalAiDbContext | 4 | External AI integrations |

**Avaliação:** A persistência de IA é forte e alinhada com o pilar "AI Governance & Developer Acceleration". Suporta model registry, token budgets, agent execution e audit trail.

### 8.2 Workflows e Change Intelligence

| DbContext | DbSets | Foco |
|---|---|---|
| ChangeIntelligenceDbContext | 10 | Releases, blast radius, change scores |
| WorkflowDbContext | 6 | Templates, instances, stages, evidence, approvals |
| PromotionDbContext | 4 | Promotion requests, approvals, SLAs |
| RulesetGovernanceDbContext | 3 | Rulesets, rules, conditions |

**Avaliação:** Suporte sólido para Change Confidence e Production Change Confidence.

### 8.3 Contratos

| DbContext | DbSets | Foco |
|---|---|---|
| ContractsDbContext | 7 | API, SOAP, Event, Background contracts |
| CatalogGraphDbContext | 8 | Service graph, dependencies |
| DeveloperPortalDbContext | 5 | Portal, documentation |

**Avaliação:** Forte alinhamento com Contract Governance.

---

## 9. Manutenibilidade

### 9.1 Pontos Fortes

| Aspeto | Detalhe |
|---|---|
| Convenções consistentes | Strongly-typed IDs, AuditableEntity, AggregateRoot |
| Separação de configurações | 132 ficheiros EntityTypeConfiguration separados |
| Base class partilhada | NexTraceDbContextBase com interceptors padronizados |
| Outbox pattern | Evita acoplamento direto entre módulos |
| Encriptação declarativa | Atributo `[EncryptedField]` — limpo e auditável |

### 9.2 Pontos de Atenção

| Aspeto | Risco |
|---|---|
| 12 DbContexts em `nextraceone_operations` | Elevado — sobrecarrega uma única BD |
| Sem DesignTimeDbContextFactory | Médio — complica tooling de migrations |
| AiGovernance com 7 migrations | Médio — candidato a consolidação |
| Configuration/Notifications sem migrations | Médio — limita evolução de schema |
| Apenas 2 filtered indexes | Médio — performance em soft-delete queries |
| 0 check constraints | Baixo-Médio — validação apenas app-level |

---

## 10. Recomendações Priorizadas

### 🔴 Prioridade Alta

| # | Recomendação | Justificação |
|---|---|---|
| R1 | Migrar ConfigurationDbContext e NotificationsDbContext para migrations formais | `EnsureCreated` não suporta evolução incremental |
| R2 | Consolidar migrations do AiGovernanceDbContext | 7 migrations com fixes de TenantId = dívida técnica |
| R3 | Criar seed de produção para roles, permissions e governance packs | Sem isto, deploy em produção requer passos manuais |
| R4 | Avaliar split de `nextraceone_operations` | 12 DbContexts numa BD é excessivo; considerar split por bounded context |

### 🟡 Prioridade Média

| # | Recomendação | Justificação |
|---|---|---|
| R5 | Adicionar filtered indexes para soft-delete (`WHERE IsDeleted = false`) | 353 índices mas apenas 2 filtered — impacto em performance |
| R6 | Adicionar check constraints para enums críticos | 0 check constraints = validação apenas app-level |
| R7 | Implementar DesignTimeDbContextFactory | Facilita tooling de migrations sem contexto da aplicação |
| R8 | Verificar unicidade dos nomes de tabelas outbox | Risco de colisão em `nextraceone_operations` |

### 🟢 Prioridade Baixa

| # | Recomendação | Justificação |
|---|---|---|
| R9 | Avaliar RowVersion/ConcurrencyToken para entidades críticas | Conflitos app-level via UpdatedAt pode falhar sob carga elevada |
| R10 | Adicionar persistência para IDE Extensions, Publication Center, Risk Center | Lacunas face à visão oficial do produto |
| R11 | Documentar estratégia de backup/restore por base de dados lógica | 4 BDs lógicas com dependências cruzadas |

---

## Referências de Código

| Artefacto | Localização |
|---|---|
| NexTraceDbContextBase | `src/platform/NexTraceOne.SharedKernel/` |
| DbContexts | `src/modules/*/Infrastructure/Persistence/` |
| EntityTypeConfigurations | `src/modules/*/Infrastructure/Persistence/Configurations/` |
| Migrations | `src/modules/*/Infrastructure/Persistence/Migrations/` |
| SQL Seeds | `src/platform/NexTraceOne.ApiHost/SeedData/` |
| C# Seeders | `src/modules/configuration/`, `src/modules/operationalintelligence/` |
| Interceptors | `src/platform/NexTraceOne.SharedKernel/Persistence/Interceptors/` |

---

*Relatório gerado como parte da auditoria modular de governança do NexTraceOne.*

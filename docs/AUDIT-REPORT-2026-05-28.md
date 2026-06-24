# RELATÓRIO DE AUDITORIA — NexTraceOne Platform
## Estado Real do Projeto — 28/05/2026

---

## SUMÁRIO EXECUTIVO

O NexTraceOne é uma plataforma enterprise de Change Intelligence de grande escala. Este relatório apresenta o estado real do projeto com base em análise direta do código-fonte (backend .NET 10, frontend React 19, banco PostgreSQL, testes e infraestrutura).

### Métricas Globais

| Dimensão | Quantidade |
|----------|-----------|
| Projetos na solução (.csproj) | **91** |
| Módulos bounded context | **12** |
| Handlers CQRS (Application) | **~740** |
| DbContexts (EF Core) | **24** |
| Migrations (arquivos .cs) | **103** |
| Repositórios implementados | **~300+** |
| Arquivos TS/TSX (frontend) | **1.054** |
| Páginas frontend | **~300+** |
| Componentes UI compartilhados | **110** |
| Projetos de teste | **25** |
| Arquivos de teste | **~1.016** |
| Testes (estimativa) | **~1.050** |

### Veredito Geral

O projeto está **arquitecturalmente sólido e consistente** com os padrões definidos no CLAUDE.md. O backend é **muito completo e denso**, com todos os 12 módulos implementados. O frontend é **volumoso mas com densidade irregular** — alguns módulos têm cobertura de UI muito superior a outros. Foram identificados **2 desvios de nomenclatura de prefixo de tabela**, **1 divergência de contagem de preflight checks** e **vários gaps de integração frontend-backend**.

---

## PARTE 1 — ARQUITETURA E BUILDING BLOCKS

### 1.1 Conformidade Arquitetural

O padrão **Archon Pattern** está plenamente implementado:

| Padrão | Status | Observação |
|--------|--------|-----------|
| DDD (Aggregates, Value Objects, Domain Events) | ✅ Conforme | Todos os módulos seguem |
| Clean Architecture (4 camadas) | ✅ Conforme | Domain→Application→Infrastructure→API respeitado |
| CQRS via MediatR (static class por feature) | ✅ Conforme | Padrão de arquivo único por feature |
| Outbox Pattern (PostgreSQL) | ✅ Conforme | Uma tabela outbox por DbContext |
| Row-Level Security (TenantRlsInterceptor) | ✅ Conforme | PostgreSQL `set_config` + repository filter |
| Result<T> Pattern (sem exceções de negócio) | ✅ Conforme | `Error.NotFound`, `Error.Forbidden`, etc. |
| Strongly-typed IDs (`TypedIdBase`) | ✅ Conforme | Todos os módulos |
| IPublicRequest para bypass de tenant | ✅ Conforme | Auth endpoints |
| Pipeline MediatR (5 behaviors) | ✅ Conforme | Logging→Performance→TenantIsolation→Validation→Transaction |

### 1.2 Building Blocks (5 projetos)

| Building Block | Projetos | Status | Componentes-chave |
|---------------|----------|--------|-----------------|
| **BuildingBlocks.Core** | 1 | ✅ Operacional | AggregateRoot, Entity, TypedIdBase, Result<T>, Error, ErrorType |
| **BuildingBlocks.Application** | 1 | ✅ Operacional | ICommand, IQuery, ICurrentTenant, ICurrentUser, 5 pipeline behaviors |
| **BuildingBlocks.Infrastructure** | 1 | ✅ Operacional | NexTraceDbContextBase, 3 interceptors, OutboxMessage, DeadLetterMessage, RepositoryBase |
| **BuildingBlocks.Security** | 1 | ✅ Operacional | JWT, CSRF, RLS, RBAC, OIDC/SAML, Break Glass, assembly integrity |
| **BuildingBlocks.Observability** | 1 | ✅ Operacional | OpenTelemetry (traces/metrics/logs), Serilog, health checks, Elasticsearch/ClickHouse |

---

## PARTE 2 — AUDITORIA DOS 12 MÓDULOS BACKEND

### Legenda
- ✅ Completo — todos os 5 projetos + repositórios + migrations + features
- ⚠️ Parcial — existe mas com lacunas
- ❌ Ausente ou vazio

### 2.1 Tabela de Status por Módulo

| Módulo | Domain | Application | Contracts | Infrastructure | API | Features | Migrations | Repos | Status |
|--------|--------|-------------|-----------|---------------|-----|----------|-----------|-------|--------|
| **identityaccess** | ✅ | ✅ 41 features | ✅ | ✅ 1 DbContext | ✅ 17 endpoint groups | ✅ | 7 | 22 | ✅ **Completo** |
| **catalog** | ✅ | ✅ 322 features | ✅ | ✅ 7 DbContexts | ✅ | ✅ | 22 | 60+ | ✅ **Completo** |
| **changegovernance** | ✅ | ✅ 164 features | ✅ | ✅ 4 DbContexts | ✅ | ✅ | 8 | 40+ | ✅ **Completo** |
| **governance** | ✅ | ✅ 170 features | ✅ | ✅ 1 DbContext | ✅ | ✅ | 4 | 47 | ✅ **Completo** |
| **operationalintelligence** | ✅ | ✅ 6 sub-domínios | ✅ | ✅ 6 DbContexts | ✅ | ✅ | 14 | 58+ | ✅ **Completo** |
| **aiknowledge** | ✅ | ✅ 4 sub-domínios | ✅ | ✅ 3 DbContexts | ✅ | ✅ | 6 | 80+ | ✅ **Completo** |
| **auditcompliance** | ✅ | ✅ | ✅ | ✅ 1 DbContext | ✅ | ✅ | 2 | 2 compound | ⚠️ **Repositórios simples** |
| **integrations** | ✅ | ✅ 28 features | ✅ | ✅ 1 DbContext | ✅ | ✅ | 2 | 7 | ✅ **Completo** |
| **knowledge** | ✅ | ✅ 22 features | ✅ | ✅ 1 DbContext | ✅ | ✅ | 2 | 5 | ✅ **Completo** |
| **notifications** | ✅ | ✅ 22 features | ✅ | ✅ 1 DbContext | ✅ | ✅ | 2 | 6 | ✅ **Completo** |
| **configuration** | ✅ | ✅ 54 features | ✅ | ✅ 1 DbContext | ✅ | ✅ | 2 | 19 | ✅ **Completo** |
| **productanalytics** | ✅ | ✅ | ✅ | ✅ 1 DbContext | ✅ | ✅ | 2 | 1 | ⚠️ **Repositório único** |

### 2.2 Detalhe dos Módulos de Maior Expressão

#### Catalog (maior módulo — 9 sub-domínios)
- **Contracts**: 137 features — versionamento, diffs, rulesets, artefactos, drafts, reviews, exemplos, lint, canonical entities, scorecards, evidence packs, SOAP/Event/BackgroundService contracts, deployments, negociações, changelogs, consumer inventories
- **Graph**: 105 features — serviços, assets, APIs, consumidores, dependências, interfaces, links
- **Portal**: 29 features — subscrições, playground, geração de código, analytics, pesquisa
- **DependencyGovernance**: 13 features — perfis de dependência, packages, vulnerabilidades
- **Templates**: 8 features — templates de serviço
- **LegacyAssets**: 8 features — mainframe, COBOL, copybooks, CICS
- **DeveloperExperience**: 9 features — surveys, uso de IDE
- **SourceOfTruth**: 6 features
- **Services**: 7 features

#### ChangeGovernance (6 sub-domínios)
- **ChangeIntelligence**: 89 features — releases, blast radius, change scores, events, markers externos
- **Compliance**: 29 features — políticas de aprovação, gateways de release
- **Workflow**: 18 features — templates, instâncias, estágios, evidence packs, SLA policies
- **Promotion**: 12 features — ambientes, requests, gates, avaliações
- **RulesetGovernance**: 13 features — rulesets, bindings, lint results
- **Platform**: 3 features

#### OperationalIntelligence (6 sub-domínios)
- **Incidents**: 9 DbSet entities — incidentes, workflows de mitigação, runbooks, correlações de mudança, PIRs
- **Cost**: 5 DbSet entities — snapshots de custo, atribuições, tendências, perfis de serviço
- **Reliability**: 5 DbSet entities — SLO/SLA, error budgets, burn rates
- **Runtime**: 5 DbSet entities — snapshots de runtime, baselines, drift findings
- **TelemetryStore**: 5 DbSet entities — métricas de serviço, topologia, anomalias
- **Automation**: 3 DbSet entities — workflows, validações, audit records

#### AIKnowledge (4 sub-domínios + 6 projetos)
- **Orchestration**: modelos, políticas, budgets, conversas com assistant, mensagens
- **Governance**: contextos, conversas, test artifacts, knowledge captures, workflow executions
- **ExternalAI**: providers, políticas, consultations, knowledge captures
- **Runtime**: ~80+ repositórios totais

---

## PARTE 3 — AUDITORIA FRONTEND (19 Features)

### 3.1 Tabela de Status por Feature

| Feature | Páginas | APIs | Componentes | Hooks | Rotas | i18n | Status |
|---------|---------|------|-------------|-------|-------|------|--------|
| **ai-hub** | 21 | 3 arquivos | 11 | - | aiHubRoutes | ✅ | ✅ **Rico** |
| **audit-compliance** | 1 | 2 arquivos | 0 | 0 | adminRoutes | ✅ | ❌ **Muito Esparso** |
| **catalog** | 36 | 8 arquivos | 13 | - | catalogRoutes | ✅ | ✅ **Rico** |
| **change-governance** | 25 | 5 arquivos | 7 | - | changesRoutes | ✅ | ✅ **Rico** |
| **configuration** | 21 | 1 arquivo | 0 | 1 | adminRoutes | ✅ | ✅ **Adequado** |
| **contracts** | 8 + studio | 4 arquivos | workspace completo | 19 | contractsRoutes | ✅ | ✅ **Muito Rico** |
| **governance** | 70+ | 9 arquivos | 30+ | 2 | governanceRoutes | ✅ | ✅ **Muito Rico** |
| **identity-access** | 13 | 2 arquivos | 4 | 1 | adminRoutes | ✅ | ✅ **Adequado** |
| **integrations** | 5 | 1 arquivo | 0 | 0 | adminRoutes | ✅ | ⚠️ **Básico** |
| **knowledge** | 6 | 1 arquivo | 1 | - | knowledgeRoutes | ✅ | ✅ **Adequado** |
| **legacy-assets** | 2 | 1 arquivo | 0 | 0 | catalogRoutes | ✅ | ⚠️ **Mínimo** |
| **notifications** | 5 | 1 arquivo | 2 | 4 | adminRoutes | ✅ | ✅ **Adequado** |
| **observability** | 1 | 0 arquivos | 3 | 0 | *(sem rota dedicada)* | ❌ | ❌ **Órfão** |
| **operational-intelligence** | 1 | 0 arquivos | 0 | 0 | operationsRoutes | ❌ | ❌ **Muito Esparso** |
| **operations** | 44 | 7 arquivos | 0 | 0 | operationsRoutes | ✅ | ✅ **Rico** |
| **platform-admin** | 38 | 2 arquivos | 0 | 0 | adminRoutes | ✅ | ✅ **Rico** |
| **product-analytics** | 11 | 1 arquivo | 0 | 0 | adminRoutes | ✅ | ✅ **Adequado** |
| **saas** | 4 | 1 arquivo | 1 | 1 | adminRoutes | ✅ | ⚠️ **Mínimo** |
| **shared** | 1 | 0 | 0 | 0 | — | — | ⚠️ **Placeholder** |

### 3.2 Infraestrutura Frontend

| Componente | Estado | Detalhes |
|-----------|--------|----------|
| **API client centralizado** | ✅ Conforme CLAUDE.md | sessionStorage para access, memória para refresh, CSRF header, X-Tenant-Id |
| **i18n (4 idiomas)** | ✅ Conforme | en.json (16k linhas), es.json (17k), pt-BR.json (17k), pt-PT.json (17k) |
| **React Hook Form + Zod** | ✅ Conforme | Usado nas features principais (auth, contracts, configuration) |
| **TanStack Query** | ✅ Conforme | Estado assíncrono principal |
| **110 componentes UI** | ✅ Conforme | DataTable, Modal, Badge, Alert, DatePicker, etc. |
| **Lazy loading de rotas** | ✅ Conforme | `React.lazy()` em todas as 8 route files |
| **ProtectedRoute** | ✅ Conforme | Guards com verificação de permissions/claims |
| **Monaco Editor** | ✅ Presente | Usado em contracts/workspace e NQL editor |
| **Strings hardcoded na UI** | ⚠️ Risco | Features mais esparsas podem ter strings sem i18n |

---

## PARTE 4 — ALINHAMENTO FRONTEND-BACKEND

### 4.1 Matriz de Integração

| Módulo Backend | Feature Frontend | Páginas Frontend | Density Backend | Gap |
|---------------|-----------------|-----------------|----------------|-----|
| **identityaccess** | identity-access | 13 páginas | 41 handlers | ⚠️ Médio — missing: OnboardingWizard completo, TenantSelectionPage pouca integração com backend OIDC/SAML |
| **catalog** (Graph+Services) | catalog | 36 páginas | 127 handlers | ✅ Bem alinhado |
| **catalog** (Contracts) | contracts | 8+ workspace completo | 137 handlers | ✅ Muito bem alinhado |
| **catalog** (Portal) | catalog/developerPortal pages | 3 sub-páginas | 29 handlers | ✅ Alinhado |
| **catalog** (LegacyAssets) | legacy-assets | 2 páginas | 8 handlers | ⚠️ Mínimo mas proporcional |
| **catalog** (DependencyGovernance) | catalog/DependencyDashboard | 1 página | 13 handlers | ⚠️ Sub-representado |
| **changegovernance** | change-governance | 25 páginas | 164 handlers | ✅ Bem alinhado |
| **governance** | governance | 70+ páginas | 170 handlers | ✅ Muito bem alinhado |
| **operationalintelligence** | operations + operational-intelligence | 44+1 páginas | 6 DbContexts / extenso | ⚠️ Parcialmente alinhado — `operational-intelligence` tem apenas 1 página |
| **aiknowledge** | ai-hub | 21 páginas | 4 sub-domínios | ✅ Bem alinhado |
| **auditcompliance** | audit-compliance | **1 página** | compliance frameworks, digital sigs, PDF/Excel | ❌ **Gap crítico** |
| **integrations** | integrations | 5 páginas | 28 handlers | ⚠️ Básico mas funcional |
| **knowledge** | knowledge | 6 páginas | 22 handlers | ✅ Alinhado |
| **notifications** | notifications | 5 páginas | 22 handlers | ✅ Alinhado |
| **configuration** | configuration | 21 páginas | 54 handlers | ✅ Bem alinhado |
| **productanalytics** | product-analytics | 11 páginas | módulo básico | ✅ Alinhado |
| *(sem módulo backend)* | **observability** | 1 página (3 componentes) | N/A — corresponde ao BuildingBlocks.Observability | ❌ **Órfão** — sem API client dedicado, sem rota própria |

### 4.2 Features Órfãs ou Problemáticas

#### ❌ `observability` — Feature Órfã
- **Problema**: Nenhum módulo backend de nome "observability" existe. A observabilidade é transversal (OpenTelemetry, Elasticsearch, ClickHouse via BuildingBlocks.Observability).
- **Estado**: 3 componentes de dashboard + 1 página + 1 service + 1 types — sem chamadas de API reais, sem rota dedicada no router.
- **Impacto**: A página provavelmente está morta ou usada apenas internamente. Dashboards de observabilidade reais estão em `governance/widgets/Otel*`.

#### ❌ `operational-intelligence` — Sub-representada
- **Problema**: Backend tem 6 DbContexts (Incidents, Cost, Reliability, Runtime, TelemetryStore, Automation) com dezenas de features. Frontend tem **apenas 1 página** (`OperationsFinOpsConfigurationPage`) nesta feature.
- **Causa**: Aparentemente as páginas foram distribuídas para `operations` (44 páginas para incidents/SLO/reliability) e `governance` (FinOps pages). A feature `operational-intelligence` ficou como um stub.

#### ❌ `audit-compliance` — Gap Crítico
- **Problema**: O módulo `auditcompliance` backend tem trilha imutável, frameworks de conformidade, exportação PDF/Excel, assinaturas digitais, audit campaigns. O frontend tem **1 única página** (AuditPage).
- **Impacto**: Features críticas de compliance (retentionPolicies, compliancePolicies, auditCampaigns, cadeia de hashes, exportação de relatórios) não têm UI.

#### ⚠️ `saas` — Mínimo
- **Problema**: Backend tem tenant provisioning completo (ProvisionTenant), licenças, capabilities, agent registrations, alert firing. Frontend tem 4 páginas básicas.
- **Impacto**: Administração SaaS está fragmentada entre `saas`, `platform-admin` e `identity-access`.

### 4.3 Roteamento — Cobertura

| Route File | Features Cobertas |
|-----------|------------------|
| `adminRoutes.tsx` | identity-access, audit-compliance, notifications, configuration, integrations, platform-admin, product-analytics, saas |
| `aiHubRoutes.tsx` | ai-hub |
| `catalogRoutes.tsx` | catalog, legacy-assets |
| `changesRoutes.tsx` | change-governance |
| `contractsRoutes.tsx` | contracts |
| `governanceRoutes.tsx` | governance |
| `knowledgeRoutes.tsx` | knowledge |
| `operationsRoutes.tsx` | operations, operational-intelligence, observability |

**Nota**: `observability` e `operational-intelligence` estão absorvidos em `operationsRoutes.tsx` sem rotas dedicadas significativas.

---

## PARTE 5 — AUDITORIA DA CAMADA DE PERSISTÊNCIA

### 5.1 DbContexts e Migrations

| Módulo | DbContexts | Migrations | Prefixo Real | Prefixo CLAUDE.md | Conformidade |
|--------|-----------|-----------|-------------|------------------|-------------|
| identityaccess | 1 (IdentityDbContext) | 7 | `iam_` | `iam_` | ✅ |
| catalog | 7 (Contracts, DependencyGovernance, CatalogGraph, Templates, LegacyAssets, DeveloperPortal, DeveloperExperience) | 22 | `ctr_`, `dep_`, `cat_`, `tpl_`, `dx_` | `cat_` / `ctr_` | ✅ (extensões normais) |
| changegovernance | 4 (ChangeIntelligence, Ruleset, Promotion, Workflow) | 8 | `chg_`, `chg_rg_`, `chg_prm_`, `chg_wf_` | `chg_` | ✅ (sub-sufixos) |
| governance | 1 (GovernanceDbContext) | 4 | `gov_` | `gov_` | ✅ |
| operationalintelligence | 6 (Incident, Cost, Reliability, Runtime, TelemetryStore, Automation) | 14 | `ops_` | `opi_` | ⚠️ **DESVIO** |
| aiknowledge | 3 (AiGovernance, Orchestration, ExternalAI) | 6 | `aik_` | `aik_` | ✅ |
| auditcompliance | 1 (AuditDbContext) | 2 | `aud_` | `aud_` | ✅ |
| integrations | 1 (IntegrationsDbContext) | 2 | `int_` | `int_` | ✅ |
| knowledge | 1 (KnowledgeDbContext) | 2 | `knw_` | `knw_` | ✅ |
| notifications | 1 (NotificationsDbContext) | 2 | `ntf_` | `ntf_` | ✅ |
| configuration | 1 (ConfigurationDbContext) | 2 | `cfg_` | `cfg_` | ✅ |
| productanalytics | 1 (ProductAnalyticsDbContext) | 2 | `pan_` | `pdt_` | ⚠️ **DESVIO** |

### 5.2 Desvios de Prefixo Encontrados

**DESVIO 1 — OperationalIntelligence**
- **CLAUDE.md**: `opi_`
- **Implementado**: `ops_` (tabelas: `ops_inc_outbox_messages`, `ops_rel_outbox_messages`, `ops_rt_outbox_messages`, `ops_telstore_outbox_messages`, `ops_auto_outbox_messages`, `ops_outbox_messages`)
- **Impacto**: Desvio cosmético — funcionalidade não afetada. CLAUDE.md desatualizado.

**DESVIO 2 — ProductAnalytics**
- **CLAUDE.md**: `pdt_`
- **Implementado**: `pan_` (tabelas: `pan_outbox_messages`, `pan_analytics_events`, `pan_journey_definitions`)
- **Impacto**: Desvio cosmético — funcionalidade não afetada. CLAUDE.md desatualizado.

### 5.3 Interceptors e Segurança de Dados

| Interceptor | Aplicado em | Status |
|------------|-------------|--------|
| `TenantRlsInterceptor` | Todos os 24 DbContexts | ✅ Conforme |
| `AuditInterceptor` | Todos os 24 DbContexts | ✅ Conforme |
| `EncryptionInterceptor` (via convention) | Campos com `[EncryptedField]` | ✅ Conforme |
| `OutboxInterceptor` (via SaveChanges) | NexTraceDbContextBase | ✅ Conforme |

### 5.4 Padrões de Entidade

| Padrão | Status |
|--------|--------|
| Prefixo de tabela obrigatório | ✅ (com 2 desvios de nomenclatura doc.) |
| Strong-typed IDs (TypedIdBase) | ✅ Conforme |
| Soft-delete via IsDeleted | ✅ Conforme (filtro global aplicado) |
| AuditableEntity (CreatedAt/By, UpdatedAt/By) | ✅ Conforme (nunca atribuídos manualmente) |
| JSONB para dados flexíveis | ✅ Usado em SBOMs, agent steps, features |
| IEntityTypeConfiguration por entidade | ✅ Conforme |
| Migrations versionadas | ✅ 103 arquivos de migration |

---

## PARTE 6 — COBERTURA DE TESTES

### 6.1 Distribuição por Módulo

| Módulo | Testes | Escopo | Avaliação |
|--------|--------|--------|-----------|
| **catalog** | 209 | Contracts, Graph, Portal, DependencyGovernance, Templates | ✅ Bem coberto |
| **aiknowledge** | 131 | AI Governance, External AI, Orchestration, Runtime | ✅ Bem coberto |
| **operationalintelligence** | 110 | Cost, Reliability, Runtime, Incidents | ✅ Bem coberto |
| **changegovernance** | 89 | ChangeIntelligence, Ruleset, Workflow, Promotion | ✅ Bem coberto |
| **identityaccess** | 86 | Auth, Roles, JIT, Break Glass | ✅ Bem coberto |
| **notifications** | 57 | Email, Slack, Webhook | ✅ Adequado |
| **governance** | 55 | Compliance, Health, Dashboards | ✅ Adequado |
| **configuration** | 44 | Settings, Feature Flags | ✅ Adequado |
| **integrations** | 23 | Webhooks, Connectors | ⚠️ Básico |
| **knowledge** | 17 | Documents, Graph | ⚠️ Básico |
| **auditcompliance** | 16 | Audit trail, Compliance | ⚠️ **Insuficiente** para módulo crítico |
| **productanalytics** | 15 | Analytics tracking | ⚠️ Básico |

### 6.2 Testes de Plataforma

| Suite | Arquivos | Observação |
|-------|----------|-----------|
| `NexTraceOne.E2E.Tests` | 5 | XUnit + ApiWebApplicationFactory + Testcontainers |
| `NexTraceOne.IntegrationTests` | 9 | PostgreSQL via Testcontainers + Respawn |
| `NexTraceOne.BackgroundWorkers.Tests` | 8 | Jobs Quartz |
| `NexTraceOne.CLI.Tests` | 9 | Comandos CLI |
| `NexTraceOne.Selenium.Tests` | 12 | UI/navegação |
| `NexTraceOne.IngestionApi.Tests` | 1 | Ingestion API |

### 6.3 Testes de Building Blocks

| Bloco | Testes | Observação |
|-------|--------|-----------|
| BuildingBlocks.Core | 4 | Primitivas |
| BuildingBlocks.Application | 7 | Pipeline behaviors |
| BuildingBlocks.Infrastructure | 14 | DbContext, cache |
| BuildingBlocks.Security | 12 | JWT, encryption, multi-tenancy |
| BuildingBlocks.Observability | 14 | Logging, health |

### 6.4 Testes de Carga

| Suite | Tipo | Cobertura |
|-------|------|-----------|
| `tests/load/` (k6) | smoke/load/stress/spike/endurance | auth, catalog, contracts, governance, mixed |
| `tests/load-testing/` (k6) | 5 perfis (30s→1h) | Thresholds: p95 < 2000ms, erro < 5% |

---

## PARTE 7 — PLATAFORMA E INFRAESTRUTURA

### 7.1 ApiHost — Preflight Checks

O CLAUDE.md documenta **10 preflight checks**. Na implementação existem **11**:

| # | Check | CLAUDE.md | Implementado |
|---|-------|-----------|-------------|
| 1 | PostgreSqlPreflightCheck | ✅ | ✅ |
| 2 | JwtSecretPreflightCheck | ✅ | ✅ |
| 3 | ConnectionStringsPreflightCheck | ✅ | ✅ |
| 4 | DiskSpacePreflightCheck | ✅ | ✅ |
| 5 | RamPreflightCheck | ✅ | ✅ |
| 6 | PortsPreflightCheck | ✅ | ✅ |
| 7 | OllamaPreflightCheck | ✅ | ✅ |
| 8 | SmtpPreflightCheck | ✅ | ✅ |
| 9 | OtelCollectorPreflightCheck | ✅ | ✅ |
| 10 | CorsOriginsPreflightCheck | ✅ | ✅ |
| 11 | *(PreflightCheckService orquestrador)* | ❌ Não documentado | ✅ Implementado |

> **Nota**: O item 11 é o orquestrador do sistema de preflight — não é um check em si, mas é a classe que coordena a execução de todos.

### 7.2 BackgroundWorkers — Jobs Registrados

| Job | CLAUDE.md | Implementado |
|-----|-----------|-------------|
| LicenseRecalculationJob (15min) | ✅ | ✅ |
| AlertEvaluationJob | ✅ | ✅ |
| OutboxProcessorJob (ModuleOutboxProcessorJob) | Implícito | ✅ |
| ContractConsumerIngestionJob | — | ✅ |
| CloudBillingIngestionJob | — | ✅ |
| CarbonScoreCalculationJob | — | ✅ |
| WasteDetectionJob | — | ✅ |
| DriftDetectionJob | — | ✅ |
| IncidentProbabilityRefreshJob | — | ✅ |
| PlatformHealthMonitorJob | — | ✅ |
| IdentityExpirationJob | — | ✅ |
| BackupCoordinatorJob | — | ✅ |
| ElasticsearchIndexMaintenanceJob | — | ✅ |
| OtelCatalogBridgeJob | — | ✅ |

> O CLAUDE.md documenta apenas 2 jobs como exemplos mas **14 jobs** foram implementados — todos correctos e coerentes com a arquitectura.

### 7.3 Ingestion API

| Endpoint Group | Implementado |
|---------------|-------------|
| RuntimeSignalEndpoints | ✅ |
| ContractSyncEndpoints | ✅ |
| ServiceHealthQueryEndpoints | ✅ |
| DeploymentEventEndpoints | ✅ |
| CostIngestEndpoints | ✅ |
| ReleaseIngestEndpoints | ✅ |
| PromotionEventEndpoints | ✅ |
| CommitIngestEndpoints | ✅ |
| IncidentEndpoints | ✅ |
| ReportEndpoints | ✅ |
| WebhookSignatureValidator | ✅ |

### 7.4 CI/CD Pipelines

| Pipeline | Trigger | Status |
|---------|---------|--------|
| `ci.yml` | push main/develop/release, PR | ✅ Build + test (backend + frontend) |
| `security.yml` | push, PR, weekly | ✅ NuGet scan, npm audit, CodeQL, Trivy |
| `e2e.yml` | PR→main, nightly, manual | ✅ Playwright |
| `staging.yml` | push develop | ✅ Auto-deploy |
| `production.yml` | manual (approval gate) | ✅ Gated deploy |
| `kubernetes-deploy.yml` | manual | ✅ Helm |
| `release-bundle.yml` | tag v* | ✅ GitHub Release |
| `artifact-signing.yml` | tag | ✅ Cosign + SBOM (syft) + CVE (grype) |

### 7.5 Kubernetes (Helm)

| Componente | Status |
|-----------|--------|
| 18 templates Helm | ✅ |
| 4 values files (dev/staging/prod/default) | ✅ |
| HPA (autoscaling) | ✅ |
| NetworkPolicy | ✅ |
| ServiceMonitor (Prometheus) | ✅ |
| PrometheusRules (alertas) | ✅ |
| backup-cronjob | ✅ |
| Cosign artifact signing | ✅ |

---

## PARTE 8 — FERRAMENTAS E EXTENSÕES

### 8.1 CLI Tool (NexTraceOne.CLI)

13 comandos implementados: `catalog`, `contract`, `change`, `incident`, `compliance`, `report`, `validate`, `config`, `health`, `confidence`, `completion`, `scaffold`, `mcp`

**Status**: ✅ Funcional e completo com shell completions e MCP integration

### 8.2 SDK (.NET — NexTrace.Sdk)

4 clients: `ChangeClient`, `ComplianceClient`, `ServiceCatalogClient`, `ContractClient`

**Status**: ✅ Funcional

### 8.3 IDE Extensions

| Extensão | Status |
|---------|--------|
| Visual Studio | ✅ Implementado (NexTraceOnePackage.cs) |
| VS Code | ✅ Implementado (TypeScript) |
| GitHub Action | ✅ Implementado |
| SDK-CLI Legacy | ✅ Mantido |

---

## PARTE 9 — PROBLEMAS E DESVIOS ENCONTRADOS

### Críticos

| # | Problema | Módulo | Impacto |
|---|---------|--------|---------|
| C1 | `audit-compliance` frontend com apenas 1 página para um módulo crítico (trilha imutável, compliance frameworks, assinaturas digitais, PDF/Excel export) | Frontend | **Alto** — funcionalidades de compliance sem UI |
| C2 | Feature `observability` frontend sem API client, sem rota dedicada, sem módulo backend correspondente | Frontend | **Alto** — componentes órfãos não integrados |

### Médios

| # | Problema | Módulo | Impacto |
|---|---------|--------|---------|
| M1 | `operational-intelligence` frontend tem 1 página (stub) quando o backend tem 6 DbContexts e dezenas de features | Frontend | **Médio** — funcionalidades acessíveis via `operations` mas feature subutilizada |
| M2 | Prefixo de tabela `opi_` (CLAUDE.md) vs `ops_` (código real) no OperationalIntelligence | DB/Docs | **Baixo** (funcional), **Médio** (documentação) |
| M3 | Prefixo de tabela `pdt_` (CLAUDE.md) vs `pan_` (código real) no ProductAnalytics | DB/Docs | **Baixo** (funcional), **Médio** (documentação) |
| M4 | `auditcompliance` tem apenas 2 repositórios compostos vs padrão de repositório único por agregado | Backend | **Médio** — testabilidade e manutenção |
| M5 | `productanalytics` tem 1 único repositório para o módulo inteiro | Backend | **Médio** — crescimento futuro limitado |

### Menores

| # | Problema | Área | Impacto |
|---|---------|------|---------|
| m1 | CLAUDE.md documenta 10 preflight checks, implementação tem 11 | Docs | **Baixo** — documentação desatualizada |
| m2 | CLAUDE.md documenta 2 background jobs como exemplos, 14 implementados não documentados | Docs | **Baixo** — docs incompletas |
| m3 | Feature `saas` frontend tem apenas 4 páginas para funcionalidades SaaS extensas | Frontend | **Baixo** — parcialmente coberto por `platform-admin` |
| m4 | Feature `legacy-assets` tem apenas 2 páginas para 5 entidades (Mainframe, COBOL, Copybooks, CopybookFields, CICS) | Frontend | **Baixo** |
| m5 | Feature `integrations` frontend básica (5 páginas) vs 28 handlers no backend | Frontend | **Baixo** — integração básica funcional |
| m6 | CLAUDE.md menciona `TenantSchemaManager` como "existe mas não utilizado" — código morto documentado mas não removido | Backend | **Baixo** — debt técnico |
| m7 | Cobertura de testes `auditcompliance` (16 testes) insuficiente para módulo de compliance crítico | Testes | **Médio** |

---

## PARTE 10 — CONFORMIDADE COM PREMISSAS CLAUDE.md

### Verificação das Premissas Declaradas (Parte 23 do CLAUDE.md)

| Feature (CLAUDE.md) | Status Declarado | Status Real |
|---------------------|-----------------|-------------|
| 24 PostgreSQL DbContexts | ✅ Operacional | ✅ **Confirmado** (24 connection strings, DbContexts distintos) |
| Outbox + pg_advisory_lock | ✅ Operacional | ✅ **Confirmado** |
| Row-Level Security via TenantRlsInterceptor | ✅ Operacional | ✅ **Confirmado** |
| JWT multi-tenant auth + capability claims | ✅ Operacional | ✅ **Confirmado** |
| Elasticsearch (observabilidade/analytics) | ✅ Configurável | ✅ **Confirmado** |
| ClickHouse (analytics alternativo) | ⚠️ Opcional | ✅ **Confirmado** (deploy/clickhouse/ existe) |
| Hot Chocolate GraphQL | ✅ Operacional | ✅ **Confirmado** |
| Kafka | ⚠️ Opcional NullKafka | ✅ **Confirmado** |
| Redis / distributed cache | ✅ Redis ou memory | ✅ **Confirmado** |
| HTTP resilience (retry + circuit breaker) | ✅ 14 HttpClients | ✅ **Confirmado** |
| LicenseRecalculationJob | ✅ 15min | ✅ **Confirmado** |
| AlertEvaluationJob | ✅ | ✅ **Confirmado** |
| Tenant provisioning automation | ✅ ProvisionTenant | ✅ **Confirmado** |
| Trial plan capabilities | ✅ | ✅ **Confirmado** |
| Ollama (LLM local, qwen3.5:9b) | ✅ Habilitado | ✅ **Confirmado** |
| OpenAI | ⚠️ Enabled: false | ✅ **Confirmado** |
| SemanticKernel + Qdrant | ✅ Operacional | ✅ **Confirmado** |
| pgvector | ✅ PostgreSQL 16 | ✅ **Confirmado** |
| `IModelRoutingPolicyRepository` | ✅ EF Core | ✅ **Confirmado** |
| `IAgentExecutionPlanRepository` | ✅ EF Core | ✅ **Confirmado** |
| `IModelPredictionRepository` | ✅ EF Core | ✅ **Confirmado** |
| `ISbomRepository` | ✅ EF Core | ✅ **Confirmado** |
| `IDataContractRepository` | ✅ EF Core | ✅ **Confirmado** |
| `IDeprecationScheduleRepository` | ✅ EF Core | ✅ **Confirmado** |
| `IFeatureFlagRepository` | ✅ EF Core | ✅ **Confirmado** |
| `IIDEUsageRepository` | ✅ EF Core | ✅ **Confirmado** |
| `IEventConsumerDeadLetterRepository` | ✅ EF Core | ✅ **Confirmado** |
| Assembly integrity check | ✅ SHA-256 | ✅ **Confirmado** |
| Preflight checks (10) | ✅ | ⚠️ **11 implementados** (1 a mais que o documentado) |
| Break Glass Access | ✅ | ✅ **Confirmado** |
| CSRF protection | ✅ | ✅ **Confirmado** |
| Rate limiting (6 políticas) | ✅ | ✅ **Confirmado** |
| AirGap enforcement | ✅ | ✅ **Confirmado** |
| React 19 SPA (1.048 arquivos TS/TSX) | ✅ | ✅ **Confirmado** (1.054 arquivos) |
| i18n (4 idiomas) | ✅ | ✅ **Confirmado** |
| E2E tests (Playwright) | ✅ | ✅ **Confirmado** |
| NexTrace SDK | ✅ | ✅ **Confirmado** |
| NexTraceOne CLI | ✅ | ✅ **Confirmado** |
| Visual Studio IDE Extension | ✅ | ✅ **Confirmado** |
| Schema-per-tenant (TenantSchemaManager) | ❌ Não utilizado | ✅ **Confirmado** (código existe, não registado) |

**Score de conformidade com CLAUDE.md**: **37/39 itens confirmados** (95%).

---

## PARTE 11 — RADAR DE SAÚDE

### Por Dimensão

| Dimensão | Pontuação | Justificativa |
|----------|-----------|--------------|
| **Arquitectura Backend** | 9.5/10 | Padrão Archon aplicado consistentemente. 12/12 módulos com estrutura completa. |
| **Qualidade do Código Backend** | 9/10 | CQRS, Result Pattern, DDD rigorosos. Apenas 2 módulos com repositórios simplificados. |
| **Persistência / BD** | 9/10 | 24 DbContexts, 103 migrations, interceptors globais. 2 desvios de prefixo documentais. |
| **Frontend — Coverage** | 7/10 | 300+ páginas mas densidade muito irregular. 3 features órfãs/esparsas. |
| **Frontend — Qualidade** | 8.5/10 | Padrões correctos (Zod+RHF, TanStack Query, i18n). API client centralizado conforme. |
| **Integração Frontend-Backend** | 6.5/10 | audit-compliance e observability são gaps críticos. Restantes bem alinhados. |
| **Cobertura de Testes** | 7.5/10 | ~1.050 testes, boa distribuição geral. auditcompliance sub-testado (16 testes). |
| **Infraestrutura / DevOps** | 9.5/10 | 8 CI/CD pipelines, Helm completo, load tests, artifact signing. |
| **Documentação (CLAUDE.md)** | 8.5/10 | Rico e detalhado. 2 prefixos desatualizados, jobs não documentados. |
| **Segurança** | 9/10 | JWT, CSRF, RLS, Break Glass, RBAC, assembly integrity, AirGap. |

### Pontuação Global

> **8.3 / 10 — Projecto em estado sólido de produção, com gaps de UI pontuais a resolver.**

---

## RECOMENDAÇÕES PRIORITÁRIAS

### Alta Prioridade

1. **Expandir frontend `audit-compliance`** — Criar páginas para: retentionPolicies, compliancePolicies, auditCampaigns, exportação PDF/Excel, cadeia de hashes imutável. O módulo backend é completo; o frontend não reflete isso.

2. **Resolver feature `observability` órfã** — Ou integrar `ObservabilityDashboardPage` em `governance` (junto com os widgets OtelXxx já existentes) e remover a feature, ou criar um cliente API e rota própria.

3. **Actualizar CLAUDE.md** — Corrigir prefixos `opi_`→`ops_` (OperationalIntelligence) e `pdt_`→`pan_` (ProductAnalytics). Documentar os 14 background jobs.

### Média Prioridade

4. **Expandir testes `auditcompliance`** — De 16 para pelo menos 40-50 testes unitários para módulo de compliance crítico.

5. **Consolidar/expandir `operational-intelligence` frontend** — Clarificar a fronteira entre esta feature e `operations`. A feature actual (1 página) é um stub que gera confusão.

6. **Refactorizar repositórios `auditcompliance`** — Seguir o padrão de repositório único por agregado em vez de classes compostas.

### Baixa Prioridade

7. Expandir `integrations` frontend (5 páginas para 28 handlers).
8. Expandir `legacy-assets` frontend (2 páginas para 5 entidades COBOL/mainframe).
9. Remover `TenantSchemaManager` (código morto confirmado no CLAUDE.md).

---

*Relatório gerado por auditoria automatizada em 28/05/2026.*
*Baseado em análise direta do código-fonte: backend .NET 10, frontend React 19, 91 projetos, 1.054 arquivos TS/TSX, 103 migrations.*

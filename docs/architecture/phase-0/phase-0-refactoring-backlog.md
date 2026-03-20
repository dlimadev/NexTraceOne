# Phase 0 — Refactoring Backlog (Fase 1+)

**Data:** 2026-03-20  
**Base:** Diagnóstico da Fase 0  
**Princípio:** Refatoração incremental, não destrutiva, orientada por produto

---

## Fase 1 — Domínio: Modelo Unificado de Tenant + Environment

**Objetivo:** Garantir que as entidades de domínio críticas carregam `TenantId` e `EnvironmentId` de forma correta e strongly typed.

**Dependências:** Nenhuma fase anterior  
**Risco:** Alto — breaking changes em agregados e migrações de dados  
**Critério de aceite:** Todos os agregados operacionais têm `TenantId`; `Environment: string` substituído por `EnvironmentId: EnvironmentId` fortemente tipado

---

### F1-001 — Unificar modelo de Environment

**Objetivo:** Definir um único modelo canônico de ambiente, tenant-scoped, com perfil operacional parametrizável.

**Motivação:** Hoje existem dois conceitos desconexos: `IdentityAccess.Domain.Entities.Environment` (tenant-scoped, para autorização) e `ChangeGovernance.Domain.Promotion.Entities.DeploymentEnvironment` (global, para pipeline). Eles representam realidades sobrepostas e devem ser unificados ou explicitamente relacionados.

**Tarefas:**
- Avaliar se `DeploymentEnvironment` deve referenciar `EnvironmentId` do módulo IdentityAccess ou ser fundido nele
- Adicionar `TenantId` a `DeploymentEnvironment`
- Adicionar campo de perfil operacional: `IsProductionLike`, `RequiresApproval`, `AllowAutomation`, `NotificationLevel`
- Garantir que slug de ambiente é único dentro do tenant (não globalmente)
- Criar evento de domínio `EnvironmentCreated` para propagação entre módulos

**Dependências:** Nenhuma  
**Risco:** Médio — altera aggregate root em ChangeGovernance  
**Estimativa:** Médio

---

### F1-002 — Adicionar TenantId a Release (ChangeGovernance)

**Objetivo:** `Release` aggregate deve pertencer a um tenant.

**Motivação:** `Release` representa um deployment de um serviço. Releases de diferentes clientes não devem ser visíveis entre si.

**Tarefas:**
- Adicionar `TenantId` ao `Release` aggregate
- Substituir `Environment: string` por `EnvironmentId: EnvironmentId` (referência ao ambiente unificado)
- Atualizar `Release.Create(...)` factory method
- Criar migration no `ChangeIntelligenceDbContext`
- Atualizar testes de domínio

**Dependências:** F1-001  
**Risco:** Alto — migration em tabela com dados de staging  
**Estimativa:** Médio

---

### F1-003 — Adicionar TenantId a PromotionRequest e PromotionGate

**Objetivo:** Promoções entre ambientes são operações tenant-específicas.

**Motivação:** `PromotionRequest` e `PromotionGate` sem `TenantId` permitem que um tenant solicite promoção em ambientes de outro tenant se os IDs forem adivinhados.

**Tarefas:**
- Adicionar `TenantId` a `PromotionRequest`
- Garantir que `SourceEnvironmentId` e `TargetEnvironmentId` pertencem ao mesmo tenant
- Atualizar domain validation em `CreatePromotionRequest`
- Criar migration no `PromotionDbContext`
- Atualizar testes

**Dependências:** F1-001, F1-002  
**Risco:** Alto  
**Estimativa:** Médio

---

### F1-004 — Adicionar TenantId a IncidentRecord (OperationalIntelligence)

**Objetivo:** Incidentes pertencem a um tenant e a um ambiente.

**Motivação:** `IncidentRecord` sem `TenantId` mistura incidentes de diferentes clientes. A correlação de incidentes deve operar por tenant.

**Tarefas:**
- Adicionar `TenantId` ao `IncidentRecord` aggregate
- Substituir `Environment: string` por `EnvironmentId: EnvironmentId`
- Atualizar `IncidentRecord.Create(...)` factory method
- Criar migration no OI DbContext
- Atualizar `IncidentCorrelationService` para operar por tenant
- Atualizar testes (279 existentes)

**Dependências:** F1-001  
**Risco:** Alto — 279 testes existentes, seed data precisa ser atualizado  
**Estimativa:** Grande

---

### F1-005 — Adicionar TenantId a ApiAsset e ServiceAsset (Catalog)

**Objetivo:** O catálogo de APIs e serviços é tenant-scoped.

**Motivação:** `ApiAsset` e `ServiceAsset` sem `TenantId` tornam o catálogo global. Tenants veriam serviços uns dos outros.

**Tarefas:**
- Adicionar `TenantId` a `ServiceAsset`
- Adicionar `TenantId` a `ApiAsset`
- Atualizar factory methods
- Criar migration no `CatalogGraphDbContext`
- Atualizar testes do Catalog module

**Dependências:** Nenhuma  
**Risco:** Muito Alto — entidade central do produto; muitas dependências  
**Estimativa:** Muito Grande

---

### F1-006 — Adicionar TenantId a ContractVersion e entidades derivadas (Catalog)

**Objetivo:** Contratos pertencem a um tenant.

**Motivação:** `ContractVersion` sem `TenantId` torna contratos globais. Contratos são o coração do produto — esta é uma lacuna crítica de segurança.

**Tarefas:**
- Adicionar `TenantId` a `ContractVersion`
- Adicionar `TenantId` a entidades que não herdam scope via agregado pai
- Criar migration no `ContractsDbContext`
- Atualizar handlers de contratos

**Dependências:** F1-005  
**Risco:** Crítico  
**Estimativa:** Muito Grande

---

### F1-007 — Trocar string TenantId por strongly typed em AIKnowledge

**Objetivo:** `AiTokenUsageLedger.TenantId: string` → `TenantId: TenantId`.

**Motivação:** Inconsistência de tipo — outros módulos usam strongly typed `TenantId(Guid Value)`, mas a entidade de AI usa `string`.

**Tarefas:**
- Alterar `AiTokenUsageLedger.TenantId` de `string` para `TenantId` strongly typed
- Alterar `AiExternalInferenceRecord.TenantId` idem
- Criar migration nos DbContexts afetados
- Atualizar testes

**Dependências:** Nenhuma  
**Risco:** Baixo — apenas mudança de tipo com migration  
**Estimativa:** Pequeno

---

### F1-008 — Adicionar TenantId a RunbookRecord e cost entities (OI)

**Objetivo:** Runbooks e entidades de custo são tenant-scoped.

**Tarefas:**
- Adicionar `TenantId` a `RunbookRecord`, `ServiceCostProfile`, `CostAttribution`, `CostSnapshot`
- Criar migrations
- Atualizar testes

**Dependências:** F1-004  
**Risco:** Médio  
**Estimativa:** Médio

---

## Fase 2 — Contexto: ICurrentEnvironment e Resolução

**Objetivo:** Criar a infraestrutura de resolução de ambiente ativo, análoga à de tenant.

**Dependências:** Fase 1 (EnvironmentId nos aggregates)  
**Risco:** Médio — nova abstração, não altera código existente  
**Critério de aceite:** `ICurrentEnvironment` resolvido em cada request HTTP; `EnvironmentIsolationBehavior` ativo no pipeline CQRS

---

### F2-001 — Criar ICurrentEnvironment nos BuildingBlocks

**Objetivo:** Criar abstração para ambiente ativo na requisição.

**Tarefas:**
- Criar `ICurrentEnvironment` em `BuildingBlocks.Application.Abstractions`
- Criar `CurrentEnvironmentAccessor` em `BuildingBlocks.Security`
- Criar `EnvironmentResolutionMiddleware` — resolve de: Header `X-Environment-Id` > query param `environmentId` > primeiro ambiente ativo do tenant
- Registrar no pipeline de DI

**Dependências:** F1-001  
**Risco:** Baixo  
**Estimativa:** Médio

---

### F2-002 — Criar EnvironmentIsolationBehavior (opcional por request)

**Objetivo:** Comportamento de pipeline CQRS para validar ambiente ativo quando necessário.

**Tarefas:**
- Criar `EnvironmentContextBehavior` que verifica `ICurrentEnvironment` para requests que implementem `IEnvironmentScopedRequest`
- Marcar queries/commands operacionais com `IEnvironmentScopedRequest`

**Dependências:** F2-001  
**Risco:** Baixo  
**Estimativa:** Médio

---

### F2-003 — Endpoint de listagem de ambientes por tenant

**Objetivo:** Backend expõe ambientes do tenant autenticado para o frontend.

**Tarefas:**
- Criar `GET /api/v1/environments` no módulo correto (IdentityAccess ou novo módulo de configuração)
- Retornar: `id`, `name`, `slug`, `sortOrder`, `isActive`, `profile` (operacional)
- Proteger com `require:environments:read` permission

**Dependências:** F1-001  
**Risco:** Baixo  
**Estimativa:** Pequeno

---

## Fase 3 — Dados: Migrations e Backfill

**Objetivo:** Aplicar as mudanças de schema e migrar dados existentes de forma segura.

**Dependências:** Fase 1 (domain) + Fase 2 (context)  
**Risco:** Muito Alto — operações de banco de dados em dados existentes  
**Critério de aceite:** Zero downtime; dados existentes com TenantId válido; rollback testado

---

### F3-001 — Migrations de adição de TenantId

**Tarefas por banco:**
- `change_intelligence_db`: Adicionar `TenantId` a `Releases`, `ReleasesBaselines`
- `promotion_db`: Adicionar `TenantId` a `DeploymentEnvironments`, `PromotionRequests`, `PromotionGates`
- `oi_db`: Adicionar `TenantId` a `IncidentRecords`, `RunbookRecords`, `ServiceCostProfiles`
- `catalog_graph_db`: Adicionar `TenantId` a `ServiceAssets`, `ApiAssets`, `ConsumerRelationships`
- `contracts_db`: Adicionar `TenantId` a `ContractVersions`

**Estratégia:** Adicionar coluna nullable first → backfill → adicionar NOT NULL constraint  
**Risco:** Crítico  
**Estimativa:** Muito Grande

---

### F3-002 — Scripts de backfill

**Tarefas:**
- Script de backfill para `Release`: inferir TenantId via `ApiAssetId → ServiceAsset → TenantId`
- Script de backfill para `IncidentRecord`: dados de staging/demo → TenantId do tenant de demo
- Script de backfill para `DeploymentEnvironment`: admin manual assignment
- Script de validação de integridade pós-backfill

**Risco:** Crítico — dados incorretos podem quebrar isolamento  
**Estimativa:** Grande

---

### F3-003 — Criar índices compostos

**Tarefas:**
- `CREATE INDEX ON releases (tenant_id, environment_id)`
- `CREATE INDEX ON incident_records (tenant_id, environment_id)`
- `CREATE INDEX ON consumer_relationships (tenant_id, consumer_environment_id)`
- `CREATE INDEX ON telemetry_* (tenant_id, environment_id)`
- Chaves únicas: `(tenant_id, slug)` em `deployment_environments`

**Risco:** Médio — índices podem demorar em tabelas grandes  
**Estimativa:** Médio

---

## Fase 4 — Backend Modular: Handlers, Repositories, Endpoints

**Objetivo:** Atualizar todos os handlers, repositórios e endpoints para usar `ICurrentTenant` e `ICurrentEnvironment` de forma consistente.

**Dependências:** Fases 1, 2, 3  
**Risco:** Alto — muito código a alterar; risco de regressão  
**Critério de aceite:** Zero queries sem `TenantId` em cláusula WHERE nos módulos operacionais

---

### F4-001 — Atualizar repositórios do Catalog

**Tarefas:**
- Todos os `FindByIdAsync`, `ListAsync`, etc. devem filtrar por `TenantId` do `ICurrentTenant`
- Remover hardcodes de ambiente nos handlers de Catalog

**Risco:** Alto  
**Estimativa:** Grande

---

### F4-002 — Atualizar repositórios do ChangeGovernance

**Tarefas:**
- `IReleaseRepository.ListAsync` → filtrar por `TenantId`
- `IDeploymentEnvironmentRepository.ListAsync` → filtrar por `TenantId`
- Validar cross-tenant nas promoções

**Risco:** Alto  
**Estimativa:** Médio

---

### F4-003 — Atualizar repositórios do OperationalIntelligence

**Tarefas:**
- `IIncidentRepository.ListAsync` → filtrar por `TenantId` + `EnvironmentId`
- Substituir `InMemoryIncidentStore` com seed data hardcoded por dados corretos
- Atualizar `IncidentSeedData` para usar tenant/environment real

**Risco:** Alto  
**Estimativa:** Grande

---

### F4-004 — Remover hardcodes de ambiente no Governance module

**Tarefas:**
- `GetPlatformReadiness`: remover `?? "Production"`
- `GetPlatformConfig`: remover `?? "Production"`
- `ListIntegrationConnectors`: remover `Environment: "Production"` hardcoded
- `GetIntegrationConnector`: idem
- `GetPackApplicability`: substituir `"Production"` por `EnvironmentId` dinâmico
- Implementar Governance Infrastructure (hoje vazia)

**Risco:** Médio  
**Estimativa:** Grande (inclui implementar Infrastructure)

---

### F4-005 — Atualizar endpoints da AI Runtime

**Tarefas:**
- Remover `TenantId` do body do `POST /api/v1/ai/chat` — usar `ICurrentTenant`
- Adicionar `EnvironmentId` ao contexto de chat (de `ICurrentEnvironment`)
- Atualizar `ExecuteAiChat.Command` para incluir `TenantId` e `EnvironmentId` do contexto
- Atualizar `SearchData.Query` — sempre usar `ICurrentTenant`

**Risco:** Médio  
**Estimativa:** Médio

---

### F4-006 — Atualizar Ingestion API

**Tarefas:**
- Aceitar `environmentSlug: string` no body dos payloads de ingestão
- Resolver para `EnvironmentId` via `IEnvironmentRepository.GetBySlugAsync(tenantId, slug)`
- Rejeitar com `400` ambientes desconhecidos para o tenant

**Risco:** Alto — breaking change no contrato da Ingestion API  
**Estimativa:** Médio

---

## Fase 5 — Telemetria e Integrações

**Objetivo:** Propagar TenantId e EnvironmentId em toda a stack de observabilidade e integrações.

**Dependências:** Fases 1-4  
**Risco:** Médio  
**Critério de aceite:** Todo trace, log e métrica inclui `tenant_id` e `environment_id`

---

### F5-001 — Tornar TenantId obrigatório nos modelos de telemetria

**Tarefas:**
- `Guid? TenantId` → `Guid TenantId` em todos os modelos de telemetria
- Atualizar `IProductStore` e `IMetricsStore` — adicionar `tenantId: Guid` às assinaturas
- Trocar `string environment` por `Guid environmentId` nas assinaturas de query

**Risco:** Médio  
**Estimativa:** Médio

---

### F5-002 — Adicionar enrichers de tenant/ambiente ao OTel e Serilog

**Tarefas:**
- Middleware de atividade: `Activity.Current?.SetTag("tenant_id", ...); Activity.Current?.SetTag("environment_id", ...)`
- Serilog enricher: `LogContext.PushProperty("TenantId", ...); LogContext.PushProperty("EnvironmentId", ...)`
- Métricas: adicionar dimensões `tenant_id` e `environment_id` ao `NexTraceMeters`

**Risco:** Baixo  
**Estimativa:** Médio

---

### F5-003 — Adicionar TenantId ao OutboxMessage

**Tarefas:**
- Adicionar `TenantId` ao `OutboxMessage`
- Garantir que consumers de integration events validam TenantId

**Risco:** Médio  
**Estimativa:** Pequeno

---

### F5-004 — Cache key builder com tenant+environment prefix

**Tarefas:**
- Criar `ICacheKeyBuilder.Build(tenantId, environmentId, resource, id)`
- Atualizar todos os pontos de uso de cache para incluir tenant/environment no prefixo

**Risco:** Médio  
**Estimativa:** Médio

---

## Fase 6 — Frontend

**Objetivo:** Eliminar hardcodes de ambiente, criar EnvironmentContext, injetar EnvironmentId nas queries.

**Dependências:** Fase 2 (endpoint `/api/v1/environments`) + Fase 4 (endpoints atualizados)  
**Risco:** Médio  
**Critério de aceite:** WorkspaceSwitcher lista ambientes reais do tenant; `X-Environment-Id` injetado em todas as requests

---

### F6-001 — Criar EnvironmentContext provider

**Tarefas:**
- Criar `contexts/EnvironmentContext.tsx` com `availableEnvironments`, `activeEnvironment`, `setActiveEnvironment`
- Persistir `activeEnvironment.id` em `sessionStorage['nxt_eid']`
- Carregar ambientes de `GET /api/v1/environments` após login + tenant selection

**Risco:** Baixo  
**Estimativa:** Pequeno

---

### F6-002 — Remover AVAILABLE_ENVIRONMENTS hardcoded

**Tarefas:**
- Remover `const AVAILABLE_ENVIRONMENTS = ['Production', 'Staging', 'Development']` do `WorkspaceSwitcher`
- Substituir por `useEnvironment().availableEnvironments`
- Permitir seleção de ambiente ativo

**Risco:** Baixo  
**Estimativa:** Pequeno

---

### F6-003 — Injetar X-Environment-Id no API client

**Tarefas:**
- Atualizar `api/client.ts` request interceptor para injetar `X-Environment-Id` header
- Adicionar `getEnvironmentId()` e `storeEnvironmentId()` ao `tokenStorage.ts`

**Risco:** Baixo  
**Estimativa:** Pequeno

---

### F6-004 — Remover defaults hardcoded de ambiente em formulários

**Tarefas:**
- `IncidentsPage`: substituir `environment: 'Production'` por `activeEnvironment?.id ?? ''`
- `ReleasesPage`: substituir `environment: 'production'` por `activeEnvironment?.slug ?? ''`
- `AutomationAdminPage`: carregar ações permitidas por ambiente do backend
- `ConnectorDetailPage`: remover mock data com `environment: 'Production'`

**Risco:** Baixo  
**Estimativa:** Médio

---

### F6-005 — Atualizar API types TypeScript

**Tarefas:**
- Substituir `environment?: string` por `environmentId?: string` nos DTOs
- Atualizar hooks React Query para usar `environmentId` do contexto ativo

**Risco:** Baixo  
**Estimativa:** Médio

---

## Fase 7 — IA

**Objetivo:** IA opera com contexto completo de tenant + ambiente injetado automaticamente.

**Dependências:** Fases 2, 4, 6  
**Risco:** Baixo (IA já usa contexto; é extensão de contexto existente)  
**Critério de aceite:** Prompts de chat incluem `tenant_id` e `environment_id`; zero possibilidade de TenantId opcional

---

### F7-001 — Context injection automática no chat

**Tarefas:**
- Injetar `TenantId` de `ICurrentTenant` no `ExecuteAiChat.Command` (não do body)
- Injetar `EnvironmentId` de `ICurrentEnvironment` no comando
- Atualizar system prompt builder para incluir contexto de tenant e ambiente
- Remover `body.TenantId` do endpoint `/api/v1/ai/chat`

**Risco:** Baixo  
**Estimativa:** Pequeno

---

### F7-002 — Implementar ExternalAI features com TenantId + EnvironmentId

**Tarefas:**
- Implementar os 8 features de ExternalAI com TenantId obrigatório desde o início
- Implementar os 8 features de Orchestration com TenantId + EnvironmentId

**Risco:** Baixo (stubs → implementação nova)  
**Estimativa:** Muito Grande

---

## Fase 8 — Testes e Rollout

**Objetivo:** Garantir cobertura de testes para os cenários de tenant/ambiente e rollout seguro.

**Dependências:** Todas as fases anteriores  
**Risco:** Médio  
**Critério de aceite:** Cobertura dos cenários críticos de isolamento; smoke tests por tenant

---

### F8-001 — Testes de isolamento por tenant

**Tarefas:**
- Adicionar integration tests: "Tenant A não vê dados do Tenant B"
- Adicionar integration tests: "Release de Tenant A não aparece para Tenant B"
- Adicionar integration tests: "Incident de Tenant A não aparece para Tenant B"
- Adicionar integration tests: "Catálogo de serviços é isolado por tenant"

**Risco:** Baixo  
**Estimativa:** Grande

---

### F8-002 — Atualizar fixtures de teste existentes

**Tarefas:**
- Atualizar seed data de testes com TenantId + EnvironmentId
- Atualizar todos os tests que criam Release/IncidentRecord sem TenantId
- Garantir que E2E tests usam ambientes reais do tenant

**Risco:** Médio  
**Estimativa:** Grande

---

### F8-003 — Feature flags de rollout (APENAS para rollout, não para lógica de negócio)

**Nota:** Os feature flags aqui são exclusivamente para habilitar/desabilitar gradualmente a nova lógica de contexto, **não** para substituir o modelo de ambientes.

**Tarefas:**
- Configurar rollout gradual do `EnvironmentResolutionMiddleware`
- Configurar rollout gradual da obrigatoriedade de `EnvironmentId` nas queries
- Monitorar logs de erros e regressões durante rollout

**Risco:** Baixo  
**Estimativa:** Médio

---

## Resumo de Prioridades

| Prioridade | Fase | Item Chave | Risco |
|-----------|------|-----------|-------|
| 🔴 Imediata | 1 | Adicionar TenantId a Release, IncidentRecord, ApiAsset, ContractVersion | Crítico |
| 🔴 Imediata | 1 | Unificar modelo de Environment com TenantId | Crítico |
| 🟠 Alta | 2 | Criar ICurrentEnvironment + endpoint de listagem | Alto |
| 🟠 Alta | 3 | Migrations e backfill | Muito Alto |
| 🟠 Alta | 4 | Remover hardcodes "Production" do Governance module | Alto |
| 🟠 Alta | 4 | Atualizar AI endpoints para usar ICurrentTenant | Alto |
| 🟡 Média | 5 | Telemetria tenant-aware | Médio |
| 🟡 Média | 6 | EnvironmentContext no frontend | Médio |
| 🟢 Baixa | 7 | AI context injection completa | Baixo |
| 🟢 Baixa | 8 | Cobertura de testes de isolamento | Médio |

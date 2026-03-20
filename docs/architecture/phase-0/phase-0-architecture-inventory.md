# Phase 0 — Architecture Inventory

**Data:** 2026-03-20  
**Scope:** Análise não-destrutiva do estado atual da solution NexTraceOne  
**Metodologia:** Inspeção direta do código-fonte, sem alteração de comportamento

---

## 1. Mapa da Solution

```
NexTraceOne.sln
├── src/
│   ├── platform/
│   │   ├── NexTraceOne.ApiHost            — API principal (REST, Minimal API)
│   │   ├── NexTraceOne.Ingestion.Api      — API pública para integrações CI/CD
│   │   └── NexTraceOne.BackgroundWorkers  — Quartz.NET jobs de expiração
│   ├── modules/
│   │   ├── catalog/                       — Catálogo de serviços, contratos, grafo, portal
│   │   ├── changegovernance/              — Releases, promoções, workflow, regras
│   │   ├── operationalintelligence/       — Incidentes, automação, custo, runbooks
│   │   ├── identityaccess/                — Usuários, tenants, ambientes, JIT, delegação
│   │   ├── aiknowledge/                   — IA: governança, runtime, orquestração
│   │   ├── governance/                    — FinOps, integrações, analytics, packs
│   │   └── auditcompliance/               — Audit trail
│   ├── building-blocks/
│   │   ├── Core                           — Primitives, Results, Guards, Events
│   │   ├── Application                    — CQRS, Behaviors, ICurrentTenant, ICurrentUser
│   │   ├── Infrastructure                 — EF, TenantRLS, AuditInterceptor, EventBus
│   │   ├── Security                       — JWT, ApiKey, TenantMiddleware, PermissionHandler
│   │   └── Observability                  — Serilog, OTel, Telemetry Models, IProductStore
│   └── frontend/
│       └── src/                           — React 18, TypeScript, Vite, Tanstack Query
├── tests/
│   ├── platform/                          — Integration (17 pass) + E2E
│   └── modules/                           — Unit tests por módulo
└── tools/
    └── NexTraceOne.CLI
```

---

## 2. Mapa dos Módulos

| Módulo | Subdomínios | DbContexts | Estado |
|--------|-------------|-----------|--------|
| **IdentityAccess** | Users, Tenants, Roles, Environments, JIT, Break Glass, Delegation, Access Review | IdentityDbContext | Funcional, TenantId presente |
| **Catalog** | Graph (ApiAsset, ServiceAsset), Contracts (ContractVersion), Portal (Playground, Subscription), SourceOfTruth | CatalogGraphDbContext, ContractsDbContext, DeveloperPortalDbContext | Funcional, **TenantId ausente** |
| **ChangeGovernance** | ChangeIntelligence (Release, Baseline), Promotion (DeploymentEnvironment, PromotionRequest, PromotionGate), Workflow, RulesetGovernance | ChangeIntelligenceDbContext, PromotionDbContext, WorkflowDbContext, RulesetGovernanceDbContext | Funcional, **TenantId ausente** |
| **OperationalIntelligence** | Incidents, Mitigation, Runbooks, Automation, Cost | OIDbContext (EF persist) + InMemoryIncidentStore | Parcialmente funcional, **TenantId ausente** |
| **AIKnowledge** | Governance (Runtime: chat, models, quotas), ExternalAI (TODO stubs), Orchestration (TODO stubs) | AiGovernanceDbContext, ExternalAiDbContext, AiOrchestrationDbContext | Parcialmente funcional, TenantId como string em AI entities |
| **Governance** | FinOps, IntegrationConnectors, Analytics, GovernancePacks | GovernanceDbContext | Infrastructure vazia, dados mock, hardcodes |
| **AuditCompliance** | AuditEvents | AuditDbContext | Funcional, TenantId presente |

---

## 3. Mapa dos Contextos Existentes

### 3.1 Contexto de Tenant (backend)

| Componente | Localização | Status |
|------------|-------------|--------|
| `ICurrentTenant` | BuildingBlocks.Application | ✅ Implementado |
| `CurrentTenantAccessor` | BuildingBlocks.Security | ✅ Implementado |
| `TenantResolutionMiddleware` | BuildingBlocks.Security | ✅ JWT > Header > Subdomain |
| `TenantIsolationBehavior` | BuildingBlocks.Application | ✅ Valida tenant em todo CQRS |
| `TenantRlsInterceptor` | BuildingBlocks.Infrastructure | ✅ Configura RLS no PostgreSQL |

**Lacuna crítica:** `ICurrentTenant` resolve o tenant mas **não resolve o ambiente ativo**. Não existe `ICurrentEnvironment`.

### 3.2 Contexto de Ambiente (backend)

| Componente | Localização | Status |
|------------|-------------|--------|
| `ICurrentEnvironment` | — | ❌ Não existe |
| Resolução de ambiente ativo | — | ❌ Não existe |
| Propagação de EnvironmentId em CQRS | — | ❌ Não existe |

### 3.3 Contexto de Usuário (backend)

| Componente | Localização | Status |
|------------|-------------|--------|
| `ICurrentUser` | BuildingBlocks.Application | ✅ Implementado |
| `HttpContextCurrentUser` | BuildingBlocks.Security | ✅ Implementado |
| Claims: `tenant_id`, `user_id`, `permissions` | JWT | ✅ Implementados |

### 3.4 Contexto no Frontend

| Componente | Localização | Status |
|------------|-------------|--------|
| `AuthContext` | `contexts/AuthContext.tsx` | ✅ Tenant + User state |
| `PersonaContext` | `contexts/PersonaContext.tsx` | ✅ Persona derivada do role |
| `EnvironmentContext` | — | ❌ Não existe |
| Ambiente ativo | `WorkspaceSwitcher` (hardcoded) | ⚠️ `'Production'` como default |
| TenantId storage | `sessionStorage['nxt_tid']` | ✅ Persistido após login |
| EnvironmentId storage | — | ❌ Não existe |

---

## 4. Pontos Fortes da Arquitetura Atual

1. **Infraestrutura de multitenant sólida** — `TenantResolutionMiddleware` + `TenantRlsInterceptor` + `TenantIsolationBehavior` formam uma camada coerente de isolamento.
2. **Strongly typed IDs** — `TenantId`, `EnvironmentId`, `UserId`, `ReleaseId`, etc. como `record TypedIdBase` previnem confusão de tipos.
3. **CQRS/MediatR com pipeline behaviors** — Validação, logging, performance, tenant isolation e transações via pipeline behaviors.
4. **RLS no PostgreSQL** — O interceptor configura `app.current_tenant_id` em cada comando SQL, fornecendo isolamento em nível de banco.
5. **Separação modular clara** — Domain, Application, Infrastructure, API, Contracts por módulo.
6. **Building blocks bem estruturados** — Primitives, Results, Guards, Events são reutilizáveis e seguros.
7. **Identity module com Environment bem modelado** — `IdentityAccess.Domain.Entities.Environment` é tenant-scoped com `TenantId` e slug único.
8. **Telemetria com TenantId nos modelos** — `ObservedTopologyEntry`, `AnomalySnapshot`, etc. já têm `Guid? TenantId`.

---

## 5. Pontos Frágeis

1. **Catálogo global** — `ApiAsset`, `ServiceAsset`, `ContractVersion` sem `TenantId`. O core do produto não tem tenant isolation.
2. **ChangeGovernance global** — `Release`, `PromotionRequest`, `DeploymentEnvironment` sem `TenantId`. Releases são compartilhadas entre tenants.
3. **OperationalIntelligence global** — `IncidentRecord` sem `TenantId`. Incidentes de diferentes clientes são misturados.
4. **Dois modelos de Environment desconexos** — IdentityAccess tem ambiente tenant-scoped; ChangeGovernance tem `DeploymentEnvironment` global.
5. **Ambientes hardcoded em múltiplos pontos** — `"Production"`, `"Staging"`, `"Development"` no backend e frontend.
6. **AutomationActionCatalog hardcoded** — Assume exatamente 3 ambientes com nomes fixos.
7. **Frontend sem EnvironmentContext** — Ambiente é um valor visual estático, não um contexto funcional.
8. **IA sem tenant/environment injection automática** — `TenantId` é campo opcional no body do request.
9. **Telemetria com TenantId nullable** — Dados sem tenant podem ser gravados e consultados.
10. **InMemoryIncidentStore com "Production"/"Staging" hardcoded** — Seed data e mock data com valores fixos.
11. **Governance module com Infrastructure vazia** — Entidades definidas mas sem persistência real; dados mock com hardcodes.

---

## 6. Premissas Incorretas Encontradas

| Premissa Incorreta | Onde | Impacto |
|---------------------|------|---------|
| "Existem 3 ambientes: Dev, Staging, Prod" | `WorkspaceSwitcher`, `AutomationActionCatalog`, seed data | Impede tenants com ambientes customizados |
| "Ambientes de deployment são globais" | `DeploymentEnvironment` sem TenantId | Promoções entre ambientes não são tenant-isolated |
| "O catálogo de APIs é global" | `ApiAsset`, `ServiceAsset` sem TenantId | Impossibilita multi-tenancy no core |
| "TenantId na IA é opcional" | `body.TenantId` em `/ai/chat` | Risco de queries cross-tenant |
| "Telemetria pode não ter tenant" | `Guid? TenantId` em modelos | Dados de telemetria sem isolamento |
| "O ambiente ativo não precisa ser propagado nas APIs" | Ausência de `EnvironmentId` em queries/headers | Filtros de dados imprecisos |
| "Environment == string == name" | Todos os módulos operacionais | Impede ambientes com nomes livres |

---

## 7. Estado dos Testes

| Área | Cobertura | Observações |
|------|-----------|-------------|
| BuildingBlocks | Alta | Tests presentes e passando |
| IdentityAccess | Alta | TenantId bem testado |
| ChangeGovernance | Média | Promotion domain testado; ChangeIntelligence menos |
| OperationalIntelligence | Alta | 279 testes passando (incluindo correlação) |
| AIKnowledge | Média | Runtime e governança testados; Orchestration/ExternalAI são stubs |
| Catalog | Média | Contracts e Graph testados; Portal menos |
| Governance | Baixa | Infrastructure vazia, testes em Application |
| AuditCompliance | Média | Testes de feature presentes |
| Integration | Presente | 17 testes passando com PostgreSQL real |
| E2E | Presente | Testes de flow de API |
| Frontend | Alta | 336 testes passando (vitest) |

**Áreas sem cobertura relevante para o refactor:**
- Nenhum teste cobre o cenário "EnvironmentId incorreto ou de outro tenant"
- Nenhum teste valida isolamento de Release entre tenants
- Nenhum teste valida que IncidentRecord fica isolado por tenant
- Nenhum teste valida que catálogo de serviços é tenant-scoped

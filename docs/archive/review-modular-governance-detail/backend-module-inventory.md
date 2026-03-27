# Inventário Modular do Backend — NexTraceOne

> **Data:** 2025-01  
> **Versão:** 1.0  
> **Tipo:** Inventário técnico detalhado  
> **Escopo:** Todos os módulos, building blocks, plataforma e CLI

---

## 1. Resumo Geral

| Categoria | Quantidade |
|-----------|-----------|
| Módulos de domínio | 9 |
| Building blocks | 5 |
| Projectos de plataforma | 3 |
| Ferramentas CLI | 1 |
| Total de projectos C# | 71 |

---

## 2. Módulos de Domínio

### 2.1 aiknowledge

| Atributo | Valor |
|----------|-------|
| **Tipo** | Módulo de domínio |
| **Finalidade** | Gestão de IA interna/externa, governança de modelos, orquestração, integração IDE |
| **Camadas** | Domain, Application, Infrastructure, API |
| **Entidades** | 72 |
| **DbContexts** | 3 (ExternalAiDb, AiGovernanceDb, AiOrchestrationDb) |
| **Migrações** | 17 |
| **Features** | 70 |
| **Endpoint modules** | 5 (ExternalAi, AiGovernance, AiIde, AiOrchestration, AiRuntime) |
| **Testes** | Incluídos na contagem modular (263 total módulos) |
| **Dependências** | Core, Application, Infrastructure, Security |
| **Base de dados** | nextraceone_ai |
| **Estado** | ACTIVO |
| **Observações** | AiGovernanceEndpointModule tem 665 linhas — necessita decomposição |

**Caminho:** `src/modules/aiknowledge/`

---

### 2.2 auditcompliance

| Atributo | Valor |
|----------|-------|
| **Tipo** | Módulo de domínio |
| **Finalidade** | Auditoria de eventos, compliance, trail de alterações, relatórios de conformidade |
| **Camadas** | Domain, Application, Infrastructure, API |
| **Entidades** | 11 |
| **DbContexts** | 1 (AuditDb) |
| **Migrações** | 2 |
| **Features** | 15 |
| **Endpoint modules** | 1 (AuditEndpointModule) |
| **Testes** | Incluídos na contagem modular |
| **Dependências** | Core, Application, Infrastructure |
| **Base de dados** | nextraceone_identity |
| **Estado** | ACTIVO |
| **Observações** | Módulo compacto e coerente |

**Caminho:** `src/modules/auditcompliance/`

---

### 2.3 catalog

| Atributo | Valor |
|----------|-------|
| **Tipo** | Módulo de domínio |
| **Finalidade** | Catálogo de serviços, contratos (REST/SOAP/Kafka), portal de developer, Source of Truth |
| **Camadas** | Domain, Application, Infrastructure, API |
| **Entidades** | 82 |
| **DbContexts** | 3 (ContractsDb, CatalogGraphDb, DeveloperPortalDb) |
| **Migrações** | 6 |
| **Features** | 83 |
| **Endpoint modules** | 5 (Contracts, ContractStudio, ServiceCatalog, DeveloperPortal, SourceOfTruth) |
| **Testes** | Incluídos na contagem modular |
| **Dependências** | Core, Application, Infrastructure, Security |
| **Base de dados** | nextraceone_catalog |
| **Estado** | ACTIVO |
| **Observações** | Módulo mais rico em entidades. Pilar central do produto (Source of Truth) |

**Caminho:** `src/modules/catalog/`

---

### 2.4 changegovernance

| Atributo | Valor |
|----------|-------|
| **Tipo** | Módulo de domínio |
| **Finalidade** | Change Intelligence, promoção de ambientes, rulesets, workflows de aprovação |
| **Camadas** | Domain, Application, Infrastructure, API |
| **Entidades** | 47 |
| **DbContexts** | 4 (ChangeIntelDb, PromotionDb, RulesetGovernanceDb, WorkflowDb) |
| **Migrações** | 8 |
| **Features** | 57 |
| **Endpoint modules** | 4 (ChangeIntelligence→6 sub, Promotion, RulesetGovernance, Workflow→4 sub) |
| **Testes** | Incluídos na contagem modular |
| **Dependências** | Core, Application, Infrastructure, Security |
| **Base de dados** | nextraceone_operations |
| **Estado** | ACTIVO |
| **Observações** | ChangeIntelligence e Workflow usam delegação para sub-endpoints |

**Caminho:** `src/modules/changegovernance/`

---

### 2.5 configuration

| Atributo | Valor |
|----------|-------|
| **Tipo** | Módulo de domínio |
| **Finalidade** | Configurações dinâmicas da plataforma, definições por fase |
| **Camadas** | Domain, Application, Infrastructure, API |
| **Entidades** | 6 |
| **DbContexts** | 1 (ConfigurationDb) |
| **Migrações** | 0 |
| **Features** | 7 |
| **Endpoint modules** | 1 (ConfigurationEndpointModule) |
| **Testes** | Incluídos na contagem modular |
| **Dependências** | Core, Application, Infrastructure |
| **Base de dados** | nextraceone_operations |
| **Estado** | ACTIVO |
| **Observações** | ⚠️ Usa EnsureCreated em vez de migrações — risco operacional |

**Caminho:** `src/modules/configuration/`

---

### 2.6 governance

| Atributo | Valor |
|----------|-------|
| **Tipo** | Módulo de domínio |
| **Finalidade** | Governança organizacional: equipas, domínios, políticas, packs, waivers, FinOps, risco, compliance, relatórios |
| **Camadas** | Domain, Application, Infrastructure, API |
| **Entidades** | 58 |
| **DbContexts** | 1 (GovernanceDb) |
| **Migrações** | 3 |
| **Features** | 73 |
| **Endpoint modules** | 18 (Teams, Domains, Policies, Packs, Waivers, FinOps, Risk, Compliance, Reports, Controls, Evidence, Executive, Integrations, Onboarding, PlatformStatus, ProductAnalytics, ScopedContext, DelegatedAdmin) |
| **Testes** | Incluídos na contagem modular |
| **Dependências** | Core, Application, Infrastructure, Security |
| **Base de dados** | nextraceone_operations |
| **Estado** | ACTIVO |
| **Observações** | Módulo com maior número de endpoint modules (18). Alta granularidade. |

**Caminho:** `src/modules/governance/`

---

### 2.7 identityaccess

| Atributo | Valor |
|----------|-------|
| **Tipo** | Módulo de domínio |
| **Finalidade** | Identidade, autenticação, autorização, sessões, BreakGlass, JIT access, delegação, access review |
| **Camadas** | Domain, Application, Infrastructure, API |
| **Entidades** | 37 |
| **DbContexts** | 1 (IdentityDb) |
| **Migrações** | 2 |
| **Features** | 71 |
| **Endpoint modules** | 1→10 sub-módulos (Auth, User, RolePermission, BreakGlass, JitAccess, Delegation, Tenant, AccessReview, Environment, RuntimeContext, CookieSession) |
| **Testes** | Incluídos na contagem modular |
| **Dependências** | Core, Application, Infrastructure, Security |
| **Base de dados** | nextraceone_identity |
| **Estado** | ACTIVO |
| **Observações** | Sub-módulos bem granulares. Falta seed de produção para roles e permissões |

**Caminho:** `src/modules/identityaccess/`

---

### 2.8 notifications

| Atributo | Valor |
|----------|-------|
| **Tipo** | Módulo de domínio |
| **Finalidade** | Centro de notificações, preferências, envio multi-canal |
| **Camadas** | Domain, Application, Infrastructure, API |
| **Entidades** | 15 |
| **DbContexts** | 1 (NotificationsDb) |
| **Migrações** | 0 |
| **Features** | 15 |
| **Endpoint modules** | 1 (NotificationCenterEndpointModule) |
| **Testes** | Incluídos na contagem modular |
| **Dependências** | Core, Application, Infrastructure |
| **Base de dados** | nextraceone_operations |
| **Estado** | PARCIAL |
| **Observações** | ⚠️ Sem migrações — schema não é versionado. Estado PARCIAL indica funcionalidade incompleta |

**Caminho:** `src/modules/notifications/`

---

### 2.9 operationalintelligence

| Atributo | Valor |
|----------|-------|
| **Tipo** | Módulo de domínio |
| **Finalidade** | Automação, cost intelligence, incidentes, mitigação, runbooks, fiabilidade, runtime intelligence |
| **Camadas** | Domain, Application, Infrastructure, API |
| **Entidades** | 51 |
| **DbContexts** | 5 (AutomationDb, CostIntelDb, IncidentDb, ReliabilityDb, RuntimeIntelDb) |
| **Migrações** | 12 |
| **Features** | 55 |
| **Endpoint modules** | 7 (Automation, CostIntelligence, Incident, Mitigation, Runbook, Reliability, RuntimeIntelligence) |
| **Testes** | Incluídos na contagem modular |
| **Dependências** | Core, Application, Infrastructure, Security |
| **Base de dados** | nextraceone_operations |
| **Estado** | ACTIVO |
| **Observações** | Módulo com maior número de DbContexts (5). Boa separação por bounded context |

**Caminho:** `src/modules/operationalintelligence/`

---

## 3. Building Blocks

### 3.1 Core

| Atributo | Valor |
|----------|-------|
| **Tipo** | Building block |
| **Camada** | Domain |
| **Finalidade** | Abstracções base para entidades, agregados, value objects, IDs tipados |
| **Componentes principais** | Entity\<TId\>, AggregateRoot\<TId\>, ValueObject, AuditableEntity\<TId\>, TypedIdBase |
| **Testes** | Incluídos nos 43 testes de building blocks |
| **Dependências** | Nenhuma (é a base) |
| **Estado** | ACTIVO |

**Caminho:** `src/buildingblocks/Core/`

---

### 3.2 Application

| Atributo | Valor |
|----------|-------|
| **Tipo** | Building block |
| **Camada** | Application |
| **Finalidade** | Abstracções CQRS, MediatR behaviors, interfaces de aplicação |
| **Componentes principais** | ICommand, IQuery, handlers, ValidationBehavior, TransactionBehavior, TenantIsolationBehavior, LoggingBehavior, PerformanceBehavior |
| **Testes** | Incluídos nos 43 testes de building blocks |
| **Dependências** | Core |
| **Estado** | ACTIVO |

**Caminho:** `src/buildingblocks/Application/`

---

### 3.3 Infrastructure

| Atributo | Valor |
|----------|-------|
| **Tipo** | Building block |
| **Camada** | Infrastructure |
| **Finalidade** | Persistência base, interceptors, outbox, event bus, conversores |
| **Componentes principais** | NexTraceDbContextBase, RepositoryBase, AuditInterceptor, TenantRlsInterceptor, EncryptedStringConverter, OutboxMessage, EventBus |
| **Testes** | Incluídos nos 43 testes de building blocks |
| **Dependências** | Core, Application |
| **Estado** | ACTIVO |

**Caminho:** `src/buildingblocks/Infrastructure/`

---

### 3.4 Security

| Atributo | Valor |
|----------|-------|
| **Tipo** | Building block |
| **Camada** | Cross-cutting |
| **Finalidade** | Autenticação, autorização, CSRF, tenant resolution, headers de segurança |
| **Componentes principais** | JWT handler, API Key handler, PermissionPolicyProvider, PermissionAuthorizationHandler, TenantResolutionMiddleware, CsrfTokenValidator |
| **Testes** | Incluídos nos 43 testes de building blocks |
| **Dependências** | Core, Application |
| **Estado** | ACTIVO |

**Caminho:** `src/buildingblocks/Security/`

---

### 3.5 Observability

| Atributo | Valor |
|----------|-------|
| **Tipo** | Building block |
| **Camada** | Cross-cutting |
| **Finalidade** | Logging, tracing, métricas, alerting, health checks |
| **Componentes principais** | Serilog config, OpenTelemetry config, AlertGateway (email/webhook), health checks, NexTraceMeters, NexTraceActivitySources |
| **Testes** | Incluídos nos 43 testes de building blocks |
| **Dependências** | Core |
| **Estado** | ACTIVO |

**Caminho:** `src/buildingblocks/Observability/`

---

## 4. Projectos de Plataforma

### 4.1 ApiHost

| Atributo | Valor |
|----------|-------|
| **Tipo** | ASP.NET Host unificado |
| **Finalidade** | Host principal que compõe todos os endpoint modules via auto-discovery por reflexão |
| **Middleware pipeline** | ResponseCompression → HttpsRedirection → CORS → RateLimiter → SecurityHeaders → GlobalExceptionHandler → CookieSessionCsrfProtection → Authentication → TenantResolution → EnvironmentResolution → Authorization |
| **Testes** | Incluídos nos 24 testes de plataforma |
| **Estado** | ACTIVO |

**Caminho:** `src/platform/ApiHost/`

---

### 4.2 BackgroundWorkers

| Atributo | Valor |
|----------|-------|
| **Tipo** | Worker Service (.NET) |
| **Finalidade** | Processamento assíncrono: outbox, expiração de acessos, detecção de drift |
| **Jobs** | OutboxProcessorJob, IdentityExpirationJob (5 handlers), DriftDetectionJob |
| **Testes** | Incluídos nos 24 testes de plataforma |
| **Estado** | ACTIVO |

**Caminho:** `src/platform/BackgroundWorkers/`

**IdentityExpirationJob — 5 Handlers:**

| Handler | Finalidade |
|---------|-----------|
| AccessReviewExpirationHandler | Expiração de access reviews |
| BreakGlassExpirationHandler | Expiração de acessos BreakGlass |
| DelegationExpirationHandler | Expiração de delegações |
| EnvironmentAccessExpirationHandler | Expiração de acessos a ambientes |
| JitAccessExpirationHandler | Expiração de acessos JIT |

---

### 4.3 Ingestion.Api

| Atributo | Valor |
|----------|-------|
| **Tipo** | ASP.NET API |
| **Finalidade** | Ingestão de telemetria, eventos e dados operacionais |
| **Testes** | Incluídos nos 24 testes de plataforma |
| **Estado** | ACTIVO |

**Caminho:** `src/platform/Ingestion.Api/`

---

## 5. Ferramenta CLI

### 5.1 nex CLI

| Atributo | Valor |
|----------|-------|
| **Tipo** | CLI tool (.NET) |
| **Finalidade** | Operações de linha de comando para validação e consulta |
| **Comandos** | `validate`, `catalog list`, `catalog get` |
| **Estado** | ACTIVO |

**Caminho:** `tools/cli/`

| Comando | Sintaxe | Finalidade |
|---------|---------|-----------|
| validate | `nex validate <file.json>` | Validação de manifesto de contrato |
| catalog list | `nex catalog list` | Listar serviços do catálogo |
| catalog get | `nex catalog get` | Obter detalhes de um serviço |

---

## 6. Distribuição de Testes

| Categoria | Ficheiros de teste | Percentagem |
|-----------|-------------------|-------------|
| Building blocks | 43 | 13% |
| Módulos | 263 | 80% |
| Plataforma | 24 | 7% |
| **Total** | **330** | **100%** |

---

## 7. Mapa de Dependências entre Módulos

```
Core (base)
  └── Application
       └── Infrastructure
            ├── Security
            └── Observability

Módulos de domínio:
  aiknowledge         → Core, Application, Infrastructure, Security
  auditcompliance     → Core, Application, Infrastructure
  catalog             → Core, Application, Infrastructure, Security
  changegovernance    → Core, Application, Infrastructure, Security
  configuration       → Core, Application, Infrastructure
  governance          → Core, Application, Infrastructure, Security
  identityaccess      → Core, Application, Infrastructure, Security
  notifications       → Core, Application, Infrastructure
  operationalintelligence → Core, Application, Infrastructure, Security

Plataforma:
  ApiHost             → todos os módulos (via auto-discovery)
  BackgroundWorkers   → identityaccess, configuration, outbox
  Ingestion.Api       → Core, Infrastructure
```

---

## 8. Classificação por Maturidade

| Módulo | Entidades | Endpoints | Migrações | Testes | Classificação |
|--------|-----------|-----------|-----------|--------|---------------|
| catalog | 82 | 5 modules | 6 | ✅ | MADURO |
| governance | 58 | 18 modules | 3 | ✅ | MADURO |
| aiknowledge | 72 | 5 modules | 17 | ✅ | MADURO |
| identityaccess | 37 | 10 sub-modules | 2 | ✅ | MADURO |
| changegovernance | 47 | 4+10 sub | 8 | ✅ | MADURO |
| operationalintelligence | 51 | 7 modules | 12 | ✅ | MADURO |
| auditcompliance | 11 | 1 module | 2 | ✅ | COERENTE |
| configuration | 6 | 1 module | 0 | ✅ | ⚠️ PARCIAL |
| notifications | 15 | 1 module | 0 | ✅ | ⚠️ PARCIAL |

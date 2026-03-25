# NexTraceOne — Environment Management Module

## Visão Geral

O módulo Environment Management gere os ambientes operacionais dos tenants no NexTraceOne.
É owner do conceito de "ambiente" como dimensão funcional que influencia:
permissões, promotion flows, change governance, operational intelligence,
readiness, drift, baseline, auditoria e configuração por escopo.

Cada tenant pode definir os seus próprios ambientes (DEV, QA, UAT, PROD, DR, etc.)
com perfil operacional, criticidade e designação do ambiente produtivo principal.

## Fronteira com Identity & Access

Environment Management é um bounded context próprio, separado do Identity & Access:
- **Identity & Access** é owner de **utilizadores, roles, sessões, permissões**
- **Environment Management** é owner de **ambientes, perfis operacionais, acessos por ambiente**

Outros módulos (Configuration, Change Governance, Operational Intelligence, Audit)
consomem ambiente como dimensão mas não são donos dele.

> **Nota**: Fisicamente, o código ainda reside em `src/modules/identityaccess/*/Entities/Environment*`.
> A extração para `src/modules/environmentmanagement/` está planeada como OI-04 (Wave 0).

## Arquitetura

```
NexTraceOne.IdentityAccess.Domain/Entities/        → Environment, EnvironmentAccess
NexTraceOne.IdentityAccess.Domain/Enums/            → EnvironmentProfile, EnvironmentCriticality
NexTraceOne.IdentityAccess.Domain/ValueObjects/     → EnvironmentUiProfile, TenantEnvironmentContext
NexTraceOne.IdentityAccess.Application/Features/    → CreateEnvironment, ListEnvironments, UpdateEnvironment, etc.
NexTraceOne.IdentityAccess.Application/Abstractions/→ IEnvironmentRepository, IEnvironmentContextAccessor
NexTraceOne.IdentityAccess.Infrastructure/Persistence/→ EnvironmentConfiguration, EnvironmentRepository
NexTraceOne.IdentityAccess.Infrastructure/Context/  → EnvironmentContextAccessor, EnvironmentResolutionMiddleware
NexTraceOne.IdentityAccess.API/Endpoints/           → EnvironmentEndpoints
```

## Entidades

### Aggregate Root
| Entidade      | Papel                                           |
|--------------|------------------------------------------------|
| `Environment` | Ambiente operacional com perfil, criticidade e lifecycle |

### Child Entity
| Entidade              | Pai          |
|----------------------|-------------|
| `EnvironmentAccess`   | Environment  |

### Related Entities
| Entidade                     | Contexto                          |
|-----------------------------|----------------------------------|
| `EnvironmentPolicy`          | Políticas de ambiente (Fase 2)    |
| `EnvironmentTelemetryPolicy` | Políticas de telemetria (Fase 2)  |
| `EnvironmentIntegrationBinding` | Bindings de integração (Fase 2) |

### Enums
| Enum                      | Valores                                                     |
|--------------------------|-------------------------------------------------------------|
| `EnvironmentProfile`      | Development(1)..PerformanceTesting(9)                       |
| `EnvironmentCriticality`  | Low(1), Medium(2), High(3), Critical(4)                     |

### Value Objects
| VO                         | Contexto                          |
|---------------------------|----------------------------------|
| `EnvironmentUiProfile`     | Perfil visual (ícone, cor, label) |
| `TenantEnvironmentContext`  | Contexto tenant+ambiente resolvido |
| `EnvironmentAccessLevel`   | Read, Write, Admin, None (constantes) |

## Lifecycle do Ambiente

```
Active (IsActive=true) → Deactivate() → Inactive (IsActive=false)
Active → DesignateAsPrimaryProduction() → Primary Production (IsPrimaryProduction=true)
Primary Production → RevokePrimaryProductionDesignation() → Active
Active → SoftDelete() → Deleted (IsDeleted=true, IsActive=false)
```

**Regras importantes:**
- Um ambiente Primary Production não pode ser desativado (guard no Deactivate)
- Um ambiente Primary Production não pode ser soft-deleted (guard no SoftDelete)
- Apenas um ambiente ativo pode ser Primary Production por tenant (índice parcial único)
- Slug é imutável após criação e único dentro do tenant

## Endpoints REST

### EnvironmentEndpoints (`/api/v1/identity/environments`)
| Método | Rota                                     | Permissão                | Descrição                    |
|--------|----------------------------------------|--------------------------|------------------------------|
| GET    | `/`                                      | `env:environments:read`  | Lista ambientes do tenant     |
| GET    | `/primary-production`                    | `env:environments:read`  | Obtém produção principal      |
| POST   | `/`                                      | `env:environments:write` | Cria ambiente                 |
| PUT    | `/{environmentId}`                       | `env:environments:write` | Atualiza ambiente             |
| PATCH  | `/{environmentId}/primary-production`    | `env:environments:admin` | Designa produção principal    |
| POST   | `/access`                                | `env:access:admin`       | Concede acesso                |

## Base de Dados

- **Tabelas**: `env_environments`, `env_environment_accesses`
- **Prefixo**: `env_` (atualizado de `identity_`)
- **Concorrência otimista**: PostgreSQL xmin via RowVersion em Environment e EnvironmentAccess
- **Check constraints**: Profile (1-9), Criticality (1-4), SortOrder ≥ 0, AccessLevel IN ('read','write','admin','none')
- **FK**: EnvironmentAccess → Environment (Cascade)
- **Soft-delete**: IsDeleted com query filter global
- **Índices**: tenant_slug (unique), primary_production (unique filtered), tenant_active (filtered), tenant_profile, tenant_criticality, unique_active_access (filtered)

## Permissões

| Permissão                | Roles                                          |
|-------------------------|-------------------------------------------------|
| `env:environments:read`  | PlatformAdmin, TechLead, Developer, Viewer, Auditor, SecurityReview |
| `env:environments:write` | PlatformAdmin, TechLead                          |
| `env:environments:admin` | PlatformAdmin                                    |
| `env:access:read`        | PlatformAdmin, TechLead, Developer, Auditor, SecurityReview |
| `env:access:admin`       | PlatformAdmin                                    |

## Frontend

### Páginas
| Página                    | Rota               | Descrição                    |
|--------------------------|--------------------|-----------------------------|
| EnvironmentsPage          | `/environments`     | Lista e gestão de ambientes  |
| EnvironmentComparisonPage | `/operations/runtime-comparison` | Comparação entre ambientes (Operations) |

### Componentes globais
| Componente          | Localização | Descrição                          |
|--------------------|-----------|------------------------------------|
| EnvironmentBanner   | Shell      | Banner de ambiente ativo             |
| EnvironmentContext   | Context    | Provider/hook de contexto de ambiente |

## Módulos Consumidores

| Módulo                  | Relação com Environment                      |
|------------------------|---------------------------------------------|
| Configuration           | Entries podem ter EnvironmentId              |
| Change Governance       | Promotion paths referenciam ambientes        |
| Operational Intelligence| Métricas e incidentes por ambiente           |
| Audit & Compliance      | Eventos auditados por ambiente               |
| AI & Knowledge          | Comparação non-prod vs prod                  |

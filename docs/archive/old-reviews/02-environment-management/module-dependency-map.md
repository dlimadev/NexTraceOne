# Environment Management — Module Dependency Map

> **Módulo:** 02 — Environment Management  
> **Data:** 2026-03-25  
> **Fase:** N4-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Visão geral de dependências

```
                    ┌────────────────────────┐
                    │   BuildingBlocks.Core   │
                    │  ICurrentEnvironment    │
                    │  EnvironmentId (typed)  │
                    │  TenantId (typed)       │
                    └──────────┬─────────────┘
                               │ expõe interface
                               │
          ┌────────────────────┼────────────────────┐
          │                    │                     │
          ▼                    ▼                     ▼
┌──────────────────┐ ┌────────────────────┐ ┌───────────────────┐
│  Identity &      │ │  Environment       │ │  Change           │
│  Access          │ │  Management        │ │  Governance       │
│                  │ │  (target module)   │ │                   │
│ • EnvironmentAccess│ │ • Environment     │ │ • DeploymentEnv   │
│   (auth reads)   │ │ • Env Policy      │ │ • PromotionGate   │
│ • Resolution MW  │ │ • Telemetry Policy│ │ • PromotionRequest│
│ • Access Validator│ │ • Integration Bind│ │                   │
└────────┬─────────┘ └────────┬──────────┘ └────────┬──────────┘
         │                    │                      │
         │        ┌───────────┼───────────┐          │
         │        │           │           │          │
         ▼        ▼           ▼           ▼          ▼
┌──────────────┐ ┌─────────────┐ ┌──────────────┐ ┌──────────────┐
│ Operational  │ │ AI &        │ │ Configuration│ │ Audit &      │
│ Intelligence │ │ Knowledge   │ │              │ │ Compliance   │
│              │ │             │ │ • Scope:     │ │              │
│ • IncidentRecord│ │ • Comparison│ │   Environment│ │ • Observes   │
│   .EnvironmentId│ │ • Risk     │ │              │ │   all writes │
└──────────────┘ └─────────────┘ └──────────────┘ └──────────────┘
```

---

## 2. O que o módulo Environment Management expõe

### 2.1 Interfaces públicas (contracts)

| Interface / Artefacto | Consumidor | Propósito | Ficheiro |
|----------------------|-----------|-----------|---------|
| `ICurrentEnvironment` | Todos os módulos | Ambiente resolvido no request actual | `BuildingBlocks.Application/Abstractions/ICurrentEnvironment.cs` |
| `EnvironmentId` (strongly-typed) | Todos os módulos | Referência tipada ao ambiente | `BuildingBlocks.Core/` (ou `EnvironmentManagement.Domain/`) |
| `Environment` aggregate (read-only) | AI & Knowledge, Ops Intel | Dados do ambiente para consulta | Via repository ou integration event |
| `IsPrimaryProduction` | Change Governance, AI | Risk scoring | Via `ICurrentEnvironment` ou query |
| `Criticality` | Change Governance, AI | Risk scoring | Via query ou integration event |
| `EnvironmentProfile` | UI shell (banner) | Badge color, warning | Via `EnvironmentProfileResolver` |

### 2.2 Integration events (a implementar)

| Evento | Payload | Consumidores |
|--------|---------|-------------|
| `EnvironmentCreatedIntegrationEvent` | `{ environmentId, tenantId, name, slug, profile, criticality }` | Audit, Notifications |
| `EnvironmentDeactivatedIntegrationEvent` | `{ environmentId, tenantId, deactivatedBy }` | Audit, Change Governance, Notifications |
| `PrimaryProductionDesignatedIntegrationEvent` | `{ environmentId, tenantId, previousPrimaryId }` | Audit, Change Governance, AI, Notifications |
| `EnvironmentAccessGrantedIntegrationEvent` | `{ environmentId, userId, accessLevel, grantedBy }` | Audit, Notifications |
| `EnvironmentAccessRevokedIntegrationEvent` | `{ environmentId, userId, revokedBy }` | Audit, Notifications |

### 2.3 Endpoints API expostos

| Endpoint | Consumidores internos | Consumidores externos |
|---------|----------------------|--------------------|
| `GET /api/v1/environments` | Frontend, AI & Knowledge | API pública |
| `GET /api/v1/environments/{id}` (a criar) | Frontend detail page | API pública |
| `GET /api/v1/environments/primary-production` | Frontend, Change Governance | API pública |

---

## 3. O que o módulo consome

### 3.1 De BuildingBlocks

| Artefacto | Tipo | Propósito |
|-----------|------|-----------|
| `NexTraceDbContextBase` | Base class | DbContext com interceptors (RLS, Audit, Encryption, Outbox) |
| `TenantRlsInterceptor` | Interceptor | Row-Level Security por `tenant_id` |
| `AuditInterceptor` | Interceptor | Audit trail genérico de writes |
| `EncryptionInterceptor` | Interceptor | Encriptação de campos sensíveis |
| `OutboxInterceptor` | Interceptor | Outbox pattern para integration events |
| `TypedIdBase` | Base class | Para `EnvironmentId`, `TenantId` |
| `IMediator` | Interface | CQRS dispatch |

### 3.2 De Identity & Access

| Artefacto | Tipo | Propósito | Direcção |
|-----------|------|-----------|----------|
| `UserId` | Reference | Referência a utilizadores (em `EnvironmentAccess.UserId`, `GrantedBy`) | Env Mgmt → Identity |
| User existence validation | Service call | Verificar se UserId existe antes de grant-access | Env Mgmt → Identity |
| `EnvironmentAccess` entity | Shared read | Identity lê para authorization decisions | Identity ← Env Mgmt |
| `EnvironmentAccessValidator` | Service | Valida acesso no middleware auth | Permanece em Identity |
| `EnvironmentResolutionMiddleware` | Middleware | Resolve header `X-Environment-Id` | Permanece em Identity |

**Nota sobre a fronteira Identity ↔ Environment Management:**

```
┌─────────────────────┐      ┌─────────────────────────┐
│  Identity & Access   │      │  Environment Management  │
│                     │      │                         │
│  Lê:                │◄─────│  Escreve:                │
│  • EnvironmentAccess│      │  • EnvironmentAccess     │
│    (para auth)      │      │    (grant, revoke)       │
│                     │      │                         │
│  Executa:           │      │  Expõe:                 │
│  • AccessValidator  │      │  • Environment aggregate │
│  • ResolutionMW     │      │  • env:* permissions     │
└─────────────────────┘      └─────────────────────────┘
```

### 3.3 De Configuration (potencial)

| Artefacto | Tipo | Propósito |
|-----------|------|-----------|
| `ConfigurationEntry` com `Scope=Environment` | Read | Configurações específicas de um ambiente |
| `ConfigurationResolutionService` | Service | Resolver configuração efectiva para um ambiente |

**Estado actual:** Sem referência directa. Configuration usa `EnvironmentId` como `ScopeReferenceId` mas sem acoplamento formal.

---

## 4. Dependências por módulo — Análise detalhada

### 4.1 Identity & Access ↔ Environment Management

| Aspecto | Estado actual | Estado alvo |
|---------|-------------|-------------|
| Acoplamento | ❌ **ALTO** — tudo embebido em Identity | Separação com interface partilhada |
| `EnvironmentAccess` | Entity em IdentityAccess.Domain | Lifecycle em Env Mgmt, leitura em Identity |
| `EnvironmentAccessValidator` | Infrastructure do Identity | Permanece — consome via interface read-only |
| `EnvironmentResolutionMiddleware` | Infrastructure do Identity | Permanece — cross-cutting concern |
| Permissões | `identity:users:*` | `env:*` (namespace dedicado) |
| DbContext | `IdentityDbContext` partilhado | `EnvironmentDbContext` dedicado |
| **Risco de migração:** | — | ALTO — requer migração coordenada de tabelas e lógica |

### 4.2 Change Governance ↔ Environment Management

| Aspecto | Estado actual | Estado alvo |
|---------|-------------|-------------|
| `DeploymentEnvironment` | Entidade independente em ChangeGovernance.Domain | Mantém — NÃO é o mesmo conceito que `Environment` |
| Enrichment | ❌ Sem ligação | `DeploymentEnvironment` pode consumir `IsPrimaryProduction` e `Criticality` para risk scoring |
| Promotion gates | `PromotionGate` usa `DeploymentEnvironmentId` | Pode consumir `EnvironmentPolicy.PromotionApproval` no futuro |
| Blast radius | Calculado independentemente | Pode enriquecer com `Criticality` do Environment Management |

**Propriedades do `DeploymentEnvironment` (Change Governance):**

| Propriedade | Tipo | Equivalente em Environment? |
|-------------|------|-----------------------------|
| `Id` | Guid | Não — IDs diferentes |
| `Name` | string | Sim — pode ser o mesmo nome |
| `Description` | string | Sim |
| `Order` | int | Similar a `SortOrder` |
| `RequiresApproval` | bool | Parcial — `EnvironmentPolicy.PromotionApproval` |
| `RequiresEvidencePack` | bool | Não — específico de deployment |
| `IsActive` | bool | Sim |

### 4.3 Operational Intelligence ↔ Environment Management

| Aspecto | Estado actual | Estado alvo |
|---------|-------------|-------------|
| `IncidentRecord.EnvironmentId` | FK lógica (ID only) | Mantém — referência por ID |
| Contextualização | Via `ICurrentEnvironment` | Mantém — interface cross-cutting |
| Métricas por ambiente | Filtro por `EnvironmentId` | Mantém |
| Dashboard por ambiente | ⚠️ Verificar existência | Considerar enrichment com `Profile` e `Criticality` |

### 4.4 AI & Knowledge ↔ Environment Management

| Aspecto | Estado actual | Estado alvo |
|---------|-------------|-------------|
| `EnvironmentComparisonContext` | Consome dados de Environment | Mantém — leitura via API ou service |
| `PromotionRiskAnalysisContext` | Consome `IsPrimaryProduction`, `Criticality` | Mantém |
| AI chat context | `ICurrentEnvironment` para contextualizar respostas | Mantém |
| Comparison backend | ❌ Frontend only (`EnvironmentComparisonPage.tsx`) | Criar backend para comparação |

### 4.5 Audit & Compliance ↔ Environment Management

| Aspecto | Estado actual | Estado alvo |
|---------|-------------|-------------|
| Audit genérico | `AuditInterceptor` captura writes | Mantém como baseline |
| Audit de domínio | ❌ Sem domain events | Implementar events para acções críticas |
| Compliance | ❌ Sem verificações | Environment policies como input para compliance |
| Reports | ❌ | Reports de acesso por ambiente, alterações de criticidade |

### 4.6 Notifications (potencial)

| Aspecto | Estado actual | Estado alvo |
|---------|-------------|-------------|
| Notificações | ❌ Sem integração | Notificar em: primary production change, access grant/revoke, policy violations |
| Canais | — | Email, in-app, webhook (via módulo Notifications) |

---

## 5. Diagrama de dependências — Direcção dos acoplamentos

| De | Para | Tipo | Mecanismo |
|----|------|------|-----------|
| Environment Management | BuildingBlocks | Structural | Base classes, interfaces, typed IDs |
| Environment Management | Identity & Access | Data reference | `UserId` para EnvironmentAccess |
| Identity & Access | Environment Management | Data read | `EnvironmentAccess` para auth decisions |
| Identity & Access | Environment Management | Service call | `EnvironmentResolutionMiddleware` resolve environment |
| Change Governance | Environment Management | Data enrichment (futuro) | `IsPrimaryProduction`, `Criticality` |
| Operational Intelligence | Environment Management | Data reference | `EnvironmentId` em IncidentRecord |
| AI & Knowledge | Environment Management | Data read | Comparison, risk analysis contexts |
| Audit & Compliance | Environment Management | Event subscription | Domain events para audit trail |
| Configuration | Environment Management | Scope reference | `EnvironmentId` como `ScopeReferenceId` |
| Notifications | Environment Management | Event subscription (futuro) | Integration events para notificações |

---

## 6. Dependências circulares — Análise

| Potencial ciclo | Existe? | Mitigação |
|----------------|---------|-----------|
| Env Mgmt → Identity → Env Mgmt | ⚠️ **SIM** — Env Mgmt referencia `UserId` (Identity), Identity lê `EnvironmentAccess` (Env Mgmt) | Quebrar com interface: Identity consome `IEnvironmentAccessReader` (read-only interface exposta por Env Mgmt) |
| Env Mgmt → Change Gov → Env Mgmt | ❌ Não — Change Governance não depende de Env Mgmt actualmente | N/A |
| Env Mgmt → Config → Env Mgmt | ❌ Não — Config usa `EnvironmentId` como referência passiva | N/A |

### Resolução do ciclo Identity ↔ Environment Management

```
┌──────────────────────┐        ┌──────────────────────────────┐
│  Identity & Access    │        │  Environment Management       │
│                      │        │                              │
│  Consome:            │◄───────│  Expõe:                      │
│  IEnvironmentAccess  │        │  IEnvironmentAccessReader    │
│  Reader (interface)  │        │  (read-only interface)       │
│                      │        │                              │
│  Não referencia      │        │  Referencia:                 │
│  Env Mgmt domain     │        │  UserId (BuildingBlocks type)│
└──────────────────────┘        └──────────────────────────────┘
```

O `UserId` deve ser definido no `BuildingBlocks.Core` (não no Identity domain) para evitar dependência directa.

---

## 7. Impacto da migração nas dependências

| Acção de migração | Módulos afectados | Risco |
|-------------------|-------------------|-------|
| Criar `EnvironmentDbContext` | Environment Management, Identity (remover DbSets) | ALTO |
| Renomear tabelas `identity_` → `env_` | Environment Management (nova migration) | MÉDIO |
| Mover `EnvironmentAccess` lifecycle | Environment Management, Identity | ALTO |
| Criar namespace `env:*` | Environment Management, Identity (permission seeds), Frontend | MÉDIO |
| Expor `IEnvironmentAccessReader` | Environment Management (expõe), Identity (consome) | MÉDIO |
| Publicar integration events | Environment Management (publica), Audit, Notifications (consomem) | BAIXO |

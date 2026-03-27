# Environment Management — Domain Model Finalization

> **Módulo:** 02 — Environment Management  
> **Data:** 2026-03-25  
> **Fase:** N4-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Aggregate Root — `Environment`

**Ficheiro actual:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/Environment.cs` (247 linhas)  
**Ficheiro alvo:** `src/modules/environmentmanagement/NexTraceOne.EnvironmentManagement.Domain/Entities/Environment.cs`

### 1.1 Propriedades

| Propriedade | Tipo | Max Length | Obrigatória | Imutável | Estado |
|-------------|------|-----------|-------------|----------|--------|
| `Id` | `EnvironmentId` (GUID) | — | ✅ | ✅ | ✅ Correcto |
| `TenantId` | `TenantId` (GUID) | — | ✅ | ✅ | ✅ Correcto |
| `Name` | `string` | 100 | ✅ | ❌ | ✅ Correcto |
| `Slug` | `string` | 50 | ✅ | ✅ | ✅ Correcto — URL-friendly, único por tenant |
| `SortOrder` | `int` | — | ✅ | ❌ | ✅ Correcto — 0 = menos restritivo |
| `IsActive` | `bool` | — | ✅ | ❌ | ✅ Correcto — default true |
| `CreatedAt` | `DateTimeOffset` | — | ✅ | ✅ | ✅ Correcto — UTC |
| `Profile` | `EnvironmentProfile` | — | ✅ | ❌ | ✅ Correcto |
| `Criticality` | `EnvironmentCriticality` | — | ✅ | ❌ | ✅ Correcto |
| `Code` | `string?` | 50 | ❌ | ❌ | ✅ Correcto — ex: "DEV", "PROD-BR" |
| `Description` | `string?` | — | ❌ | ❌ | ✅ Correcto |
| `Region` | `string?` | 100 | ❌ | ❌ | ✅ Correcto — ex: "eu-west-1" |
| `IsProductionLike` | `bool` | — | ✅ | ❌ | ✅ Correcto |
| `IsPrimaryProduction` | `bool` | — | ✅ | ❌ | ✅ Correcto — unique filtrado por tenant |

### 1.2 Factory Methods

| Método | Parâmetros | Notas | Estado |
|--------|-----------|-------|--------|
| `Create(TenantId, string name, string slug, int sortOrder)` | Básico — sem profile/criticality | ⚠️ Cria com Profile/Criticality default — considerar obrigar | ✅ Existe |
| `Create(TenantId, string name, string slug, int sortOrder, EnvironmentProfile, EnvironmentCriticality, string? code, string? description, string? region, bool isProductionLike)` | Completo | ✅ Guard clauses presentes | ✅ Existe |

### 1.3 Métodos de domínio

| Método | Comportamento | Validações | Estado |
|--------|-------------|-----------|--------|
| `Activate()` | `IsActive = true` | — | ✅ |
| `Deactivate()` | `IsActive = false` | ⚠️ Deveria verificar se é PrimaryProduction antes de desactivar | ✅ Existe |
| `DesignateAsPrimaryProduction()` | `IsPrimaryProduction = true`, `IsProductionLike = true` | — | ✅ |
| `RevokePrimaryProductionDesignation()` | `IsPrimaryProduction = false` | — | ✅ |
| `UpdateProfile(EnvironmentProfile, EnvironmentCriticality)` | Actualiza classificação | Guard clauses | ✅ |
| `UpdateLocationInfo(string? code, string? region)` | Actualiza metadados geográficos | — | ✅ |
| `UpdateBasicInfo(string name, string? description)` | Nome e descrição | Guard clauses em Name | ✅ |
| `UpdateSortOrder(int sortOrder)` | Reordena | — | ✅ |

### 1.4 Gaps no aggregate root

| # | Gap | Severidade | Recomendação |
|---|-----|-----------|--------------|
| DM-01 | Sem `RowVersion` / `ConcurrencyToken` | ALTA | Adicionar `UseXminAsConcurrencyToken()` no EF config |
| DM-02 | `Deactivate()` não verifica `IsPrimaryProduction` | MÉDIA | Adicionar guard: `if (IsPrimaryProduction) throw` |
| DM-03 | Sem `IsDeleted` / soft-delete | MÉDIA | Adicionar para suportar exclusão segura |
| DM-04 | Sem domain events | MÉDIA | Adicionar `EnvironmentCreated`, `EnvironmentDeactivated`, `PrimaryProductionDesignated` |
| DM-05 | Sem `UpdatedAt` / `UpdatedBy` | MÉDIA | Ou via interceptor ou explícito no aggregate |
| DM-06 | Factory method básico permite Profile/Criticality default | BAIXA | Considerar tornar obrigatório ou documentar default |

---

## 2. Entidade — `EnvironmentAccess`

**Ficheiro actual:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentAccess.cs` (174 linhas)  
**Ficheiro alvo:** Entidade partilhada — lifecycle no Environment Management, leitura no Identity

### 2.1 Propriedades

| Propriedade | Tipo | Obrigatória | Imutável | Estado |
|-------------|------|-------------|----------|--------|
| `Id` | `Guid` | ✅ | ✅ | ⚠️ Deveria ser strongly-typed `EnvironmentAccessId` |
| `UserId` | `Guid` | ✅ | ✅ | ⚠️ Deveria referenciar `UserId` typed |
| `TenantId` | `TenantId` | ✅ | ✅ | ✅ |
| `EnvironmentId` | `EnvironmentId` | ✅ | ✅ | ✅ |
| `AccessLevel` | `string` (read/write/admin/none) | ✅ | ❌ | ⚠️ Deveria ser enum `EnvironmentAccessLevel` |
| `GrantedAt` | `DateTimeOffset` | ✅ | ✅ | ✅ |
| `ExpiresAt` | `DateTimeOffset?` | ❌ | ❌ | ✅ — Temporal access |
| `GrantedBy` | `Guid` | ✅ | ✅ | ✅ |
| `IsActive` | `bool` | ✅ | ❌ | ✅ |
| `RevokedAt` | `DateTimeOffset?` | ❌ | ❌ | ✅ |

### 2.2 Métodos de domínio

| Método | Comportamento | Estado |
|--------|-------------|--------|
| `Create(...)` | Factory method | ✅ |
| `Revoke()` | `IsActive = false`, `RevokedAt = now` | ✅ |
| `IsActiveAt(DateTimeOffset)` | Verifica active + não expirado + não revocado | ✅ |
| `ChangeAccessLevel(string)` | Muda nível de acesso | ⚠️ Aceita string — deveria ser enum |

### 2.3 Gaps na entidade

| # | Gap | Severidade | Recomendação |
|---|-----|-----------|--------------|
| DM-07 | `Id` é `Guid` em vez de `EnvironmentAccessId` | MÉDIA | Criar strongly-typed ID |
| DM-08 | `AccessLevel` é `string` em vez de enum | MÉDIA | Criar `EnvironmentAccessLevel` enum |
| DM-09 | `UserId` é `Guid` não tipado | BAIXA | Usar `UserId` do Identity domain |
| DM-10 | Sem domain events para audit trail | MÉDIA | `AccessGranted`, `AccessRevoked`, `AccessLevelChanged` |
| DM-11 | Sem `RowVersion` | ALTA | Adicionar `UseXminAsConcurrencyToken()` |

---

## 3. Entidades Phase 2 — Definidas, sem persistência

### 3.1 `EnvironmentPolicy` (124 linhas)

**Ficheiro:** `NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentPolicy.cs`

| Tipo de política | Propósito | Estado |
|-----------------|-----------|--------|
| `PromotionApproval` | Quem aprova promoções para este ambiente | ⚠️ Definida, sem DbSet |
| `FreezeWindow` | Janelas de congelamento (ex: sexta 18h a segunda 8h) | ⚠️ Definida, sem DbSet |
| `AlertEscalation` | Regras de escalação para ambiente | ⚠️ Definida, sem DbSet |
| `DeployQualityGate` | Gates de qualidade antes de deploy | ⚠️ Definida, sem DbSet |

### 3.2 `EnvironmentTelemetryPolicy` (122 linhas)

**Ficheiro:** `NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentTelemetryPolicy.cs`

| Propriedade | Tipo | Propósito |
|-------------|------|-----------|
| `RetentionDays` | `int` | Retenção de dados de telemetria |
| `VerbosityLevel` | `string/enum` | Nível de detalhe de colecção |
| `AllowCrossEnvironmentComparison` | `bool` | Permite comparação com outros ambientes |

### 3.3 `EnvironmentIntegrationBinding` (90+ linhas)

**Ficheiro:** `NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentIntegrationBinding.cs`

| Tipo de binding | Propósito |
|----------------|-----------|
| `observability` | Ligação a ferramentas de observabilidade |
| `alerting` | Ligação a sistemas de alerta |
| `ci_cd` | Ligação a pipelines CI/CD |
| `event_broker` | Ligação a message brokers |

---

## 4. Enums

| Enum | Ficheiro | Valores | Estado |
|------|---------|---------|--------|
| `EnvironmentProfile` | `Domain/Enums/EnvironmentProfile.cs` | Development(1), Validation(2), Staging(3), Production(4), Sandbox(5), DisasterRecovery(6), Training(7), UserAcceptanceTesting(8), PerformanceTesting(9) | ✅ Completo |
| `EnvironmentCriticality` | `Domain/Enums/EnvironmentCriticality.cs` | Low(1), Medium(2), High(3), Critical(4) | ✅ Completo |
| `EnvironmentAccessLevel` | ❌ Não existe | read, write, admin, none — actualmente `string` | ❌ Criar |

---

## 5. Value Objects

| Value Object | Ficheiro | Propósito | Estado |
|-------------|---------|-----------|--------|
| `TenantEnvironmentContext` | `Domain/ValueObjects/TenantEnvironmentContext.cs` | Contexto resolvido tenant+ambiente | ✅ |
| `EnvironmentUiProfile` | `Domain/ValueObjects/EnvironmentUiProfile.cs` | Badge color, protection warning, AI assistance flag | ✅ |

---

## 6. Relações entre domínios

### 6.1 Relação com Identity & Access

```
Environment ──(1:N)──▶ EnvironmentAccess ──referencia──▶ User (Identity)
```

- `EnvironmentAccess.UserId` referencia utilizadores do Identity
- `EnvironmentAccess.GrantedBy` referencia utilizador que concedeu
- Identity lê `EnvironmentAccess` para authorization decisions via `EnvironmentAccessValidator`
- **Decisão:** Environment Management é dono do lifecycle, Identity consome para auth

### 6.2 Relação com Change Governance

```
Environment (Env Mgmt)  ≠  DeploymentEnvironment (Change Governance)
```

- `DeploymentEnvironment` em `NexTraceOne.ChangeGovernance.Domain/Entities/` é entidade **separada**
- Propriedades: `Id`, `Name`, `Description`, `Order`, `RequiresApproval`, `RequiresEvidencePack`, `IsActive`, `CreatedAt`
- **NÃO** tem FK para `Environment` do Environment Management
- **Decisão futura:** Considerar enrichment via `IsPrimaryProduction` e `Criticality` para risk scoring

### 6.3 Relação com Operational Intelligence

```
IncidentRecord.EnvironmentId ──referencia──▶ Environment.Id
```

- Referência por ID — sem dependência de domínio directa
- Operational Intelligence consome `ICurrentEnvironment` do BuildingBlocks

### 6.4 Relação com AI & Knowledge

```
EnvironmentComparisonContext ──consome──▶ Environment data
PromotionRiskAnalysisContext ──consome──▶ IsPrimaryProduction, Criticality
```

- Leitura only — AI consome dados do Environment Management

---

## 7. Interface cross-cutting — `ICurrentEnvironment`

**Ficheiro:** `src/modules/buildingblocks/NexTraceOne.BuildingBlocks.Application/Abstractions/ICurrentEnvironment.cs`

```csharp
public interface ICurrentEnvironment
{
    EnvironmentId EnvironmentId { get; }
    bool IsResolved { get; }
    bool IsProductionLike { get; }
}
```

- Implementada por `CurrentEnvironmentAdapter` na Infrastructure do Identity
- Resolvida pelo `EnvironmentResolutionMiddleware` via header `X-Environment-Id`
- **Decisão:** Interface permanece no BuildingBlocks; implementação migra para Environment Management

---

## 8. Modelo de domínio alvo (Phase 1 + Phase 2)

### Phase 1 — Modelo funcional mínimo

```
Environment (aggregate root)
├── EnvironmentId (strongly-typed)
├── TenantId
├── Name, Slug, Code, Description, Region
├── Profile, Criticality
├── SortOrder, IsActive, IsDeleted (NEW)
├── IsPrimaryProduction, IsProductionLike
├── CreatedAt, UpdatedAt (NEW), UpdatedBy (NEW)
└── RowVersion (xmin) (NEW)

EnvironmentAccess (entity, lifecycle owned by Env Mgmt)
├── EnvironmentAccessId (NEW, strongly-typed)
├── UserId, TenantId, EnvironmentId
├── AccessLevel (enum, NEW — was string)
├── GrantedAt, ExpiresAt, GrantedBy
├── IsActive, RevokedAt
└── RowVersion (xmin) (NEW)
```

### Phase 2 — Governança e políticas

```
EnvironmentPolicy (new aggregate)
├── EnvironmentPolicyId
├── EnvironmentId (FK)
├── PolicyType (PromotionApproval | FreezeWindow | AlertEscalation | DeployQualityGate)
├── Configuration (JSON)
├── IsActive, CreatedAt, UpdatedAt

EnvironmentTelemetryPolicy (new entity)
├── EnvironmentTelemetryPolicyId
├── EnvironmentId (FK)
├── RetentionDays, VerbosityLevel
├── AllowCrossEnvironmentComparison

EnvironmentIntegrationBinding (new entity)
├── EnvironmentIntegrationBindingId
├── EnvironmentId (FK)
├── BindingType (observability | alerting | ci_cd | event_broker)
├── Configuration (JSON)
├── IsActive
```

---

## 9. Domain Events (a implementar)

| Evento | Quando | Consumers potenciais |
|--------|--------|---------------------|
| `EnvironmentCreated` | Após `Create()` | Audit, Notifications |
| `EnvironmentDeactivated` | Após `Deactivate()` | Audit, Notifications, Change Governance |
| `EnvironmentActivated` | Após `Activate()` | Audit |
| `PrimaryProductionDesignated` | Após `DesignateAsPrimaryProduction()` | Audit, Change Governance, Notifications |
| `PrimaryProductionRevoked` | Após `RevokePrimaryProductionDesignation()` | Audit, Change Governance |
| `EnvironmentProfileUpdated` | Após `UpdateProfile()` | Audit, AI & Knowledge |
| `EnvironmentAccessGranted` | Após `EnvironmentAccess.Create()` | Audit, Notifications |
| `EnvironmentAccessRevoked` | Após `EnvironmentAccess.Revoke()` | Audit, Notifications |

---

## 10. Sumário de gaps de modelação

| # | Gap | Impacto | Prioridade |
|---|-----|---------|-----------|
| DM-01 | Sem `RowVersion` em `Environment` | Conflitos concorrentes não detectados | ALTA |
| DM-02 | `Deactivate()` não valida `IsPrimaryProduction` | Pode desactivar ambiente de produção principal | ALTA |
| DM-03 | Sem `IsDeleted` em `Environment` | Impossível eliminar ambientes | MÉDIA |
| DM-04 | Sem domain events | Audit e cross-module communication inexistente | MÉDIA |
| DM-05 | Sem `UpdatedAt` / `UpdatedBy` em `Environment` | Tracking de alterações incompleto | MÉDIA |
| DM-06 | `EnvironmentAccess.Id` não é strongly-typed | Inconsistente com padrão do codebase | MÉDIA |
| DM-07 | `EnvironmentAccess.AccessLevel` é `string` | Type safety ausente | MÉDIA |
| DM-08 | 3 entidades Phase 2 sem persistência | Funcionalidade de governança indisponível | BAIXA (Phase 2) |
| DM-09 | `DeploymentEnvironment` não referencia `Environment` | Sem enrichment cross-module | BAIXA (futuro) |
| DM-10 | Sem invariantes de aggregate na collection de Access | Environment não controla seus EnvironmentAccess | MÉDIA |
